from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.9.1"
PREVIOUS="4.2.0.9.9"
REVISION="R61.106.52.13.16"
ROOT=Path(sys.argv[1] if len(sys.argv)>1 else ".release-work").resolve()
OUT=Path(sys.argv[2] if len(sys.argv)>2 else "YOMI-Windows.zip").resolve()

def read(rel): return (ROOT/rel).read_text(encoding="utf-8-sig")
def write(rel,text): (ROOT/rel).write_text(text,encoding="utf-8",newline="")
def replace_once(text,old,new,label):
    n=text.count(old)
    if n!=1: raise SystemExit(f"{label}: anchor count {n}, expected 1")
    return text.replace(old,new,1)

text_exts={".ps1",".cs",".json",".txt",".xaml",".cmd",".config",".manifest",".md"}
for p in list(ROOT.rglob("*")):
    if not p.is_file() or p.name=="build-manifest.json" or p.suffix.lower() not in text_exts: continue
    try: t=p.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError: continue
    if PREVIOUS in t: p.write_text(t.replace(PREVIOUS,VERSION),encoding="utf-8",newline="")

write("payload/VERSION.txt",VERSION+"\n")
write("payload/app/FOCUSED-BUILD.txt",f"YOMI {VERSION} Focused Media Player\n{REVISION}\n")

path="payload/app/YomiControllerWpf.cs"
c=read(path)

anchor='''        private static bool Checked(CheckBox box) { return box.IsChecked == true; }
        private static int ComboInt(ComboBox box, int fallback) { int n; return Int32.TryParse(ComboText(box, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : fallback; }
'''
insert='''        private static bool Checked(CheckBox box) { return box.IsChecked == true; }
        private static int NormalizeOverlayTextSize(int value)
        {
            int clamped = Math.Max(24, Math.Min(64, value));
            return Math.Max(24, Math.Min(64, (int)Math.Round(clamped / 4.0, MidpointRounding.AwayFromZero) * 4));
        }
        private static string NormalizeOverlayFontChoice(string value)
        {
            return String.Equals((value ?? "").Trim(), "Bahnschrift SemiCondensed", StringComparison.OrdinalIgnoreCase)
                ? "Bahnschrift Condensed"
                : (value ?? "").Trim();
        }
        private static int ComboInt(ComboBox box, int fallback) { int n; return Int32.TryParse(ComboText(box, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : fallback; }
'''
c=replace_once(c,anchor,insert,"normalizer insertion")

c=replace_once(c,
    'SetCombo(_settingsTextSize, GetInt(c, "text_size", DefaultOverlayTextSize).ToString(CultureInfo.InvariantCulture));',
    'SetCombo(_settingsTextSize, NormalizeOverlayTextSize(GetInt(c, "text_size", DefaultOverlayTextSize)).ToString(CultureInfo.InvariantCulture));',
    "text size migration")

c=replace_once(c,
    'SetComboPreserve(_settingsTextFont, GetString(c, "text_font", DefaultOverlayTextFont));',
    'SetComboPreserve(_settingsTextFont, NormalizeOverlayFontChoice(GetString(c, "text_font", DefaultOverlayTextFont)));',
    "font duplicate migration")

write(path,c)

c=read(path)
assert 'NormalizeOverlayTextSize' in c
assert 'Math.Round(clamped / 4.0, MidpointRounding.AwayFromZero)' in c
assert 'NormalizeOverlayFontChoice' in c
assert 'NormalizeOverlayTextSize(GetInt(c, "text_size", DefaultOverlayTextSize))' in c
assert 'NormalizeOverlayFontChoice(GetString(c, "text_font", DefaultOverlayTextFont))' in c

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
(ROOT/"installer/build-manifest.json").write_text(json.dumps({"version":"v"+VERSION,"product":"YOMI - YouTube OBS Music Interface","release":REVISION,"files":files},indent=2)+"\n",encoding="utf-8",newline="")

if OUT.exists(): OUT.unlink()
with zipfile.ZipFile(OUT,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for p in sorted(ROOT.rglob("*")):
        if p.is_file(): z.write(p,p.relative_to(ROOT).as_posix())
print(json.dumps({"version":VERSION,"revision":REVISION,"bytes":OUT.stat().st_size,"sha256":sha256(OUT),"files":len(files)+1},indent=2))
