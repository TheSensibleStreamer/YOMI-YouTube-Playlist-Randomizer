param([switch]$Manual)

$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
$installRoot=Split-Path $PSScriptRoot -Parent
$dataRoot=Join-Path $env:LOCALAPPDATA 'YOMI';$stateRoot=Join-Path $dataRoot 'state';$updateRoot=Join-Path $dataRoot 'updates'
$lastCheckFile=Join-Path $stateRoot 'last-update-check.txt';$lastPromptFile=Join-Path $stateRoot 'last-update-prompt.json'
$manifestUri='https://raw.githubusercontent.com/TheSensibleStreamer/YOMI-YouTube-Playlist-Randomizer/main/update.json'
$deploy=Join-Path $PSScriptRoot 'update-deployment.ps1';$txFile=Join-Path $stateRoot 'update-transaction.json'
New-Item -ItemType Directory -Path $stateRoot,$updateRoot -Force|Out-Null
$deploymentPolicy='Verified rollback';$configPath=Join-Path $dataRoot 'config.json';if(Test-Path $configPath){try{$uc=Get-Content $configPath -Raw -Encoding UTF8|ConvertFrom-Json;if($uc.PSObject.Properties['update_deployment_policy']){$deploymentPolicy=[string]$uc.update_deployment_policy}}catch{}}
$created=$false;$updateMutex=New-Object Threading.Mutex($true,'Local\YOMI_Update_Check',[ref]$created);$ownsMutex=$created
if(-not $created){if(-not $Manual -or -not $updateMutex.WaitOne(5000)){$updateMutex.Dispose();exit 0};$ownsMutex=$true}
function Version([string]$Text){$m=[regex]::Match($Text,'\d+(?:\.\d+){1,3}');if(-not $m.Success){return [version]'0.0'};try{return [version]$m.Value}catch{return [version]'0.0'}}
function Msg([string]$Text,[Windows.Forms.MessageBoxIcon]$Icon){if($Manual){[Windows.Forms.MessageBox]::Show($Text,'YOMI Update',[Windows.Forms.MessageBoxButtons]::OK,$Icon)|Out-Null}}
function Update-Tx([string]$State,[string]$Reason){if(-not(Test-Path $txFile)){return};try{$tx=Get-Content $txFile -Raw -Encoding UTF8|ConvertFrom-Json;$tx.state=$State;$tx.reason=$Reason;$tx.updated_utc=[DateTime]::UtcNow.ToString('o');[IO.File]::WriteAllText($txFile,($tx|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))}catch{}}
try{
 [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
 if(-not(Test-Path -LiteralPath $deploy)){throw 'The transactional update deployment engine is missing.'}
 $versionFile=Join-Path $installRoot 'VERSION.txt';$current=Version ($(if(Test-Path $versionFile){Get-Content $versionFile -Raw}else{'0.0'}));$headers=@{'User-Agent'=('YOMI-'+$current.ToString()+'-Updater')}
 $manifest=Invoke-RestMethod -Uri $manifestUri -Headers $headers -UseBasicParsing -TimeoutSec 20;Set-Content $lastCheckFile ((Get-Date).ToString('o')) -Encoding ASCII;$latest=Version ([string]$manifest.version)
 if($latest -le $current){Msg "YOMI $current is current.`r`n`r`nNo newer public build is available." ([Windows.Forms.MessageBoxIcon]::Information);exit 0}
 if(-not $Manual -and (Test-Path $lastPromptFile)){try{$pp=Get-Content $lastPromptFile -Raw|ConvertFrom-Json;$pt=[DateTime]::Parse([string]$pp.time);if([string]$pp.version -eq $latest.ToString() -and ((Get-Date)-$pt).TotalDays -lt 30){exit 0}}catch{}}
 $notes=[string]$manifest.summary;if([string]::IsNullOrWhiteSpace($notes)){$notes='A newer public YOMI build is available.'}
 $answer=[Windows.Forms.MessageBox]::Show("YOMI $latest is available. You have $current.`r`n`r`n$notes`r`n`r`nDownload, cryptographically verify, rehearse the package, capture last-known-good, install, and verify the new control plane? If installed health fails, YOMI will offer automatic rollback.",'YOMI Update Available',[Windows.Forms.MessageBoxButtons]::YesNo,[Windows.Forms.MessageBoxIcon]::Information)
 if($answer -ne [Windows.Forms.DialogResult]::Yes){[pscustomobject]@{version=$latest.ToString();time=(Get-Date).ToString('o')}|ConvertTo-Json -Compress|Set-Content $lastPromptFile -Encoding ASCII;exit 0}
 $packageName=[string]$manifest.package_name;if([string]::IsNullOrWhiteSpace($packageName) -or $packageName -notmatch '^YOMI-v[0-9.]+\.zip$'){throw 'The public update manifest contains an invalid package name.'}
 $packageUri=[string]$manifest.package_url;$expectedHash=([string]$manifest.sha256).Trim().ToLowerInvariant();if($packageUri -notlike 'https://raw.githubusercontent.com/TheSensibleStreamer/YOMI-YouTube-Playlist-Randomizer/*'){throw 'The public update manifest points outside the official YOMI repository.'};if($expectedHash -notmatch '^[a-f0-9]{64}$'){throw 'The public update manifest contains an invalid SHA-256 value.'}
 $packagePath=Join-Path $updateRoot $packageName;$downloading=$packagePath+'.downloading';Remove-Item $downloading -Force -ErrorAction SilentlyContinue
 Invoke-WebRequest -Uri $packageUri -Headers $headers -UseBasicParsing -TimeoutSec 120 -OutFile $downloading;$actual=(Get-FileHash $downloading -Algorithm SHA256).Hash.ToLowerInvariant();if($actual -ne $expectedHash){Remove-Item $downloading -Force -ErrorAction SilentlyContinue;throw "Update integrity check failed. Expected $expectedHash but received $actual."};Move-Item $downloading $packagePath -Force
 $extractRoot=Join-Path $updateRoot ('ready-'+$latest.ToString());$prep=& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deploy -Prepare -PackagePath $packagePath -ExtractRoot $extractRoot -ExpectedVersion $latest.ToString() -ExpectedPackageHash $expectedHash -InstallRoot $installRoot -DataRoot $dataRoot 2>&1;if($LASTEXITCODE -ne 0){Update-Tx 'PACKAGE_REJECTED' ($prep -join ' ');throw ('Package rehearsal failed: '+($prep -join ' '))}
 if($deploymentPolicy -eq 'Verified rollback'){$snap=& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deploy -Snapshot -InstallRoot $installRoot -DataRoot $dataRoot 2>&1;if($LASTEXITCODE -ne 0){throw ('Could not capture last-known-good installation: '+($snap -join ' '))}}else{Update-Tx 'PACKAGE_VERIFIED' 'verify-only-policy-no-lkg-snapshot'}
 $tx=Get-Content $txFile -Raw -Encoding UTF8|ConvertFrom-Json;$installer=[string]$tx.installer;if(-not(Test-Path -LiteralPath $installer)){throw 'Prepared installer path disappeared before activation.'};Update-Tx 'INSTALLING' 'verified-package-installer-running'
 $activationPsi=New-Object System.Diagnostics.ProcessStartInfo
 $activationPsi.FileName=$env:ComSpec
 $activationPsi.Arguments='/d /s /c ""'+$installer+'""'
 $activationPsi.UseShellExecute=$false
 $activation=New-Object System.Diagnostics.Process;$activation.StartInfo=$activationPsi
 try{
   if(-not $activation.Start()){throw 'Could not start the verified YOMI installer.'}
   $activationTimeoutMs=12600000
   if(-not $activation.WaitForExit($activationTimeoutMs)){try{$activation.Kill()}catch{};try{$activation.WaitForExit()}catch{};Update-Tx 'INSTALL_FAILED' 'installer-timeout';throw 'YOMI installer exceeded the 3.5-hour update activation ceiling.'}
   $activationExit=[int]$activation.ExitCode
 }finally{if($null -ne $activation){$activation.Dispose()}}
 if($activationExit -ne 0){Update-Tx 'INSTALL_FAILED' ('installer-exit-'+$activationExit);throw ('YOMI installer exited with code '+$activationExit+'. Last-known-good remains available.')}
 Update-Tx 'VERIFYING' 'installer-finished-control-plane-health-check'
 $health=& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deploy -VerifyInstalled -ExpectedVersion $latest.ToString() -InstallRoot $installRoot -DataRoot $dataRoot 2>&1
 if($LASTEXITCODE -ne 0){
   if($deploymentPolicy -ne 'Verified rollback'){Update-Tx 'HEALTH_FAILED' 'verify-only-policy-post-install-health-failed';throw 'The new installation failed health verification. Verify only policy does not keep an automatic rollback snapshot.'}
   $rollback=[Windows.Forms.MessageBox]::Show("YOMI $latest installed but failed the post-install health proof.`r`n`r`n$($health -join "`r`n")`r`n`r`nRestore the last-known-good installation now?",'YOMI Update Health Failure',[Windows.Forms.MessageBoxButtons]::YesNo,[Windows.Forms.MessageBoxIcon]::Warning)
   if($rollback -eq [Windows.Forms.DialogResult]::Yes){Update-Tx 'ROLLING_BACK' 'post-install-health-failed';$rb=& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deploy -Rollback -InstallRoot $installRoot -DataRoot $dataRoot 2>&1;if($LASTEXITCODE -ne 0){Update-Tx 'ROLLBACK_REQUIRED' ($rb -join ' ');throw ('Automatic rollback could not complete: '+($rb -join ' '))};Msg 'The failed update was rolled back to the last-known-good YOMI installation.' ([Windows.Forms.MessageBoxIcon]::Information);exit 4}
   Update-Tx 'ROLLBACK_REQUIRED' 'post-install-health-failed-user-deferred-rollback';throw 'The new installation failed health verification. Last-known-good is preserved for rollback.'
 }
 Update-Tx 'HEALTHY' 'package-and-installed-control-plane-verified';Msg "YOMI $latest passed package verification and the post-install control-plane health proof." ([Windows.Forms.MessageBoxIcon]::Information)
}catch{Msg ("YOMI could not complete the update deployment.`r`n`r`n"+$_.Exception.Message) ([Windows.Forms.MessageBoxIcon]::Warning);exit 1}
finally{if($ownsMutex){try{$updateMutex.ReleaseMutex()}catch{}};if($updateMutex){$updateMutex.Dispose()}}
