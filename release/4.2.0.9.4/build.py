from pathlib import Path
import hashlib, json, sys, zipfile

VERSION = "4.2.0.9.4"
PREVIOUS = "4.2.0.9.3"
REVISION = "R61.106.52.13.10"
ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".release-work").resolve()
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else "YOMI-Windows.zip").resolve()

def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

def write(path, text):
    (ROOT / path).write_text(text, encoding="utf-8", newline="")

def replace_once(path, old, new):
    text = read(path)
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: expected one anchor, got {count}: {old[:120]!r}")
    write(path, text.replace(old, new, 1))

# Font picker: the controller already creates each font choice as a ComboBoxItem
# with the correct FontFamily/FontStretch. The inline ItemTemplate overrode that
# and reduced everything to a generic binding, which is why normal families such
# as Times New Roman visibly previewed while face variants such as Bahnschrift
# Condensed did not.
replace_once(
    "payload/app/YomiControllerWpf.xaml",
    '<StackPanel><TextBlock Text="Font" Style="{StaticResource SettingsLabel}"/><ComboBox x:Name="SettingsTextFont" IsEditable="True" Style="{StaticResource SettingsCombo}" Margin="0,6,0,0"><ComboBox.ItemTemplate><DataTemplate><TextBlock Text="{Binding}" FontFamily="{Binding}" FontSize="15"/></DataTemplate></ComboBox.ItemTemplate></ComboBox></StackPanel>',
    '<StackPanel><TextBlock Text="Font" Style="{StaticResource SettingsLabel}"/><ComboBox x:Name="SettingsTextFont" IsEditable="True" Style="{StaticResource SettingsCombo}" Margin="0,6,0,0"/></StackPanel>'
)

# Settings ComboBox chrome: fully suppress any residual native focus/background
# treatment and make the popup itself use the active YOMI surface instead of the
# generic raised/readability surface that could read as a gray slab.
replace_once(
    "payload/app/YomiDesign.xaml",
    '''    <Style x:Key="SettingsComboItem" TargetType="ComboBoxItem">
        <Setter Property="FontSize" Value="13.5"/>
        <Setter Property="Foreground" Value="{DynamicResource TextPrimary}"/>
        <Setter Property="Background" Value="Transparent"/>''',
    '''    <Style x:Key="SettingsComboItem" TargetType="ComboBoxItem">
        <Setter Property="FontSize" Value="13.5"/>
        <Setter Property="Foreground" Value="{DynamicResource TextPrimary}"/>
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
        <Setter Property="OverridesDefaultStyle" Value="True"/>'''
)

replace_once(
    "payload/app/YomiDesign.xaml",
    'Background="{DynamicResource ReadableSurfaceRaised}"\n                                    BorderBrush="{DynamicResource BorderStrong}"\n                                    BorderThickness="1"\n                                    CornerRadius="3">\n                                <ScrollViewer Margin="1"\n                                              SnapsToDevicePixels="True"',
    'Background="{DynamicResource Surface}"\n                                    BorderBrush="{DynamicResource BorderStrong}"\n                                    BorderThickness="1"\n                                    CornerRadius="3">\n                                <ScrollViewer Margin="1"\n                                              Background="Transparent"\n                                              SnapsToDevicePixels="True"'
)

# Keep the closed field on the same base surface while open; the popup/hover state
# already communicates openness, so no extra gray raised slab is needed.
replace_once(
    "payload/app/YomiDesign.xaml",
    '<Trigger Property="IsDropDownOpen" Value="True">\n                            <Setter TargetName="ComboBorder" Property="Background" Value="{DynamicResource SurfaceRaised}"/>\n                        </Trigger>',
    '<Trigger Property="IsDropDownOpen" Value="True">\n                            <Setter TargetName="ComboBorder" Property="Background" Value="{DynamicResource Surface}"/>\n                        </Trigger>'
)

# Bump public identity everywhere relevant.
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

# Static assertions targeted at this revision.
xaml = read("payload/app/YomiControllerWpf.xaml")
design = read("payload/app/YomiDesign.xaml")
controller = read("payload/app/YomiControllerWpf.cs")

if '<ComboBox.ItemTemplate>' in xaml[xaml.find('x:Name="SettingsTextFont"')-250:xaml.find('x:Name="SettingsTextFont"')+500]:
    raise SystemExit("font picker still has an inline ItemTemplate")
for required in [
    'FontFamily = new FontFamily(PreviewFontFamilySource(choice))',
    'FontStretch = PreviewFontStretch(choice)',
    'return FontStretches.Condensed',
    'return FontStretches.SemiCondensed'
]:
    if required not in controller:
        raise SystemExit("font preview runtime contract missing: " + required)
if '<Setter Property="FocusVisualStyle" Value="{x:Null}"/>' not in design:
    raise SystemExit("combo item native focus suppression missing")
if '<Setter Property="OverridesDefaultStyle" Value="True"/>' not in design:
    raise SystemExit("combo item native style suppression missing")
if 'Background="{DynamicResource Surface}"\n                                    BorderBrush="{DynamicResource BorderStrong}"' not in design:
    raise SystemExit("settings combo popup does not use theme Surface")
if 'Background="Transparent"\n                                              SnapsToDevicePixels="True"' not in design:
    raise SystemExit("settings combo scroll viewer is not explicitly transparent")

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
