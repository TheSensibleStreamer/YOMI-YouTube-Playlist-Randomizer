from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="420.69.9016"
PREVIOUS="420.69.9015"
REVISION="R61.106.53.16.1"
ROOT=Path(sys.argv[1] if len(sys.argv)>1 else ".release-work").resolve()
OUT=Path(sys.argv[2] if len(sys.argv)>2 else "YOMI-Windows.zip").resolve()

def read(rel):
    return (ROOT/rel).read_text(encoding="utf-8-sig")

def write(rel,text):
    p=ROOT/rel
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding="utf-8",newline="")

def sha256(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""):
            h.update(chunk)
    return h.hexdigest()

text_exts={".ps1",".cs",".json",".txt",".xaml",".cmd",".config",".manifest",".md",".lua"}
for p in list(ROOT.rglob("*")):
    if not p.is_file() or p.name=="build-manifest.json" or p.suffix.lower() not in text_exts:
        continue
    try:
        t=p.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        continue
    if PREVIOUS in t:
        p.write_text(t.replace(PREVIOUS,VERSION),encoding="utf-8",newline="")

write("payload/VERSION.txt",VERSION+"\n")
write("payload/app/FOCUSED-BUILD.txt",f"YOMI {VERSION} Focused Media Player\n{REVISION}\n")

music=read("payload/app/music.lua")
for required in [
    "permanent_reprobe_attempted={}",
    "local function next_occurrence(i,step,include_known_bad)",
    "(include_known_bad or not known_bad(candidate))",
    'return "UNAVAILABLE"',
    'audio=="UNAVAILABLE" and "UNAVAILABLE"',
    'next_subset_transport_occurrence(base,direction,false)',
    'next_occurrence(base,direction,false)',
    'if known_bad(i) then return end',
    'AUDIO QUARANTINE track ',
    'exhausted_unavailable_error',
    'web_embedded,default',
    'android,default',
    'safe_register_script_message("yomi-playback-subset-restore"',
    "playback_subset_restore_token",
    "r6110620-restored-pixel-density",
]:
    if required not in music:
        raise SystemExit("playback quarantine/session gate missing: "+required)
for forbidden in [
    "AUDIO PERMANENT RECHECK track ",
    "next_occurrence(cursor,1,true)",
    "next_subset_transport_occurrence(base,direction,true)",
]:
    if forbidden in music:
        raise SystemExit("obsolete unavailable-track resurrection path still present: "+forbidden)
permanent_block=music.split("local function permanent_error(raw)",1)[1].split("end",1)[0]
if 'video unavailable' in permanent_block.lower():
    raise SystemExit("generic Video unavailable was restored to immediate permanent classification")

obs=read("payload/app/YomiObsServerHost.cs")
for required in [
    "fixedAspect=aspectLayout!=='reflow'",
    "artCanvas=document.getElementById('artCanvas')",
    "videoCanvas=document.getElementById('videoCanvas')",
    "imageSmoothingEnabled=false",
    "mediaResampleMode=nearestScale?'nearest':'smooth'",
    "c.obs_media_scaling||'Smooth'",
    "c.media_aspect_layout||'Reflow'",
    "mediaPairSvg=document.getElementById('mediaPairSvg')",
    "mediaPairRect=document.getElementById('mediaPairRect')",
    "mediaPairLine=document.getElementById('mediaPairLine')",
    "function layoutMediaPair(active,vertical,bp,color,radiusPx)",
    "mediaPairRect.setAttribute('stroke',color)",
    "mediaPairLine.setAttribute('stroke-linecap','butt')",
    "function parseHexColor(v)",
    "vizCtx.imageSmoothingEnabled=false",
    "forceSolid=vizColorMode==='solid'",
    "let mr=(showVid?vid:art).getBoundingClientRect()",
]:
    if required not in obs:
        raise SystemExit("OBS border/resampling/reflow gate missing: "+required)
for forbidden in ["mediaPairOutline","mediaSeam","pixelScale?'pixelated':'auto'"]:
    if forbidden in obs:
        raise SystemExit("obsolete OBS media geometry/scaling path still present: "+forbidden)
if "layoutViz(showViz,showText,vizLayer,vizMatchText,vizAspect,fixedAspect,showArt,showVid,vertical)" not in obs:
    raise SystemExit("visualizer media-edge anchor call missing")

controller=read("payload/app/YomiControllerWpf.cs")
for required in [
    "_settingsObsMediaScaling",
    'FillCombo(_settingsObsMediaScaling, "Smooth", "Nearest")',
    'VisualizerPixelSizeChoiceFromConfig',
    'width = 40; height = 10',
    'page.ScrollToVerticalOffset',
    '"media_aspect_layout", "Reflow"',
    'ComboText(_settingsMediaAspectLayout, "Reflow")',
    'LaunchUnavailableTrackAudit',
    'unavailable-track-audit.ps1',
    'OpenSelectedQueueSource',
    'Open source in browser',
    'Unavailable track audit',
]:
    if required not in controller:
        raise SystemExit("controller audit/settings gate missing: "+required)
if "combo.SelectedIndex = next" in controller:
    raise SystemExit("settings wheel can still mutate closed combo boxes")

audit=read("payload/app/unavailable-track-audit.ps1")
for required in [
    "controller-library.json",
    "state\\session.json",
    "pool-current.json",
    "web_embedded,default",
    "android,default",
    "UNAVAILABLE TRACK AUDIT",
    "Export-Csv",
]:
    if required not in audit:
        raise SystemExit("unavailable-track audit gate missing: "+required)

