from pathlib import Path
import hashlib, json, re, sys, zipfile

RELEASE_VERSION = '4.2.0.9'
RELEASE_TAG = 'R61.106.52.13.6'
ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else 'build').resolve()
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else 'YOMI-v4.2.0.9.zip').resolve()

def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')

def write(path, text):
    (ROOT / path).write_text(text, encoding='utf-8', newline='')

def replace_exact(path, old, new, expected=None):
    text = read(path)
    count = text.count(old)
    if expected is not None and count != expected:
        raise SystemExit(f'{path}: expected {expected} replacements for {old!r}, found {count}')
    if count == 0:
        raise SystemExit(f'{path}: anchor missing: {old!r}')
    write(path, text.replace(old, new))

# Patch OBS font rendering without transporting its giant generated HTML line as a diff.
obs = read('payload/app/YomiObsServer.ps1')
obs_replacements = [
    (
        "let font=String(c.text_font||'Bahnschrift Condensed'),color=",
        "let fontChoice=String(c.text_font||'Bahnschrift Condensed'),font=fontChoice,fontStretch='normal',color="
    ),
    (
        "opacity=Math.max(.05,Math.min(1,num(c.text_opacity,1)));\\n  let safeX=",
        "opacity=Math.max(.05,Math.min(1,num(c.text_opacity,1)));\\n  if(fontChoice.toLowerCase()==='bahnschrift condensed'){font='Bahnschrift';fontStretch='condensed';}\\n  else if(fontChoice.toLowerCase()==='bahnschrift semicondensed'){font='Bahnschrift';fontStretch='semi-condensed';}\\n  let safeX="
    ),
    (
        "e.style.fontFamily=font;e.style.fontWeight='400';",
        "e.style.fontFamily=font;e.style.fontStretch=fontStretch;e.style.fontWeight='400';"
    ),
]
for old, new in obs_replacements:
    if obs.count(old) != 1:
        raise SystemExit(f'YomiObsServer font anchor count was {obs.count(old)}, expected 1: {old}')
    obs = obs.replace(old, new, 1)
write('payload/app/YomiObsServer.ps1', obs)

# Keep the required Queue semantics: normal = B tracks; filtered/search = A of B tracks.
controller = read('payload/app/YomiControllerWpf.cs')
regressed = '''            _queueFilterSummaryText.Text = activeTotal.ToString(CultureInfo.InvariantCulture) + (activeTotal == 1 ? " track" : " tracks");
            _queueFilterSummaryText.ToolTip = "Playable tracks currently shown by the Queue model";'''
correct = '''            if (simpleAllView)
            {
                _queueFilterSummaryText.Text = activeTotal.ToString(CultureInfo.InvariantCulture) + (activeTotal == 1 ? " track" : " tracks");
                _queueFilterSummaryText.ToolTip = "Playable tracks in the Queue";
            }
            else
            {
                _queueFilterSummaryText.Text = visible.ToString(CultureInfo.InvariantCulture) + " of " + activeTotal.ToString(CultureInfo.InvariantCulture) + " tracks";
                _queueFilterSummaryText.ToolTip = "Tracks shown by the current Queue filter out of all playable tracks";
            }'''
if regressed not in controller:
    raise SystemExit('Queue counter post-patch anchor missing')
write('payload/app/YomiControllerWpf.cs', controller.replace(regressed, correct, 1))

# Current-version identity. Historical filenames/ledger entries remain historical 4.2.0.8 evidence.
for path, expected in [
    ('installer/install.ps1', 4),
    ('payload/README-EASY.txt', 1),
    ('payload/app/YomiControllerWpf.cs', 9),
    ('payload/app/YomiControllerWpf.xaml', 2),
    ('payload/app/YomiUpdateHost.ps1', 2),
    ('payload/app/common.ps1', 1),
]:
    replace_exact(path, '4.2.0.8', RELEASE_VERSION, expected)

write('payload/VERSION.txt', RELEASE_VERSION + '\n')
write('payload/app/FOCUSED-BUILD.txt', f'YOMI {RELEASE_VERSION} Focused Media Player\n{RELEASE_TAG}\n')

# Only the top-level current-version field changes in these JSON contracts.
for path in [
    'payload/app/INSTALLATION-RECEIPT.json',
    'payload/app/NO-GAPS-CONSOLIDATION-LEDGER.json',
    'payload/app/RUNTIME-INTEGRITY-MANIFEST.json',
    'payload/app/YOMI-CAPABILITY-CONTRACT.json',
    'payload/app/default-config.json',
    'payload/app/lineage-registry.json',
]:
    text = read(path)
    new, n = re.subn(r'("version"\s*:\s*")4\.2\.0\.8("\s*,?)', rf'\g<1>{RELEASE_VERSION}\2', text, count=1)
    if n != 1:
        raise SystemExit(f'{path}: current version field not found exactly once')
    write(path, new)

def sha256(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()

# Rebuild and then verify the exact inner package manifest.
files = {}
for p in sorted(ROOT.rglob('*')):
    if not p.is_file():
        continue
    rel = p.relative_to(ROOT).as_posix()
    if rel == 'installer/build-manifest.json':
        continue
    files[rel] = {'bytes': p.stat().st_size, 'sha256': sha256(p)}
manifest = {
    'version': 'v' + RELEASE_VERSION,
    'product': 'YOMI - YouTube OBS Music Interface',
    'release': RELEASE_TAG,
    'files': files,
}
(ROOT / 'installer/build-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8', newline='')

actual = {p.relative_to(ROOT).as_posix() for p in ROOT.rglob('*')
          if p.is_file() and p.relative_to(ROOT).as_posix() != 'installer/build-manifest.json'}
if actual != set(files):
    raise SystemExit('Manifest exact-file-set validation failed')
for rel, meta in files.items():
    p = ROOT / rel
    if p.stat().st_size != meta['bytes'] or sha256(p) != meta['sha256']:
        raise SystemExit(f'Manifest verification failed: {rel}')

# Required release assertions.
controller = read('payload/app/YomiControllerWpf.cs')
updater = read('payload/app/update.ps1')
obs = read('payload/app/YomiObsServer.ps1')
if 'CheckForUpdates(true)' not in controller:
    raise SystemExit('startup update check missing')
if 'CompareDottedVersions' not in controller or 'Compare-VersionText' not in updater:
    raise SystemExit('revision-aware version comparison missing')
if 'PreviewFontStretch' not in controller or '"Bahnschrift Condensed"' not in controller:
    raise SystemExit('Bahnschrift Condensed picker support missing')
if "fontStretch='condensed'" not in obs:
    raise SystemExit('Bahnschrift Condensed OBS rendering support missing')
if ' + " of " + activeTotal.ToString' not in controller:
    raise SystemExit('filtered A of B Queue counter missing')

if OUT.exists():
    OUT.unlink()
with zipfile.ZipFile(OUT, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for p in sorted(ROOT.rglob('*')):
        if p.is_file():
            z.write(p, p.relative_to(ROOT).as_posix())

print(json.dumps({
    'version': RELEASE_VERSION,
    'release': RELEASE_TAG,
    'path': str(OUT),
    'bytes': OUT.stat().st_size,
    'sha256': sha256(OUT),
    'files': len(files) + 1,
}, indent=2))
