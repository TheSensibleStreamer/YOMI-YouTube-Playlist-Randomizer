from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="4.2.0.9.9"
PREVIOUS="4.2.0.9.8"
REVISION="R61.106.52.13.15"
ROOT=Path(sys.argv[1] if len(sys.argv)>1 else ".release-work").resolve()
OUT=Path(sys.argv[2] if len(sys.argv)>2 else "YOMI-Windows.zip").resolve()

def read(rel):
    return (ROOT/rel).read_text(encoding="utf-8-sig")

def write(rel,text):
    (ROOT/rel).write_text(text,encoding="utf-8",newline="")

def replace_once(text, old, new, label):
    n=text.count(old)
    if n != 1:
        raise SystemExit(f"{label}: anchor count {n}, expected 1")
    return text.replace(old,new,1)

# Version bump across the shipped text payload.
text_exts={".ps1",".cs",".json",".txt",".xaml",".cmd",".config",".manifest",".md"}
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

# Controller settings/defaults.
path="payload/app/YomiControllerWpf.cs"
c=read(path)

c=replace_once(c,
    'private const int DefaultOverlayTextSize = 33;',
    'private const int DefaultOverlayTextSize = 32;',
    "default overlay text size")

c=replace_once(c,
    'private ThemeIntensity _themeIntensity = ThemeIntensity.Soft;\n        private double _themeIntensityAmount = 0.0;',
    'private ThemeIntensity _themeIntensity = ThemeIntensity.Vivid;\n        private double _themeIntensityAmount = 1.0;',
    "initial theme intensity")

c=replace_once(c,
    'private double _windowTransparencyPercent = 0.0;',
    'private double _windowTransparencyPercent = 10.0;',
    "initial window transparency")

c=replace_once(c,
    'FillCombo(_settingsTextSize, "24", "28", "30", "33", "36", "40", "44", "48");',
    'FillCombo(_settingsTextSize, "24", "28", "32", "36", "40", "44", "48", "52", "56", "60", "64");',
    "text size choices")

c=replace_once(c,
    'FillCombo(_settingsTextOutline, "0", "1", "2", "3", "4", "5", "6", "7", "8");',
    'FillCombo(_settingsTextOutline, "0", "1", "2", "3", "4", "5", "6", "7", "8", "10", "12", "14", "16");',
    "outline choices")

old_fonts='''                "Bahnschrift Condensed", "Bahnschrift SemiCondensed", "Franklin Gothic Medium", "Agency FB",
                "Aptos Narrow", "Franklin Gothic Medium Cond", "News Gothic MT", "Tw Cen MT Condensed", "Gill Sans MT Condensed", "Rockwell Condensed",
                "Haettenschweiler", "Roboto Condensed", "IBM Plex Sans Condensed", "Barlow Condensed", "Oswald"
'''
new_fonts='''                "Bahnschrift Condensed", "Aptos Narrow", "Franklin Gothic Medium Cond", "News Gothic MT",
                "Tw Cen MT Condensed", "Gill Sans MT Condensed", "Roboto Condensed", "IBM Plex Sans Condensed",
                "Barlow Condensed", "Oswald", "Franklin Gothic Medium", "Rockwell Condensed"
'''
c=replace_once(c,old_fonts,new_fonts,"curated compact font list")

c=replace_once(c,
    'if (box.Items.Count >= 10) break;',
    'if (box.Items.Count >= 12) break;',
    "compact font slot limit")

c=replace_once(c,
    'SetCombo(_settingsWorkspace, "Player"); SetCombo(_settingsTheme, "System Theme"); SetCombo(_settingsThemeIntensity, "Soft"); SetCombo(_settingsThemeAccent, "None"); SetCombo(_settingsThemeBlend, "Balanced"); if (_settingsThemeIntensitySlider != null) _settingsThemeIntensitySlider.Value = 0; if (_settingsThemeBlendSlider != null) _settingsThemeBlendSlider.Value = 60; if (_settingsWindowTransparencySlider != null) _settingsWindowTransparencySlider.Value = 0;',
    'SetCombo(_settingsWorkspace, "Player"); SetCombo(_settingsTheme, "System Theme"); SetCombo(_settingsThemeIntensity, "Vivid"); SetCombo(_settingsThemeAccent, "None"); SetCombo(_settingsThemeBlend, "Balanced"); if (_settingsThemeIntensitySlider != null) _settingsThemeIntensitySlider.Value = 100; if (_settingsThemeBlendSlider != null) _settingsThemeBlendSlider.Value = 60; if (_settingsWindowTransparencySlider != null) _settingsWindowTransparencySlider.Value = 10;',
    "settings factory draft appearance")

