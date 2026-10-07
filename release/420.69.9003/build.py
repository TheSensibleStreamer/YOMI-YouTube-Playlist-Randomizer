from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="420.69.9003"
PREVIOUS="420.69.9002"
REVISION="R61.106.52.13.27"
ROOT=Path(sys.argv[1] if len(sys.argv)>1 else ".release-work").resolve()
OUT=Path(sys.argv[2] if len(sys.argv)>2 else "YOMI-Windows.zip").resolve()

def read(rel):
    return (ROOT/rel).read_text(encoding="utf-8-sig")

def write(rel,text):
    p=ROOT/rel
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding="utf-8",newline="")

def replace_once(text,old,new,label):
    n=text.count(old)
    if n!=1:
        raise SystemExit(f"{label}: anchor count {n}, expected 1")
    return text.replace(old,new,1)

def sha256(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""):
            h.update(chunk)
    return h.hexdigest()

# Carry the version through the shipped text surfaces first.
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

# 1) Filtered Listen: the engine-side fix is already in main. Gate it so a stale source
# can never silently republish the old outgoing-song leak.
music_path="payload/app/music.lua"
music=read(music_path)
for required in [
    'PLAYBACK SUBSET STOP outgoing audio; uncached target ',
    'pcall(function() mp.commandv("stop") end)',
]:
    if required not in music:
        raise SystemExit("filtered Listen handoff fix missing: "+required)

# 2) Filtered Listen is explicit playback intent. Consume stale startup-prewarm state so
# the first Play/Pause click cannot be swallowed as a phantom "start warm player" action.
controller_path="payload/app/YomiControllerWpf.cs"
controller=read(controller_path)
controller=replace_once(
    controller,
    '''            if (!SendMpv("script-message", "yomi-playback-subset", csv, query)) return;
            _queuePlaybackSubsetActive = true;''',
    '''            if (!SendMpv("script-message", "yomi-playback-subset", csv, query)) return;
            // Listen is explicit playback intent. A completed startup prewarm must not survive
            // this handoff or the next Play/Pause click is incorrectly consumed as "start warm player".
            _startupPrewarmReady = false;
            _startupPrewarmPlayLatched = false;
            _pauseIntentPending = false;
            _pauseIntentState = false;
            _paused = false;
            BeginPlaybackTransitionProjection();
            ApplyPrimaryTransportGlyph();
            _queuePlaybackSubsetActive = true;''',
    "controller filtered Listen prewarm consumption"
)
controller=replace_once(
    controller,
    '''            _lastPlaybackPhase = phase;

            int currentOccurrence''',
    '''            // Any confirmed unpaused playback consumes the silent-prewarm Ready latch.
            // This protects playback paths that do not originate at the main Play button.
            if (_startupPrewarmReady && _running &&
                ((haveMpvSnapshot && !idleActive && !mpvPaused) || phase == "playing"))
            {
                _startupPrewarmReady = false;
                _startupPrewarmPlayLatched = false;
            }
            _lastPlaybackPhase = phase;

            int currentOccurrence''',
    "controller live playback prewarm safety"
)
controller=replace_once(
    controller,
    '''            bool silentWarm = _startupPrewarmReady || _startupPrewarmInFlight || (_engineLaunchMonitorActive && _engineLaunchPrewarm);
            bool showPause = !silentWarm && _running && _audioActive && !_paused;''',
    '''            bool silentWarm = (_startupPrewarmReady && !_audioActive) || _startupPrewarmInFlight || (_engineLaunchMonitorActive && _engineLaunchPrewarm);
            bool showPause = !silentWarm && _running && _audioActive && !_paused;''',
    "transport glyph stale prewarm guard"
)
write(controller_path,controller)

# 3) Reflow existed in the legacy server but not in the compiled OBS server that actually owns :8876.
# Apply the same Fixed/Reflow slot rule to the native renderer.
host_path="payload/app/YomiObsServerHost.cs"
host=read(host_path)
host=replace_once(
    host,
    r"""  let va=(Number.isFinite(videoAspect)&&videoAspect>.05)?videoAspect:artAspect,vb=fitMediaBox(va,mw,mh),vw=vb[0],vh=vb[1];\n  // Media modules keep a fixed resolution-linked slot. Aspect changes resize only the centered\n  // inner frame, so the video center and the text/viz anchors never walk left or right.\n  art.style.width=mw+'px';art.style.height=mh+'px';vid.style.width=mw+'px';vid.style.height=mh+'px';\n  artFrame.style.width=aw+'px';artFrame.style.height=ah+'px';vidFrame.style.width=vw+'px';vidFrame.style.height=vh+'px';\n""",
    r"""  let va=(Number.isFinite(videoAspect)&&videoAspect>.05)?videoAspect:artAspect,vb=fitMediaBox(va,mw,mh),vw=vb[0],vh=vb[1];\n  let aspectLayout=String(c.media_aspect_layout||'Fixed').toLowerCase(),fixedAspect=aspectLayout!=='reflow';\n  // Fixed preserves the configured media slot and centers narrower video inside it.\n  // Reflow collapses the slot to the fitted frame so following video/text modules move left/up.\n  art.style.width=mw+'px';art.style.height=mh+'px';\n  vid.style.width=(fixedAspect?mw:vw)+'px';vid.style.height=(fixedAspect?mh:vh)+'px';\n  artFrame.style.width=aw+'px';artFrame.style.height=ah+'px';vidFrame.style.width=vw+'px';vidFrame.style.height=vh+'px';\n""",
    "native OBS Fixed/Reflow layout"
)
host=replace_once(
    host,
    "c.text_alignment,c.media_width,c.media_height,c.overlay_safe_margin_px",
    "c.text_alignment,c.media_width,c.media_height,c.media_aspect_layout,c.overlay_safe_margin_px",
    "native OBS Reflow config invalidation"
)
host=host.replace("YOMI OBS compiled server R61.106.33","YOMI OBS compiled server "+REVISION)
write(host_path,host)

