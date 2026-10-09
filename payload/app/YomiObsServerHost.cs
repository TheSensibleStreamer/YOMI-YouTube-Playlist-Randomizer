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
    private static string PresentedGeometry = "";
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
    private static string CachedConfigPath = "";
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
    private static readonly Regex MediaRegex = new Regex(@"^/media/(artwork|fullartwork|video|visualizer)/(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
            return String.Format(CultureInfo.InvariantCulture,"{{\"video_fps\":{0:0.###},\"visualizer_fps\":{1:0.###},\"age_ms\":{2},\"route\":\"{3}\",\"browser_geometry\":\"{4}\"}}",PresentedVideoFps,PresentedVisualizerFps,age==Int64.MaxValue?-1:age,EscapeJson(PresentedRoute),EscapeJson(PresentedGeometry));
        }
    }

    private static void UpdatePresentation(SimpleRequest req)
    {
        if(req==null)return;
        double video=-1,viz=-1;
        Double.TryParse(req.QueryValue("video_fps"),NumberStyles.Float,CultureInfo.InvariantCulture,out video);
        Double.TryParse(req.QueryValue("viz_fps"),NumberStyles.Float,CultureInfo.InvariantCulture,out viz);
        string route=req.QueryValue("route")??"";
        string geometry=req.QueryValue("geometry")??"";
        if(geometry.Length>800)geometry="oversized-geometry-ignored";
        lock(PresentationLock){PresentedVideoFps=video;PresentedVisualizerFps=viz;PresentedRoute=route;PresentedGeometry=geometry;PresentedStampTicks=DateTime.UtcNow.Ticks;}
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
        string[] keys={"artwork","full_artwork","video","visualizer"};
        string[] kinds={"artwork","fullartwork","video","visualizer"};
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
            string preview=Path.Combine(DataRoot,"state","settings-live-preview.json");
            string path=File.Exists(preview)?preview:Path.Combine(DataRoot,"config.json");
            var file=new FileInfo(path);if(!file.Exists)return null;
            long stamp=file.LastWriteTimeUtc.Ticks;
            if(CachedConfig==null || !String.Equals(CachedConfigPath,path,StringComparison.OrdinalIgnoreCase) || stamp!=CachedConfigStamp || file.Length!=CachedConfigLength)
            {
                string raw=File.ReadAllText(path,Encoding.UTF8).Trim();
                CachedConfig=raw.StartsWith("{")?raw:null;CachedConfigStamp=stamp;CachedConfigLength=file.Length;CachedConfigPath=path;
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


    // Font faces are loaded only when selected. Files are local to YOMI; never installed into Windows.
    private static string YomiOverlayFontFacesCss()
    {
        return "@font-face{font-family:'Press Start 2P';src:url('/fonts/PressStart2P-Regular.ttf') format('truetype');font-display:swap;}@font-face{font-family:'Pixelify Sans';src:url('/fonts/PixelifySans-Variable.ttf') format('truetype');font-display:swap;}@font-face{font-family:'Audiowide';src:url('/fonts/Audiowide-Regular.ttf') format('truetype');font-display:swap;}@font-face{font-family:'Righteous';src:url('/fonts/Righteous-Regular.ttf') format('truetype');font-display:swap;}@font-face{font-family:'Black Ops One';src:url('/fonts/BlackOpsOne-Regular.ttf') format('truetype');font-display:swap;}@font-face{font-family:'Teko';src:url('/fonts/Teko-Variable.ttf') format('truetype');font-display:swap;}@font-face{font-family:'Barlow Condensed';src:url('/fonts/BarlowCondensed-Regular.ttf') format('truetype');font-display:swap;}@font-face{font-family:'IBM Plex Sans Condensed';src:url('/fonts/IBMPlexSansCondensed-Regular.ttf') format('truetype');font-display:swap;}@font-face{font-family:'UnifrakturCook';src:url('/fonts/UnifrakturCook-Bold.ttf') format('truetype');font-display:swap;}@font-face{font-family:'Great Vibes';src:url('/fonts/GreatVibes-Regular.ttf') format('truetype');font-display:swap;}";
    }

    private static string OverlayHtml(SimpleRequest req)
    {
        return "<!doctype html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">\n<title>YOMI OBS</title>\n<style>\n"+YomiOverlayFontFacesCss()+"html,body{margin:0;width:100%;height:100%;overflow:hidden;background:transparent}\n*{box-sizing:border-box}\n#root{position:absolute;inset:0;display:flex;align-items:stretch;justify-content:flex-start;gap:0;background:transparent;color:#f1f1f1;font-family:\"Segoe UI\",Arial,sans-serif}\n.module{min-width:0;min-height:0;overflow:visible}\n.media{position:relative;flex:0 0 auto;height:100%;display:flex;align-items:center;justify-content:center;background:transparent;overflow:visible}\n.media-frame{position:relative;flex:0 0 auto;display:flex;align-items:center;justify-content:center;overflow:hidden;isolation:isolate;background:transparent;border-style:solid;border-color:var(--yomi-media-border-color,#252525);border-width:var(--yomi-border-top,var(--yomi-media-border-width,0px)) var(--yomi-border-right,var(--yomi-media-border-width,0px)) var(--yomi-border-bottom,var(--yomi-media-border-width,0px)) var(--yomi-border-left,var(--yomi-media-border-width,0px))}\n.media-frame img,.media-frame video,.media-frame canvas{position:absolute;inset:0;display:block;width:100%;height:100%;object-position:center center}\n.media-frame canvas{display:none}\n#art img{object-fit:contain}\n#vid video{object-fit:contain;background:transparent}\n#vid img{object-fit:contain;background:transparent;display:none}\n#text{flex:1 1 auto;display:flex;min-width:0;justify-content:center;flex-direction:column;overflow:hidden}\n#title,#channel{white-space:nowrap;overflow:visible;text-overflow:clip;text-shadow:-2px -2px 0 #0b0b0b,2px -2px 0 #0b0b0b,-2px 2px 0 #0b0b0b,2px 2px 0 #0b0b0b;font-weight:400}\n#channel{opacity:.82}\n#viz{position:absolute;left:0;top:0;width:0;height:0;flex:0 0 0;display:flex;align-items:center;justify-content:center;overflow:hidden}\n#viz video{position:absolute;width:1px;height:1px;left:0;top:0;opacity:0;pointer-events:none}\n#viz canvas{display:block;width:100%;height:100%;image-rendering:pixelated;background:transparent}\n#mediaPairSvg{position:absolute;display:none;pointer-events:none;z-index:10;overflow:visible}\n.hidden{display:none!important}\n.vertical{flex-direction:column}\n.vertical .media{flex:0 0 auto}\n.vertical #text{width:100%}\n</style>\n</head>\n<body>\n<div id=\"root\">\n  <div id=\"art\" class=\"module media hidden\"><div id=\"artFrame\" class=\"media-frame\"><img id=\"artImg\" alt=\"\"><canvas id=\"artCanvas\"></canvas></div></div>\n  <div id=\"vid\" class=\"module media hidden\"><div id=\"vidFrame\" class=\"media-frame\"><video id=\"videoEl\" muted loop playsinline preload=\"auto\"></video><img id=\"videoFullArt\" alt=\"\"><canvas id=\"videoCanvas\"></canvas></div></div>\n  <svg id=\"mediaPairSvg\" xmlns=\"http://www.w3.org/2000/svg\" aria-hidden=\"true\"><rect id=\"mediaPairRect\" fill=\"none\"></rect><line id=\"mediaPairLine\"></line></svg>\n  <div id=\"text\" class=\"module hidden\"><div id=\"title\"></div><div id=\"channel\"></div></div>\n  <div id=\"viz\" class=\"module hidden\"><video id=\"vizEl\" muted loop playsinline preload=\"auto\"></video><canvas id=\"vizCanvas\"></canvas></div>\n</div>\n<script>\n(function(){\n'use strict';\nconst root=document.getElementById('root'),art=document.getElementById('art'),vid=document.getElementById('vid'),txt=document.getElementById('text'),viz=document.getElementById('viz'),mediaPairSvg=document.getElementById('mediaPairSvg'),mediaPairRect=document.getElementById('mediaPairRect'),mediaPairLine=document.getElementById('mediaPairLine');\nconst artFrame=document.getElementById('artFrame'),vidFrame=document.getElementById('vidFrame'),artImg=document.getElementById('artImg'),artCanvas=document.getElementById('artCanvas'),artCtx=artCanvas.getContext('2d'),videoEl=document.getElementById('videoEl'),videoFullArt=document.getElementById('videoFullArt'),videoCanvas=document.getElementById('videoCanvas'),videoCtx=videoCanvas.getContext('2d'),vizEl=document.getElementById('vizEl'),vizCanvas=document.getElementById('vizCanvas'),vizCtx=vizCanvas.getContext('2d',{willReadFrequently:true}),titleEl=document.getElementById('title'),channelEl=document.getElementById('channel');\nlet lastArt='',lastVideo='',lastFullArt='',lastViz='',lastConfigKey='',pollMs=333,busy=false,videoRetryAt=0,vizRetryAt=0,videoAspect=16/9,mediaResampleMode='smooth',artCropMode=true,lastArtCanvasKey='',vizLengthMultiplier=1,vizDisplayColumns=0,vizColorMode='solid',vizSolidColor='#8A8A84';\nlet lastClock=null,lastClockAt=0,lastSeek={video:0,viz:0},lastDriftMs={video:null,viz:null};\nconst vizSourceCanvas=document.createElement('canvas'),vizSourceCtx=vizSourceCanvas.getContext('2d',{willReadFrequently:true});\nwindow.addEventListener('resize',()=>{lastConfigKey='';});\nlet videoFrames=0,vizFrames=0,lastFrameReport=performance.now(),videoPresentedFps=-1,vizPresentedFps=-1;\nfunction bool(v,d){return typeof v==='boolean'?v:d}\nfunction num(v,d){v=Number(v);return Number.isFinite(v)?v:d}\nfunction outputById(c,id){let a=Array.isArray(c.director_outputs)?c.director_outputs:[];return a.find(x=>Number(x&&x.id)===id)||null}\nfunction routeSpec(c){\n  let p=location.pathname.toLowerCase(),m=p.match(/^\\/source\\/(\\d+)$/);\n  if(p==='/visualizer')return {modules:['visualizer'],layout:'Horizontal',enabled:true};\n  if(m){let o=outputById(c,Number(m[1]));return o?{modules:String(o.modules||'').split(',').map(x=>x.trim().toLowerCase()).filter(Boolean),layout:String(o.layout||'Horizontal'),enabled:bool(o.enabled,false)}:{modules:[],layout:'Horizontal',enabled:false}}\n  if(p==='/director'){\n    let a=Array.isArray(c.director_outputs)?c.director_outputs:[],o=a.find(x=>x&&bool(x.enabled,false));\n    if(o)return {modules:String(o.modules||'').split(',').map(x=>x.trim().toLowerCase()).filter(Boolean),layout:String(o.layout||'Horizontal'),enabled:true};\n  }\n  let modules=[];\n  if(bool(c.artwork_enabled,true))modules.push('artwork');\n  if(bool(c.video_enabled,true))modules.push('video');\n  if(bool(c.title_enabled,true))modules.push('title');\n  if(bool(c.channel_enabled,true))modules.push('channel');\n  if(bool(c.visualizer_enabled,true))modules.push('visualizer');\n  return {modules:modules,layout:'Horizontal',enabled:true};\n}\nfunction setMedia(el,url,last,kind){\n  url=url||'';\n  let now=Date.now(),retryAt=kind==='video'?videoRetryAt:vizRetryAt,failed=!!el.error;\n  if(url===last&&!failed)return last;\n  if(url===last&&failed&&now<retryAt)return last;\n  if(url){if(kind==='video')videoRetryAt=now+900;else vizRetryAt=now+900;el.dataset.yomiStable='0';el.style.visibility='hidden';try{el.pause()}catch(e){}el.src=url+(url.includes('?')?'&':'?')+'r='+now;}\n  else{try{el.pause()}catch(e){}el.dataset.yomiStable='0';el.style.visibility=kind==='video'?'hidden':'visible';el.removeAttribute('src');try{el.load()}catch(e){}if(kind==='video')videoRetryAt=0;else vizRetryAt=0;}\n  return url;\n}\nvideoEl.addEventListener('loadeddata',()=>{videoRetryAt=0;syncAV(videoEl,'video',null)});videoEl.addEventListener('loadedmetadata',()=>{if(videoEl.videoWidth>0&&videoEl.videoHeight>0){videoAspect=videoEl.videoWidth/videoEl.videoHeight;lastConfigKey='';}});videoEl.addEventListener('error',()=>{videoRetryAt=Date.now()+900});\nvizEl.addEventListener('loadeddata',()=>{vizRetryAt=0;syncAV(vizEl,'viz',null)});vizEl.addEventListener('error',()=>{vizRetryAt=Date.now()+900});\nfunction parseHexColor(v){let s=String(v||'').trim(),m=s.match(/^#?([0-9a-f]{6})$/i);if(!m)return [138,138,132];let n=parseInt(m[1],16);return [(n>>16)&255,(n>>8)&255,n&255];}\nfunction renderVizFrame(){\n  if(!vizCtx||!vizSourceCtx||!vizEl.videoWidth||!vizEl.videoHeight)return;\n  let sw=vizEl.videoWidth,sh=vizEl.videoHeight,requested=visualizerColumns(sw,vizLengthMultiplier),w=vizDisplayColumns>0?Math.min(requested,vizDisplayColumns):requested,h=sh;\n  if(vizCanvas.width!==w)vizCanvas.width=w;if(vizCanvas.height!==h)vizCanvas.height=h;\n  if(vizSourceCanvas.width!==sw)vizSourceCanvas.width=sw;if(vizSourceCanvas.height!==sh)vizSourceCanvas.height=sh;\n  try{\n    vizSourceCtx.imageSmoothingEnabled=false;vizSourceCtx.drawImage(vizEl,0,0,sw,sh);\n    let source=vizSourceCtx.getImageData(0,0,sw,sh).data,frame=vizCtx.createImageData(w,h),d=frame.data;\n    let solid=parseHexColor(vizSolidColor),forceSolid=vizColorMode==='solid';\n    // Peak-pool frequency columns rather than discarding alternate frequencies.\n    // All source and output cells stay square: 1.0 = 1/8, 8.0 = full.\n    for(let y=0;y<h;y++){\n      for(let x=0;x<w;x++){\n        let lo=Math.floor(x*sw/w),hi=Math.max(lo+1,Math.ceil((x+1)*sw/w)),best=-1,bright=6;\n        for(let sx=lo;sx<hi;sx++){\n          let at=(y*sw+sx)*4,v=Math.max(source[at],source[at+1],source[at+2]);\n          if(v>bright){bright=v;best=at;}\n        }\n        if(best<0)continue;\n        let target=(y*w+x)*4;\n        d[target]=forceSolid?solid[0]:source[best];\n        d[target+1]=forceSolid?solid[1]:source[best+1];\n        d[target+2]=forceSolid?solid[2]:source[best+2];d[target+3]=255;\n      }\n    }\n    vizCtx.putImageData(frame,0,0);\n  }catch(e){}\n}\nfunction ensureCanvas(canvas,w,h){w=Math.max(1,Math.round(w));h=Math.max(1,Math.round(h));if(canvas.width!==w)canvas.width=w;if(canvas.height!==h)canvas.height=h;return [w,h];}\nfunction renderArtFrame(){\n  if(mediaResampleMode!=='nearest'||!artImg.complete||!artImg.naturalWidth||!artImg.naturalHeight)return;\n  let size=ensureCanvas(artCanvas,artFrame.clientWidth,artFrame.clientHeight),w=size[0],h=size[1],sw=artImg.naturalWidth,sh=artImg.naturalHeight,key=[artImg.src,w,h,artCropMode].join('|');\n  if(key===lastArtCanvasKey)return;lastArtCanvasKey=key;artCtx.clearRect(0,0,w,h);artCtx.imageSmoothingEnabled=false;\n  if(artCropMode){let sa=sw/sh,ta=w/h,sx=0,sy=0,cw=sw,ch=sh;if(sa>ta){cw=sh*ta;sx=(sw-cw)/2}else if(sa<ta){ch=sw/ta;sy=(sh-ch)/2}artCtx.drawImage(artImg,sx,sy,cw,ch,0,0,w,h)}\n  else{let scale=Math.min(w/sw,h/sh),dw=Math.max(1,sw*scale),dh=Math.max(1,sh*scale),dx=(w-dw)/2,dy=(h-dh)/2;artCtx.drawImage(artImg,0,0,sw,sh,dx,dy,dw,dh)}\n}\nfunction renderVideoFrame(){\n  if(mediaResampleMode!=='nearest'||videoEl.readyState<2||!videoEl.videoWidth||!videoEl.videoHeight)return;\n  let size=ensureCanvas(videoCanvas,vidFrame.clientWidth,vidFrame.clientHeight),w=size[0],h=size[1];videoCtx.clearRect(0,0,w,h);videoCtx.imageSmoothingEnabled=false;videoCtx.drawImage(videoEl,0,0,w,h);\n}\nartImg.addEventListener('load',()=>{lastArtCanvasKey='';renderArtFrame();});\nfunction watchFrames(el,kind){\n  if(!el||typeof el.requestVideoFrameCallback!=='function')return;\n  const loop=()=>{el.requestVideoFrameCallback((now,metadata)=>{if(kind==='video'){videoFrames++;syncAV(el,'video',metadata);renderVideoFrame();}else{vizFrames++;syncAV(el,'viz',metadata);renderVizFrame();}loop();});};loop();\n}\nwatchFrames(videoEl,'video');watchFrames(vizEl,'viz');\nasync function reportFrames(){\n  let now=performance.now(),dt=Math.max(.25,(now-lastFrameReport)/1000);videoPresentedFps=videoFrames/dt;vizPresentedFps=vizFrames/dt;videoFrames=0;vizFrames=0;lastFrameReport=now;\n  // Small read-only geometry proof accompanies regular FPS telemetry (every 2s).\n  // The diagnostics bundle can now show whether OBS CEF placed a visualizer\n  // behind media or cropped a rounded corner, rather than guessing from CSS.\n  function rect(el){let r=el.getBoundingClientRect();return [Math.round(r.left),Math.round(r.top),Math.round(r.right),Math.round(r.bottom)];}\n  let geo=JSON.stringify({art:rect(artFrame),video:rect(vidFrame),viz:rect(viz),text:rect(txt),vizCanvas:rect(vizCanvas),vizRaster:[vizCanvas.width,vizCanvas.height],vizNative:[vizEl.videoWidth,vizEl.videoHeight],vizPaused:vizEl.paused,videoDriftMs:lastDriftMs.video,vizDriftMs:lastDriftMs.viz,\n    radius:getComputedStyle(vidFrame).borderTopLeftRadius,\n    leftBorder:getComputedStyle(vidFrame).borderLeftWidth,\n    artRightBorder:getComputedStyle(artFrame).borderRightWidth,\n    visible:[!art.classList.contains('hidden'),!vid.classList.contains('hidden'),!viz.classList.contains('hidden')]});\n  try{await fetch('/present?client=obs-renderer&video_fps='+videoPresentedFps.toFixed(2)+'&viz_fps='+vizPresentedFps.toFixed(2)+'&route='+encodeURIComponent(location.pathname)+'&geometry='+encodeURIComponent(geo),{cache:'no-store'});}catch(e){}\n}\nsetInterval(reportFrames,2000);\nfunction fitText(c){\n  let max=num(c.text_size,32),min=num(c.overlay_min_text_size,18),auto=bool(c.overlay_auto_fit_text,true);\n  let font=String(c.text_font||'Bahnschrift Condensed'),color=String(c.text_color||'#F2F0E8'),outline=Math.max(0,num(c.text_outline,6)),outlineColor=String(c.outline_color||'#151515'),opacity=Math.max(.05,Math.min(1,num(c.text_opacity,1)));\n  let safeX=Math.ceil(outline/2)+3,safeY=Math.ceil(outline/2)+2;\n  [titleEl,channelEl].forEach(e=>{e.style.fontFamily=font;e.style.fontWeight='400';e.style.color=color;e.style.opacity=opacity;e.style.webkitTextStroke=outline+'px '+outlineColor;e.style.paintOrder='stroke fill';e.style.boxSizing='border-box';e.style.width='100%';e.style.paddingLeft=safeX+'px';e.style.paddingRight=safeX+'px';e.style.fontSize=max+'px';e.style.lineHeight='1';e.style.overflow='visible';});\n  let glow=bool(c.text_glow,false)?('0 0 '+Math.max(2,outline*2)+'px '+color):'none';titleEl.style.textShadow=glow;channelEl.style.textShadow=glow;\n  let align=String(c.text_alignment||'Auto').toLowerCase();txt.style.alignItems='stretch';txt.style.paddingTop='0px';txt.style.paddingBottom='0px';titleEl.style.textAlign=align==='center'?'center':(align==='right'?'right':'left');channelEl.style.textAlign=titleEl.style.textAlign;\n  let titleVisible=!titleEl.classList.contains('hidden'),channelVisible=!channelEl.classList.contains('hidden'),both=titleVisible&&channelVisible;\n  if(!both){txt.style.justifyContent='center';txt.style.gap='0px';}\n  else{txt.style.justifyContent='space-evenly';txt.style.gap='0px';}\n  titleEl.style.marginTop='0';channelEl.style.marginTop='0';\n  let titleSize=max;\n  function sizeTitle(v){titleSize=v;titleEl.style.fontSize=v+'px';}\n  if(auto){while(titleSize>min&&titleEl.scrollWidth>titleEl.clientWidth){sizeTitle(titleSize-1);}}\n}\nfunction clockPositionAt(displayAt){\n  if(!lastClock||lastClock.connected===false||!lastClockAt)return null;\n  let speed=Math.max(.25,Math.min(4,num(lastClock.speed,1)));\n  let clockAge=Math.max(0,Math.min(1000,num(lastClock.age_ms,0)));\n  let localAge=Math.max(0,Math.min(2500,displayAt-lastClockAt));\n  let elapsed=bool(lastClock.paused,true)?0:(clockAge+localAge)*speed/1000;\n  return Math.max(0,num(lastClock.time,0)+elapsed);\n}\nfunction syncAV(el,kind,frame){\n  if(!el||!lastClock||el.readyState<2)return;\n  if((kind==='viz'?lastViz:lastVideo)===''||(kind==='viz'?viz:vid).classList.contains('hidden'))return;\n  let paused=bool(lastClock.paused,true)||lastClock.connected===false;\n  if(paused){if(!el.paused)el.pause();return;}\n  let now=performance.now();\n  let displayAt=frame&&Number.isFinite(frame.expectedDisplayTime)?Math.max(now,frame.expectedDisplayTime):now;\n  let desired=clockPositionAt(displayAt);if(desired===null)return;\n  if(Number.isFinite(el.duration)&&el.duration>0)desired=desired%el.duration;\n  let actual=frame&&Number.isFinite(frame.mediaTime)?frame.mediaTime:num(el.currentTime,0);\n  let drift=desired-actual;\n  if(Number.isFinite(el.duration)&&el.duration>0&&Math.abs(drift)>el.duration/2)drift-=Math.sign(drift)*el.duration;\n  lastDriftMs[kind]=Math.round(drift*1000);\n  let primed=el.dataset.yomiStable==='1';\n  // Seek once at startup, thereafter only for meaningful discontinuities.\n  // Small drift is corrected with a tiny speed trim and no expensive seek loop.\n  if(!el.seeking&&(!primed||(Math.abs(drift)>.10&&now-lastSeek[kind]>1200))){\n    if(Math.abs(drift)>.020){el.currentTime=desired;lastSeek[kind]=now;}\n    el.dataset.yomiStable='1';el.style.visibility='visible';\n  }\n  let speed=Math.max(.25,Math.min(4,num(lastClock.speed,1)));\n  let trim=primed&&Math.abs(drift)>.017?Math.max(-.03,Math.min(.03,drift*.6)):0;\n  let requested=speed*(1+trim);\n  if(Math.abs(num(el.playbackRate,1)-requested)>.006)el.playbackRate=requested;\n  if(el.paused){let promise=el.play();if(promise&&promise.catch)promise.catch(()=>{});}\n}\nfunction layoutMediaPair(active,vertical,bp,color,radiusPx){\n  mediaPairSvg.style.display='none';\n  if(!active||bp<=0)return;\n  let rr=root.getBoundingClientRect(),ar=artFrame.getBoundingClientRect(),vr=vidFrame.getBoundingClientRect(),size=Math.max(1,bp);\n  let left=Math.min(ar.left,vr.left),top=Math.min(ar.top,vr.top),right=Math.max(ar.right,vr.right),bottom=Math.max(ar.bottom,vr.bottom),w=Math.max(1,right-left),h=Math.max(1,bottom-top),half=size/2;\n  mediaPairSvg.style.left=(left-rr.left)+'px';mediaPairSvg.style.top=(top-rr.top)+'px';mediaPairSvg.style.width=w+'px';mediaPairSvg.style.height=h+'px';mediaPairSvg.setAttribute('viewBox','0 0 '+w+' '+h);\n  mediaPairRect.setAttribute('x',half);mediaPairRect.setAttribute('y',half);mediaPairRect.setAttribute('width',Math.max(0,w-size));mediaPairRect.setAttribute('height',Math.max(0,h-size));mediaPairRect.setAttribute('rx',Math.max(0,radiusPx-half));mediaPairRect.setAttribute('ry',Math.max(0,radiusPx-half));mediaPairRect.setAttribute('stroke',color);mediaPairRect.setAttribute('stroke-width',size);mediaPairRect.setAttribute('vector-effect','non-scaling-stroke');\n  mediaPairLine.setAttribute('stroke',color);mediaPairLine.setAttribute('stroke-width',size);mediaPairLine.setAttribute('stroke-linecap','round');mediaPairLine.setAttribute('vector-effect','non-scaling-stroke');\n  if(vertical){let y=((ar.bottom+vr.top)/2)-top;mediaPairLine.setAttribute('x1',half);mediaPairLine.setAttribute('x2',Math.max(half,w-half));mediaPairLine.setAttribute('y1',y);mediaPairLine.setAttribute('y2',y);}\n  else{let x=((ar.right+vr.left)/2)-left;mediaPairLine.setAttribute('x1',x);mediaPairLine.setAttribute('x2',x);mediaPairLine.setAttribute('y1',half);mediaPairLine.setAttribute('y2',Math.max(half,h-half));}\n  mediaPairSvg.style.display='block';\n}\nfunction visualizerColumns(sourceWidth,length){\n  // 8.0 exposes all encoded frequency bins; 4.0 uses four of eight units.\n  return Math.max(1,Math.min(sourceWidth,Math.round(sourceWidth*Math.max(1,Math.min(8,length))/8)));\n}\nfunction fitVizPixelGrid(maxW,maxH,cols,rows){\n  // Use whole square CSS cells. When horizontal space is tight, peak-pool\n  // the frequency columns instead of rounding the cell size down and leaving\n  // a huge empty strip (which could make 8.0 SHORTER than 4.0).\n  let capacityW=Math.max(1,maxW),capacityH=Math.max(1,maxH);\n  let largestCell=Math.max(1,Math.floor(capacityH/rows));\n  let cell=Math.min(largestCell,Math.max(1,Math.ceil(capacityW/cols)));\n  let columns=Math.max(1,Math.min(cols,Math.floor(capacityW/cell)));\n  return [columns*cell,rows*cell,columns];\n}\nfunction layoutViz(showViz,showText,layer,matchText,aspect,fixedAspect,showArt,showVid,vertical){\n  // A visible visualizer sharing text is ABSOLUTE from the first layout pass.\n  // Resetting it to a flex child before measuring text was changing the text\n  // region and could flash the visualizer behind the media on each update.\n  txt.style.position='relative';txt.style.zIndex='2';\n  viz.style.position='absolute';viz.style.left='0px';viz.style.top='0px';\n  viz.style.width='0px';viz.style.height='0px';viz.style.flex='0 0 0px';\n  viz.style.zIndex='0';\n  if(!showViz){vizDisplayColumns=0;return;}\n  // Source spectrum carries all 8.0 units. Pick logical frequency columns\n  // without distorting cells; the renderer peak-pools all source frequencies.\n  let sampleW=Math.max(1,Number(vizEl.videoWidth)||192),sampleH=Math.max(1,Number(vizEl.videoHeight)||6);\n  let selectedColumns=visualizerColumns(sampleW,vizLengthMultiplier);\n  if(!showText){\n    let rr=root.getBoundingClientRect(),left=0;\n    if(!vertical&&(showArt||showVid)){\n      if(showArt)left=Math.max(left,artFrame.getBoundingClientRect().right-rr.left);\n      if(showVid)left=Math.max(left,vidFrame.getBoundingClientRect().right-rr.left);\n    }\n    let size=fitVizPixelGrid(rr.width-left,rr.height,selectedColumns,sampleH),width=size[0],height=size[1];\n    vizDisplayColumns=size[2];\n    viz.style.left=left+'px';viz.style.top=((rr.height-height)/2)+'px';\n    viz.style.width=width+'px';viz.style.height=height+'px';viz.style.zIndex='1';\n    return;\n  }\n  let rr=root.getBoundingClientRect(),tr=txt.getBoundingClientRect();\n  if(tr.width<=0||tr.height<=0)return;\n  // Start at the last visible media PIXEL (the frame, not the placeholder slot).\n  // Text has moduleGap after media, so the visualizer begins just left of text.\n  let left=Math.max(0,tr.left-rr.left);\n  if(!vertical&&(showArt||showVid)){\n    let mediaRight=-Infinity;\n    if(showArt)mediaRight=Math.max(mediaRight,artFrame.getBoundingClientRect().right);\n    if(showVid)mediaRight=Math.max(mediaRight,vidFrame.getBoundingClientRect().right);\n    if(Number.isFinite(mediaRight))left=Math.max(0,mediaRight-rr.left);\n  }\n  let right=Math.max(left+1,tr.right-rr.left),available=Math.max(1,right-left);\n  // Matching text overhang only limits the available canvas. It may NOT\n  // stretch source pixels into rectangles. When space is narrower than the\n  // square-pixel aspect, shrink both dimensions and center vertically.\n  let size=fitVizPixelGrid(available,tr.height,selectedColumns,sampleH),width=size[0],height=size[1];\n  vizDisplayColumns=size[2];\n  viz.style.position='absolute';viz.style.flex='0 0 auto';viz.style.left=left+'px';\n  viz.style.top=Math.max(0,tr.top-rr.top+(tr.height-height)/2)+'px';\n  viz.style.height=height+'px';viz.style.width=width+'px';\n  viz.style.zIndex=layer.includes('above')?'3':'0';\n}\nfunction apply(c,t,clock){\n  c=c||{};t=t||{};\n  root.style.display='';\n  let spec=routeSpec(c),mods=new Set(spec.modules),hasTrack=Number(t.index||t.occurrence_id||0)>0;\n  let vertical=String(spec.layout||'').toLowerCase().startsWith('vertical');\n  root.classList.toggle('vertical',vertical);\n  root.style.gap='0px';\n  root.style.padding='0 '+Math.max(0,num(c.overlay_safe_margin_px,8))+'px 0 0';\n  let mw=Math.max(1,num(c.media_width,160)),mh=Math.max(1,num(c.media_height,90)),moduleGap=Math.max(0,num(c.overlay_text_gap_px,14));\n  // The browser source may be shorter than the configured media box (e.g. 135px\n  // overlay versus 144px media). Fit BOTH dimensions before border geometry:\n  // clipping the extra pixels discards the bottom rounded corners entirely.\n  let visibleH=Math.max(1,root.getBoundingClientRect().height);\n  if(mh>visibleH){let scale=visibleH/mh;mw=Math.max(1,mw*scale);mh=visibleH;}\n  let on=bool(c.media_border_enabled,true),bp=on?Math.max(0,num(c.media_border_px,2)):0,borderColor=String(c.media_border_color||'#252525'),cs=String(c.media_corner_style||'Square').toLowerCase(),radiusPx=(cs==='rounded'?13:(cs==='soft'?6.5:0));\n  let showArt=spec.enabled&&hasTrack&&mods.has('artwork')&&!!t.artwork;\n  let showVid=spec.enabled&&hasTrack&&mods.has('video')&&!!(t.video||t.full_artwork);\n  let showTitle=spec.enabled&&hasTrack&&mods.has('title');\n  let showChannel=spec.enabled&&hasTrack&&mods.has('channel');\n  let showText=showTitle||showChannel;\n  let showViz=spec.enabled&&hasTrack&&mods.has('visualizer')&&!!t.visualizer;\n  function fitMediaBox(aspect,maxW,maxH){let w=maxW,h=maxH,box=maxW/Math.max(1,maxH);if(aspect>box)h=Math.max(1,maxW/aspect);else w=Math.max(1,maxH*aspect);return [w,h];}\n  let artAspect=16/9,ab=fitMediaBox(artAspect,mw,mh),aw=ab[0],ah=ab[1];\n  let va=(!t.video&&t.full_artwork)?artAspect:((Number.isFinite(videoAspect)&&videoAspect>.05)?videoAspect:artAspect),vb=fitMediaBox(va,mw,mh),vw=vb[0],vh=vb[1];\n  let aspectLayout=String(c.media_aspect_layout||'Reflow').toLowerCase(),fixedAspect=aspectLayout!=='reflow';\n  // Fixed preserves the configured media slot and centers narrower video inside it.\n  // Reflow collapses the slot to the fitted frame so following video/text modules move left/up.\n  art.style.width=mw+'px';art.style.height=mh+'px';\n  vid.style.width=(fixedAspect?mw:vw)+'px';vid.style.height=(fixedAspect?mh:vh)+'px';\n  artFrame.style.width=aw+'px';artFrame.style.height=ah+'px';vidFrame.style.width=vw+'px';vidFrame.style.height=vh+'px';\n  [artFrame,vidFrame].forEach(e=>{e.style.setProperty('--yomi-media-border-width',bp+'px');e.style.setProperty('--yomi-media-border-color',borderColor);e.style.setProperty('--yomi-border-top',bp+'px');e.style.setProperty('--yomi-border-right',bp+'px');e.style.setProperty('--yomi-border-bottom',bp+'px');e.style.setProperty('--yomi-border-left',bp+'px');});\n  art.style.margin='0';vid.style.margin='0';txt.style.margin='0';\n  let shareSeam=on&&bp>0&&showArt&&showVid&&(!fixedAspect||Math.abs(va-artAspect)<=0.025);\n  let effectiveRadius=radiusPx>0?(shareSeam?Math.max(radiusPx,bp*2+2):radiusPx):0,r=effectiveRadius+'px';\n  // Media clips to the ACTUAL solid frame border rather than painting a border\n  // pseudo-element ON TOP of the picture. Both sides of the shared seam are\n  // disjoint, and their physical border widths add to exactly bp pixels.\n  if(shareSeam){\n    let first=Math.floor(bp/2),second=bp-first;\n    artFrame.style.setProperty(vertical?'--yomi-border-bottom':'--yomi-border-right',first+'px');\n    vidFrame.style.setProperty(vertical?'--yomi-border-top':'--yomi-border-left',second+'px');\n  }\n  artFrame.style.borderRadius=r;vidFrame.style.borderRadius=r;\n  // All four corners must clip real image/video pixels; a true CSS border owns\n  // the edge so neither the image nor a neighboring video can bleed across it.\n  let mediaClip=effectiveRadius>0?'inset(0px round '+r+')':'none';artFrame.style.clipPath=mediaClip;vidFrame.style.clipPath=mediaClip;\n  viz.style.opacity=Math.max(.05,Math.min(1,num(c.visualizer_opacity,.3)));\n  vizLengthMultiplier=Math.max(1,num(c.visualizer_length_multiplier,4));vizColorMode=String(c.visualizer_color_mode||'Solid').toLowerCase();vizSolidColor=String(c.visualizer_solid_color||'#8A8A84');\n  let vizAspect=Math.max(1,(Math.max(1,num(c.visualizer_internal_width,40))*vizLengthMultiplier)/Math.max(1,num(c.visualizer_internal_height,10)));\n  let vizLayer=String(c.visualizer_layer||'Behind text').toLowerCase(),vizMatchText=bool(c.visualizer_match_text_overhang,true);\n  if(showText&&(showArt||showVid)){if(vertical)txt.style.marginTop=moduleGap+'px';else txt.style.marginLeft=moduleGap+'px';}\n  art.classList.toggle('hidden',!showArt);vid.classList.toggle('hidden',!showVid);txt.classList.toggle('hidden',!showText);viz.classList.toggle('hidden',!showViz);\n  layoutMediaPair(false,vertical,bp,borderColor,radiusPx);\n  titleEl.classList.toggle('hidden',!showTitle);channelEl.classList.toggle('hidden',!showChannel);\n  titleEl.textContent=showTitle?String(t.title||''):'';\n  channelEl.textContent=showChannel?String(t.channel||''):'';\n  artCropMode=bool(c.smart_artwork_crop,true);artImg.style.objectFit=artCropMode?'cover':'contain';\n  let mediaScale=String(c.obs_media_scaling||'Smooth').toLowerCase(),nearestScale=mediaScale.includes('nearest')||mediaScale.includes('point');\n  mediaResampleMode=nearestScale?'nearest':'smooth';artImg.style.opacity=nearestScale?'0':'1';videoEl.style.opacity=nearestScale?'0':'1';artCanvas.style.display=nearestScale?'block':'none';videoCanvas.style.display=(nearestScale&&!!t.video)?'block':'none';\n  if(nearestScale){renderArtFrame();renderVideoFrame()}else{lastArtCanvasKey='';}\n  if(showArt&&t.artwork!==lastArt){lastArt=t.artwork;artImg.src=t.artwork+(t.artwork.includes('?')?'&':'?')+'r='+Date.now()}else if(!showArt&&lastArt){lastArt='';artImg.removeAttribute('src')}\n  let fallbackImage=showVid&&!t.video&&!!t.full_artwork;\n  if(fallbackImage){\n    if(t.full_artwork!==lastFullArt){\n      lastFullArt=t.full_artwork;\n      videoFullArt.src=t.full_artwork+(t.full_artwork.includes('?')?'&':'?')+'r='+Date.now();\n    }\n    videoFullArt.style.display='block';\n  }else{\n    videoFullArt.style.display='none';\n    if(lastFullArt){lastFullArt='';videoFullArt.removeAttribute('src');}\n  }\n  lastVideo=setMedia(videoEl,showVid&&!!t.video?t.video:'',lastVideo,'video');\n  lastViz=setMedia(vizEl,showViz?t.visualizer:'',lastViz,'viz');\n  syncAV(videoEl,'video',null);syncAV(vizEl,'viz',null);\n  let configKey=JSON.stringify([showTitle?String(t.title||''):'',showChannel?String(t.channel||''):'',showTitle,showChannel,window.innerWidth,window.innerHeight,c.text_size,c.overlay_min_text_size,c.overlay_auto_fit_text,c.text_font,c.text_color,c.text_outline,c.outline_color,c.text_opacity,c.text_glow,c.text_alignment,c.media_width,c.media_height,c.media_aspect_layout,c.obs_media_scaling,c.overlay_safe_margin_px,c.overlay_text_gap_px,c.visualizer_internal_width,c.visualizer_internal_height,c.visualizer_length_multiplier,c.visualizer_layer,c.visualizer_match_text_overhang]);\n  if(configKey!==lastConfigKey){lastConfigKey=configKey;fitText(c);}\n  layoutViz(showViz,showText,vizLayer,vizMatchText,vizAspect,fixedAspect,showArt,showVid,vertical);\n}\nasync function tick(){\n  if(busy)return;busy=true;\n  try{\n    let sr=await fetch('/snapshot?client=obs-renderer&t='+Date.now(),{cache:'no-store'});\n    if(!sr.ok)throw new Error('snapshot unavailable');\n    let snap=await sr.json(),s=snap&&snap.state?snap.state:{},c=snap&&snap.config?snap.config:{},track=s&&s.track?s.track:{},clock=s&&s.clock?s.clock:{};\n    // One control-plane snapshot at ~3 Hz is enough for title/clock state. HTML video elements\n    // present frames independently; repeated JSON fetches and text reflow must not steal CEF time.\n    pollMs=333;\n    lastClock=clock;lastClockAt=performance.now();apply(c,track,clock);\n  }catch(e){}\n  finally{busy=false;setTimeout(tick,pollMs)}\n}\ntick();\n})();\n</script>\n</body>\n</html>";
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

            // Serve only single local font filenames; no arbitrary paths or directory traversal.
            if(path.StartsWith("/fonts/",StringComparison.Ordinal))
            {
                string name=path.Substring(7);
                if(!Regex.IsMatch(name,@"^[A-Za-z0-9-]{1,80}\.ttf$",RegexOptions.CultureInvariant)) { Empty(stream,404,req.Method,null);return; }
                string fontPath=Path.Combine(DataRoot,"fonts",name);
                FileSimple(stream,req,fontPath,"font/ttf");return;
            }
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
                if(type=="artwork" || type=="fullartwork")
                {
                    string dir=Path.Combine(DataRoot,"cache","artwork");string[] ext={"jpg","jpeg","png","webp"};
                    foreach(string e in ext)
                    {
                        string f=Path.Combine(dir,"track-"+n+(type=="fullartwork"?".full":"")+"."+e);
                        if(File.Exists(f)){FileSimple(stream,req,f,Mime(e));return;}
                    }
                    if(type=="fullartwork")
                    {
                        foreach(string e in ext)
                        {
                            string f=Path.Combine(dir,"track-"+n+"."+e);
                            if(File.Exists(f)){FileSimple(stream,req,f,Mime(e));return;}
                        }
                    }
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


public static class YomiObsServerHostR611069
{
    private static string Env(string name,string fallback)
    {
        string value=Environment.GetEnvironmentVariable(name);
        return String.IsNullOrWhiteSpace(value)?fallback:value;
    }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            string programFiles=Env("ProgramFiles",@"C:\Program Files");
            string localApp=Env("LOCALAPPDATA",Environment.CurrentDirectory);
            string installRoot=Env("YOMI_INSTALL_ROOT",Path.Combine(programFiles,"YOMI"));
            string dataRoot=Env("YOMI_DATA_ROOT",Path.Combine(localApp,"YOMI"));
            string webRoot=Path.Combine(installRoot,"web");
            string stateFile=Path.Combine(dataRoot,"state","current.json");
            string runtimeId=Env("YOMI_RUNTIME_ID","compiled-host");
            string authority=Env("YOMI_SERVER_AUTHORITY","supervisor");
            int supervisorPid=0; Int32.TryParse(Env("YOMI_SUPERVISOR_PID","0"),out supervisorPid);
            int port=8876; Int32.TryParse(Env("YOMI_SERVER_PORT","8876"),out port);
            if(port<1 || port>65535)port=8876;
            YomiObsHttpServerR6183.Start(port,webRoot,dataRoot,stateFile,"yomi-v4",runtimeId,supervisorPid,authority);
            Console.WriteLine("YOMI OBS compiled server R61.106.52.13.27 TCP loopback: http://127.0.0.1:"+port.ToString(CultureInfo.InvariantCulture)+"/ runtime="+runtimeId+" supervisor="+supervisorPid.ToString(CultureInfo.InvariantCulture)+" authority="+authority);
            while(true)Thread.Sleep(60000);
        }
        catch(Exception ex)
        {
            try{Console.Error.WriteLine("YOMI OBS server fatal: "+ex.ToString());}catch{}
            return 1;
        }
    }
}
