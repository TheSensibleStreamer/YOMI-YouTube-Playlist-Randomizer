from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.9.9"
PREVIOUS="4.2.0.9.9.8"
REVISION="R61.106.52.13.24"
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
updater=read("payload/app/update.ps1")
deployment=read("payload/app/update-deployment.ps1")
controller=read("payload/app/YomiControllerWpf.cs")
diagnostics=read("payload/app/YomiDiagnosticBundle.ps1")

# This is intentionally a tiny updater canary. The release must prove restart,
# not introduce unrelated playback/cache/OBS behavior.
for required in [
    "function Test-YomiControllerRunning",
    "direct controller launch verified",
    "launcher fallback verified",
    "Update installed, but YOMI did not reopen automatically.",
    "$doneButton.Content='Open YOMI'",
    "update-restart.log",
]:
    if required not in host:
        raise SystemExit("verified restart gate missing: "+required)

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

for required in [
    "if (installProbe || selfTest)",
    "--semantic-self-test",
    "InstalledVersionText()",
]:
    if required not in controller:
        raise SystemExit("one-hop old-updater compatibility gate missing: "+required)

if "update-restart.log" not in diagnostics:
    raise SystemExit("restart diagnostic gate missing")

for forbidden in [
    "Ready to update. Download, verification, installation and restart stay in this window.",
    'Content="×"',
]:
    if forbidden in host:
        raise SystemExit("old updater UI artifact returned: "+forbidden)

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
