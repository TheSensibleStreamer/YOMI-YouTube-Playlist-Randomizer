from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.9.4"
PREVIOUS="4.2.0.9.9.3"
REVISION="R61.106.52.13.19"
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

# Version bump only. This release is intentionally a tiny updater canary.
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

# One harmless, unmistakable UI marker for updater verification.
controller_path="payload/app/YomiControllerWpf.cs"
controller=read(controller_path)
old='''                UpdateLaneSummary() + "\\r\\n\\r\\n" +
                "A music player built for lightweight YouTube playback and OBS presentation.";
'''
new='''                UpdateLaneSummary() + "\\r\\n" +
                "Updater canary: integrated updater\\r\\n\\r\\n" +
                "A music player built for lightweight YouTube playback and OBS presentation.";
'''
controller=replace_once(controller,old,new,"about updater canary")
write(controller_path,controller)

# Release gates: nothing about the new updater may regress in this canary.
controller=read(controller_path)
updater=read("payload/app/update.ps1")
installer=read("installer/install.ps1")
host=read("payload/app/YomiPublicUpdateHost.ps1")
deployment=read("payload/app/update-deployment.ps1")

for required in [
    "Updater canary: integrated updater",
    "ShowPublicUpdateHost(string latestVersion, string currentVersion)",
    'Path.Combine(_appDir, "YomiPublicUpdateHost.ps1")',
    '"public-runner"',
]:
    if required not in controller:
        raise SystemExit("controller canary/updater gate missing: "+required)

for required in [
    "Download-PackageWithStatus",
    "Write-UpdateStatus 60 'installing'",
    "CreateNoWindow=$true",
    "Write-UpdateStatus 100 'complete'",
]:
    if required not in updater:
        raise SystemExit("updater gate missing: "+required)

for forbidden in [
    "Choose what you want YOMI to install:",
    "Full YOMI (recommended)",
    "Minimal Player - mpv + yt-dlp only",
    "OPT IN: Reduce Windows Defender CPU spikes",
]:
    if forbidden in installer:
        raise SystemExit("legacy installer UI returned: "+forbidden)

for required in [
    "$defenderPaths = @($installRoot,$dataRoot)",
    "defender-yomi-exclusions.json",
    "Preserve desktop-shortcut state",
    "YomiPublicUpdateHost.ps1",
]:
    if required not in installer:
        raise SystemExit("installer updater gate missing: "+required)

if "payload/app/YomiPublicUpdateHost.ps1" not in deployment:
    raise SystemExit("deployment verifier no longer requires public update host")

for required in [
    "Ready to update. Download, verification, installation and restart stay in this window.",
    "function Start-YomiPublicUpdate",
    "Update complete. Restarting YOMI...",
]:
    if required not in host:
        raise SystemExit("public update host gate missing: "+required)

# Build complete content manifest and public archive.
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
