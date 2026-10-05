from pathlib import Path
import hashlib, json, sys, zipfile

VERSION = "4.2.0.9.7"
PREVIOUS = "4.2.0.9.6"
REVISION = "R61.106.52.13.13"
ROOT = Path(sys.argv[1] if len(sys.argv) > 1 else ".release-work").resolve()
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else "YOMI-Windows.zip").resolve()

def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

def write(path, text):
    (ROOT / path).write_text(text, encoding="utf-8", newline="")

def replace_exact(path, old, new, expected=1):
    text = read(path)
    count = text.count(old)
    if count != expected:
        raise SystemExit(f"{path}: anchor count {count}, expected {expected}: {old!r}")
    write(path, text.replace(old, new))

# Bump public identity without touching binary payloads.
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

# OBS runtime fix: server paths must exist even when YOMI started in Player mode,
# because the unified OBS package can now be enabled without restarting audio.
replace_exact(
    "payload/app/YomiSupervisor.ps1",
    """ }
 if($streamer){try{
  Set-StartStatus 'Starting OBS overlay server...'
""",
    """ }
 # R61.106.52.13.13 live OBS package switching: these paths must exist even when
 # YOMI started in Player mode so enabling OBS can start only the Browser Source child.
 $serverExe=Join-Path $PSScriptRoot 'YomiObsServer.exe'
 $serverOut=Join-Path $DataRoot 'logs\\server.log'
 $serverErr=Join-Path $DataRoot 'logs\\server-error.log'
 if($streamer){try{
  Set-StartStatus 'Starting OBS overlay server...'
"""
)

# Add a cheap config-stamp watcher. It reacts to the unified OBS toggle without
# restarting playback, safely stops only the supervisor-owned server when disabled,
# and handles live OBS port changes.
replace_exact(
    "payload/app/YomiSupervisor.ps1",
    """ function Consume-YomiObsRepairRequest {
""",
    """ $obsConfigPath=Join-Path $DataRoot 'config.json'
 $obsConfigStamp=0
 try{$obsConfigStamp=(Get-Item -LiteralPath $obsConfigPath -ErrorAction Stop).LastWriteTimeUtc.Ticks}catch{}

 function Sync-YomiObsRuntimeMode {
  $item=$null
  try{$item=Get-Item -LiteralPath $script:obsConfigPath -ErrorAction Stop}catch{return}
  $stamp=[int64]$item.LastWriteTimeUtc.Ticks
  if($stamp -eq [int64]$script:obsConfigStamp){return}
  $script:obsConfigStamp=$stamp

  $latest=$null
  try{$latest=Get-YomiConfig}catch{return}
  if($null -eq $latest){return}

  $desiredStreamer=([string]$latest.app_mode -eq 'Streamer / OBS')
  $desiredPort=8876
  try{$desiredPort=[int]$latest.server_port}catch{}
  if($desiredPort -lt 1 -or $desiredPort -gt 65535){$desiredPort=8876}
  $currentPort=8876
  try{$currentPort=[int]$script:config.server_port}catch{}
  if($currentPort -lt 1 -or $currentPort -gt 65535){$currentPort=8876}
  $portChanged=($desiredPort -ne $currentPort)

  if($desiredStreamer){
   $wasStreamer=[bool]$script:streamer
   if($wasStreamer -and $portChanged -and $script:server -and -not $script:server.HasExited){
    Stop-Process -Id $script:server.Id -Force -ErrorAction SilentlyContinue
    $script:server=$null
    Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue
   }
   $script:config=$latest
   $script:streamer=$true
   $env:YOMI_SERVER_PORT=[string]$desiredPort
   if(-not $wasStreamer -or $portChanged){
    $script:serverRestartCount=0
    $script:serverRestartWindow=[DateTime]::UtcNow
    $script:nextServerRestartAttempt=[DateTime]::MinValue
    $script:serverHealthFailures=0
    $reason=$(if($portChanged){'OBS configured port changed at runtime'}else{'OBS package enabled at runtime'})
    Write-SupervisorLog ('OBS RUNTIME SYNC enabling local output port='+$desiredPort+' reason='+$reason)
    [void](Restart-YomiOverlayServerWatchdog -BypassCircuit $true -Reason $reason)
   }
   return
  }

  if($script:streamer){
   if($script:server -and -not $script:server.HasExited){Stop-Process -Id $script:server.Id -Force -ErrorAction SilentlyContinue}
   $script:server=$null
   Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue
   $script:serverHealthFailures=0
   Write-SupervisorLog 'OBS RUNTIME SYNC disabled local output without restarting audio'
   Write-WatchdogStatus 'healthy' 'OBS output disabled by current configuration.'
  }
  $script:config=$latest
  $script:streamer=$false
 }

 function Consume-YomiObsRepairRequest {
"""
)