# Keep the PowerShell-hosted compiled-server fallback consistent with the same setting.
fallback_path="payload/app/YomiObsServer.ps1"
fallback=read(fallback_path)
fallback=replace_once(
    fallback,
    r"""  let va=(Number.isFinite(videoAspect)&&videoAspect>.05)?videoAspect:(16/9),vw=mw,vh=mh,boxAspect=mw/mh;\n  if(va>boxAspect)vh=Math.max(1,mw/va);else vw=Math.max(1,mh*va);\n  vid.style.width=vw+'px';vid.style.height=vh+'px';vid.style.border=border;vid.style.borderRadius=radius;\n""",
    r"""  let va=(Number.isFinite(videoAspect)&&videoAspect>.05)?videoAspect:(16/9),vw=mw,vh=mh,boxAspect=mw/mh;\n  if(va>boxAspect)vh=Math.max(1,mw/va);else vw=Math.max(1,mh*va);\n  let aspectLayout=String(c.media_aspect_layout||'Fixed').toLowerCase(),fixedAspect=aspectLayout!=='reflow';\n  if(fixedAspect){\n    vid.style.width=mw+'px';vid.style.height=mh+'px';vid.style.border='none';vid.style.borderRadius='0px';\n    videoEl.style.width=vw+'px';videoEl.style.height=vh+'px';videoEl.style.border=border;videoEl.style.borderRadius=radius;\n  }else{\n    vid.style.width=vw+'px';vid.style.height=vh+'px';vid.style.border=border;vid.style.borderRadius=radius;\n    videoEl.style.width='100%';videoEl.style.height='100%';videoEl.style.border='none';videoEl.style.borderRadius=radius;\n  }\n""",
    "fallback OBS Fixed/Reflow layout"
)
fallback=replace_once(
    fallback,
    "c.text_alignment,c.media_width,c.media_height,c.overlay_safe_margin_px",
    "c.text_alignment,c.media_width,c.media_height,c.media_aspect_layout,c.overlay_safe_margin_px",
    "fallback OBS Reflow config invalidation"
)
write(fallback_path,fallback)

# The original server implementation already had the intended Fixed/Reflow math.
legacy_server=read("payload/app/server.ps1")
for required in ["c.media_aspect_layout||'Fixed'","fixedAspect=aspectLayout!=='reflow'"]:
    if required not in legacy_server:
        raise SystemExit("legacy OBS Fixed/Reflow contract missing: "+required)

# 4) 9002 introduced the detached restart relay. 9003 is the first update for which that
# installed 9002 host can actually execute the new completion path.
update_host=read("payload/app/YomiPublicUpdateHost.ps1")
for required in [
    "function Queue-DetachedRelaunch",
    "detached shell relay queued",
    "relay started; waiting for updater host pid",
    "closing updater host; relay will launch YOMI after this process exits",
    "shell-broker direct controller launch",
    "restart verified after updater host exit",
    "update-restart.log",
]:
    if required not in update_host:
        raise SystemExit("detached restart relay gate missing: "+required)

updater=read("payload/app/update.ps1")
deployment=read("payload/app/update-deployment.ps1")
for required in [
    "$manifestFreshUri=$manifestUri+'?yomi_manifest='",
    "Downloaded bytes did not match the manifest. Retrying from a fresh cache path...",
    "yomi_version=",
]:
    if required not in updater:
        raise SystemExit("fresh update delivery gate missing: "+required)
for required in [
    "health=$null;rollback_health=$null",
    "Add-Member -NotePropertyName health -NotePropertyValue $health -Force",
]:
    if required not in deployment:
        raise SystemExit("transaction health gate missing: "+required)

# Final behavior gates after patching.
controller=read(controller_path)
host=read(host_path)
fallback=read(fallback_path)
for required in [
    "Listen is explicit playback intent",
    "Any confirmed unpaused playback consumes the silent-prewarm Ready latch",
    "(_startupPrewarmReady && !_audioActive)",
]:
    if required not in controller:
        raise SystemExit("transport-state fix missing: "+required)
for required in [
    "c.media_aspect_layout||'Fixed'",
    "fixedAspect=aspectLayout!=='reflow'",
    "vid.style.width=(fixedAspect?mw:vw)+'px'",
]:
    if required not in host:
        raise SystemExit("native Reflow fix missing: "+required)
for required in ["c.media_aspect_layout||'Fixed'","fixedAspect=aspectLayout!=='reflow'"]:
    if required not in fallback:
        raise SystemExit("fallback Reflow fix missing: "+required)

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