c=replace_once(c,
    'Enum.TryParse(GetString(map, "theme_intensity", "Soft"), true, out savedThemeIntensity) ? savedThemeIntensity : ThemeIntensity.Soft;',
    'Enum.TryParse(GetString(map, "theme_intensity", "Vivid"), true, out savedThemeIntensity) ? savedThemeIntensity : ThemeIntensity.Vivid;',
    "missing state theme default")

c=replace_once(c,
    'GetDouble(map, "window_transparency_percent", 0.0)',
    'GetDouble(map, "window_transparency_percent", 10.0)',
    "missing state transparency default")

c=replace_once(c,
    '_themeIntensity = ThemeIntensity.Soft; _themeIntensityAmount = 0.0; _appearanceAccentEnabled = false; _appearanceAccentPreset = AppearancePreset.Dusk; _themeBlendStrength = ThemeBlendStrength.Balanced; _themeBlendAmount = 0.60; _windowTransparencyPercent = 0.0;',
    '_themeIntensity = ThemeIntensity.Vivid; _themeIntensityAmount = 1.0; _appearanceAccentEnabled = false; _appearanceAccentPreset = AppearancePreset.Dusk; _themeBlendStrength = ThemeBlendStrength.Balanced; _themeBlendAmount = 0.60; _windowTransparencyPercent = 10.0;',
    "factory controller appearance")

write(path,c)

# Shipped config defaults for fresh installs / factory reset.
cfg_path="payload/app/default-config.json"
cfg=json.loads(read(cfg_path))
cfg["text_size"]=32
cfg["text_outline"]=6
cfg["text_font"]="Bahnschrift Condensed"
write(cfg_path,json.dumps(cfg,indent=2,ensure_ascii=False)+"\n")

# Browser Source fallback must agree with the controller's 32 px default.
obs_path="payload/app/YomiObsServerHost.cs"
obs=read(obs_path)
obs=replace_once(obs,'let max=num(c.text_size,33),min=num(c.overlay_min_text_size,18)',
                    'let max=num(c.text_size,32),min=num(c.overlay_min_text_size,18)',
                    "OBS text fallback")
write(obs_path,obs)

# Match the hidden/unpainted XAML defaults too, avoiding any 0% flash during construction.
xaml_path="payload/app/YomiControllerWpf.xaml"
x=read(xaml_path)
x=replace_once(x,'x:Name="SettingsThemeIntensityValueText" Text="0%"',
                 'x:Name="SettingsThemeIntensityValueText" Text="100%"',
                 "theme intensity XAML label")
x=replace_once(x,'x:Name="SettingsWindowTransparencyValueText" Text="0%"',
                 'x:Name="SettingsWindowTransparencyValueText" Text="10%"',
                 "transparency XAML label")
write(xaml_path,x)

# Regression assertions.
c=read(path)
assert 'FillCombo(_settingsTextSize, "24", "28", "32", "36", "40", "44", "48", "52", "56", "60", "64");' in c
assert 'FillCombo(_settingsTextOutline, "0", "1", "2", "3", "4", "5", "6", "7", "8", "10", "12", "14", "16");' in c
assert '"Bahnschrift Condensed", "Aptos Narrow", "Franklin Gothic Medium Cond", "News Gothic MT"' in c
assert '"Bahnschrift Condensed", "Bahnschrift SemiCondensed"' not in c
assert 'private ThemeIntensity _themeIntensity = ThemeIntensity.Vivid;' in c
assert 'private double _themeIntensityAmount = 1.0;' in c
assert 'private double _windowTransparencyPercent = 10.0;' in c
assert '_settingsThemeIntensitySlider.Value = 100' in c
assert '_settingsWindowTransparencySlider.Value = 10' in c
cfg=json.loads(read(cfg_path))
assert cfg["text_size"]==32 and cfg["text_outline"]==6

def sha256(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""):
            h.update(chunk)
    return h.hexdigest()

files={}
for p in sorted(ROOT.rglob("*")):
    if not p.is_file():
        continue
    rel=p.relative_to(ROOT).as_posix()
    if rel=="installer/build-manifest.json":
        continue
    files[rel]={"bytes":p.stat().st_size,"sha256":sha256(p)}
manifest={"version":"v"+VERSION,"product":"YOMI - YouTube OBS Music Interface","release":REVISION,"files":files}
(ROOT/"installer/build-manifest.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8",newline="")

if OUT.exists():
    OUT.unlink()
with zipfile.ZipFile(OUT,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for p in sorted(ROOT.rglob("*")):
        if p.is_file():
            z.write(p,p.relative_to(ROOT).as_posix())

print(json.dumps({"version":VERSION,"revision":REVISION,"bytes":OUT.stat().st_size,"sha256":sha256(OUT),"files":len(files)+1},indent=2))
