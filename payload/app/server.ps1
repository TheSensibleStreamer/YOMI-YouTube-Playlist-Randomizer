$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
$config = Get-YomiConfig

$port = [int]$config.server_port
$prefix = "http://127.0.0.1:$port/"
$webRoot = Join-Path $InstallRoot 'web'
$stateFile = Join-Path $DataRoot 'state\current.json'
$pipeName = 'yomi-v4'

$source = @'
using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System.Globalization;

public static class MpvObsMusicServer
{
    private static readonly object StateLock = new object();
    private static readonly object TrackLock = new object();
    private static double Position = 0.0;
    private static bool Paused = true;
    private static double Speed = 1.0;
    private static bool Connected = false;
    private static long SampleStamp = 0;
    private static string Prefix, WebRoot, DataRoot, StateFile, PipeName;
    private static string CachedTrack = "null";
    private static long CachedTrackStamp = -1;
    private static long CachedTrackLength = -1;

    private static readonly Regex RequestIdRegex = new Regex(@"""request_id""\s*:\s*(-?\d+)", RegexOptions.Compiled);
    private static readonly Regex NumberDataRegex = new Regex(@"""data""\s*:\s*(-?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)", RegexOptions.Compiled);
    private static readonly Regex BoolDataRegex = new Regex(@"""data""\s*:\s*(true|false)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MediaRegex = new Regex(@"^/media/(artwork|video|visualizer)/(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SourceRegex = new Regex(@"^/(?:source/[^/]+|director)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void Start(string prefix,string webRoot,string dataRoot,string stateFile,string pipeName)
    {
        Prefix=prefix; WebRoot=webRoot; DataRoot=dataRoot; StateFile=stateFile; PipeName=pipeName;
        var h = new Thread(HttpLoop) { IsBackground=true, Name="MPV OBS HTTP" };
        var p = new Thread(PipeLoop) { IsBackground=true, Name="MPV OBS Clock" };
        h.Start(); p.Start();
    }

    private static string ClockJson()
    {
        lock(StateLock)
        {
            double age = SampleStamp == 0 ? 0.0 : (Stopwatch.GetTimestamp()-SampleStamp)*1000.0/Stopwatch.Frequency;
            return String.Format(CultureInfo.InvariantCulture,
                "{{\"connected\":{0},\"time\":{1:0.000000},\"paused\":{2},\"speed\":{3:0.000000},\"age_ms\":{4:0.000}}}",
                Connected?"true":"false",Position,Paused?"true":"false",Speed,Math.Max(0,age));
        }
    }

    private static string TrackJson()
    {
        lock(TrackLock)
        {
            try
            {
                var file = new FileInfo(StateFile);
                if(!file.Exists)
                {
                    CachedTrack="null";CachedTrackStamp=-1;CachedTrackLength=-1;
                    return CachedTrack;
                }
                long stamp=file.LastWriteTimeUtc.Ticks;
                if(stamp!=CachedTrackStamp || file.Length!=CachedTrackLength)
                {
                    string raw=File.ReadAllText(StateFile,Encoding.UTF8).Trim();
                    CachedTrack=raw.StartsWith("{")?raw:"null";
                    CachedTrackStamp=stamp;CachedTrackLength=file.Length;
                }
            }
            catch { }
            return CachedTrack;
        }
    }

    private static string StateJson()
    {
        return "{\"clock\":" + ClockJson() + ",\"track\":" + TrackJson() + "}";
    }

    private static void HttpLoop()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(Prefix);
        listener.Start();
        while(listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx=listener.GetContext(); } catch { break; }
            ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
        }
    }

    private static void Handle(HttpListenerContext ctx)
    {
        try
        {
            var req=ctx.Request; var res=ctx.Response;
            res.Headers["Access-Control-Allow-Origin"]="*";
            res.Headers["Cache-Control"]="no-store, no-cache, must-revalidate, max-age=0";
            string path=req.Url==null?"":req.Url.AbsolutePath;

            if(path=="/health") { Text(res,"OK","text/plain"); return; }
            if(path=="/state") { Text(res,StateJson(),"application/json"); return; }
            if(path=="/history")
            {
                string f=Path.Combine(DataRoot,"state","history.jsonl");
                if(!File.Exists(f)){Text(res,"","text/plain; charset=utf-8");return;}
                FileSimple(res,f,"text/plain; charset=utf-8",req.HttpMethod); return;
            }
            if(path=="/config")
            {
                string f=Path.Combine(DataRoot,"config.json");
                if(!File.Exists(f)){res.StatusCode=404;res.Close();return;}
                Bytes(res,File.ReadAllBytes(f),"application/json",req.HttpMethod); return;
            }
            if(path=="/" || path=="/overlay") { FileSimple(res,Path.Combine(WebRoot,"overlay.html"),"text/html; charset=utf-8",req.HttpMethod); return; }
            if(path=="/visualizer") { FileSimple(res,Path.Combine(WebRoot,"visualizer.html"),"text/html; charset=utf-8",req.HttpMethod); return; }
            if(SourceRegex.IsMatch(path)) { FileSimple(res,Path.Combine(WebRoot,"director.html"),"text/html; charset=utf-8",req.HttpMethod); return; }

            Match m=MediaRegex.Match(path);
            if(m.Success)
            {
                string type=m.Groups[1].Value.ToLowerInvariant();
                string n=m.Groups[2].Value;
                if(type=="artwork")
                {
                    string dir=Path.Combine(DataRoot,"cache","artwork");
                    string[] ext={"jpg","jpeg","png","webp"};
                    foreach(string e in ext)
                    {
                        string f=Path.Combine(dir,"track-"+n+"."+e);
                        if(File.Exists(f)) { FileSimple(res,f,Mime(e),req.HttpMethod); return; }
                    }
                    res.StatusCode=404;res.Close();return;
                }
                if(type=="video")
                {
                    string f=Path.Combine(DataRoot,"cache","video","track-"+n+".mp4");
                    if(!File.Exists(f)){res.StatusCode=404;res.Close();return;}
                    RangeFile(ctx,f,"video/mp4"); return;
                }
                if(type=="visualizer")
                {
                    string f=Path.Combine(DataRoot,"cache","visualizer","track-"+n+".mp4");
                    if(!File.Exists(f)){res.StatusCode=404;res.Close();return;}
                    RangeFile(ctx,f,"video/mp4"); return;
                }
            }

            res.StatusCode=404; res.Close();
        }
        catch { try{ctx.Response.StatusCode=500;ctx.Response.Close();}catch{} }
    }

    private static string Mime(string ext)
    {
        switch(ext.ToLowerInvariant())
        {
            case "webp": return "image/webp";
            case "png": return "image/png";
            default: return "image/jpeg";
        }
    }

    private static void Text(HttpListenerResponse r,string s,string mime)
    {
        Bytes(r,Encoding.UTF8.GetBytes(s),mime,"GET");
    }
    private static void Bytes(HttpListenerResponse r,byte[] b,string mime,string method)
    {
        r.StatusCode=200;r.ContentType=mime;r.ContentLength64=b.Length;
        if(!String.Equals(method,"HEAD",StringComparison.OrdinalIgnoreCase)) r.OutputStream.Write(b,0,b.Length);
        r.Close();
    }
    private static void FileSimple(HttpListenerResponse r,string f,string mime,string method)
    {
        if(!File.Exists(f)){r.StatusCode=404;r.Close();return;}
        Bytes(r,File.ReadAllBytes(f),mime,method);
    }

    private static void RangeFile(HttpListenerContext ctx,string f,string mime)
    {
        var req=ctx.Request; var res=ctx.Response;
        res.Headers["Accept-Ranges"]="bytes";
        using(var fs=new FileStream(f,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete,65536,FileOptions.SequentialScan))
        {
            long len=fs.Length,start=0,end=len-1; bool partial=false;
            string range=req.Headers["Range"];
            if(!String.IsNullOrWhiteSpace(range) && range.StartsWith("bytes=",StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string spec=range.Substring(6).Split(',')[0].Trim();
                    string[] parts=spec.Split('-');
                    if(spec.StartsWith("-")) { long suffix=Int64.Parse(spec.Substring(1)); if(suffix>len)suffix=len; start=len-suffix; }
                    else { start=Int64.Parse(parts[0]); if(parts.Length>1&&!String.IsNullOrWhiteSpace(parts[1]))end=Int64.Parse(parts[1]); }
                    if(start<0||start>=len||end<start)throw new Exception();
                    if(end>=len)end=len-1; partial=true;
                }
                catch { res.StatusCode=416;res.Headers["Content-Range"]="bytes */"+len;res.Close();return; }
            }
            long count=end-start+1;
            res.ContentType=mime;res.ContentLength64=count;
            if(partial){res.StatusCode=206;res.Headers["Content-Range"]="bytes "+start+"-"+end+"/"+len;} else res.StatusCode=200;
            if(String.Equals(req.HttpMethod,"HEAD",StringComparison.OrdinalIgnoreCase)){res.Close();return;}
            fs.Seek(start,SeekOrigin.Begin); byte[] buf=new byte[65536]; long left=count;
            while(left>0){int want=(int)Math.Min(buf.Length,left);int got=fs.Read(buf,0,want);if(got<=0)break;res.OutputStream.Write(buf,0,got);left-=got;}
        }
        res.Close();
    }

    private static void PipeLoop()
    {
        int id=1000;
        while(true)
        {
            try
            {
                using(var pipe=new NamedPipeClientStream(".",PipeName,PipeDirection.InOut,PipeOptions.None))
                {
                    pipe.Connect(1000);
                    using(var reader=new StreamReader(pipe,new UTF8Encoding(false),false,4096,true))
                    using(var writer=new StreamWriter(pipe,new UTF8Encoding(false),4096,true))
                    {
                        writer.AutoFlush=true;writer.NewLine="\n";
                        while(pipe.IsConnected)
                        {
                            double a=QueryDouble(writer,reader,"audio-pts",id++);
                            double t=QueryDouble(writer,reader,"time-pos",id++);
                            bool pause=QueryBool(writer,reader,"pause",id++);
                            double speed=QueryDouble(writer,reader,"speed",id++);
                            double pos=(!Double.IsNaN(a)&&a>=0)?a:((!Double.IsNaN(t)&&t>=0)?t:0);
                            lock(StateLock){Position=pos;Paused=pause;Speed=Double.IsNaN(speed)?1:speed;Connected=true;SampleStamp=Stopwatch.GetTimestamp();}
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch { lock(StateLock){Connected=false;Paused=true;SampleStamp=Stopwatch.GetTimestamp();} Thread.Sleep(500); }
        }
    }

    private static string Query(StreamWriter w,StreamReader r,string prop,int id)
    {
        w.WriteLine("{\"command\":[\"get_property\",\""+prop+"\"],\"request_id\":"+id+"}");
        while(true){string line=r.ReadLine();if(line==null)throw new IOException();Match m=RequestIdRegex.Match(line);if(m.Success&&Int32.Parse(m.Groups[1].Value)==id)return line;}
    }
    private static double QueryDouble(StreamWriter w,StreamReader r,string prop,int id)
    {
        string line=Query(w,r,prop,id);if(line.IndexOf("\"error\":\"success\"",StringComparison.OrdinalIgnoreCase)<0)return Double.NaN;
        Match m=NumberDataRegex.Match(line);if(!m.Success)return Double.NaN;double v;if(!Double.TryParse(m.Groups[1].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out v))return Double.NaN;return v;
    }
    private static bool QueryBool(StreamWriter w,StreamReader r,string prop,int id)
    {
        string line=Query(w,r,prop,id);Match m=BoolDataRegex.Match(line);return !m.Success || String.Equals(m.Groups[1].Value,"true",StringComparison.OrdinalIgnoreCase);
    }
}
'@

Add-Type -TypeDefinition $source -Language CSharp
[MpvObsMusicServer]::Start($prefix,$webRoot,$DataRoot,$stateFile,$pipeName)
Write-Host "YOMI server: $prefix"
while ($true) { Start-Sleep -Seconds 60 }
