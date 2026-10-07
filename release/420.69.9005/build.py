from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="420.69.9005"
PREVIOUS="420.69.9004"
REVISION="R61.106.52.13.29"
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
    'function artwork_profile()',
    'r6110615-crop-restored1',
    'if artwork_profile_ready(i) then',
    'ArtworkEdgeDetector.exe',
    'ARTWORK COLOR DETECT track ',
    'ARTWORK COLOR CROP track ',
    'ART READY SMART-CROP track ',
    'scale=%d:%d:force_original_aspect_ratio=increase,crop=%d:%d,format=rgba',
    'elseif kind=="art" then mark_artwork_profile(i) end',
    'elseif kind=="art" then clear_artwork_profile(i) end',
]:
    if required not in music:
        raise SystemExit("Smart Crop pipeline gate missing: "+required)

# Ensure the consolidation regression itself is gone.
regressed='run_ffmpeg_media({"-y","-hide_banner","-loglevel","error","-i",found,"-frames:v","1","-vf","format=rgba",normalized},25,normalized,function(ok,probe,probe_error,probe_timeout,direct_fallback)\\n            local final=artwork_dir.."\\\\track-"..i..".png"'
if regressed in music:
    raise SystemExit("normalize-only artwork pipeline is still present")

detector=read("payload/app/ArtworkEdgeDetector.cs")
for required in [
    "const double ColorTolerance = 38.0;",
    "const double RequiredMatch = 0.90;",
    "const int MinimumCrop = 8;",
    "TryDeepBlackCrop",
    "CROP {0}:{1}:{2}:{3}",
]:
    if required not in detector:
        raise SystemExit("artwork detector gate missing: "+required)

# Recovery release intentionally returns the installer/updater mechanics to the
# last field-proven 9003 path. Do not carry the failed compiled-relay experiment.
installer=read("installer/install.ps1")
host=read("payload/app/YomiPublicUpdateHost.ps1")
if "Compiling restart relay..." in installer or "YomiRestartRelay.exe" in installer:
    raise SystemExit("failed 9004 restart-relay installer experiment is still present")
if "compiled restart relay queued pid" in host:
    raise SystemExit("failed 9004 compiled relay host path is still present")
for required in [
    "Compiling console-free YOMI GUI launcher...",
    "Add-Type",
    "Creating Start Menu shortcuts...",
    "Update install stage complete; the YOMI updater will verify and restart the app.",
]:
    if required not in installer:
        raise SystemExit("known-good installer gate missing: "+required)

# Preserve the already-field-confirmed runtime repairs.
controller=read("payload/app/YomiControllerWpf.cs")
obs=read("payload/app/YomiObsServerHost.cs")
for required in [
    "Listen is explicit playback intent",
    "Any confirmed unpaused playback consumes the silent-prewarm Ready latch",
]:
    if required not in controller:
        raise SystemExit("transport fix missing: "+required)
for required in [
    "c.media_aspect_layout||'Fixed'",
    "fixedAspect=aspectLayout!=='reflow'",
]:
    if required not in obs:
        raise SystemExit("Reflow fix missing: "+required)

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
