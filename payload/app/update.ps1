param([switch]$Manual,[switch]$Approved,[string]$StatusFile,[string]$InstallRootOverride,[string]$DataRootOverride)

$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
$installRoot=$(if([string]::IsNullOrWhiteSpace($InstallRootOverride)){Split-Path $PSScriptRoot -Parent}else{[IO.Path]::GetFullPath($InstallRootOverride)})
$dataRoot=$(if([string]::IsNullOrWhiteSpace($DataRootOverride)){Join-Path $env:LOCALAPPDATA 'YOMI'}else{[IO.Path]::GetFullPath($DataRootOverride)});$stateRoot=Join-Path $dataRoot 'state';$updateRoot=Join-Path $dataRoot 'updates'
$lastCheckFile=Join-Path $stateRoot 'last-update-check.txt';$lastPromptFile=Join-Path $stateRoot 'last-update-prompt.json'
$manifestUri='https://raw.githubusercontent.com/TheSensibleStreamer/YOMI-YouTube-Playlist-Randomizer/main/update.json'
$deploy=Join-Path $PSScriptRoot 'update-deployment.ps1';$txFile=Join-Path $stateRoot 'update-transaction.json'
New-Item -ItemType Directory -Path $stateRoot,$updateRoot -Force|Out-Null
# Never keep Program Files\YOMI\app as this process's current directory while an update
# replaces the installation tree. This prevents the updater itself from locking the app folder.
try {
    [Environment]::CurrentDirectory = $updateRoot
    Set-Location -LiteralPath $updateRoot
} catch {}
$deploymentPolicy='Verified rollback';$configPath=Join-Path $dataRoot 'config.json';if(Test-Path $configPath){try{$uc=Get-Content $configPath -Raw -Encoding UTF8|ConvertFrom-Json;if($uc.PSObject.Properties['update_deployment_policy']){$deploymentPolicy=[string]$uc.update_deployment_policy}}catch{}}
$created=$false;$updateMutex=New-Object Threading.Mutex($true,'Local\YOMI_Update_Check',[ref]$created);$ownsMutex=$created
if(-not $created){if(-not $Manual -or -not $updateMutex.WaitOne(5000)){$updateMutex.Dispose();exit 0};$ownsMutex=$true}
function VersionText([string]$Text){$m=[regex]::Match($Text,'\d+(?:\.\d+)+');if(-not $m.Success){return '0.0'};return $m.Value}
function Compare-VersionText([string]$Left,[string]$Right){$a=@((VersionText $Left).Split('.')|ForEach-Object{[int64]$_});$b=@((VersionText $Right).Split('.')|ForEach-Object{[int64]$_});$n=[Math]::Max($a.Count,$b.Count);for($i=0;$i -lt $n;$i++){$av=if($i -lt $a.Count){[int64]$a[$i]}else{0};$bv=if($i -lt $b.Count){[int64]$b[$i]}else{0};if($av -lt $bv){return -1};if($av -gt $bv){return 1}};return 0}
function Write-UpdateStatus([int]$Percent,[string]$State,[string]$Message){
 if([string]::IsNullOrWhiteSpace($StatusFile)){return}
 try{
  $parent=Split-Path $StatusFile -Parent;if($parent){New-Item -ItemType Directory -Path $parent -Force|Out-Null}
  $obj=[ordered]@{schema=1;percent=[Math]::Max(0,[Math]::Min(100,$Percent));state=$State;message=$Message;updated_utc=[DateTime]::UtcNow.ToString('o')}
  $tmp=$StatusFile+'.tmp-'+[Guid]::NewGuid().ToString('N')
  [IO.File]::WriteAllText($tmp,($obj|ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
  Move-Item -LiteralPath $tmp -Destination $StatusFile -Force
 }catch{}
}
function Msg([string]$Text,[Windows.Forms.MessageBoxIcon]$Icon){if($Manual -and [string]::IsNullOrWhiteSpace($StatusFile)){[Windows.Forms.MessageBox]::Show($Text,'YOMI Update',[Windows.Forms.MessageBoxButtons]::OK,$Icon)|Out-Null}}
function Download-PackageWithStatus([string]$Uri,[string]$OutFile,[hashtable]$Headers,[string]$Version){
 $request=[Net.HttpWebRequest]::Create($Uri);$request.Method='GET';$request.AllowAutoRedirect=$true;$request.MaximumAutomaticRedirections=10;$request.Timeout=120000;$request.ReadWriteTimeout=120000;$request.KeepAlive=$true
 foreach($key in $Headers.Keys){if($key -ieq 'User-Agent'){$request.UserAgent=[string]$Headers[$key]}else{$request.Headers[$key]=[string]$Headers[$key]}}
 $response=$null;$input=$null;$output=$null
 try{
  $response=$request.GetResponse();$total=[int64]$response.ContentLength;$input=$response.GetResponseStream()
  $output=New-Object IO.FileStream($OutFile,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None,1048576,[IO.FileOptions]::SequentialScan)
  $buffer=New-Object byte[] 1048576;[int64]$downloaded=0;$lastStatus=[DateTime]::MinValue
  while(($read=$input.Read($buffer,0,$buffer.Length)) -gt 0){
   $output.Write($buffer,0,$read);$downloaded+=$read
   $now=Get-Date
   if(($now-$lastStatus).TotalMilliseconds -ge 120){
    if($total -gt 0){$ratio=[Math]::Min(1.0,$downloaded/[double]$total);$pct=10+[int][Math]::Floor(17*$ratio);$detail=("{0:N1} / {1:N1} MB" -f ($downloaded/1MB),($total/1MB))}
    else{$pct=18;$detail=("{0:N1} MB" -f ($downloaded/1MB))}
    Write-UpdateStatus $pct 'downloading' ("Downloading YOMI $Version... "+$detail);$lastStatus=$now
   }
  }
  $output.Flush()
  if($downloaded -le 0){throw 'The update package download returned zero bytes.'}
  if($total -gt 0 -and $downloaded -ne $total){throw "The update package download was incomplete: expected $total bytes, received $downloaded."}
 }finally{if($output){$output.Dispose()};if($input){$input.Dispose()};if($response){$response.Dispose()}}
}
function Update-Tx([string]$State,[string]$Reason){if(-not(Test-Path $txFile)){return};try{$tx=Get-Content $txFile -Raw -Encoding UTF8|ConvertFrom-Json;$tx.state=$State;$tx.reason=$Reason;$tx.updated_utc=[DateTime]::UtcNow.ToString('o');[IO.File]::WriteAllText($txFile,($tx|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))}catch{}}
try{
 Write-UpdateStatus 3 'checking' 'Checking the public YOMI update manifest...'
 [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
 if(-not(Test-Path -LiteralPath $deploy)){throw 'The transactional update deployment engine is missing.'}
 $versionFile=Join-Path $installRoot 'VERSION.txt';$current=VersionText ($(if(Test-Path $versionFile){Get-Content $versionFile -Raw}else{'0.0'}));$headers=@{'User-Agent'=('YOMI-'+$current+'-Updater')}
 $manifestHeaders=@{}+$headers;$manifestHeaders['Cache-Control']='no-cache, no-store, max-age=0';$manifestHeaders['Pragma']='no-cache'
 $manifestFreshUri=$manifestUri+'?yomi_manifest='+[Uri]::EscapeDataString([DateTime]::UtcNow.Ticks.ToString())
 $manifest=Invoke-RestMethod -Uri $manifestFreshUri -Headers $manifestHeaders -UseBasicParsing -TimeoutSec 20;Set-Content $lastCheckFile ((Get-Date).ToString('o')) -Encoding ASCII;$latest=VersionText ([string]$manifest.version)
 if((Compare-VersionText $latest $current) -le 0){Write-UpdateStatus 100 'current' "YOMI $current is already current.";Msg "YOMI $current is current.`r`n`r`nNo newer public build is available." ([Windows.Forms.MessageBoxIcon]::Information);exit 0}
  $answer=if($Approved){[Windows.Forms.DialogResult]::Yes}else{[Windows.Forms.MessageBox]::Show("YOMI $latest is available. You have $current.`r`n`r`nUpdate now?",'YOMI Update Available',[Windows.Forms.MessageBoxButtons]::YesNo,[Windows.Forms.MessageBoxIcon]::Information)}
 if($answer -ne [Windows.Forms.DialogResult]::Yes){exit 0}
  $packageName=[string]$manifest.package_name;if([string]::IsNullOrWhiteSpace($packageName) -or ($packageName -ne 'YOMI-Windows.zip' -and $packageName -notmatch '^YOMI-v[0-9.]+\.zip$')){throw 'The public update manifest contains an invalid package name.'}
 $packageUri=[string]$manifest.package_url;$expectedHash=([string]$manifest.sha256).Trim().ToLowerInvariant();if($packageUri -notlike 'https://raw.githubusercontent.com/TheSensibleStreamer/YOMI-YouTube-Playlist-Randomizer/*'){throw 'The public update manifest points outside the official YOMI repository.'};if($expectedHash -notmatch '^[a-f0-9]{64}$'){throw 'The public update manifest contains an invalid SHA-256 value.'}
 Write-UpdateStatus 10 'downloading' "Downloading YOMI $latest..."
 $packagePath=Join-Path $updateRoot $packageName;$downloading=$packagePath+'.downloading';Remove-Item $downloading -Force -ErrorAction SilentlyContinue
 $packageReady=$false
 if(Test-Path -LiteralPath $packagePath -PathType Leaf){
  try{$cachedHash=(Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant();if($cachedHash -eq $expectedHash){$packageReady=$true}else{Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue}}catch{Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue}
 }
 if(-not $packageReady){
  Download-PackageWithStatus $packageUri $downloading $headers $latest
  Write-UpdateStatus 28 'verifying-download' 'Verifying downloaded package...'
  $actual=(Get-FileHash $downloading -Algorithm SHA256).Hash.ToLowerInvariant()
  if($actual -ne $expectedHash){
   Remove-Item $downloading -Force -ErrorAction SilentlyContinue
   Write-UpdateStatus 18 'downloading' 'Downloaded bytes did not match the manifest. Retrying from a fresh cache path...'
   $separator=$(if($packageUri.Contains('?')){'&'}else{'?'})
   $retryUri=$packageUri+$separator+'yomi_version='+[Uri]::EscapeDataString($latest)+'&yomi_sha='+$expectedHash.Substring(0,16)
   Download-PackageWithStatus $retryUri $downloading $headers $latest
   Write-UpdateStatus 28 'verifying-download' 'Verifying retried package...'
   $actual=(Get-FileHash $downloading -Algorithm SHA256).Hash.ToLowerInvariant()
   if($actual -ne $expectedHash){Remove-Item $downloading -Force -ErrorAction SilentlyContinue;throw "Update integrity check failed after a fresh retry. Expected $expectedHash but received $actual."}
  }
  Move-Item $downloading $packagePath -Force
 }else{Write-UpdateStatus 28 'verifying-download' 'Using already verified cached package...'}
 Write-UpdateStatus 38 'preparing' 'Checking package contents and preparing the update...'
 $extractRoot=Join-Path $updateRoot ('ready-'+$latest);$prep=& powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File $deploy -Prepare -PackagePath $packagePath -ExtractRoot $extractRoot -ExpectedVersion $latest -ExpectedPackageHash $expectedHash -InstallRoot $installRoot -DataRoot $dataRoot -OuterHashAlreadyVerified 2>&1;if($LASTEXITCODE -ne 0){Update-Tx 'PACKAGE_REJECTED' ($prep -join ' ');throw ('Package rehearsal failed: '+($prep -join ' '))}
 Write-UpdateStatus 52 'snapshot' 'Saving the current working installation for automatic rollback...'
 if($deploymentPolicy -eq 'Verified rollback'){$snap=& powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File $deploy -Snapshot -InstallRoot $installRoot -DataRoot $dataRoot 2>&1;if($LASTEXITCODE -ne 0){throw ('Could not capture last-known-good installation: '+($snap -join ' '))}}else{Update-Tx 'PACKAGE_VERIFIED' 'verify-only-policy-no-lkg-snapshot'}
 $tx=Get-Content $txFile -Raw -Encoding UTF8|ConvertFrom-Json;$installer=[string]$tx.installer;if(-not(Test-Path -LiteralPath $installer)){throw 'Prepared installer path disappeared before activation.'};Update-Tx 'INSTALLING' 'verified-package-installer-running'
 Write-UpdateStatus 60 'installing' 'Installing the verified YOMI package...'
 $installerScript=Join-Path (Split-Path $installer -Parent) 'installer\install.ps1'
 if(-not(Test-Path -LiteralPath $installerScript -PathType Leaf)){throw 'Prepared installer script disappeared before activation.'}
 $powershell=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
 $activationPsi=New-Object System.Diagnostics.ProcessStartInfo
 $activationPsi.FileName=$powershell
 $activationPsi.Arguments='-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "'+$installerScript+'" -UpdateMode'+$(if([string]::IsNullOrWhiteSpace($StatusFile)){''}else{' -UpdateStatusFile "'+$StatusFile+'"'})
 $activationPsi.UseShellExecute=$false
 $activationPsi.CreateNoWindow=$true
 $activationPsi.WindowStyle=[System.Diagnostics.ProcessWindowStyle]::Hidden
 $activation=New-Object System.Diagnostics.Process;$activation.StartInfo=$activationPsi
 try{
   if(-not $activation.Start()){throw 'Could not start the verified YOMI installer.'}
   $activationTimeoutMs=12600000
   if(-not $activation.WaitForExit($activationTimeoutMs)){try{$activation.Kill()}catch{};try{$activation.WaitForExit()}catch{};Update-Tx 'INSTALL_FAILED' 'installer-timeout';throw 'YOMI installer exceeded the 3.5-hour update activation ceiling.'}
   $activationExit=[int]$activation.ExitCode
 }finally{if($null -ne $activation){$activation.Dispose()}}
 if($activationExit -ne 0){Update-Tx 'INSTALL_FAILED' ('installer-exit-'+$activationExit);throw ('YOMI installer exited with code '+$activationExit+'. Last-known-good remains available.')}
 Write-UpdateStatus 94 'verifying-install' 'Verifying the installed YOMI control plane...'
 Update-Tx 'VERIFYING' 'installer-finished-control-plane-health-check'
 $health=& powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File $deploy -VerifyInstalled -ExpectedVersion $latest -InstallRoot $installRoot -DataRoot $dataRoot 2>&1
 if($LASTEXITCODE -ne 0){
   if($deploymentPolicy -ne 'Verified rollback'){Update-Tx 'HEALTH_FAILED' 'verify-only-policy-post-install-health-failed';throw 'The new installation failed health verification. Verify only policy does not keep an automatic rollback snapshot.'}
   Write-UpdateStatus 96 'rolling-back' 'Installed health check failed. Restoring the last working YOMI installation...'
   Update-Tx 'ROLLING_BACK' 'post-install-health-failed'
   $rb=& powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File $deploy -Rollback -InstallRoot $installRoot -DataRoot $dataRoot 2>&1
   if($LASTEXITCODE -ne 0){Update-Tx 'ROLLBACK_REQUIRED' ($rb -join ' ');throw ('Automatic rollback could not complete: '+($rb -join ' '))}
   Write-UpdateStatus 100 'rolled-back' 'The update failed health verification and YOMI restored the last working installation.'
   exit 4
 }
 Update-Tx 'HEALTHY' 'package-and-installed-control-plane-verified'
 Write-UpdateStatus 98 'restarting' "YOMI $latest is installed and verified. Opening YOMI..."
 $restartConfirmed=$false
 try{
   $appDir=Join-Path $installRoot 'app'
   $launcher=Join-Path $appDir 'YomiLauncher.exe'
   if(-not(Test-Path -LiteralPath $launcher -PathType Leaf)){throw 'The installed YOMI launcher is missing.'}
   $started=Start-Process -FilePath $launcher -ArgumentList 'controller' -WorkingDirectory $appDir -PassThru
   for($i=0;$i -lt 30 -and -not $restartConfirmed;$i++){
     Start-Sleep -Milliseconds 150
     foreach($p in @(Get-CimInstance Win32_Process -Filter "Name='YomiControllerWpf.exe'" -ErrorAction SilentlyContinue)){
       try{
         if($p.ExecutablePath -and [IO.Path]::GetFullPath([string]$p.ExecutablePath) -eq [IO.Path]::GetFullPath((Join-Path $appDir 'YomiControllerWpf.exe'))){
           $restartConfirmed=$true;break
         }
       }catch{}
     }
   }
 }catch{}
 if($restartConfirmed){
   Write-UpdateStatus 100 'complete' "YOMI $latest is installed, verified, and reopened."
 }else{
   Write-UpdateStatus 100 'complete' "YOMI $latest is installed and verified. Automatic reopen was not confirmed."
 }
 Msg "YOMI $latest passed package verification and the post-install control-plane health proof." ([Windows.Forms.MessageBoxIcon]::Information)
}catch{Write-UpdateStatus 100 'error' ("Update failed: "+$_.Exception.Message);Msg ("YOMI could not complete the update deployment.`r`n`r`n"+$_.Exception.Message) ([Windows.Forms.MessageBoxIcon]::Warning);exit 1}
finally{if($ownsMutex){try{$updateMutex.ReleaseMutex()}catch{}};if($updateMutex){$updateMutex.Dispose()}}
