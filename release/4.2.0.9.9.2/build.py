from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.9.2"
PREVIOUS="4.2.0.9.9.1"
REVISION="R61.106.52.13.17"
ROOT=Path(sys.argv[1] if len(sys.argv)>1 else ".release-work").resolve()
OUT=Path(sys.argv[2] if len(sys.argv)>2 else "YOMI-Windows.zip").resolve()

def read(rel): return (ROOT/rel).read_text(encoding="utf-8-sig")
def write(rel,text): (ROOT/rel).write_text(text,encoding="utf-8",newline="")
def replace_once(text,old,new,label):
    n=text.count(old)
    if n!=1: raise SystemExit(f"{label}: anchor count {n}, expected 1")
    return text.replace(old,new,1)
def replace_all_checked(text,old,new,expected,label):
    n=text.count(old)
    if n!=expected: raise SystemExit(f"{label}: anchor count {n}, expected {expected}")
    return text.replace(old,new)

# Version bump all shipped text.
text_exts={".ps1",".cs",".json",".txt",".xaml",".cmd",".config",".manifest",".md",".lua"}
for p in list(ROOT.rglob("*")):
    if not p.is_file() or p.name=="build-manifest.json" or p.suffix.lower() not in text_exts: continue
    try: t=p.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError: continue
    if PREVIOUS in t:
        p.write_text(t.replace(PREVIOUS,VERSION),encoding="utf-8",newline="")

write("payload/VERSION.txt",VERSION+"\n")
write("payload/app/FOCUSED-BUILD.txt",f"YOMI {VERSION} Focused Media Player\n{REVISION}\n")

# ------------------------------------------------------------------
# Controller: transition transport must stay live; visualizer preview
# gets normal scheduling priority and the new visualizer defaults.
# ------------------------------------------------------------------
controller_path="payload/app/YomiControllerWpf.cs"
c=read(controller_path)

c=replace_all_checked(c,
    '"visualizer_adaptive_fill", "Adaptive"',
    '"visualizer_adaptive_fill", "Off"',
    3,"controller visualizer fill fallbacks")
c=replace_all_checked(c,
    'ComboText(_settingsVisualizerFill, "Adaptive")',
    'ComboText(_settingsVisualizerFill, "Off")',
    2,"controller visualizer fill save fallbacks")
c=replace_all_checked(c,
    '"visualizer_high_frequency_lift_db", 4',
    '"visualizer_high_frequency_lift_db", 0',
    3,"controller visualizer lift fallbacks")
c=replace_all_checked(c,
    'ComboInt(_settingsVisualizerLift, 4)',
    'ComboInt(_settingsVisualizerLift, 0)',
    2,"controller visualizer lift save fallbacks")
c=replace_all_checked(c,
    '"visualizer_fps", "30 FPS"',
    '"visualizer_fps", "60 FPS"',
    3,"controller visualizer fps fallbacks")
c=replace_all_checked(c,
    'ComboText(_settingsVisualizerFps, "30 FPS")',
    'ComboText(_settingsVisualizerFps, "60 FPS")',
    2,"controller visualizer fps save fallbacks")

old_transport='''        private static bool PrimaryTransportIsActionable(bool running, bool audioActive, bool supervisorStarting, string phase)
        {
            if (supervisorStarting) return false;
            if (audioActive) return true;
            if (!running) return true;
            return !IsTransitionPhase(phase);
        }
'''
new_transport='''        private static bool PrimaryTransportIsActionable(bool running, bool audioActive, bool supervisorStarting, string phase)
        {
            // Preparing a replacement track must never freeze Play/Pause. The MPV pause
            // property remains authoritative even while the requested occurrence is resolving.
            return !supervisorStarting;
        }
'''
c=replace_once(c,old_transport,new_transport,"primary transport availability")

