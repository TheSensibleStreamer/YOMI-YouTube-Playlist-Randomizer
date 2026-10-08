from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="420.69.9026"
PREVIOUS="420.69.9025"
REVISION="R61.106.53.26.1"
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
    "local function next_occurrence(i,step,include_known_bad)",
    "(include_known_bad or not known_bad(candidate))",
    'audio=="UNAVAILABLE" and "UNAVAILABLE"',
    'next_subset_transport_occurrence(base,direction,false)',
    'next_occurrence(base,direction,false)',
    'if known_bad(i) then return end',
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
for forbidden in ['AUDIO QUARANTINE track ', 'exhausted_unavailable_error',
                  'youtube-music-checked-v1|', 'permanent_reprobe_attempted={}',
                  'write_all(status_path(i,"audio.permanent")']:
    if forbidden in music:
        raise SystemExit("permanent extractor quarantine was restored: "+forbidden)


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
    "mediaPairLine.setAttribute('stroke-linecap','round')",
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
    "function Finish-ProbeRoute(",
    "AVAILABLE_MUSIC",
    "EXTRACTOR_UNAVAILABLE",
    "'music'",
]:
    if required not in audit:
        raise SystemExit("Bounded YouTube Music audit gate missing: "+required)


# A visible audit must reliably show progress, prevent duplicate scans and
# collect output without swallowing a process failure.
for required in ['YOMI_AUDIT_PROGRESS|','Write-AuditProgress', '$progressTotal',
                 '$Handle.StdoutTask.Wait(5000)', 'ReadToEndAsync()']:
    if required not in audit:
        raise SystemExit("track audit progress or bounded extraction missing: "+required)
if 'return \'\\"\' +' in audit:
    raise SystemExit("audit still sends backslash-escaped argument delimiters")
for required in ['_unavailableTrackAuditRunning','YOMI_AUDIT_PROGRESS|',
                 'auditProgressBar.Value = percent','auditWindow.Show()',
                 'auditWindow.Close()']:
    if required not in controller:
        raise SystemExit("visible audit progress window missing: "+required)


# A new release may only use concurrent but bounded route extraction. The
# controller must still receive track-progress messages, and completed workers
# must attach their stderr/metadata to their original video IDs.
for required in ['function Start-ProbeRoute(', 'function Finish-ProbeRoute(',
                 '$maxConcurrent=4', '$handle.Process.HasExited', '$handle.LimitMs',
                 'ReadToEndAsync()', 'Write-AuditProgress', '--skip-download',
                 'return [pscustomobject]@{']:
    if required not in audit:
        raise SystemExit("parallel audit gate missing: "+required)
if 'Invoke-ProbeRoute -ProbeRows' in audit:
    raise SystemExit("old blocking audit batch loop was restored")

for required in ['local route_deadline=pot_attempt and 70 or ((desired_index==i) and 30 or 50)',
                 'AUDIO EXTRACT FAIL track ', 'STREAM ABANDON result track ',
                 'STREAM ABANDON track ', 'youtube_music_url(i)',
                 'AUDIO FAIL-FORWARD track ']:
    if required not in music:
        raise SystemExit("responsive playback/failure diagnostics missing: "+required)


# yt-dlp is an external moving dependency. Stable caches must not silently
# force YOMI to ship the same extractor forever.
installer = (ROOT / 'installer' / 'install.ps1').read_text(encoding='utf-8-sig')
for required in ['yt-dlp-nightly-builds/releases/latest/download/yt-dlp.exe',
                 "yt-dlp-nightly-current.exe", '-MaxCacheAgeHours 24',
                 'function Get-CachedDownload', 'LastWriteTimeUtc']:
    if required not in installer:
        raise SystemExit("yt-dlp current-release install gate missing: " + required)
for required in ['youtube:player_client=web_music,default',
                 'client_route="web_safari,default"',
                 'mode:sub(1,6)=="music-"']:
    if required not in music:
        raise SystemExit("YouTube Music player-client fallback gate missing: " + required)
if "youtube:player_client=web_music,default" not in audit:
    raise SystemExit("audit Music client does not match playback Music client")
if '$_ -notmatch' not in audit:
    raise SystemExit("per-video yt-dlp errors can be misattributed across audit batches")

for required in [
    'AUDIO QUARANTINE CLEARED track ',
    'AUDIO EXTRACTION EXHAUSTED track ',
    'extractor-failed-v2|',
    'audio_prefetch_backoff(i)',
    'return age<900',
    'explicit_audio_retry(n,"transport")',
    'explicit_audio_retry(target,"filtered-listen")',
]:
    if required not in music:
        raise SystemExit("Retryable extraction requirement missing: "+required)

# Do not create more yt-dlp subprocesses after full-route exhaustion.
for required in [
    "function report_exhausted_audio(i,summary)",
    "if attempt_number>max_attempts then",
    "AUDIO BUDGET EXHAUSTED track ",
    "STREAM CACHE FALLBACK SKIPPED track ",
    "if audio_prefetch_backoff(i) or (audio_failures[i] or 0)>=audio_attempt_limit(i)",
    "if not audio_ready(i) and not audio_prefetch_backoff(i) then enqueue",
]:
    if required not in music:
        raise SystemExit("bounded extraction route-budget gate missing: "+required)
if "if not audio_ready(i) and (p<=0 or not audio_prefetch_backoff(i))" in music:
    raise SystemExit("current-position prefetch bypasses failed-source cooldown")

# Exhausted prefetch must not waste time probing the same clients again on EOF.
if 'STREAM PREFETCH EXHAUSTED track ' not in music:
    raise SystemExit("missing prefetched-source direct stream gate")
if 'if audio_prefetch_backoff(i) then' not in music:
    raise SystemExit("missing exact source-aware cache failure guard")
