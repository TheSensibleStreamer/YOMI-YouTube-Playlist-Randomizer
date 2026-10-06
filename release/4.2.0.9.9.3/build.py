from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.9.3"
PREVIOUS="4.2.0.9.9.2"
REVISION="R61.106.52.13.18"
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

# Version-bump every shipped text source while leaving binary assets untouched.
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

# Release gates for the integrated updater overhaul.
controller=read("payload/app/YomiControllerWpf.cs")
updater=read("payload/app/update.ps1")
deployment=read("payload/app/update-deployment.ps1")
installer=read("installer/install.ps1")
uninstaller=read("payload/app/uninstall.ps1")
cmd=read("INSTALL YOMI.cmd")
host=read("payload/app/YomiPublicUpdateHost.ps1")

required_controller=[
    "ShowPublicUpdateHost(string latestVersion, string currentVersion)",
    'Path.Combine(_appDir, "YomiPublicUpdateHost.ps1")',
    '"public-runner"',
    "ShowPublicUpdateHost(latestText, currentText);",
]
for x in required_controller:
    if x not in controller:
        raise SystemExit("integrated updater controller gate missing: "+x)
if "StartPublicUpdaterHidden" in controller or '"Run updater"' in controller:
    raise SystemExit("legacy updater handoff returned")

required_updater=[
    "Download-PackageWithStatus",
    "Write-UpdateStatus 60 'installing'",
    "-UpdateMode",
    "CreateNoWindow=$true",
    "Write-UpdateStatus 96 'rolling-back'",
    "Write-UpdateStatus 100 'complete'",
]
for x in required_updater:
    if x not in updater:
        raise SystemExit("updater gate missing: "+x)
if "$env:ComSpec" in updater:
    raise SystemExit("visible cmd.exe update activation returned")

required_installer=[
    "param([switch]$UpdateMode,[string]$UpdateStatusFile)",
    "$installFfmpeg = $true",
    "$installDeno = $true",
    "$defenderPaths = @($installRoot,$dataRoot)",
    "runtime\\yt-dlp\\yt-dlp.exe",
    "runtime\\mpv\\mpv.exe",
    "runtime\\ffmpeg\\ffmpeg.exe",
    "runtime\\deno\\deno.exe",
    "defender-yomi-exclusions.json",
    "Preserve desktop-shortcut state",
    "YomiPublicUpdateHost.ps1",
    "Waiting for Windows administrator approval...",
]
for x in required_installer:
    if x not in installer:
        raise SystemExit("installer gate missing: "+x)
for forbidden in [
    "Choose what you want YOMI to install:",
    "Full YOMI (recommended)",
    "Minimal Player - mpv + yt-dlp only",
    "OPT IN: Reduce Windows Defender CPU spikes",
]:
    if forbidden in installer:
        raise SystemExit("legacy installer choice returned: "+forbidden)

if "payload/app/YomiPublicUpdateHost.ps1" not in deployment or "app\\YomiPublicUpdateHost.ps1" not in deployment:
    raise SystemExit("deployment verifier does not require the new update host")
if "defender-yomi-exclusions.json" not in uninstaller or "added_paths" not in uninstaller or "added_processes" not in uninstaller:
    raise SystemExit("uninstaller does not own broad Defender cleanup")
if "-UpdateMode" not in cmd or 'if exist "%ProgramFiles%\\YOMI\\VERSION.txt"' not in cmd:
    raise SystemExit("bootstrap setup is not update-aware")
for x in [
    "Ready to update. Download, verification, installation and restart stay in this window.",
    "function Start-YomiPublicUpdate",
    "Update complete. Restarting YOMI...",
]:
    if x not in host:
        raise SystemExit("public update host gate missing: "+x)

# Build a complete content manifest after all source/version changes.
files={}
for p in sorted(ROOT.rglob("*")):
    if not p.is_file():
        continue
    rel=p.relative_to(ROOT).as_posix()
    if rel=="installer/build-manifest.json":
        continue
    files[rel]={"bytes":p.stat().st_size,"sha256":sha256(p)}

manifest={
    "version":"v"+VERSION,
    "product":"YOMI - YouTube OBS Music Interface",
    "release":REVISION,
    "files":files,
}
write("installer/build-manifest.json",json.dumps(manifest,indent=2)+"\n")

# Verify the just-written manifest before creating the public archive.
for rel,meta in files.items():
    p=ROOT/rel
    if p.stat().st_size!=meta["bytes"] or sha256(p)!=meta["sha256"]:
        raise SystemExit("manifest verification failed: "+rel)

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
