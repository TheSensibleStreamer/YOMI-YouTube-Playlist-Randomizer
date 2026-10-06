from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.9.6"
PREVIOUS="4.2.0.9.9.5"
REVISION="R61.106.52.13.21"
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
deployment=read("payload/app/update-deployment.ps1")
installer=read("installer/install.ps1")

for required in [
    "$statusText.Text='A new version of YOMI is ready to install.'",
    "$progressTrack.Visibility='Visible'",
    'Data="M 1,1 L 8,8 M 8,1 L 1,8"',
]:
    if required not in host:
        raise SystemExit("updater prompt polish missing: "+required)

for forbidden in ["stay in this window",'Content="×"']:
    if forbidden in host:
        raise SystemExit("old updater prompt returned: "+forbidden)

for required in [
    "Downloaded bytes did not match the manifest. Retrying from a fresh cache path...",
    "yomi_version=",
    "Update integrity check failed after a fresh retry.",
]:
    if required not in updater:
        raise SystemExit("cache-busted retry missing: "+required)

for required in [
    "health=$null;rollback_health=$null",
    "Add-Member -NotePropertyName health -NotePropertyValue $health -Force",
    "Add-Member -NotePropertyName rollback_health -NotePropertyValue $health -Force",
]:
    if required not in deployment:
        raise SystemExit("health transaction fix missing: "+required)

for required in [
    "Compatibility with 9.9.3/9.9.4 deployment scripts running under StrictMode",
    "Add-Member -NotePropertyName health -NotePropertyValue $null -Force",
    "Add-Member -NotePropertyName rollback_health -NotePropertyValue $null -Force",
]:
    if required not in installer:
        raise SystemExit("old-runner health compatibility shim missing: "+required)

for required in [
    "ShowPublicUpdateHost(string latestVersion, string currentVersion)",
    '"public-runner"',
]:
    if required not in controller:
        raise SystemExit("integrated updater controller gate missing: "+required)

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
