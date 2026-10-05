from pathlib import Path
import hashlib, json, sys, zipfile

VERSION = "4.2.0.9.3"
PREVIOUS = "4.2.0.9.2"
REVISION = "R61.106.52.13.9"
ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".release-work").resolve()
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else f"YOMI-v{VERSION}.zip").resolve()

def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

def write(path, text):
    (ROOT / path).write_text(text, encoding="utf-8", newline="")

def replace_once(path, old, new):
    t = read(path)
    if t.count(old) != 1:
        raise SystemExit(f"{path}: anchor count {t.count(old)} for {old[:100]!r}")
    write(path, t.replace(old, new, 1))

# Dedicated installer health probe: load the actual WPF resource dictionary and
# window XAML, then succeed before any strict development/fidelity assertions.
replace_once(
    "payload/app/YomiControllerWpf.cs",
    '            bool selfTest = args != null && args.Any(a => String.Equals(a, "--self-test", StringComparison.OrdinalIgnoreCase));\n            bool startupSelfTest = args != null && args.Any(a => String.Equals(a, "--startup-self-test", StringComparison.OrdinalIgnoreCase));\n            bool startupSelfTestAll = args != null && args.Any(a => String.Equals(a, "--startup-self-test-all", StringComparison.OrdinalIgnoreCase));\n            bool anySelfTest = selfTest || startupSelfTest || startupSelfTestAll;',
    '            bool installProbe = args != null && args.Any(a => String.Equals(a, "--install-probe", StringComparison.OrdinalIgnoreCase));\n            bool selfTest = args != null && args.Any(a => String.Equals(a, "--self-test", StringComparison.OrdinalIgnoreCase));\n            bool startupSelfTest = args != null && args.Any(a => String.Equals(a, "--startup-self-test", StringComparison.OrdinalIgnoreCase));\n            bool startupSelfTestAll = args != null && args.Any(a => String.Equals(a, "--startup-self-test-all", StringComparison.OrdinalIgnoreCase));\n            bool anySelfTest = installProbe || selfTest || startupSelfTest || startupSelfTestAll;'
)

replace_once(
    "payload/app/YomiControllerWpf.cs",
    '                    using (var stream = File.OpenRead(windowPath))\n                        window = (Window)XamlReader.Load(stream);\n\n                    if (selfTest)',
    '                    using (var stream = File.OpenRead(windowPath))\n                        window = (Window)XamlReader.Load(stream);\n\n                    // Install verification only proves that the shipped WPF resources and\n                    // window can load. Strict semantic/fidelity assertions remain dev tests.\n                    if (installProbe)\n                    {\n                        try { window.Close(); } catch { }\n                        try { app.Shutdown(); } catch { }\n                        return 0;\n                    }\n\n                    if (selfTest)'
)

replace_once(
    "installer/install.ps1",
    "-ArgumentList '--self-test'",
    "-ArgumentList '--install-probe'"
)

replace_once(
    "installer/install.ps1",
    "throw ('Final verification failed: YomiControllerWpf self-test exit ' + $controllerSelfTest.ExitCode)",
    "throw ('Final verification failed: YomiControllerWpf install probe exit ' + $controllerSelfTest.ExitCode)"
)

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

controller = read("payload/app/YomiControllerWpf.cs")
installer = read("installer/install.ps1")
assert '--install-probe' in controller
assert 'if (installProbe)' in controller
assert "-ArgumentList '--install-probe'" in installer
assert "install probe exit" in installer
assert read("payload/VERSION.txt").strip() == VERSION

def sha256(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

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
