from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.9.8"
PREVIOUS="4.2.0.9.9.7"
REVISION="R61.106.52.13.23"
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

controller=read("payload/app/YomiControllerWpf.cs")
xaml=read("payload/app/YomiControllerWpf.xaml")
defaults=read("payload/app/default-config.json")
server=read("payload/app/server.ps1")
installer=read("installer/install.ps1")
updater=read("payload/app/update.ps1")
deployment=read("payload/app/update-deployment.ps1")
diagnostics=read("payload/app/YomiDiagnosticBundle.ps1")
host=read("payload/app/YomiPublicUpdateHost.ps1")

# Production update health must stay stable across intentional UI redesigns.
for required in [
    "if (installProbe || selfTest)",
    "--semantic-self-test",
    "Deep semantic/fidelity assertions remain available as --semantic-self-test",
    "InstalledVersionText()",
]:
    if required not in controller:
        raise SystemExit("stable update-health/About gate missing: "+required)
if "Updater canary: integrated updater" in controller:
    raise SystemExit("temporary updater canary text returned")

# Fixed/Reflow is a durable preference in controller + OBS.
for required in [
    "_settingsMediaAspectLayout",
    '"media_aspect_layout"',
    "MediaAspectReflowEnabled()",
    "Reflow collapses unused horizontal reservation",
]:
    if required not in controller:
        raise SystemExit("controller Fixed/Reflow gate missing: "+required)
for required in [
    'x:Name="SettingsMediaAspectLayout"',
    "Fixed keeps the configured video slot in place",
    "Reflow collapses unused width",
]:
    if required not in xaml:
        raise SystemExit("settings Fixed/Reflow gate missing: "+required)
if '"media_aspect_layout": "Fixed"' not in defaults:
    raise SystemExit("Fixed is no longer the default media aspect layout")
for required in ["c.media_aspect_layout||'Fixed'","fixedAspect=aspectLayout!=='reflow'"]:
    if required not in server:
        raise SystemExit("OBS Fixed/Reflow gate missing: "+required)

# Start Menu icons must not depend on an icon path that disappears during Program Files swap.
for required in [
    "Publish-YomiShellIcon",
    "$mainIconLocation",
    "$settingsIconLocation",
    "Recreate Start Menu links rather than editing a stale .lnk in place",
]:
    if required not in installer:
        raise SystemExit("shell icon stability gate missing: "+required)

# Old updater compatibility / one-hop latest invariants.
for required in [
    "$manifestFreshUri=$manifestUri+'?yomi_manifest='",
    "$manifestHeaders['Cache-Control']='no-cache, no-store, max-age=0'",
    "Downloaded bytes did not match the manifest. Retrying from a fresh cache path...",
    "yomi_version=",
]:
    if required not in updater:
        raise SystemExit("fresh update delivery gate missing: "+required)
for required in [
    "health=$null;rollback_health=$null",
    "Add-Member -NotePropertyName health -NotePropertyValue $health -Force",
    "Add-Member -NotePropertyName rollback_health -NotePropertyValue $health -Force",
]:
    if required not in deployment:
        raise SystemExit("transaction health gate missing: "+required)
for required in [
    "Compatibility with 9.9.3/9.9.4 deployment scripts running under StrictMode",
    "Add-Member -NotePropertyName health -NotePropertyValue $null -Force",
    "Add-Member -NotePropertyName rollback_health -NotePropertyValue $null -Force",
]:
    if required not in installer:
        raise SystemExit("old updater transaction compatibility missing: "+required)

# Diagnostic truth must distinguish inner installer PASS from outer updater result.
for required in ["update-transaction.json","updates\\public-runner\\update-status.json"]:
    if required not in diagnostics:
        raise SystemExit("updater diagnostic truth gate missing: "+required)

# Polished integrated updater prompt remains intact.
for required in [
    "$statusText.Text='A new version of YOMI is ready to install.'",
    "$progressTrack.Visibility='Visible'",
    'Data="M 1,1 L 8,8 M 8,1 L 1,8"',
]:
    if required not in host:
        raise SystemExit("integrated updater UI gate missing: "+required)
for forbidden in ["stay in this window",'Content="×"']:
    if forbidden in host:
        raise SystemExit("old updater prompt artifact returned: "+forbidden)

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
