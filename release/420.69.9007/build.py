from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="420.69.9007"
PREVIOUS="420.69.9006"
REVISION="R61.106.52.13.31"
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
for required in [
    '"schema", 50',
    '"queue_listening_active"',
    '"queue_listening_query"',
    '"queue_listening_occurrences"',
    'RestoreQueuePlaybackSubsetIfNeeded',
    '"yomi-playback-subset-restore"',
    'resumed filtered listening',
]:
    if required not in controller:
        raise SystemExit("filtered-session persistence gate missing: "+required)

music=read("payload/app/music.lua")
for required in [
    'safe_register_script_message("yomi-playback-subset-restore"',
    'PLAYBACK SUBSET RESTORE count=',
    'playback_subset_position[active_occ]',
    'PLAYBACK SUBSET STOP outgoing audio; uncached target ',
    'r6110615-crop-restored1',
    'ArtworkEdgeDetector.exe',
    'ART READY SMART-CROP track ',
]:
    if required not in music:
        raise SystemExit("engine/crop gate missing: "+required)

update=read("payload/app/update.ps1")
for required in [
    "Write-UpdateStatus 98 'restarting'",
    "Start-Process -FilePath $launcher -ArgumentList 'controller'",
    "Automatic reopen was not confirmed.",
    "installed, verified, and reopened.",
]:
    if required not in update:
        raise SystemExit("future updater direct-restart gate missing: "+required)

host=read("payload/app/YomiPublicUpdateHost.ps1")
for required in [
    "$versionText.Text='YOMI '+$installed+' is installed.'",
    "verified updater process reopened YOMI; closing updater host",
    "YOMI '+$installed+' is installed, but it did not reopen automatically.",
]:
    if required not in host:
        raise SystemExit("truthful updater-host gate missing: "+required)

installer=read("installer/install.ps1")
for required in [
    "$elevatedExitCode = [int]$elevated.ExitCode",
    "installer-wrapper | elevated update succeeded; opening installed YOMI",
    "Start-Process -FilePath $installedLauncher -ArgumentList 'controller'",
    "installer-wrapper | launcher pid ",
    "installer-wrapper | reopen failed: ",
]:
    if required not in installer:
        raise SystemExit("same-update restart bootstrap gate missing: "+required)
if "YomiRestartRelay.exe" in installer or "Compiling restart relay..." in installer:
    raise SystemExit("failed compiled relay experiment returned")

obs=read("payload/app/YomiObsServerHost.cs")
for required in ["c.media_aspect_layout||'Fixed'","fixedAspect=aspectLayout!=='reflow'"]:
    if required not in obs:
        raise SystemExit("Reflow gate missing: "+required)

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