# Poll only the tiny config timestamp / repair-request path quickly enough for the
# controller's Preview wait, while keeping expensive health and lease probes at 2 s.
replace_exact(
    "payload/app/YomiSupervisor.ps1",
    """ while(-not $mpvProcess.HasExited){
  Start-Sleep -Seconds 2
  if($mpvProcess.HasExited){break}

  if($streamer){
   Consume-YomiObsRepairRequest
""",
    """ $nextSupervisorHealthProbe=[DateTime]::MinValue
 while(-not $mpvProcess.HasExited){
  Start-Sleep -Milliseconds 350
  if($mpvProcess.HasExited){break}

  Sync-YomiObsRuntimeMode
  if($streamer){Consume-YomiObsRepairRequest}
  $runSlowProbe=([DateTime]::UtcNow -ge $nextSupervisorHealthProbe)
  if($runSlowProbe){$nextSupervisorHealthProbe=[DateTime]::UtcNow.AddSeconds(2)}

  if($streamer -and $runSlowProbe){
"""
)

replace_exact(
    "payload/app/YomiSupervisor.ps1",
    """   if($serverDead -and [DateTime]::UtcNow -ge $nextServerRestartAttempt){
    [void](Restart-YomiOverlayServerWatchdog -BypassCircuit $false -Reason 'periodic watchdog')
   }
  }

  $leaseFile=Join-Path $stateRoot 'runtime-lease.json'
""",
    """   if($serverDead -and [DateTime]::UtcNow -ge $nextServerRestartAttempt){
    [void](Restart-YomiOverlayServerWatchdog -BypassCircuit $false -Reason 'periodic watchdog')
   }
  }

  if(-not $runSlowProbe){continue}
  $leaseFile=Join-Path $stateRoot 'runtime-lease.json'
"""
)

# Updater speed: the controller already SHA-256 verifies the downloaded archive.
# Tell the deployment stage not to read/hash the entire archive two more times.
replace_exact(
    "payload/app/update-deployment.ps1",
    """    [switch]$CopyReport,
    [switch]$OpenReport
)
""",
    """    [switch]$CopyReport,
    [switch]$OpenReport,
    [switch]$OuterHashAlreadyVerified
)
"""
)
replace_exact(
    "payload/app/update-deployment.ps1",
    """    if($OuterHash -and (Hash $Zip) -ne $OuterHash.ToLowerInvariant()){throw 'Outer package SHA-256 does not match the update manifest.'}
""",
    """    if($OuterHash -and -not $OuterHashAlreadyVerified -and (Hash $Zip) -ne $OuterHash.ToLowerInvariant()){throw 'Outer package SHA-256 does not match the update manifest.'}
"""
)
replace_exact(
    "payload/app/update-deployment.ps1",
    """& $robo $From $To /E /COPY:DAT /DCOPY:T /R:1 /W:1 /XJ /NFL /NDL /NJH /NJS /NP|Out-Null""",
    """& $robo $From $To /E /COPY:DAT /DCOPY:T /MT:16 /R:1 /W:1 /XJ /NFL /NDL /NJH /NJS /NP|Out-Null"""
)
replace_exact(
    "payload/app/update-deployment.ps1",
    """package_sha256=(Hash $PackagePath);staging_path=$ExtractRoot;""",
    """package_sha256=$(if($OuterHashAlreadyVerified -and $ExpectedPackageHash){$ExpectedPackageHash.ToLowerInvariant()}else{Hash $PackagePath});staging_path=$ExtractRoot;"""
)

