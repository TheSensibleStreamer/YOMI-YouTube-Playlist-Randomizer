from pathlib import Path
import hashlib, json, sys, zipfile

VERSION = "4.2.0.9.6"
PREVIOUS = "4.2.0.9.5"
REVISION = "R61.106.52.13.12"
ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".release-work").resolve()
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else "YOMI-Windows.zip").resolve()

def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

def write(path, text):
    (ROOT / path).write_text(text, encoding="utf-8", newline="")

# Bump public identity while preserving the already-restored icon and 9.4 UI fixes.
text_exts = {".ps1", ".cs", ".json", ".txt", ".xaml", ".cmd", ".config", ".manifest", ".md"}
changed = 0
for p in list(ROOT.rglob("*")):
    if not p.is_file() or p.name == "build-manifest.json" or p.suffix.lower() not in text_exts:
        continue
    try:
        t = p.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        continue
    if PREVIOUS in t:
        p.write_text(t.replace(PREVIOUS, VERSION), encoding="utf-8", newline="")
        changed += 1

write("payload/VERSION.txt", VERSION + "\n")
write("payload/app/FOCUSED-BUILD.txt", f"YOMI {VERSION} Focused Media Player\n{REVISION}\n")

# Targeted assertions for this bootstrap/update revision.
updater = read("payload/app/update.ps1")
controller = read("payload/app/YomiControllerWpf.cs")
xaml = read("payload/app/YomiControllerWpf.xaml")
design = read("payload/app/YomiDesign.xaml")
installer = read("installer/install.ps1")

if "$packageName -ne 'YOMI-Windows.zip'" not in updater:
    raise SystemExit("stable package-name compatibility missing")
if "Update now?" not in updater or "$manifest.summary" in updater:
    raise SystemExit("updater prompt is not simplified")
if 'BuildYomiStatusDialog("Update available", "YOMI " + latestText + " is available. You have " + currentText + "."' not in controller:
    raise SystemExit("controller update dialog is not simplified")
if 'string summary = GetString(manifest, "summary"' in controller:
    raise SystemExit("controller still consumes update summary")
if '<ComboBox.ItemTemplate>' in xaml[xaml.find('x:Name="SettingsTextFont"')-250:xaml.find('x:Name="SettingsTextFont"')+500]:
    raise SystemExit("font picker regression: inline ItemTemplate returned")
if '<Setter Property="OverridesDefaultStyle" Value="True"/>' not in design:
    raise SystemExit("settings ComboBox native chrome suppression missing")
if not (ROOT / "payload/app/yomi.ico").exists():
    raise SystemExit("canonical main icon missing")
if not (ROOT / "payload/assets/yomi-v408.ico").exists():
    raise SystemExit("restored main icon missing")
if not (ROOT / "payload/assets/yomi-settings-v408.ico").exists():
    raise SystemExit("settings icon missing")

def sha256(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

if sha256(ROOT / "payload/app/yomi.ico") != sha256(ROOT / "payload/assets/yomi-v408.ico"):
    raise SystemExit("canonical main icon does not match restored main icon")
if sha256(ROOT / "payload/app/yomi.ico") == sha256(ROOT / "payload/assets/yomi-settings-v408.ico"):
    raise SystemExit("main icon still equals Settings icon")
if "(Join-Path $installRoot 'app\\yomi.ico')" not in installer:
    raise SystemExit("installer shortcuts are not using canonical icon")

files = {}
for p in sorted(ROOT.rglob("*")):
    if not p.is_file():
        continue
    rel = p.relative_to(ROOT).as_posix()
    if rel == "installer/build-manifest.json":
        continue
    files[rel] = {"bytes": p.stat().st_size, "sha256": sha256(p)}

manifest = {
    "version": "v" + VERSION,
    "product": "YOMI - YouTube OBS Music Interface",
    "release": REVISION,
    "files": files,
}
(ROOT / "installer/build-manifest.json").write_text(
    json.dumps(manifest, indent=2) + "\n",
    encoding="utf-8",
    newline=""
)

for rel, meta in files.items():
    p = ROOT / rel
    if p.stat().st_size != meta["bytes"] or sha256(p) != meta["sha256"]:
        raise SystemExit("manifest verification failed: " + rel)

if OUT.exists():
    OUT.unlink()
with zipfile.ZipFile(OUT, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for p in sorted(ROOT.rglob("*")):
        if p.is_file():
            z.write(p, p.relative_to(ROOT).as_posix())

print(json.dumps({
    "version": VERSION,
    "revision": REVISION,
    "changed_identity_files": changed,
    "bytes": OUT.stat().st_size,
    "sha256": sha256(OUT),
    "files": len(files) + 1
}, indent=2))
