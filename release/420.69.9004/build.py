from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="420.69.9004"
PREVIOUS="420.69.9003"
REVISION="R61.106.52.13.28"
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

# Version every shipped text surface.
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

# Restart: compiled helper must exist in source, be compiled/verified by the installer,
# be queued by the installer itself on UpdateMode, and be preferred by future updater hosts.
relay=read("payload/app/YomiRestartRelay.cs")
for required in [
    "public static class YomiRestartRelay",
    "Explorer-shell launch failed",
    "launching controller through Explorer shell",
    "controller did not stay up; launcher fallback",
    "restart verified",
]:
    if required not in relay:
        raise SystemExit("compiled restart relay gate missing: "+required)

installer=read("installer/install.ps1")
for required in [
    "Compiling restart relay...",
    "YomiRestartRelay.exe failed to compile.",
    "installer queued compiled restart relay pid",
    "YomiRestartRelay.exe",
    "YomiRestartRelay.cs",
    "CompilerOptions ('/win32icon:",
    "$mainIconLocation = $guiLauncher + ',0'",
    "ie4uinit.exe",
]:
    if required not in installer:
        raise SystemExit("installer restart/icon gate missing: "+required)

host=read("payload/app/YomiPublicUpdateHost.ps1")
for required in [
    "compiled restart relay queued pid",
    "YomiRestartRelay.exe",
    "Compatibility fallback for pre-relay installations",
    "update-restart.log",
]:
    if required not in host:
        raise SystemExit("updater compiled relay gate missing: "+required)

# Keep the previous playback/Reflow corrections in the canary too.
music=read("payload/app/music.lua")
controller=read("payload/app/YomiControllerWpf.cs")
obs=read("payload/app/YomiObsServerHost.cs")
for required in [
    "PLAYBACK SUBSET STOP outgoing audio; uncached target ",
]:
    if required not in music:
        raise SystemExit("filtered Listen fix missing: "+required)
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
