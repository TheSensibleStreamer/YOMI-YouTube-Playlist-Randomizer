from pathlib import Path
import hashlib, json, sys, zipfile

VERSION="420.69.9001"
PREVIOUS="4.2.0.9.9.9"
REVISION="R61.106.52.13.25"
ROOT=Path(sys.argv[1] if len(sys.argv)>1 else ".release-work").resolve()
OUT=Path(sys.argv[2] if len(sys.argv)>2 else "YOMI-Windows.zip").resolve()

def read(rel):
    return (ROOT/rel).read_text(encoding="utf-8-sig")

def write(rel,text):
    p=ROOT/rel
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding="utf-8",newline="")

def replace_once(text,old,new,label):
    n=text.count(old)
    if n!=1:
        raise SystemExit(f"{label}: anchor count {n}, expected 1")
    return text.replace(old,new,1)

def sha256(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""):
            h.update(chunk)
    return h.hexdigest()

# First, carry the new deliberately shorter public version through every shipped text surface.
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

# Queue regression: hard-unavailable rows are already excluded by QueueFilter, but a deferred
# projection could lose the required ICollectionView refresh after metadata flips a row to
# Unavailable/Private/Deleted. Preserve that visibility invalidation until final reconciliation.
controller_path="payload/app/YomiControllerWpf.cs"
controller=read(controller_path)
controller=replace_once(
    controller,
    '''        private string _queueProjectionApplyReason = "";
        private readonly HashSet<int> _queueProjectionDeferredOccurrences = new HashSet<int>();''',
    '''        private string _queueProjectionApplyReason = "";
        private bool _queueVisibilityRefreshPending;
        private readonly HashSet<int> _queueProjectionDeferredOccurrences = new HashSet<int>();''',
    "queue visibility field"
)
controller=replace_once(
    controller,
    '''                bool wasHardUnavailable = IsHardUnavailableTrackTitle(row.Title);
                row.Title = meta != null && !String.IsNullOrWhiteSpace(meta.Title) ? meta.Title : "Track " + occ;
                if (wasHardUnavailable != IsHardUnavailableTrackTitle(row.Title)) queueVisibilityChanged = true;''',
    '''                bool wasHardUnavailable = IsHardUnavailableTrackTitle(row.Title);
                row.Title = meta != null && !String.IsNullOrWhiteSpace(meta.Title) ? meta.Title : "Track " + occ;
                bool isHardUnavailable = IsHardUnavailableTrackTitle(row.Title);
                if (wasHardUnavailable != isHardUnavailable)
                {
                    queueVisibilityChanged = true;
                    _queueVisibilityRefreshPending = true;
                }''',
    "queue visibility transition"
)
controller=replace_once(
    controller,
    '''                bool dynamicView = !incremental || queueVisibilityChanged || _queueScope != QueueScope.All || _queueQueryKind == QueueQueryKind.Text;
                if (_queueView != null && dynamicView) _queueView.Refresh();''',
    '''                bool dynamicView = !incremental || queueVisibilityChanged || _queueVisibilityRefreshPending || _queueScope != QueueScope.All || _queueQueryKind == QueueQueryKind.Text;
                if (_queueView != null && dynamicView)
                {
                    _queueView.Refresh();
                    _queueVisibilityRefreshPending = false;
                }''',
    "queue visibility final refresh"
)
write(controller_path,controller)

# Make Fixed/Reflow obvious: keep it in Broadcast, but directly beside the OBS video controls
# instead of burying it in canvas geometry.
xaml_path="payload/app/YomiControllerWpf.xaml"
xaml=read(xaml_path)
fps_help='''                                        <TextBlock Text="Source FPS preserves the prepared video rate. Choose 30 or 60 FPS when you want an explicit presentation cadence. Visualizer FPS is configured separately below." Style="{StaticResource SettingsHelp}" Margin="0,4,0,0" TextWrapping="Wrap"/>
'''
aspect_block='''                                        <TextBlock Text="Non-16:9 video layout" Style="{StaticResource SettingsLabel}" Margin="0,12,0,0"/>
                                        <ComboBox x:Name="SettingsMediaAspectLayout" Style="{StaticResource SettingsCombo}" Margin="0,6,0,0"/>
                                        <TextBlock Text="Fixed keeps the normal video slot and centers narrower video inside it. Reflow removes the unused width so the video and text slide left." Style="{StaticResource SettingsHelp}" Margin="0,4,0,0" TextWrapping="Wrap"/>
'''
if xaml.count(fps_help)!=1:
    raise SystemExit("OBS video settings insertion anchor changed")