old_toggle='''            // Toggle only confirmed active audio. A live MPV process is not the same
            // thing as a playing track: PREPARING / STARTING / COMPLETE / ERROR must
            // never turn the button into a misleading pause control.
            if (_audioActive)
            {
                bool basis = _pauseIntentPending ? _pauseIntentState : _paused;
                bool targetPause = !basis;
                long serial = ++_pauseIntentSerial;
                _pauseIntentPending = true;
                _pauseIntentState = targetPause;
                // Project the user's pause/resume intent onto local previews immediately.
                // Audio authority still belongs to MPV; a rejected command rolls this back.
                ApplyPreviewPauseProjection(targetPause);
                DispatchTransportCommand(new object[] { "set_property", "pause", targetPause }, delegate(bool accepted)
                {
                    if (serial != _pauseIntentSerial) return;
                    _pauseIntentPending = false;
                    if (accepted)
                    {
                        _paused = targetPause;
                        ProjectPlaybackSnapshotPause(targetPause);
                        ApplyPreviewPauseProjection(targetPause);
                        ApplyPrimaryTransportGlyph();
                        ScheduleFastPlaybackRefresh();
                    }
                    else
                    {
                        ApplyPreviewPauseProjection(_paused);
                        ShowToast("Playback", targetPause ? "Pause command was not accepted." : "Resume command was not accepted.");
                        ScheduleFastPlaybackRefresh();
                    }
                });
                return;
            }

            string phase = (_lastPlaybackPhase ?? "").ToLowerInvariant();
            if (phase == "preparing" || phase == "starting" || phase == "advancing")
            {
                ScheduleFastPlaybackRefresh();
                return;
            }
'''
new_toggle='''            string phase = (_lastPlaybackPhase ?? "").ToLowerInvariant();
            // Pause/Play stays authoritative while an uncached queue jump is PREPARING,
            // STARTING or ADVANCING. The outgoing audio may already be retired, but MPV
            // retains the requested pause state and applies it when the incoming file loads.
            if (_audioActive || IsTransitionPhase(phase))
            {
                bool basis = _pauseIntentPending ? _pauseIntentState : _paused;
                bool targetPause = !basis;
                long serial = ++_pauseIntentSerial;
                _pauseIntentPending = true;
                _pauseIntentState = targetPause;
                ApplyPreviewPauseProjection(targetPause);
                ApplyPrimaryTransportGlyph();
                DispatchTransportCommand(new object[] { "set_property", "pause", targetPause }, delegate(bool accepted)
                {
                    if (serial != _pauseIntentSerial) return;
                    _pauseIntentPending = false;
                    if (accepted)
                    {
                        _paused = targetPause;
                        ProjectPlaybackSnapshotPause(targetPause);
                        ApplyPreviewPauseProjection(targetPause);
                        ApplyPrimaryTransportGlyph();
                        ScheduleFastPlaybackRefresh();
                    }
                    else
                    {
                        ApplyPreviewPauseProjection(_paused);
                        ApplyPrimaryTransportGlyph();
                        ShowToast("Playback", targetPause ? "Pause command was not accepted." : "Resume command was not accepted.");
                        ScheduleFastPlaybackRefresh();
                    }
                });
                return;
            }
'''
c=replace_once(c,old_toggle,new_toggle,"transition pause/play command path")

c=replace_once(c,
    'if (PrimaryTransportIsActionable(true, false, false, "preparing")) return SelfTestFail("SelfTestCohesionFidelity assertion 6");',
    'if (!PrimaryTransportIsActionable(true, false, false, "preparing")) return SelfTestFail("SelfTestCohesionFidelity assertion 6");',
    "transport self-test transition contract")

c=replace_once(c,
    'var preview = new FfmpegFramePreview(ffmpeg, args, target, width, height, fps, start, visualizer,',
    'var preview = new FfmpegFramePreview(ffmpeg, args, target, width, height, fps, start, false,',
    "visualizer preview normal priority")

write(controller_path,c)

# ------------------------------------------------------------------
# Runtime: uncached explicit queue jumps retire outgoing audio
# immediately instead of leaving the old song audible while resolver
# work runs. New visualizer fallbacks match the factory config.
# ------------------------------------------------------------------
music_path="payload/app/music.lua"
m=read(music_path)
m=replace_all_checked(m,'cfg.visualizer_fps or "30 FPS"','cfg.visualizer_fps or "60 FPS"',1,"lua visualizer fps fallback")
m=replace_all_checked(m,'cfg.visualizer_adaptive_fill or "Adaptive"','cfg.visualizer_adaptive_fill or "Off"',3,"lua visualizer fill fallback")
m=replace_all_checked(m,'cfg.visualizer_high_frequency_lift_db or 4','cfg.visualizer_high_frequency_lift_db or 0',1,"lua visualizer lift fallback")

old_jump='''safe_register_script_message("yomi-jump",function(raw)
    local n=math.floor(tonumber(raw) or 0)
    if n>=1 and n<=#urls then
        explicit_audio_retry(n,"jump")
        cancel_pending_transport();sync_playback_subset_cursor(n);desired_index=n;requested_index=0;work_generation=work_generation+1
        -- "Play now" means play now even if the old occurrence happened to be paused.
        mp.set_property_native("pause",false)
        play_index(n)
    end
end)
'''
new_jump='''safe_register_script_message("yomi-jump",function(raw)
    local n=math.floor(tonumber(raw) or 0)
    if n>=1 and n<=#urls then
        explicit_audio_retry(n,"jump")
        cancel_pending_transport();sync_playback_subset_cursor(n);desired_index=n;requested_index=0;work_generation=work_generation+1
        -- An uncached Play-now request replaces the audible occurrence immediately.
        -- Do not leave the previous song playing for several seconds while yt-dlp resolves.
        if playing_index>0 and playing_index~=n and not audio_ready(n) then
            pcall(function() mp.commandv("stop") end)
            playing_index=0
            loaded_waiting_for_restart=0
            log("JUMP STOP outgoing audio; uncached target "..n.." is preparing")
        end
        -- Play now starts the destination unless the controller subsequently latches Pause.
        mp.set_property_native("pause",false)
        play_index(n)
    end
end)
'''
m=replace_once(m,old_jump,new_jump,"uncached queue jump handoff")
write(music_path,m)