replace_exact(
    "payload/app/update.ps1",
    """ $packagePath=Join-Path $updateRoot $packageName;$downloading=$packagePath+'.downloading';Remove-Item $downloading -Force -ErrorAction SilentlyContinue
 Invoke-WebRequest -Uri $packageUri -Headers $headers -UseBasicParsing -TimeoutSec 120 -OutFile $downloading;$actual=(Get-FileHash $downloading -Algorithm SHA256).Hash.ToLowerInvariant();if($actual -ne $expectedHash){Remove-Item $downloading -Force -ErrorAction SilentlyContinue;throw "Update integrity check failed. Expected $expectedHash but received $actual."};Move-Item $downloading $packagePath -Force
 $extractRoot=Join-Path $updateRoot ('ready-'+$latest);$prep=& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deploy -Prepare -PackagePath $packagePath -ExtractRoot $extractRoot -ExpectedVersion $latest -ExpectedPackageHash $expectedHash -InstallRoot $installRoot -DataRoot $dataRoot 2>&1;if($LASTEXITCODE -ne 0){Update-Tx 'PACKAGE_REJECTED' ($prep -join ' ');throw ('Package rehearsal failed: '+($prep -join ' '))}
""",
    """ $packagePath=Join-Path $updateRoot $packageName;$downloading=$packagePath+'.downloading';Remove-Item $downloading -Force -ErrorAction SilentlyContinue
 $packageReady=$false
 if(Test-Path -LiteralPath $packagePath -PathType Leaf){
  try{$cachedHash=(Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant();if($cachedHash -eq $expectedHash){$packageReady=$true}else{Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue}}catch{Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue}
 }
 if(-not $packageReady){Invoke-WebRequest -Uri $packageUri -Headers $headers -UseBasicParsing -TimeoutSec 120 -OutFile $downloading;$actual=(Get-FileHash $downloading -Algorithm SHA256).Hash.ToLowerInvariant();if($actual -ne $expectedHash){Remove-Item $downloading -Force -ErrorAction SilentlyContinue;throw "Update integrity check failed. Expected $expectedHash but received $actual."};Move-Item $downloading $packagePath -Force}
 $extractRoot=Join-Path $updateRoot ('ready-'+$latest);$prep=& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deploy -Prepare -PackagePath $packagePath -ExtractRoot $extractRoot -ExpectedVersion $latest -ExpectedPackageHash $expectedHash -InstallRoot $installRoot -DataRoot $dataRoot -OuterHashAlreadyVerified 2>&1;if($LASTEXITCODE -ne 0){Update-Tx 'PACKAGE_REJECTED' ($prep -join ' ');throw ('Package rehearsal failed: '+($prep -join ' '))}
"""
)

# Assertions: fail publication rather than shipping a cosmetic toggle again.
supervisor = read("payload/app/YomiSupervisor.ps1")
updater = read("payload/app/update.ps1")
deployment = read("payload/app/update-deployment.ps1")
controller = read("payload/app/YomiControllerWpf.cs")
xaml = read("payload/app/YomiControllerWpf.xaml")
design = read("payload/app/YomiDesign.xaml")
installer = read("installer/install.ps1")

for required in [
    "function Sync-YomiObsRuntimeMode",
    "OBS package enabled at runtime",
    "Start-Sleep -Milliseconds 350",
    "if($streamer){Consume-YomiObsRepairRequest}",
    "if(-not $runSlowProbe){continue}",
]:
    if required not in supervisor:
        raise SystemExit("OBS runtime-switch fix missing: " + required)
if "-OuterHashAlreadyVerified" not in updater:
    raise SystemExit("updater is not skipping redundant package hashes")
if "/MT:16" not in deployment or "OuterHashAlreadyVerified" not in deployment:
    raise SystemExit("updater acceleration changes are incomplete")
if "Update now?" not in updater or "$manifest.summary" in updater:
    raise SystemExit("updater prompt regressed")
if 'BuildYomiStatusDialog("Update available", "YOMI " + latestText + " is available. You have " + currentText + "."' not in controller:
    raise SystemExit("controller update dialog regressed")
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
