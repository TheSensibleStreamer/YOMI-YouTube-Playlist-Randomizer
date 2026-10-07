from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="420.69.9010"
PREVIOUS="420.69.9009"
REVISION="R61.106.52.13.34"
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
    "AUDIO PERMANENT RECHECK track ",
    "next_occurrence(cursor,1,true)",
    'safe_register_script_message("yomi-playback-subset-restore"',
    "playback_subset_restore_token",
    "r6110615-crop-restored1",
]:
    if required not in music:
        raise SystemExit("queue/restore/crop gate missing: "+required)

obs=read("payload/app/YomiObsServerHost.cs")
for required in [
    "fixedAspect=aspectLayout!=='reflow'",
    "(!fixedAspect||Math.abs(va-artAspect)<=0.025)",
    "c.obs_media_scaling||'Bicubic'",
    "pixelScale?'pixelated':'auto'",
    "c.media_aspect_layout,c.obs_media_scaling",
]:
    if required not in obs:
        raise SystemExit("OBS Reflow/scaling gate missing: "+required)

fallback=read("payload/app/server.ps1")
for required in [
    "if(!fixedAspect&&showArt&&showVid&&bp>0)",
    "c.obs_media_scaling||'Bicubic'",
    "pixelScale?'pixelated':'auto'",
]:
    if required not in fallback:
        raise SystemExit("fallback OBS gate missing: "+required)

controller=read("payload/app/YomiControllerWpf.cs")
for required in [
    "_settingsObsMediaScaling",
    'Find<ComboBox>("SettingsObsMediaScaling")',
    'FillCombo(_settingsObsMediaScaling, "Bicubic", "Nearest neighbor")',
    'GetString(c, "obs_media_scaling", "Bicubic")',
    'c["obs_media_scaling"] = ComboText(_settingsObsMediaScaling, "Bicubic")',
    'config["obs_media_scaling"] = ComboText(_settingsObsMediaScaling, "Bicubic")',
    '"SettingsObsMediaScaling"',
    'RestoreQueuePlaybackSubsetIfNeeded(Dictionary<string, object> queueState)',
]:
    if required not in controller:
        raise SystemExit("controller OBS/session gate missing: "+required)

xaml=read("payload/app/YomiControllerWpf.xaml")
for required in [
    'x:Name="SettingsObsMediaScaling"',
    'Text="OBS media scaling"',
    'Bicubic is the smooth default. Nearest neighbor keeps hard pixel edges',
]:
    if required not in xaml:
        raise SystemExit("OBS scaling UI gate missing: "+required)

config=json.loads(read("payload/app/default-config.json"))
if config.get("obs_media_scaling")!="Bicubic":
    raise SystemExit("default OBS scaling must be Bicubic")

host=read("payload/app/YomiPublicUpdateHost.ps1")
for required in ['Height="212"','$displayProgress=0.0',"verified updater process reopened YOMI"]:
    if required not in host:
        raise SystemExit("updater polish/relaunch gate missing: "+required)

installer=read("installer/install.ps1")
for required in [
    "function Set-InstallBuildProgress",
    "Set-InstallBuildProgress 88 'Compiling main YOMI controller...'",
    "installer-wrapper | elevated update succeeded; opening installed YOMI",
]:
    if required not in installer:
        raise SystemExit("installer progress/relaunch gate missing: "+required)

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