if 'report_exhausted_audio(i,"Prefetch already exhausted the source; click Play to retry.")' not in music:
    raise SystemExit("failed prefetch not handed to same bounded fail-forward")
if 'explicit_audio_retry(n,"jump")' not in music:
    raise SystemExit("manual play no longer bypasses failed-source cooldown")

# Regression gates for real 9021 diagnostics: avoid auto-retrying an
# exhausted source through stale transport authority, and do not call a
# deleted classifier in artwork/video asynchronous callbacks.
if 'TRANSPORT FAILED ACK-CLEAR track ' not in music:
    raise SystemExit("failed transport target not cleared on extraction exhaustion")
if 'if transport_pending_target==i and cancel_pending_transport then' not in music:
    raise SystemExit("failed transport cancel not scoped to originating target")
if 'permanent_error(' in music:
    raise SystemExit("optional artwork/video still calls removed permanent_error")
if 'mark_optional_failure("art",i,"download")' not in music:
    raise SystemExit("artwork failure no longer terminates safely")
if 'VIDEO ROUTE FAIL track ' not in music or 'mark_optional_failure("video",i,"routes")' not in music:
    raise SystemExit("video route failure no longer terminates safely")

# PO token provider is optional, pinned, off by default unless installed,
# and only invoked on a bounded mweb route after the regular extractor routes.
for required in [
    'pot_exe = install_root',
    'pot_plugin_root = install_root',
    'function audio_attempt_limit(i)',
    'local pot_attempt=pot_available and attempt_number==max_attempts',
    'youtubepot-bgutilcli:cli_path=',
    'youtube:player_client=mweb',
    'local route_deadline=pot_attempt and 70',
    'if attempt_number>max_attempts then',
]:
    if required not in music:
        raise SystemExit("on-demand PO recovery Lua gate missing: "+required)

for required in [
    'bgutil-pot-rs-v0.8.1-windows.exe',
    'bgutil-pot-rs-v0.8.1-plugin.zip',
    '25d6b05c79176aa792454c3d1727922ca47e56cf11cb1e866615d751819b14a0',
    '99fd83b98fa93b193d6a3b69dc74410d76e7a2b889868c54d16121cac9060344',
    'PO token CLI SHA-256 mismatch',
    'PO token plugin SHA-256 mismatch',
    'bgutil-rs',
    'bgutil-pot.exe',
    'Get-FileHash',
]:
    if required not in installer:
        raise SystemExit("PO provider installation and integrity gate missing: "+required)

# Preserve the successful provider integration while making its behavior
# measurable without disclosing token bytes or complete yt-dlp debug traces.
for required in [
    'if pot_attempt then',
    'table.insert(a,"--verbose")',
    'PO PROVIDER TRACE track ',
    'provider_cli_verified=',
    'plugin_paths_listed=',
    'provider_import_error=',
    'token_request_logged=',
    'youtube_unavailable=',
    'error_line=error_line:gsub',
    'failure_detail="status="',
]:
    if required not in music:
        raise SystemExit("PO provider telemetry and redaction gate missing: "+required)
if 'local plugins=lower:match' in music or 'plugin_mentioned=' in music:
    raise SystemExit("obsolete misleading plugin telemetry restored")


# Regression: the specific Music replacement from the 9025 failed Maize diagnostic.
if 'music_endpoint_candidates["FGjWXtNh0Mg"]="TJ2QCLCe9l8"' not in music:
    raise SystemExit("Maize known-good Music replacement missing")
if 'MUSIC ENDPOINT DECISION track ' not in music:
    raise SystemExit("Missing endpoint lookup skip-reason telemetry")
if "YomiMusicEndpointResolver.ps1" not in installer:
    raise SystemExit("Installer does not require the Music replacement helper")
if "mediaPairLine.setAttribute('stroke-linecap','round')" not in obs:
    raise SystemExit("Shared media seam cap is square")
if "artFrame.style.borderRadius=r+' 0px 0px '+r" not in obs:
    raise SystemExit("Shared media content is not rounded/clipped at exterior")
if "vidFrame.style.borderRadius='0px '+r+' '+r+' 0px'" not in obs:
    raise SystemExit("Shared media right frame corner clip is missing")

# YOMI Music replacements: preserve originals in queue and verify a working
# replacement by actual downloaded audio before persisting its 14-day mapping.
helper=read("payload/app/YomiMusicEndpointResolver.ps1")
fontpack=read("payload/app/YomiFontPack.ps1")
for required in ["INITIAL_ENDPOINT", "watchEndpoint", "MUSIC_ENDPOINT_ID="]:
    if required not in helper: raise SystemExit("Music endpoint helper missing: "+required)
for required in ["try_music_endpoint_replacement","MUSIC ENDPOINT VERIFIED track ",
                 "music_media_url(i)","music_resolved_music_url(i)",
                 "music_endpoint_map_path", "audio_failures[i]=nil"]:
    if required not in music: raise SystemExit("Music replacement recovery missing: "+required)
for required in ["Press Start 2P", "Pixelify Sans", "Audiowide", "Righteous", "Black Ops One",
                 "Teko", "Barlow Condensed", "IBM Plex Sans Condensed", "UnifrakturCook","Great Vibes"]:
    if required not in fontpack or required not in controller or required not in obs:
        raise SystemExit("private font selection or OBS route missing "+required)
if 'if (combo.IsKeyboardFocusWithin)' not in controller:
    raise SystemExit("Settings closed focused dropdown wheel handling missing")
if 'path.StartsWith("/fonts/"' not in obs:
    raise SystemExit("OBS local font serving missing")
if 'YomiFontPack.ps1' not in installer:
    raise SystemExit("Private fonts are not prepared by installer")


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
