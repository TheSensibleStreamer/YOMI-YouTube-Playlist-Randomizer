from pathlib import Path
import hashlib, json, sys, zipfile

VERSION = "4.2.0.9.1"
PREVIOUS = "4.2.0.9"
REVISION = "R61.106.52.13.7"
ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".release-work").resolve()
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else f"YOMI-v{VERSION}.zip").resolve()

def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

def write(path, text):
    (ROOT / path).write_text(text, encoding="utf-8", newline="")

# Canonical launcher: Settings belongs to the WPF shell just like Controller.
path = "payload/app/YomiLauncher.cs"
text = read(path)
replacements = [
    (
        'if (mode == "controller" || mode == "player" || mode == "expedition")',
        'if (mode == "controller" || mode == "player" || mode == "expedition" || mode == "settings")'
    ),
    (
        'shellPsi.Arguments = mode == "expedition" ? "--expedition" : "";',
        'shellPsi.Arguments = mode == "settings" ? "--settings" : (mode == "expedition" ? "--expedition" : "");'
    ),
    (
        'mode = "legacy-controller";',
        'mode = mode == "settings" ? "legacy-settings" : "legacy-controller";'
    ),
    (
        '(mode == "settings" ? "settings.ps1" : "controller.ps1");',
        '(mode == "legacy-settings" ? "settings.ps1" : "controller.ps1");'
    ),
]
for old, new in replacements:
    if text.count(old) != 1:
        raise SystemExit(f"launcher anchor count for {old!r}: {text.count(old)}")
    text = text.replace(old, new, 1)
write(path, text)

# Bump current package identity everywhere in the package payload.
text_exts = {".ps1", ".cs", ".json", ".txt", ".xaml", ".cmd", ".config", ".manifest", ".md"}
changed = 0
for p in ROOT.rglob("*"):
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

# Assertions before packaging.
launcher = read("payload/app/YomiLauncher.cs")
if 'mode == "settings" ? "--settings"' not in launcher:
    raise SystemExit("WPF Settings routing missing")
if 'mode == "legacy-settings" ? "settings.ps1"' not in launcher:
    raise SystemExit("legacy Settings fallback missing")
if read("payload/VERSION.txt").strip() != VERSION:
    raise SystemExit("VERSION.txt mismatch")

def sha256(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

# Exact inner manifest.
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
(ROOT / "installer/build-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="")

# Verify exact files and hashes.
actual = {p.relative_to(ROOT).as_posix() for p in ROOT.rglob("*")
          if p.is_file() and p.relative_to(ROOT).as_posix() != "installer/build-manifest.json"}
if actual != set(files):
    raise SystemExit("manifest file set mismatch")
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
