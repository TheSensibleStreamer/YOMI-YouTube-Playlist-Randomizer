from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.8"
PREVIOUS="4.2.0.9.7"
REVISION="R61.106.52.13.14"
ROOT=Path(sys.argv[1] if len(sys.argv)>1 else ".release-work").resolve()
OUT=Path(sys.argv[2] if len(sys.argv)>2 else "YOMI-Windows.zip").resolve()

def read(p): return (ROOT/p).read_text(encoding="utf-8-sig")
def write(p,t): (ROOT/p).write_text(t,encoding="utf-8",newline="")

# Version bump across text payloads.
text_exts={".ps1",".cs",".json",".txt",".xaml",".cmd",".config",".manifest",".md"}
for p in list(ROOT.rglob("*")):
    if not p.is_file() or p.name=="build-manifest.json" or p.suffix.lower() not in text_exts: continue
    try: t=p.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError: continue
    if PREVIOUS in t:
        p.write_text(t.replace(PREVIOUS,VERSION),encoding="utf-8",newline="")

write("payload/VERSION.txt",VERSION+"\n")
write("payload/app/FOCUSED-BUILD.txt",f"YOMI {VERSION} Focused Media Player\n{REVISION}\n")

installer=read("installer/install.ps1")
anchor="$installRoot = Join-Path $env:ProgramFiles 'YOMI'\n$dataRoot = Join-Path $env:LOCALAPPDATA 'YOMI'"
replacement="$installRoot = Join-Path $env:ProgramFiles 'YOMI'\n$existingInstallAtStart = Test-Path -LiteralPath (Join-Path $installRoot 'VERSION.txt') -PathType Leaf\n$dataRoot = Join-Path $env:LOCALAPPDATA 'YOMI'"
if installer.count(anchor)!=1:
    raise SystemExit("installer existing-install anchor missing")
installer=installer.replace(anchor,replacement,1)

old="""    Write-Host 'Opening Settings now...' -ForegroundColor Yellow

    Start-Process (Join-Path $installRoot 'app\\YomiLauncher.exe') -ArgumentList 'settings'
"""
new="""    if ($existingInstallAtStart) {
        Write-Host 'Opening YOMI now...' -ForegroundColor Yellow
        Start-Process (Join-Path $installRoot 'app\\YomiLauncher.exe') -ArgumentList 'controller'
    }
    else {
        Write-Host 'Opening Settings for first-time setup...' -ForegroundColor Yellow
        Start-Process (Join-Path $installRoot 'app\\YomiLauncher.exe') -ArgumentList 'settings'
    }
"""
if installer.count(old)!=1:
    raise SystemExit("installer post-install launch anchor missing")
installer=installer.replace(old,new,1)
write("installer/install.ps1",installer)

# Guard the exact regression.
installer=read("installer/install.ps1")
if "$existingInstallAtStart = Test-Path" not in installer:
    raise SystemExit("upgrade detection missing")
if "Start-Process (Join-Path $installRoot 'app\\YomiLauncher.exe') -ArgumentList 'controller'" not in installer:
    raise SystemExit("upgrade no longer launches normal controller")
if "Opening Settings for first-time setup" not in installer:
    raise SystemExit("fresh-install settings flow missing")

def sha256(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""): h.update(chunk)
    return h.hexdigest()

files={}
for p in sorted(ROOT.rglob("*")):
    if not p.is_file(): continue
    rel=p.relative_to(ROOT).as_posix()
    if rel=="installer/build-manifest.json": continue
    files[rel]={"bytes":p.stat().st_size,"sha256":sha256(p)}
manifest={"version":"v"+VERSION,"product":"YOMI - YouTube OBS Music Interface","release":REVISION,"files":files}
(ROOT/"installer/build-manifest.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8",newline="")

if OUT.exists(): OUT.unlink()
with zipfile.ZipFile(OUT,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for p in sorted(ROOT.rglob("*")):
        if p.is_file(): z.write(p,p.relative_to(ROOT).as_posix())

print(json.dumps({"version":VERSION,"revision":REVISION,"bytes":OUT.stat().st_size,"sha256":sha256(OUT),"files":len(files)+1},indent=2))