diagnostic=read("payload/app/YomiDiagnosticBundle.ps1")
for required in [
    "YOMI Diagnostic Bundle v12 / R61.106.53",
    "$excludedTreeRegex",
    "Log-Tails",
    "$f.Length -lt 4MB",
    "Select-Object -First 1500",
]:
    if required not in diagnostic:
        raise SystemExit("compact diagnostic bundle gate missing: "+required)

xaml=read("payload/app/YomiControllerWpf.xaml")
for required in [
    'x:Name="SettingsObsMediaScaling"',
    'Text="Media resampling"',
    'Smooth uses filtered scaling. Nearest keeps hard pixel sampling',
]:
    if required not in xaml:
        raise SystemExit("media resampling UI gate missing: "+required)

config=json.loads(read("payload/app/default-config.json"))
if config.get("obs_media_scaling")!="Smooth":
    raise SystemExit("default media resampling must be Smooth")
if config.get("media_aspect_layout")!="Reflow":
    raise SystemExit("default media aspect layout must be Reflow")
if config.get("visualizer_pixel_size")!="Extra Chunky" or config.get("visualizer_internal_width")!=40 or config.get("visualizer_internal_height")!=10:
    raise SystemExit("default visualizer pixel ladder is not restored")

host=read("payload/app/YomiPublicUpdateHost.ps1")
for required in ['Height="212"','$displayProgress=0.0',"Queue-DetachedRelaunch","detached restart relay owns the single post-update relaunch"]:
    if required not in host:
        raise SystemExit("updater single-restart gate missing: "+required)
if "verified updater process reopened YOMI; closing updater host" in host:
    raise SystemExit("obsolete competing updater restart path still present")

updater=read("payload/app/update.ps1")
if "Restart handoff is ready." not in updater:
    raise SystemExit("update engine handoff gate missing")
if "Start-Process -FilePath $launcher -ArgumentList 'controller'" in updater:
    raise SystemExit("update engine still owns a competing controller relaunch")

installer=read("installer/install.ps1")
for required in [
    "function Set-InstallBuildProgress",
    "Set-InstallBuildProgress 88 'Compiling main YOMI controller...'",
    "installer-wrapper | elevated update succeeded; opening installed YOMI",
]:
    if required not in installer:
        raise SystemExit("installer progress/relaunch gate missing: "+required)


# New Topic/YouTube Music recovery gates, in addition to preserved release gates.
for required in [
    'function youtube_music_url(i)',
    'https://music.youtube.com/watch?v=',
    'youtube-music-checked-v1|',
    'music_attempt and music_url or urls[i]',
    'table.insert(routes,"music-no-js")',
    'full_artwork=',
    'function full_artwork_path(i)',
    'transient_skip_streak',
    'AUDIO FAIL-FORWARD track ',
]:
    if required not in music:
        raise SystemExit("YouTube Music playback/fail-forward gate missing: "+required)
for required in [
    'videoFullArt',
    't.video||t.full_artwork',
    'artwork|fullartwork|video|visualizer',
    'showVid&&!t.video&&!!t.full_artwork',
]:
    if required not in obs:
        raise SystemExit("OBS full-artwork fallback gate missing: "+required)
for required in [
    'BeginVideoArtworkFallback',
    'FullArtworkReference',
    'FullArtworkPath',
    '"full_artwork"',
]:
    if required not in controller:
        raise SystemExit("Controller full-artwork fallback gate missing: "+required)
for required in [
    "ReadToEndAsync()",
    "WaitForExit($limitMs)",
    "AVAILABLE_MUSIC",
    "EXTRACTOR_UNAVAILABLE",
    "'music'",
]:
    if required not in audit:
        raise SystemExit("Bounded YouTube Music audit gate missing: "+required)


# A visible audit must reliably show progress, prevent duplicate scans and
# collect output without swallowing a process failure.
for required in ['YOMI_AUDIT_PROGRESS|','Write-AuditProgress', '$progressTotal',
                 'WaitForExit($limitMs)', 'ReadToEndAsync()']:
    if required not in audit:
        raise SystemExit("track audit progress or bounded extraction missing: "+required)
if 'return \'\\"\' +' in audit:
    raise SystemExit("audit still sends backslash-escaped argument delimiters")
for required in ['_unavailableTrackAuditRunning','YOMI_AUDIT_PROGRESS|',
                 'auditProgressBar.Value = percent','auditWindow.Show()',
                 'auditWindow.Close()']:
    if required not in controller:
        raise SystemExit("visible audit progress window missing: "+required)

files={}
for p in sorted(ROOT.rglob("*")):
    if not p.is_file():
        continue
    rel=p.relative_to(ROOT).as_posix()
    if rel=="installer/build-manifest.json":
        continue
    files[rel]={"bytes":p.stat().st_size,"sha256":sha256(p)}

write("installer/build-manifest.json",json.dumps({
    "version":"v"+VERSION,
    "product":"YOMI - YouTube OBS Music Interface",
    "release":REVISION,
    "update_compatibility":"one-hop-latest",
    "files":files,
},indent=2)+"\n")

if OUT.exists():
    OUT.unlink()
with zipfile.ZipFile(OUT,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for p in sorted(ROOT.rglob("*")):
        if p.is_file():
            z.write(p,p.relative_to(ROOT).as_posix())

print(json.dumps({
    "version":VERSION,
    "revision":REVISION,
    "bytes":OUT.stat().st_size,
    "sha256":sha256(OUT),
    "files":len(files)+1,
},indent=2))
