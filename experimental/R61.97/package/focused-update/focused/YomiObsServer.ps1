$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
$config = Get-YomiConfig

$port = [int]$config.server_port
if($port -lt 1 -or $port -gt 65535){$port=8876}
$prefix = "http://127.0.0.1:$port/"
$webRoot = Join-Path $InstallRoot 'web'
$stateFile = Join-Path $DataRoot 'state\current.json'
$pipeName = 'yomi-v4'
$runtimeId = [string]$env:YOMI_RUNTIME_ID
$serverAuthority = [string]$env:YOMI_SERVER_AUTHORITY
$supervisorPid = 0
[void][int]::TryParse([string]$env:YOMI_SUPERVISOR_PID,[ref]$supervisorPid)
if([string]::IsNullOrWhiteSpace($serverAuthority)){$serverAuthority='unscoped'}

$source = @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

public static class YomiObsHttpServerR6183
{
    private static readonly object StateLock = new object();
    private static readonly object TrackLock = new object();
    private static readonly object ClientLock = new object();
    private static readonly object PresentationLock = new object();
    private static double PresentedVideoFps = -1.0;
    private static double PresentedVisualizerFps = -1.0;
    private static long PresentedStampTicks = 0;
    private static string PresentedRoute = "";
    private static double Position = 0.0;
    private static bool Paused = true;
    private static double Speed = 1.0;
    private static bool Connected = false;
    private static long SampleStamp = 0;
    private static string WebRoot, DataRoot, StateFile, PipeName;
    private static string RuntimeId = "";
    private static string Authority = "";
    private static int SupervisorPid;
    private static int Port;
    private static TcpListener Listener;
    private static string CachedTrack = "null";
    private static long CachedTrackStamp = -1;
    private static long CachedTrackLength = -1;
    private static long MissingTrackSinceTicks = 0;
    private static string CachedConfig = null;
    private static long CachedConfigStamp = -1;
    private static long CachedConfigLength = -1;
    private sealed class ClientRecord
    {
        public string Id="";
        public string Kind="browser";
        public string Agent="";
        public string Referer="";
        public string Route="";
        public long FirstTicks;
        public long LastTicks;
        public long Requests;
    }
    private sealed class SimpleRequest
    {
        public string Method="GET";
        public string Target="/";
        public string Path="/";
        public readonly Dictionary<string,string> Headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string,string> Query=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public string Header(string name){string value;return Headers.TryGetValue(name,out value)?value:"";}
        public string QueryValue(string name){string value;return Query.TryGetValue(name,out value)?value:"";}
    }
    private static readonly Dictionary<string,ClientRecord> Clients = new Dictionary<string,ClientRecord>(StringComparer.OrdinalIgnoreCase);
    private static readonly long StartedUtcTicks = DateTime.UtcNow.Ticks;

