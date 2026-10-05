from pathlib import Path
import hashlib, json, sys, zipfile

VERSION = "4.2.0.9.5"
PREVIOUS = "4.2.0.9.4"
REVISION = "R61.106.52.13.11"
ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".release-work").resolve()
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else "YOMI-Windows.zip").resolve()

def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

def write(path, text):
    (ROOT / path).write_text(text, encoding="utf-8", newline="")

def replace_count(path, old, new, expected):
    text = read(path)
    count = text.count(old)
    if count != expected:
        raise SystemExit(f"{path}: expected {expected} anchors, got {count}: {old!r}")
    write(path, text.replace(old, new))

# Main YOMI shortcuts now use the canonical app icon path. Settings keeps the
# distinct Settings icon. This also changes IconLocation so Windows is less
# likely to reuse the stale cached Settings icon for main YOMI shortcuts.
replace_count(
    "installer/install.ps1",
    "(Join-Path $installRoot 'assets\\yomi-v408.ico')",
    "(Join-Path $installRoot 'app\\yomi.ico')",
    5
)

# Final verification must prove the canonical main icon exists.
anchor = """        (Join-Path $installRoot 'app\\YomiLauncher.exe'),
        (Join-Path $installRoot 'assets\\yomi-v408.ico'),
        (Join-Path $installRoot 'assets\\yomi-settings-v408.ico'),"""
replacement = """        (Join-Path $installRoot 'app\\YomiLauncher.exe'),
        (Join-Path $installRoot 'app\\yomi.ico'),
        (Join-Path $installRoot 'assets\\yomi-v408.ico'),
        (Join-Path $installRoot 'assets\\yomi-settings-v408.ico'),"""
text = read("installer/install.ps1")
if text.count(anchor) != 1:
    raise SystemExit("installer icon verification anchor missing")
write("installer/install.ps1", text.replace(anchor, replacement, 1))

# Identity bump.
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

# Targeted assertions.
installer = read("installer/install.ps1")
controller = read("payload/app/YomiControllerWpf.cs")
if not (ROOT / "payload/app/yomi.ico").exists():
    raise SystemExit("canonical payload/app/yomi.ico missing")
if not (ROOT / "payload/assets/yomi-v408.ico").exists():
    raise SystemExit("restored legacy main icon missing")
if installer.count("(Join-Path $installRoot 'app\\yomi.ico')") < 6:
    raise SystemExit("main shortcuts/final verification are not using canonical icon")
if "string canonical = Path.Combine(_appDir, \"yomi.ico\");" not in controller:
    raise SystemExit("controller canonical icon lookup missing")
if "string legacy = Path.Combine(_installRoot, \"assets\", \"yomi-v408.ico\");" not in controller:
    raise SystemExit("controller legacy icon fallback missing")

def sha256(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

# Prove main and settings icons are now distinct again.
main_sha = sha256(ROOT / "payload/assets/yomi-v408.ico")
settings_sha = sha256(ROOT / "payload/assets/yomi-settings-v408.ico")
canonical_sha = sha256(ROOT / "payload/app/yomi.ico")
if main_sha == settings_sha:
    raise SystemExit("main YOMI icon still equals Settings icon")
if canonical_sha != main_sha:
    raise SystemExit("canonical yomi.ico does not match restored main icon")

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
    "main_icon_sha256": main_sha,
    "settings_icon_sha256": settings_sha,
    "canonical_icon_sha256": canonical_sha,
    "files": len(files) + 1
}, indent=2))