xaml=xaml.replace(fps_help,fps_help+aspect_block,1)
old_aspect='''                                        <TextBlock Text="Video aspect layout" Style="{StaticResource SettingsLabel}" Margin="0,12,0,0"/>
                                        <ComboBox x:Name="SettingsMediaAspectLayout" Style="{StaticResource SettingsCombo}" Margin="0,6,0,0"/>
                                        <TextBlock Text="Fixed keeps the configured video slot in place and centers non-16:9 video inside it. Reflow collapses unused width so the video and text move left." Style="{StaticResource SettingsHelp}" Margin="0,4,0,0" TextWrapping="Wrap"/>
'''
if xaml.count(old_aspect)!=1:
    raise SystemExit("old Fixed/Reflow geometry block changed")
xaml=xaml.replace(old_aspect,"",1)
write(xaml_path,xaml)

host=read("payload/app/YomiPublicUpdateHost.ps1")
updater=read("payload/app/update.ps1")
deployment=read("payload/app/update-deployment.ps1")
server=read("payload/app/server.ps1")
diagnostics=read("payload/app/YomiDiagnosticBundle.ps1")

# Version parser must support the intentional 420.69.9001 version family.
for required in [
    "function VersionText",
    "function Compare-VersionText",
]:
    if required not in updater:
        raise SystemExit("numeric dotted version comparator missing: "+required)

# 9.9.9 is the first installed build that contains the verified restart implementation;
# this release is the first real update that can exercise it.
for required in [
    "function Test-YomiControllerRunning",
    "direct controller launch verified",
    "launcher fallback verified",
    "Update installed, but YOMI did not reopen automatically.",
    "$doneButton.Content='Open YOMI'",
    "update-restart.log",
]:
    if required not in host:
        raise SystemExit("verified restart gate missing: "+required)

# Delivery / transaction safety remains mandatory.
for required in [
    "$manifestFreshUri=$manifestUri+'?yomi_manifest='",
    "Downloaded bytes did not match the manifest. Retrying from a fresh cache path...",
    "yomi_version=",
]:
    if required not in updater:
        raise SystemExit("fresh update delivery gate missing: "+required)
for required in [
    "health=$null;rollback_health=$null",
    "Add-Member -NotePropertyName health -NotePropertyValue $health -Force",
]:
    if required not in deployment:
        raise SystemExit("transaction health gate missing: "+required)

# Queue hard-unavailable rows must never leak back into the visible table as track 0.
controller=read(controller_path)
for required in [
    "IsHardUnavailableTrackTitle(row.Title)",
    "_queueVisibilityRefreshPending = true;",
    "_queueVisibilityRefreshPending || _queueScope",
    "_queueVisibilityRefreshPending = false;",
]:
    if required not in controller:
        raise SystemExit("unavailable-row visibility gate missing: "+required)

# Fixed/Reflow must remain one durable setting in both controller and OBS, now beside video settings.
xaml=read(xaml_path)
if xaml.count('x:Name="SettingsMediaAspectLayout"')!=1:
    raise SystemExit("Fixed/Reflow control must exist exactly once")
overlay_index=xaml.find('x:Name="SettingsSectionBroadcastOverlay"')
aspect_index=xaml.find('Text="Non-16:9 video layout"')
geometry_index=xaml.find('x:Name="SettingsSectionBroadcastGeometry"')
if not (overlay_index >= 0 and aspect_index > overlay_index and geometry_index > aspect_index):
    raise SystemExit("Fixed/Reflow control is not visibly located in the OBS overlay/video section")
for required in [
    '"media_aspect_layout"',
    "MediaAspectReflowEnabled()",
]:
    if required not in controller:
        raise SystemExit("controller Fixed/Reflow behavior missing: "+required)
for required in ["c.media_aspect_layout||'Fixed'","fixedAspect=aspectLayout!=='reflow'"]:
    if required not in server:
        raise SystemExit("OBS Fixed/Reflow behavior missing: "+required)

if "update-restart.log" not in diagnostics:
    raise SystemExit("restart diagnostic evidence missing")

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