# ------------------------------------------------------------------
# OBS renderer: shared borders keep all softened/rounded corners.
# One seam side still owns the border line, but neither media frame
# is forced square at the shared edge.
# ------------------------------------------------------------------
obs_path="payload/app/YomiObsServerHost.cs"
obs=read(obs_path)
old_seam="""  if(shareSeam&&vertical){artFrame.style.borderRadius=r+' '+r+' 0 0';vidFrame.style.borderRadius='0 0 '+r+' '+r;vidFrame.style.setProperty('--yomi-border-top','0px');}\n  else if(shareSeam){artFrame.style.borderRadius=r+' 0 0 '+r;vidFrame.style.borderRadius='0 '+r+' '+r+' 0';vidFrame.style.setProperty('--yomi-border-left','0px');}\n  else{artFrame.style.borderRadius=r;vidFrame.style.borderRadius=r;}\n"""
new_seam="""  if(shareSeam&&vertical){artFrame.style.borderRadius=r;vidFrame.style.borderRadius=r;vidFrame.style.setProperty('--yomi-border-top','0px');}\n  else if(shareSeam){artFrame.style.borderRadius=r;vidFrame.style.borderRadius=r;vidFrame.style.setProperty('--yomi-border-left','0px');}\n  else{artFrame.style.borderRadius=r;vidFrame.style.borderRadius=r;}\n"""
obs=replace_once(obs,old_seam,new_seam,"OBS shared seam corner radius")
write(obs_path,obs)

# ------------------------------------------------------------------
# Factory defaults.
# ------------------------------------------------------------------
cfg_path="payload/app/default-config.json"
cfg=json.loads(read(cfg_path))
cfg["visualizer_adaptive_fill"]="Off"
cfg["visualizer_high_frequency_trim"]=0
cfg["visualizer_high_frequency_lift_db"]=0
cfg["visualizer_fps"]="60 FPS"
write(cfg_path,json.dumps(cfg,indent=2,ensure_ascii=False)+"\n")

# Regression assertions.
c=read(controller_path)
assert 'return !supervisorStarting;' in c
assert '_audioActive || IsTransitionPhase(phase)' in c
assert 'FfmpegFramePreview(ffmpeg, args, target, width, height, fps, start, false,' in c
assert 'if (!PrimaryTransportIsActionable(true, false, false, "preparing"))' in c
assert '"visualizer_adaptive_fill", "Adaptive"' not in c
assert '"visualizer_high_frequency_lift_db", 4' not in c
assert '"visualizer_fps", "30 FPS"' not in c

m=read(music_path)
assert 'JUMP STOP outgoing audio; uncached target ' in m
assert 'cfg.visualizer_fps or "60 FPS"' in m
assert 'cfg.visualizer_adaptive_fill or "Off"' in m
assert 'cfg.visualizer_high_frequency_lift_db or 0' in m

obs=read(obs_path)
assert "if(shareSeam&&vertical){artFrame.style.borderRadius=r;vidFrame.style.borderRadius=r;" in obs
assert "else if(shareSeam){artFrame.style.borderRadius=r;vidFrame.style.borderRadius=r;" in obs
assert "artFrame.style.borderRadius=r+' '+r+' 0 0'" not in obs

cfg=json.loads(read(cfg_path))
assert cfg["visualizer_adaptive_fill"]=="Off"
assert cfg["visualizer_high_frequency_trim"]==0
assert cfg["visualizer_high_frequency_lift_db"]==0
assert cfg["visualizer_fps"]=="60 FPS"

def sha256(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""): h.update(chunk)
    return h.hexdigest()

files={}
for p in sorted(ROOT.rglob("*")):
    if not p.is_file(): continue
    rel=p.relative_to(ROOT).as_posix()
    if rel=="installer/build-manifest.json": continue
    files[rel]={"bytes":p.stat().st_size,"sha256":sha256(p)}
(ROOT/"installer/build-manifest.json").write_text(json.dumps({"version":"v"+VERSION,"product":"YOMI - YouTube OBS Music Interface","release":REVISION,"files":files},indent=2)+"\n",encoding="utf-8",newline="")

if OUT.exists(): OUT.unlink()
with zipfile.ZipFile(OUT,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for p in sorted(ROOT.rglob("*")):
        if p.is_file(): z.write(p,p.relative_to(ROOT).as_posix())

print(json.dumps({"version":VERSION,"revision":REVISION,"bytes":OUT.stat().st_size,"sha256":sha256(OUT),"files":len(files)+1},indent=2))
