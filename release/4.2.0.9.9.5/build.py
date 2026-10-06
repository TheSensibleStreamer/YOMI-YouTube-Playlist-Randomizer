from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.9.5"
PREVIOUS="4.2.0.9.9.4"
REVISION="R61.106.52.13.20"
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

host=read("payload/app/YomiPublicUpdateHost.ps1")
controller=read("payload/app/YomiControllerWpf.cs")
updater=read("payload/app/update.ps1")
installer=read("installer/install.ps1")

required_host=[
    'Data="M 1,1 L 8,8 M 8,1 L 1,8"',
    'x:Name="ProgressTrack"',
    'Visibility="Collapsed"',
    "$statusText.Text='A new version of YOMI is ready to install.'",
    "$progressTrack.Visibility='Visible'",
    "function Start-YomiPublicUpdate",
]
for x in required_host:
    if x not in host:
        raise SystemExit("updater prompt polish gate missing: "+x)

for forbidden in [
    "stay in this window",
    'Content="×"',
    "Ready to update. Download, verification, installation and restart",
]:
    if forbidden in host:
        raise SystemExit("old updater prompt returned: "+forbidden)

for x in [
    "ShowPublicUpdateHost(string latestVersion, string currentVersion)",
    '"public-runner"',
]:
    if x not in controller:
        raise SystemExit("controller updater gate missing: "+x)

for x in [
    "Download-PackageWithStatus",
    "Write-UpdateStatus 60 'installing'",
    "Write-UpdateStatus 100 'complete'",
]:
    if x not in updater:
        raise SystemExit("update engine gate missing: "+x)

for forbidden in [
    "Choose what you want YOMI to install:",
    "OPT IN: Reduce Windows Defender CPU spikes",
]:
    if forbidden in installer:
        raise SystemExit("legacy installer UI returned: "+forbidden)

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
    "files":len(files)+1
},indent=2))