    private static readonly Regex RequestIdRegex = new Regex(@"""request_id""\s*:\s*(-?\d+)", RegexOptions.Compiled);
    private static readonly Regex NumberDataRegex = new Regex(@"""data""\s*:\s*(-?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)", RegexOptions.Compiled);
    private static readonly Regex BoolDataRegex = new Regex(@"""data""\s*:\s*(true|false)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MediaRegex = new Regex(@"^/media/(artwork|video|visualizer)/(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ObjectRegex = new Regex(@"^/object/(artwork|video|visualizer)/([a-f0-9]{16,64}-[a-f0-9]{16}\.(?:jpg|jpeg|png|webp|mp4))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SourceRegex = new Regex(@"^/(?:source/[^/]+|director)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TrackIndexRegex = new Regex("\"(?:index|occurrence_id)\"\\s*:\\s*(\\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void Start(int port,string webRoot,string dataRoot,string stateFile,string pipeName,string runtimeId,int supervisorPid,string authority)
    {
        Port=port; WebRoot=webRoot; DataRoot=dataRoot; StateFile=stateFile; PipeName=pipeName;
        RuntimeId=runtimeId??""; SupervisorPid=Math.Max(0,supervisorPid); Authority=authority??"";
        Listener=new TcpListener(IPAddress.Loopback,Port);
        Listener.Start(64);
        var h = new Thread(AcceptLoop) { IsBackground=true, Name="YOMI OBS TCP HTTP R61.94" };
        var p = new Thread(PipeLoop) { IsBackground=true, Name="YOMI OBS Clock R61.94" };
        h.Start(); p.Start();
    }

    private static string EscapeJson(string value)
    {
        if(value==null)return "";
        return value.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n");
    }

    private static string CompactClientToken(string value,int max)
    {
        if(String.IsNullOrWhiteSpace(value))return "";
        var sb=new StringBuilder();
        foreach(char c in value)
        {
            if(Char.IsLetterOrDigit(c) || c=='-' || c=='_' || c=='.' || c=='/')sb.Append(c);
            else if(c==' ' || c==':' || c=='|')sb.Append('-');
            if(sb.Length>=max)break;
        }
        return sb.ToString().Trim('-');
    }

    private static string ClientKind(string agent)
    {
        string a=agent??"";
        if(a.IndexOf("YOMI-Controller-Probe",StringComparison.OrdinalIgnoreCase)>=0)return "probe";
        if(a.IndexOf("OBS",StringComparison.OrdinalIgnoreCase)>=0)return "obs";
        if(a.IndexOf("CEF",StringComparison.OrdinalIgnoreCase)>=0 || a.IndexOf("Chrom",StringComparison.OrdinalIgnoreCase)>=0)return "browser";
        if(a.IndexOf("PowerShell",StringComparison.OrdinalIgnoreCase)>=0)return "probe";
        return "client";
    }

    private static void TouchClient(SimpleRequest req,string route)
    {
        if(req==null)return;
        string explicitId=req.QueryValue("client")??"";
        string agent=req.Header("User-Agent")??"";
        string referer=req.Header("Referer")??"";
        Uri refererUri;
        if(Uri.TryCreate(referer,UriKind.Absolute,out refererUri))referer=refererUri.AbsolutePath;
        string kind=String.Equals(explicitId,"controller-probe",StringComparison.OrdinalIgnoreCase) ? "probe" : ClientKind(agent);
        string id=CompactClientToken(explicitId,80);
        if(String.IsNullOrWhiteSpace(id))
        {
            string source=!String.IsNullOrWhiteSpace(referer)?referer:(route??"/");
            string agentToken=CompactClientToken(agent,28);
            id=CompactClientToken(kind+"-"+source+"-"+agentToken,80);
            if(String.IsNullOrWhiteSpace(id))id=kind+"-local";
        }
        long now=DateTime.UtcNow.Ticks;
        lock(ClientLock)
        {
            ClientRecord r;
            if(!Clients.TryGetValue(id,out r))
            {
                r=new ClientRecord{Id=id,Kind=kind,Agent=agent,Referer=referer,Route=route??"",FirstTicks=now,LastTicks=now,Requests=0};
                Clients[id]=r;
            }
            r.Kind=kind;r.Agent=agent;r.Referer=referer;r.Route=route??"";r.LastTicks=now;r.Requests++;
        }
    }

    private static string ClientsJson()
    {
        long now=DateTime.UtcNow.Ticks;
        var stale=new List<string>();
        var rows=new List<ClientRecord>();
        lock(ClientLock)
        {
            foreach(var kv in Clients)
            {
                long age=(now-kv.Value.LastTicks)/TimeSpan.TicksPerMillisecond;
                if(age>60000){stale.Add(kv.Key);continue;}
                rows.Add(kv.Value);
            }
            foreach(string key in stale)Clients.Remove(key);
        }
        rows.Sort(delegate(ClientRecord a,ClientRecord b){return b.LastTicks.CompareTo(a.LastTicks);});
        var sb=new StringBuilder();sb.Append("{\"clients\":[");bool first=true;
        foreach(var r in rows)
        {
            long age=Math.Max(0,(now-r.LastTicks)/TimeSpan.TicksPerMillisecond);
            if(!first)sb.Append(',');first=false;
            sb.Append("{\"id\":\"").Append(EscapeJson(r.Id)).Append("\",\"kind\":\"").Append(EscapeJson(r.Kind))
              .Append("\",\"age_ms\":").Append(age.ToString(CultureInfo.InvariantCulture))
              .Append(",\"requests\":").Append(Math.Max(0,r.Requests).ToString(CultureInfo.InvariantCulture))
              .Append(",\"route\":\"").Append(EscapeJson(r.Route)).Append("\",\"referer\":\"").Append(EscapeJson(r.Referer)).Append("\"}");
        }
        sb.Append("]}");return sb.ToString();
    }

    private static string HealthJson()
    {
        return "{\"server\":\"ready\",\"revision\":\"R61.94\",\"authority\":\""+EscapeJson(Authority)+"\",\"runtime_id\":\""+EscapeJson(RuntimeId)+
               "\",\"supervisor_pid\":"+SupervisorPid.ToString(CultureInfo.InvariantCulture)+",\"pid\":"+Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)+
               ",\"port\":"+Port.ToString(CultureInfo.InvariantCulture)+",\"transport\":\"tcp-loopback\",\"clock\":"+ClockJson()+"}";
    }

    private static string ObsDiagnosticsJson()
    {
        string track=TrackJson();
        Match im=TrackIndexRegex.Match(track??"");
        string index=im.Success?im.Groups[1].Value:"0";
        bool trackPresent=!String.IsNullOrWhiteSpace(track) && !String.Equals(track,"null",StringComparison.OrdinalIgnoreCase);
        bool overlayFile=true;
        bool configPresent=ConfigJson()!=null;
        bool statePresent=File.Exists(StateFile);
        long uptime=Math.Max(0,(DateTime.UtcNow.Ticks-StartedUtcTicks)/TimeSpan.TicksPerMillisecond);
        return "{\"revision\":\"R61.94\",\"authority\":\""+EscapeJson(Authority)+"\",\"runtime_id\":\""+EscapeJson(RuntimeId)+
               "\",\"supervisor_pid\":"+SupervisorPid.ToString(CultureInfo.InvariantCulture)+",\"transport\":\"tcp-loopback\",\"port\":"+Port.ToString(CultureInfo.InvariantCulture)+",\"pid\":"+Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)+
               ",\"uptime_ms\":"+uptime.ToString(CultureInfo.InvariantCulture)+",\"renderer\":\"embedded-r61.75\",\"overlay_file\":"+(overlayFile?"true":"false")+
               ",\"config\":"+(configPresent?"true":"false")+",\"state_file\":"+(statePresent?"true":"false")+
               ",\"track_present\":"+(trackPresent?"true":"false")+",\"track_index\":"+index+
               ",\"presentation\":"+PresentationJson()+",\"clock\":"+ClockJson()+",\"clients\":"+ClientsJson()+"}";
    }

    private static string PresentationJson()
    {
        lock(PresentationLock)
        {
            long age=PresentedStampTicks==0?Int64.MaxValue:Math.Max(0,(DateTime.UtcNow.Ticks-PresentedStampTicks)/TimeSpan.TicksPerMillisecond);
            return String.Format(CultureInfo.InvariantCulture,"{{\"video_fps\":{0:0.###},\"visualizer_fps\":{1:0.###},\"age_ms\":{2},\"route\":\"{3}\"}}",PresentedVideoFps,PresentedVisualizerFps,age==Int64.MaxValue?-1:age,EscapeJson(PresentedRoute));
        }
    }

    private static void UpdatePresentation(SimpleRequest req)
    {
        if(req==null)return;
        double video=-1,viz=-1;
        Double.TryParse(req.QueryValue("video_fps"),NumberStyles.Float,CultureInfo.InvariantCulture,out video);
        Double.TryParse(req.QueryValue("viz_fps"),NumberStyles.Float,CultureInfo.InvariantCulture,out viz);
        string route=req.QueryValue("route")??"";
        lock(PresentationLock){PresentedVideoFps=video;PresentedVisualizerFps=viz;PresentedRoute=route;PresentedStampTicks=DateTime.UtcNow.Ticks;}
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

    private static string NormalizeFocusedMediaRoutes(string raw)
    {
        if(String.IsNullOrWhiteSpace(raw) || !raw.StartsWith("{"))return raw;
        Match im=TrackIndexRegex.Match(raw);if(!im.Success)return raw;
        string index=im.Groups[1].Value;
        string[] keys={"artwork","video","visualizer"};
        string[] kinds={"artwork","video","visualizer"};
        for(int k=0;k<keys.Length;k++)
        {
            string key=keys[k];string kind=kinds[k];
            var rx=new Regex("(\\\""+Regex.Escape(key)+"\\\"\\s*:\\s*\\\")([^\\\"]*)(\\\")",RegexOptions.IgnoreCase);
            raw=rx.Replace(raw,delegate(Match m)
            {
                string value=m.Groups[2].Value;
                if(String.IsNullOrWhiteSpace(value))return m.Value;
                if(value.StartsWith("/",StringComparison.Ordinal) || value.StartsWith("http://",StringComparison.OrdinalIgnoreCase) || value.StartsWith("https://",StringComparison.OrdinalIgnoreCase))return m.Value;
                return m.Groups[1].Value+"/media/"+kind+"/"+index+m.Groups[3].Value;
            },1);
        }
        return raw;
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
                    long now=DateTime.UtcNow.Ticks;
                    if(MissingTrackSinceTicks==0)MissingTrackSinceTicks=now;
                    double missingMs=(now-MissingTrackSinceTicks)/10000.0;
                    if(missingMs<1500.0 && !String.IsNullOrWhiteSpace(CachedTrack) && CachedTrack!="null")return CachedTrack;
                    CachedTrack="null";CachedTrackStamp=-1;CachedTrackLength=-1;return CachedTrack;
                }
                MissingTrackSinceTicks=0;
                long stamp=file.LastWriteTimeUtc.Ticks;
                if(stamp!=CachedTrackStamp || file.Length!=CachedTrackLength)
                {
                    string raw=File.ReadAllText(StateFile,Encoding.UTF8).Trim();
                    CachedTrack=raw.StartsWith("{")?NormalizeFocusedMediaRoutes(raw):"null";
                    CachedTrackStamp=stamp;CachedTrackLength=file.Length;
                }
            }
            catch { }
            return CachedTrack;
        }
    }

    private static string ConfigJson()
    {
        try
        {
            string path=Path.Combine(DataRoot,"config.json");
            var file=new FileInfo(path);if(!file.Exists)return null;
            long stamp=file.LastWriteTimeUtc.Ticks;
            if(CachedConfig==null || stamp!=CachedConfigStamp || file.Length!=CachedConfigLength)
            {
                string raw=File.ReadAllText(path,Encoding.UTF8).Trim();
                CachedConfig=raw.StartsWith("{")?raw:null;CachedConfigStamp=stamp;CachedConfigLength=file.Length;
            }
            return CachedConfig;
        }
        catch{return null;}
    }

    private static string StateJson(){return "{\"clock\":"+ClockJson()+",\"track\":"+TrackJson()+"}";}
    private static string SnapshotJson()
    {
        string config=ConfigJson();
        if(String.IsNullOrWhiteSpace(config))config="null";
        return "{\"state\":"+StateJson()+",\"config\":"+config+"}";
    }

    private static string UrlDecode(string value)
    {
        if(value==null)return "";
        try{return Uri.UnescapeDataString(value.Replace('+',' '));}catch{return value;}
    }

    private static SimpleRequest ReadRequest(NetworkStream stream)
    {
        var reader=new StreamReader(stream,new UTF8Encoding(false),false,4096,true);
        string first=reader.ReadLine();
        if(String.IsNullOrWhiteSpace(first))return null;
        string[] parts=first.Split(' ');
        if(parts.Length<2)return null;
        var req=new SimpleRequest();
        req.Method=(parts[0]??"GET").Trim().ToUpperInvariant();
        req.Target=parts[1]??"/";
        string rawPath=req.Target;
        int q=rawPath.IndexOf('?');
        string query="";
        if(q>=0){query=rawPath.Substring(q+1);rawPath=rawPath.Substring(0,q);}
        req.Path=UrlDecode(String.IsNullOrWhiteSpace(rawPath)?"/":rawPath);
        if(!String.IsNullOrWhiteSpace(query))
        {
            foreach(string pair in query.Split('&'))
            {
                if(String.IsNullOrWhiteSpace(pair))continue;
                int eq=pair.IndexOf('=');
                string key=eq>=0?pair.Substring(0,eq):pair;
                string value=eq>=0?pair.Substring(eq+1):"";
                key=UrlDecode(key);value=UrlDecode(value);
                if(!String.IsNullOrWhiteSpace(key))req.Query[key]=value;
            }
        }
        while(true)
        {
            string line=reader.ReadLine();
            if(line==null || line.Length==0)break;
            int colon=line.IndexOf(':');
            if(colon<=0)continue;
            string name=line.Substring(0,colon).Trim();
            string value=line.Substring(colon+1).Trim();
            if(name.Length>0)req.Headers[name]=value;
        }
        return req;
    }

    private static void AcceptLoop()
    {
        while(true)
        {
            TcpClient client=null;
            try
            {
                client=Listener.AcceptTcpClient();
                client.NoDelay=true;
                client.ReceiveTimeout=5000;
                client.SendTimeout=15000;
                TcpClient owned=client;
                ThreadPool.QueueUserWorkItem(delegate(object _){HandleClient(owned);});
            }
            catch
            {
                try{if(client!=null)client.Close();}catch{}
                Thread.Sleep(40);
            }
        }
    }

    private static void HandleClient(TcpClient client)
    {
        try
        {
            using(client)
            using(NetworkStream stream=client.GetStream())
            {
                SimpleRequest req=ReadRequest(stream);
                if(req==null)return;
                Handle(req,stream);
            }
        }
        catch { try{client.Close();}catch{} }
    }


    private static string OverlayHtml(SimpleRequest req)
    {
        return "<!doctype html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">\n<title>YOMI OBS</title>\n<style>\nhtml,body{margin:0;width:100%;height:100%;overflow:hidden;background:transparent}\n*{box-sizing:border-box}\n#root{position:absolute;inset:0;display:flex;align-items:stretch;justify-content:flex-start;gap:0;background:transparent;color:#f1f1f1;font-family:\"Segoe UI\",Arial,sans-serif}\n.module{min-width:0;min-height:0;overflow:hidden}\n.media{flex:0 0 auto;height:100%;display:flex;align-items:center;justify-content:center;background:transparent}\n.media img,.media video{display:block;width:100%;height:100%;object-position:center center}\n#art img{object-fit:contain}\n#vid video{object-fit:contain;background:transparent}\n#text{flex:1 1 auto;display:flex;min-width:0;justify-content:center;flex-direction:column;overflow:hidden}\n#title,#channel{white-space:nowrap;overflow:hidden;text-overflow:ellipsis;text-shadow:-2px -2px 0 #0b0b0b,2px -2px 0 #0b0b0b,-2px 2px 0 #0b0b0b,2px 2px 0 #0b0b0b}\n#title{font-weight:650}\n#channel{opacity:.82;margin-top:4px}\n#viz{flex:1 1 auto;align-self:stretch;display:flex;align-items:center;justify-content:center}\n#viz video{position:absolute;width:1px;height:1px;left:0;top:0;opacity:0;pointer-events:none}\n#viz canvas{display:block;width:100%;height:100%;image-rendering:pixelated;background:#000}\n.hidden{display:none!important}\n.vertical{flex-direction:column}\n.vertical .media{width:100%!important;flex:0 0 auto}\n.vertical #text{width:100%}\n</style>\n</head>\n<body>\n<div id=\"root\">\n  <div id=\"art\" class=\"module media hidden\"><img id=\"artImg\" alt=\"\"></div>\n  <div id=\"vid\" class=\"module media hidden\"><video id=\"videoEl\" muted autoplay loop playsinline preload=\"auto\"></video></div>\n  <div id=\"text\" class=\"module hidden\"><div id=\"title\"></div><div id=\"channel\"></div></div>\n  <div id=\"viz\" class=\"module hidden\"><video id=\"vizEl\" muted autoplay loop playsinline preload=\"auto\"></video><canvas id=\"vizCanvas\"></canvas></div>\n</div>\n<script>\n(function(){\n'use strict';\nconst root=document.getElementById('root'),art=document.getElementById('art'),vid=document.getElementById('vid'),txt=document.getElementById('text'),viz=document.getElementById('viz');\nconst artImg=document.getElementById('artImg'),videoEl=document.getElementById('videoEl'),vizEl=document.getElementById('vizEl'),vizCanvas=document.getElementById('vizCanvas'),vizCtx=vizCanvas.getContext('2d',{willReadFrequently:true}),titleEl=document.getElementById('title'),channelEl=document.getElementById('channel');\nlet lastArt='',lastVideo='',lastViz='',lastConfigKey='',pollMs=333,busy=false,videoRetryAt=0,vizRetryAt=0,vizRgb=[138,138,132];\nwindow.addEventListener('resize',()=>{lastConfigKey='';});\nlet videoFrames=0,vizFrames=0,lastFrameReport=performance.now(),videoPresentedFps=-1,vizPresentedFps=-1;\nfunction bool(v,d){return typeof v==='boolean'?v:d}\nfunction num(v,d){v=Number(v);return Number.isFinite(v)?v:d}\nfunction outputById(c,id){let a=Array.isArray(c.director_outputs)?c.director_outputs:[];return a.find(x=>Number(x&&x.id)===id)||null}\nfunction routeSpec(c){\n  let p=location.pathname.toLowerCase(),m=p.match(/^\\/source\\/(\\d+)$/);\n  if(p==='/visualizer')return {modules:['visualizer'],layout:'Horizontal',enabled:true};\n  if(m){let o=outputById(c,Number(m[1]));return o?{modules:String(o.modules||'').split(',').map(x=>x.trim().toLowerCase()).filter(Boolean),layout:String(o.layout||'Horizontal'),enabled:bool(o.enabled,false)}:{modules:[],layout:'Horizontal',enabled:false}}\n  if(p==='/director'){\n    let a=Array.isArray(c.director_outputs)?c.director_outputs:[],o=a.find(x=>x&&bool(x.enabled,false));\n    if(o)return {modules:String(o.modules||'').split(',').map(x=>x.trim().toLowerCase()).filter(Boolean),layout:String(o.layout||'Horizontal'),enabled:true};\n  }\n  let modules=[];\n  if(bool(c.artwork_enabled,true))modules.push('artwork');\n  if(bool(c.video_enabled,true))modules.push('video');\n  if(bool(c.title_enabled,true))modules.push('title');\n  if(bool(c.channel_enabled,true))modules.push('channel');\n  if(bool(c.visualizer_enabled,true))modules.push('visualizer');\n  return {modules:modules,layout:'Horizontal',enabled:true};\n}\nfunction setMedia(el,url,last,kind){\n  url=url||'';\n  let now=Date.now(),retryAt=kind==='video'?videoRetryAt:vizRetryAt,failed=!!el.error;\n  if(url===last&&!failed)return last;\n  if(url===last&&failed&&now<retryAt)return last;\n  if(url){if(kind==='video')videoRetryAt=now+900;else vizRetryAt=now+900;el.dataset.yomiStable='0';el.style.visibility=kind==='video'?'hidden':'visible';el.src=url+(url.includes('?')?'&':'?')+'r='+now;try{let p=el.play();if(p&&p.catch)p.catch(()=>{});}catch(e){}}\n  else{try{el.pause()}catch(e){}el.dataset.yomiStable='0';el.style.visibility=kind==='video'?'hidden':'visible';el.removeAttribute('src');try{el.load()}catch(e){}if(kind==='video')videoRetryAt=0;else vizRetryAt=0;}\n  return url;\n}\nvideoEl.addEventListener('loadeddata',()=>{videoRetryAt=0});videoEl.addEventListener('error',()=>{videoRetryAt=Date.now()+900});\nvizEl.addEventListener('loadeddata',()=>{vizRetryAt=0});vizEl.addEventListener('error',()=>{vizRetryAt=Date.now()+900});\nfunction renderVizBinary(){\n  if(!vizCtx||!vizEl.videoWidth||!vizEl.videoHeight)return;\n  let w=vizEl.videoWidth,h=vizEl.videoHeight;if(vizCanvas.width!==w)vizCanvas.width=w;if(vizCanvas.height!==h)vizCanvas.height=h;\n  try{vizCtx.drawImage(vizEl,0,0,w,h);let im=vizCtx.getImageData(0,0,w,h),d=im.data,r=vizRgb[0],g=vizRgb[1],b=vizRgb[2];for(let i=0;i<d.length;i+=4){let on=(d[i]+d[i+1]+d[i+2])>12;d[i]=on?r:0;d[i+1]=on?g:0;d[i+2]=on?b:0;d[i+3]=255;}vizCtx.putImageData(im,0,0);}catch(e){}\n}\nfunction watchFrames(el,kind){\n  if(!el||typeof el.requestVideoFrameCallback!=='function')return;\n  const loop=()=>{el.requestVideoFrameCallback(()=>{if(kind==='video')videoFrames++;else{vizFrames++;renderVizBinary();}loop();});};loop();\n}\nwatchFrames(videoEl,'video');watchFrames(vizEl,'viz');\nasync function reportFrames(){\n  let now=performance.now(),dt=Math.max(.25,(now-lastFrameReport)/1000);videoPresentedFps=videoFrames/dt;vizPresentedFps=vizFrames/dt;videoFrames=0;vizFrames=0;lastFrameReport=now;\n  try{await fetch('/present?client=obs-renderer&video_fps='+videoPresentedFps.toFixed(2)+'&viz_fps='+vizPresentedFps.toFixed(2)+'&route='+encodeURIComponent(location.pathname),{cache:'no-store'});}catch(e){}\n}\nsetInterval(reportFrames,2000);\nfunction fitText(c){\n  let max=num(c.text_size,33),min=num(c.overlay_min_text_size,18),auto=bool(c.overlay_auto_fit_text,true);\n  let font=String(c.text_font||'Bahnschrift Condensed'),color=String(c.text_color||'#F2F0E8'),outline=Math.max(0,num(c.text_outline,6)),outlineColor=String(c.outline_color||'#151515'),opacity=Math.max(.05,Math.min(1,num(c.text_opacity,1)));\n  let safe=Math.ceil(outline)+2;\n  [titleEl,channelEl].forEach(e=>{e.style.fontFamily=font;e.style.color=color;e.style.opacity=opacity;e.style.webkitTextStroke=outline+'px '+outlineColor;e.style.paintOrder='stroke fill';e.style.boxSizing='border-box';e.style.width='100%';e.style.paddingLeft=safe+'px';e.style.paddingRight=safe+'px';});\n  let glow=bool(c.text_glow,false)?('0 0 '+Math.max(2,outline*2)+'px '+color):'none';titleEl.style.textShadow=glow;channelEl.style.textShadow=glow;\n  let align=String(c.text_alignment||'Auto').toLowerCase();txt.style.alignItems='stretch';txt.style.paddingTop='2px';titleEl.style.textAlign=align==='center'?'center':(align==='right'?'right':'left');channelEl.style.textAlign=titleEl.style.textAlign;\n  let spacing=String(c.title_channel_spacing||'Normal').toLowerCase(),gap=spacing==='tight'?0:(spacing==='loose'?Math.round(max*.45):(spacing==='extra loose'?Math.round(max*.9):(spacing==='maximum'?Math.round(max*1.5):Math.round(max*.12))));channelEl.style.marginTop=gap+'px';\n  let titleSize=max,channelSize=Math.max(min,Math.round(max*.68));\n  function sizeTitle(v){titleSize=v;titleEl.style.fontSize=v+'px';titleEl.style.lineHeight=(v+outline+2)+'px';}\n  sizeTitle(titleSize);channelEl.style.fontSize=channelSize+'px';channelEl.style.lineHeight=(channelSize+outline+2)+'px';\n  if(auto){while(titleSize>min&&titleEl.scrollWidth>titleEl.clientWidth){sizeTitle(titleSize-1);}}\n}\nfunction syncClock(el,clock,visible){\n  if(!visible||!el||!clock)return;\n  let desired=Math.max(0,num(clock.time,0)),speed=Math.max(.25,Math.min(4,num(clock.speed,1))),paused=bool(clock.paused,false);\n  try{\n    if(Number.isFinite(el.duration)&&el.duration>0)desired=desired%el.duration;\n    let drift=num(el.currentTime,0)-desired,stable=el.dataset.yomiStable==='1';\n    if(el.readyState>=2&&!stable){if(Math.abs(drift)>.30){el.currentTime=desired;return;}el.dataset.yomiStable='1';el.style.visibility='visible';}\n    else if(el.readyState>=2&&Math.abs(drift)>.45)el.currentTime=desired;\n    if(Math.abs(num(el.playbackRate,1)-speed)>.01)el.playbackRate=speed;\n    if(paused){if(!el.paused)el.pause()}else if(el.paused){let p=el.play();if(p&&p.catch)p.catch(()=>{})}\n  }catch(e){}\n}\nfunction layoutViz(showViz,showText,layer,matchText,aspect){\n  viz.style.position='relative';viz.style.left='';viz.style.top='';viz.style.width='';viz.style.height='';viz.style.flex='1 1 auto';viz.style.zIndex='1';txt.style.position='relative';txt.style.zIndex='2';\n  if(!showViz||!showText)return;\n  let rr=root.getBoundingClientRect(),tr=txt.getBoundingClientRect();\n  if(tr.width<=0||tr.height<=0)return;\n  viz.style.position='absolute';viz.style.flex='0 0 auto';viz.style.left=Math.max(0,tr.left-rr.left)+'px';viz.style.top=Math.max(0,tr.top-rr.top)+'px';viz.style.height=Math.max(1,tr.height)+'px';\n  viz.style.width=(matchText?Math.max(1,tr.width):Math.max(1,Math.min(tr.width,tr.height*aspect)))+'px';viz.style.zIndex=layer.includes('above')?'3':'0';\n}\nfunction apply(c,t,clock){\n  c=c||{};t=t||{};\n  root.style.display='';\n  let spec=routeSpec(c),mods=new Set(spec.modules),hasTrack=Number(t.index||t.occurrence_id||0)>0;\n  root.classList.toggle('vertical',String(spec.layout||'').toLowerCase().startsWith('vertical'));\n  root.style.gap=Math.max(0,num(c.overlay_text_gap_px,14))+'px';\n  root.style.padding='0 '+Math.max(0,num(c.overlay_safe_margin_px,8))+'px 0 0';\n  let mw=Math.max(1,num(c.media_width,160)),mh=Math.max(1,num(c.media_height,90));\n  [art,vid].forEach(e=>{e.style.width=mw+'px';e.style.height=mh+'px';let on=bool(c.media_border_enabled,true),bp=on?Math.max(0,num(c.media_border_px,2)):0;e.style.border=bp+'px solid '+String(c.media_border_color||'#252525');let cs=String(c.media_corner_style||'Square').toLowerCase();e.style.borderRadius=(cs==='rounded'?12:(cs==='soft'?4:0))+'px';});\n  viz.style.opacity=Math.max(.05,Math.min(1,num(c.visualizer_opacity,.3)));\n  let vc=String(c.visualizer_solid_color||'#8A8A84');if(!/^#[0-9a-f]{6}$/i.test(vc))vc='#8A8A84';vizRgb=[parseInt(vc.slice(1,3),16),parseInt(vc.slice(3,5),16),parseInt(vc.slice(5,7),16)];\n  let vizAspect=Math.max(1,(Math.max(16,num(c.visualizer_internal_width,40))*Math.max(1,num(c.visualizer_length_multiplier,4)))/Math.max(4,num(c.visualizer_internal_height,10)));\n  let vizLayer=String(c.visualizer_layer||'Behind text').toLowerCase(),vizMatchText=bool(c.visualizer_match_text_overhang,true);\n  let showArt=spec.enabled&&hasTrack&&mods.has('artwork')&&!!t.artwork;\n  let showVid=spec.enabled&&hasTrack&&mods.has('video')&&!!t.video;\n  let showTitle=spec.enabled&&hasTrack&&mods.has('title');\n  let showChannel=spec.enabled&&hasTrack&&mods.has('channel');\n  let showText=showTitle||showChannel;\n  let showViz=spec.enabled&&hasTrack&&mods.has('visualizer')&&!!t.visualizer;\n  art.classList.toggle('hidden',!showArt);vid.classList.toggle('hidden',!showVid);txt.classList.toggle('hidden',!showText);viz.classList.toggle('hidden',!showViz);\n  titleEl.classList.toggle('hidden',!showTitle);channelEl.classList.toggle('hidden',!showChannel);\n  titleEl.textContent=showTitle?String(t.title||''):'';\n  channelEl.textContent=showChannel?String(t.channel||''):'';\n  artImg.style.objectFit='contain';\n  if(showArt&&t.artwork!==lastArt){lastArt=t.artwork;artImg.src=t.artwork+(t.artwork.includes('?')?'&':'?')+'r='+Date.now()}else if(!showArt&&lastArt){lastArt='';artImg.removeAttribute('src')}\n  lastVideo=setMedia(videoEl,showVid?t.video:'',lastVideo,'video');\n  lastViz=setMedia(vizEl,showViz?t.visualizer:'',lastViz,'viz');\n  syncClock(videoEl,clock,showVid);syncClock(vizEl,clock,showViz);\n  let configKey=JSON.stringify([showTitle?String(t.title||''):'',showChannel?String(t.channel||''):'',showTitle,showChannel,window.innerWidth,window.innerHeight,c.text_size,c.overlay_min_text_size,c.overlay_auto_fit_text,c.text_font,c.text_color,c.text_outline,c.outline_color,c.text_opacity,c.text_glow,c.text_alignment,c.title_channel_spacing,c.media_width,c.media_height,c.overlay_safe_margin_px,c.overlay_text_gap_px,c.visualizer_internal_width,c.visualizer_internal_height,c.visualizer_length_multiplier,c.visualizer_layer,c.visualizer_match_text_overhang]);\n  if(configKey!==lastConfigKey){lastConfigKey=configKey;fitText(c);}\n  layoutViz(showViz,showText,vizLayer,vizMatchText,vizAspect);\n}\nasync function tick(){\n  if(busy)return;busy=true;\n  try{\n    let sr=await fetch('/snapshot?client=obs-renderer&t='+Date.now(),{cache:'no-store'});\n    if(!sr.ok)throw new Error('snapshot unavailable');\n    let snap=await sr.json(),s=snap&&snap.state?snap.state:{},c=snap&&snap.config?snap.config:{},track=s&&s.track?s.track:{},clock=s&&s.clock?s.clock:{};\n    // One control-plane snapshot at ~3 Hz is enough for title/clock state. HTML video elements\n    // present frames independently; repeated JSON fetches and text reflow must not steal CEF time.\n    pollMs=333;\n    apply(c,track,clock);\n  }catch(e){}\n  finally{busy=false;setTimeout(tick,pollMs)}\n}\ntick();\n})();\n</script>\n</body>\n</html>";
    }

    private static string PreviewHtml(SimpleRequest req)
    {
        int sourceId=0;
        Int32.TryParse(req.QueryValue("source"),NumberStyles.Integer,CultureInfo.InvariantCulture,out sourceId);
        if(sourceId<0)sourceId=0;
        string target=sourceId>0?"/source/"+sourceId.ToString(CultureInfo.InvariantCulture):"/";
        string label=sourceId>0?"OUTPUT "+sourceId.ToString(CultureInfo.InvariantCulture):"PRIMARY / CLASSIC";
        return "<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>YOMI Source Preview</title><style>"+
               "html,body{margin:0;height:100%;background:#111;color:#bbb;font:12px Consolas,monospace}body{display:grid;grid-template-rows:32px 1fr;overflow:hidden}"+
               ".bar{display:flex;align-items:center;gap:16px;padding:0 10px;border-bottom:1px solid #2c2c2c;background:#171717}.name{color:#ddd}.status{color:#8f8f8f}"+
               ".stage{position:relative;overflow:auto;padding:18px;background-color:#151515;background-image:linear-gradient(45deg,#1d1d1d 25%,transparent 25%),linear-gradient(-45deg,#1d1d1d 25%,transparent 25%),linear-gradient(45deg,transparent 75%,#1d1d1d 75%),linear-gradient(-45deg,transparent 75%,#1d1d1d 75%);background-size:24px 24px;background-position:0 0,0 12px,12px -12px,-12px 0}"+
               ".frame{min-width:640px;min-height:180px;height:calc(100% - 38px);border:1px solid #3a3a3a;box-shadow:0 0 0 1px #090909;background:transparent}.frame iframe{display:block;width:100%;height:100%;border:0;background:transparent}"+
               ".hint{position:absolute;left:20px;bottom:4px;color:#707070} </style></head><body><div class=\"bar\"><span class=\"name\">YOMI PREVIEW / "+label+"</span><span id=\"state\" class=\"status\">checking state...</span></div><div class=\"stage\"><div class=\"frame\"><iframe src=\""+target+"\"></iframe></div><div class=\"hint\">Checkerboard is preview-only. OBS receives the transparent source directly.</div></div><script>"+
               "async function ping(){try{let r=await fetch('/snapshot?t='+Date.now(),{cache:'no-store'});let j=r.ok?await r.json():null,s=j&&j.state?j.state:null;document.getElementById('state').textContent='STATE '+(r.ok?'LIVE':'OFF')+' / CONFIG '+(j&&j.config?'LIVE':'OFF')+(s&&s.track&&s.track.title?' / '+s.track.title:'');}catch(e){document.getElementById('state').textContent='SERVER / STATE UNAVAILABLE';}}ping();setInterval(ping,1500);"+
               "</script></body></html>";
    }

    private static void Handle(SimpleRequest req,NetworkStream stream)
    {
        try
        {
            string path=req.Path??"/";
            if(String.IsNullOrEmpty(path))path="/";
            if(req.Method=="OPTIONS"){Empty(stream,204,req.Method,null);return;}

            if(path=="/health") { Text(stream,req,HealthJson(),"application/json"); return; }
            if(path=="/v1/ready") { Text(stream,req,"{\"ready\":true,\"protocol\":1,\"transport\":\"tcp-loopback\"}","application/json"); return; }
            if(path=="/clients") { Text(stream,req,ClientsJson(),"application/json"); return; }
            if(path=="/v1/obs") { Text(stream,req,ObsDiagnosticsJson(),"application/json"); return; }
            if(path=="/present") { TouchClient(req,path); UpdatePresentation(req); Text(stream,req,"{\"ok\":true}","application/json"); return; }
            if(path=="/snapshot" || path=="/v1/snapshot") { TouchClient(req,path); Text(stream,req,SnapshotJson(),"application/json"); return; }
            if(path=="/state" || path=="/v1/state") { TouchClient(req,path); Text(stream,req,StateJson(),"application/json"); return; }
            if(path=="/v1/health") { Text(stream,req,HealthJson(),"application/json"); return; }
            if(path=="/v1/metrics") { Text(stream,req,"{\"health\":"+HealthJson()+",\"clients\":"+ClientsJson()+"}","application/json"); return; }
            if(path=="/history")
            {
                string f=Path.Combine(DataRoot,"state","history.jsonl");
                if(!File.Exists(f)){Text(stream,req,"","text/plain; charset=utf-8");return;}
                FileSimple(stream,req,f,"text/plain; charset=utf-8"); return;
            }
            if(path=="/preview") { TouchClient(req,path); Text(stream,req,PreviewHtml(req),"text/html; charset=utf-8"); return; }
            if(path=="/config")
            {
                TouchClient(req,path);
                string json=ConfigJson();if(json==null){Empty(stream,404,req.Method,null);return;}
                Text(stream,req,json,"application/json"); return;
            }
            if(path=="/" || path=="/overlay" || path=="/overlay.html" || path=="/visualizer" || SourceRegex.IsMatch(path))
            {
                TouchClient(req,path);
                Text(stream,req,OverlayHtml(req),"text/html; charset=utf-8");
                return;
            }

            Match om=ObjectRegex.Match(path);
            if(om.Success)
            {
                TouchClient(req,path);
                string type=om.Groups[1].Value.ToLowerInvariant();string name=om.Groups[2].Value.ToLowerInvariant();
                string f=Path.Combine(DataRoot,"cache","objects",type,name);
                if(!File.Exists(f)){Empty(stream,404,req.Method,null);return;}
                if(type=="artwork"){string ext=Path.GetExtension(f).TrimStart('.');FileSimple(stream,req,f,Mime(ext));return;}
                RangeFile(stream,req,f,"video/mp4");return;
            }

            Match m=MediaRegex.Match(path);
            if(m.Success)
            {
                TouchClient(req,path);
                string type=m.Groups[1].Value.ToLowerInvariant();string n=m.Groups[2].Value;
                if(type=="artwork")
                {
                    string dir=Path.Combine(DataRoot,"cache","artwork");string[] ext={"jpg","jpeg","png","webp"};
                    foreach(string e in ext){string f=Path.Combine(dir,"track-"+n+"."+e);if(File.Exists(f)){FileSimple(stream,req,f,Mime(e));return;}}
                    Empty(stream,404,req.Method,null);return;
                }
                string kind=type=="video"?"video":"visualizer";
                string file=Path.Combine(DataRoot,"cache",kind,"track-"+n+".mp4");
                if(!File.Exists(file)){Empty(stream,404,req.Method,null);return;}RangeFile(stream,req,file,"video/mp4");return;
            }

            if(path=="/favicon.ico"){Empty(stream,204,req.Method,null);return;}
            Empty(stream,404,req.Method,null);
        }
        catch { try{Empty(stream,500,req.Method,null);}catch{} }
    }

    private static string Mime(string ext)
    {
        switch((ext??"").ToLowerInvariant())
        {
            case "webp": return "image/webp";
            case "png": return "image/png";
            default: return "image/jpeg";
        }
    }

    private static string StatusText(int status)
    {
        switch(status)
        {
            case 200:return "OK";
            case 204:return "No Content";
            case 206:return "Partial Content";
            case 404:return "Not Found";
            case 416:return "Range Not Satisfiable";
            case 500:return "Internal Server Error";
            default:return "OK";
        }
    }

    private static void Headers(NetworkStream stream,int status,string mime,long length,Dictionary<string,string> extra)
    {
        var sb=new StringBuilder();
        sb.Append("HTTP/1.1 ").Append(status.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(StatusText(status)).Append("\r\n");
        sb.Append("Date: ").Append(DateTime.UtcNow.ToString("R",CultureInfo.InvariantCulture)).Append("\r\n");
        sb.Append("Server: YOMI-R61.94\r\n");
        sb.Append("X-YOMI-Revision: R61.94\r\n");
        sb.Append("Access-Control-Allow-Origin: *\r\n");
        sb.Append("Access-Control-Allow-Methods: GET, HEAD, OPTIONS\r\n");
        sb.Append("Access-Control-Allow-Headers: Range, Content-Type\r\n");
        sb.Append("Cache-Control: no-store, no-cache, must-revalidate, max-age=0\r\n");
        sb.Append("Pragma: no-cache\r\n");
        sb.Append("X-Content-Type-Options: nosniff\r\n");
        sb.Append("Connection: close\r\n");
        if(!String.IsNullOrWhiteSpace(mime))sb.Append("Content-Type: ").Append(mime).Append("\r\n");
        if(extra!=null)foreach(var kv in extra)sb.Append(kv.Key).Append(": ").Append(kv.Value).Append("\r\n");
        sb.Append("Content-Length: ").Append(Math.Max(0,length).ToString(CultureInfo.InvariantCulture)).Append("\r\n\r\n");
        byte[] bytes=Encoding.ASCII.GetBytes(sb.ToString());
        stream.Write(bytes,0,bytes.Length);
    }

    private static void Empty(NetworkStream stream,int status,string method,Dictionary<string,string> extra)
    {
        Headers(stream,status,null,0,extra);
    }

    private static void Text(NetworkStream stream,SimpleRequest req,string text,string mime)
    {
        byte[] bytes=Encoding.UTF8.GetBytes(text??"");
        Headers(stream,200,mime,bytes.Length,null);
        if(req.Method!="HEAD" && bytes.Length>0)stream.Write(bytes,0,bytes.Length);
    }

    private static void FileSimple(NetworkStream stream,SimpleRequest req,string f,string mime)
    {
        if(!File.Exists(f)){Empty(stream,404,req.Method,null);return;}
        using(var fs=new FileStream(f,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete,65536,FileOptions.SequentialScan))
        {
            Headers(stream,200,mime,fs.Length,null);
            if(req.Method=="HEAD")return;
            CopyCount(fs,stream,fs.Length);
        }
    }

    private static void RangeFile(NetworkStream stream,SimpleRequest req,string f,string mime)
    {
        if(!File.Exists(f)){Empty(stream,404,req.Method,null);return;}
        using(var fs=new FileStream(f,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete,65536,FileOptions.SequentialScan))
        {
            long len=fs.Length,start=0,end=len-1;bool partial=false;string range=req.Header("Range");
            if(!String.IsNullOrWhiteSpace(range)&&range.StartsWith("bytes=",StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string spec=range.Substring(6).Split(',')[0].Trim();string[] parts=spec.Split('-');
                    if(spec.StartsWith("-")){long suffix=Int64.Parse(spec.Substring(1),CultureInfo.InvariantCulture);if(suffix<=0)throw new Exception();if(suffix>len)suffix=len;start=len-suffix;}
                    else{start=Int64.Parse(parts[0],CultureInfo.InvariantCulture);if(parts.Length>1&&!String.IsNullOrWhiteSpace(parts[1]))end=Int64.Parse(parts[1],CultureInfo.InvariantCulture);}
                    if(start<0||start>=len||end<start)throw new Exception();if(end>=len)end=len-1;partial=true;
                }
                catch
                {
                    var invalid=new Dictionary<string,string>();invalid["Content-Range"]="bytes */"+len.ToString(CultureInfo.InvariantCulture);invalid["Accept-Ranges"]="bytes";
                    Empty(stream,416,req.Method,invalid);return;
                }
            }
            long count=end-start+1;
            var extra=new Dictionary<string,string>();extra["Accept-Ranges"]="bytes";
            if(partial)extra["Content-Range"]="bytes "+start.ToString(CultureInfo.InvariantCulture)+"-"+end.ToString(CultureInfo.InvariantCulture)+"/"+len.ToString(CultureInfo.InvariantCulture);
            Headers(stream,partial?206:200,mime,count,extra);
            if(req.Method=="HEAD")return;
            fs.Seek(start,SeekOrigin.Begin);CopyCount(fs,stream,count);
        }
    }

    private static void CopyCount(Stream input,Stream output,long count)
    {
        byte[] buf=new byte[65536];long left=count;
        while(left>0)
        {
            int want=(int)Math.Min(buf.Length,left);int got=input.Read(buf,0,want);if(got<=0)break;
            output.Write(buf,0,got);left-=got;
        }
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
                            double a=QueryDouble(writer,reader,"audio-pts",id++);double t=QueryDouble(writer,reader,"time-pos",id++);bool pause=QueryBool(writer,reader,"pause",id++);double speed=QueryDouble(writer,reader,"speed",id++);
                            double pos=(!Double.IsNaN(a)&&a>=0)?a:((!Double.IsNaN(t)&&t>=0)?t:0);
                            lock(StateLock){Position=pos;Paused=pause;Speed=Double.IsNaN(speed)?1:speed;Connected=true;SampleStamp=Stopwatch.GetTimestamp();}
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch{lock(StateLock){Connected=false;Paused=true;SampleStamp=Stopwatch.GetTimestamp();}Thread.Sleep(500);}
        }
    }

    private static string Query(StreamWriter w,StreamReader r,string prop,int id)
    {
        w.WriteLine("{\"command\":[\"get_property\",\""+prop+"\"],\"request_id\":"+id.ToString(CultureInfo.InvariantCulture)+"}");
        while(true){string line=r.ReadLine();if(line==null)throw new IOException();Match m=RequestIdRegex.Match(line);if(m.Success&&Int32.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture)==id)return line;}
    }
    private static double QueryDouble(StreamWriter w,StreamReader r,string prop,int id)
    {
        string line=Query(w,r,prop,id);if(line.IndexOf("\"error\":\"success\"",StringComparison.OrdinalIgnoreCase)<0)return Double.NaN;
        Match m=NumberDataRegex.Match(line);if(!m.Success)return Double.NaN;double v;if(!Double.TryParse(m.Groups[1].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out v))return Double.NaN;return v;
    }
    private static bool QueryBool(StreamWriter w,StreamReader r,string prop,int id)
    {
        string line=Query(w,r,prop,id);Match m=BoolDataRegex.Match(line);return !m.Success||String.Equals(m.Groups[1].Value,"true",StringComparison.OrdinalIgnoreCase);
    }
}

'@

Add-Type -TypeDefinition $source -Language CSharp
[YomiObsHttpServerR6183]::Start($port,$webRoot,$DataRoot,$stateFile,$pipeName,$runtimeId,$supervisorPid,$serverAuthority)
Write-Host "YOMI OBS server R61.94 TCP loopback: $prefix runtime=$runtimeId supervisor=$supervisorPid authority=$serverAuthority"
while ($true) { Start-Sleep -Seconds 60 }
