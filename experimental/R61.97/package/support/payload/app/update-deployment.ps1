param(
    [switch]$Prepare,
    [switch]$Snapshot,
    [switch]$VerifyInstalled,
    [switch]$Rollback,
    [switch]$Analyze,
    [string]$PackagePath,
    [string]$ExtractRoot,
    [string]$ExpectedVersion,
    [string]$ExpectedPackageHash,
    [string]$InstallRoot,
    [string]$DataRoot,
    [string]$SnapshotPath,
    [switch]$CopyReport,
    [switch]$OpenReport
)

$ErrorActionPreference='Stop'
Set-StrictMode -Version 2.0
if([string]::IsNullOrWhiteSpace($InstallRoot)){$InstallRoot=Join-Path $env:ProgramFiles 'YOMI'}
if([string]::IsNullOrWhiteSpace($DataRoot)){$DataRoot=Join-Path $env:LOCALAPPDATA 'YOMI'}
$stateRoot=Join-Path $DataRoot 'state';$updateRoot=Join-Path $DataRoot 'updates';$txFile=Join-Path $stateRoot 'update-transaction.json'
New-Item -ItemType Directory -Path $stateRoot,$updateRoot -Force|Out-Null

function Write-Utf8([string]$Path,[string]$Text){$dir=Split-Path $Path -Parent;if($dir){New-Item -ItemType Directory -Path $dir -Force|Out-Null};$tmp=$Path+'.tmp-'+[Guid]::NewGuid().ToString('N');[IO.File]::WriteAllText($tmp,$Text,[Text.UTF8Encoding]::new($false));Move-Item -LiteralPath $tmp -Destination $Path -Force}
function Read-Json([string]$Path){if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){return $null};try{return Get-Content -LiteralPath $Path -Raw -Encoding UTF8|ConvertFrom-Json}catch{return $null}}
function Hash([string]$Path){return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Write-Tx($Obj){$Obj.updated_utc=[DateTime]::UtcNow.ToString('o');Write-Utf8 $txFile ($Obj|ConvertTo-Json -Depth 12)}
function Set-State([string]$State,[string]$Reason){$tx=Read-Json $txFile;if($null -eq $tx){$tx=[pscustomobject]@{schema=1;transaction_id=[Guid]::NewGuid().ToString('N')}};$tx.state=$State;$tx.reason=$Reason;Write-Tx $tx;return $tx}
function VersionText([string]$Root){$p=Join-Path $Root 'VERSION.txt';if(-not(Test-Path $p)){return '0.0'};$m=[regex]::Match((Get-Content $p -Raw),'\d+(?:\.\d+){1,3}');if($m.Success){return $m.Value};return '0.0'}
function Test-ZipPath([string]$Name){if([string]::IsNullOrWhiteSpace($Name)){return $false};if($Name.StartsWith('/') -or $Name.StartsWith('\')){return $false};if($Name -match '^[A-Za-z]:'){return $false};$parts=$Name.Replace('\','/').Split('/');return -not($parts -contains '..')}
function Test-Package([string]$Zip,[string]$Out,[string]$Version,[string]$OuterHash){
    if(-not(Test-Path -LiteralPath $Zip -PathType Leaf)){throw 'Update package is missing.'}
    if($OuterHash -and (Hash $Zip) -ne $OuterHash.ToLowerInvariant()){throw 'Outer package SHA-256 does not match the update manifest.'}
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive=[IO.Compression.ZipFile]::OpenRead($Zip)
    try{
        $names=@{};foreach($entry in $archive.Entries){if(-not(Test-ZipPath $entry.FullName)){throw ('Unsafe ZIP path: '+$entry.FullName)};if($names.ContainsKey($entry.FullName)){throw ('Duplicate ZIP path: '+$entry.FullName)};$names[$entry.FullName]=$true}
        foreach($required in @('INSTALL YOMI.cmd','installer/install.ps1','installer/build-manifest.json','payload/app/supervisor.ps1','payload/app/update.ps1')){if(-not $names.ContainsKey($required)){throw ('Required update payload entry missing: '+$required)}}
    }finally{$archive.Dispose()}
    Remove-Item -LiteralPath $Out -Recurse -Force -ErrorAction SilentlyContinue;New-Item -ItemType Directory -Path $Out -Force|Out-Null
    Expand-Archive -LiteralPath $Zip -DestinationPath $Out -Force
    $manifestPath=Join-Path $Out 'installer\build-manifest.json';$manifest=Read-Json $manifestPath;if($null -eq $manifest -or $null -eq $manifest.files){throw 'Package build manifest is missing or malformed.'}
    $manifestVersion=([string]$manifest.version).TrimStart('v');if($Version -and $manifestVersion -ne $Version){throw "Package build manifest version $manifestVersion does not match expected $Version."}
    $verified=0;$manifestNames=@{};foreach($prop in $manifest.files.PSObject.Properties){$rel=[string]$prop.Name;if(-not(Test-ZipPath $rel)){throw ('Unsafe build-manifest path: '+$rel)};$manifestNames[$rel.Replace('\','/')]=$true;$path=Join-Path $Out ($rel.Replace('/','\'));if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw ('Manifest file missing after extraction: '+$rel)};$expectedBytes=[int64]$prop.Value.bytes;$expectedHash=([string]$prop.Value.sha256).ToLowerInvariant();$item=Get-Item -LiteralPath $path;if($item.Length -ne $expectedBytes){throw ('Manifest size mismatch: '+$rel)};if((Hash $path) -ne $expectedHash){throw ('Manifest SHA-256 mismatch: '+$rel)};$verified++}
    foreach($file in @(Get-ChildItem -LiteralPath $Out -File -Recurse)){ $rel=$file.FullName.Substring($Out.Length).TrimStart('\').Replace('\','/');if($rel -eq 'installer/build-manifest.json'){continue};if(-not $manifestNames.ContainsKey($rel)){throw ('Unmanifested package file: '+$rel)}}
    return [pscustomobject]@{version=$manifestVersion;verified_files=$verified;installer=(Join-Path $Out 'INSTALL YOMI.cmd');manifest=$manifestPath}
}
function Copy-Tree([string]$From,[string]$To){if(-not(Test-Path -LiteralPath $From)){throw ('Source tree missing: '+$From)};Remove-Item -LiteralPath $To -Recurse -Force -ErrorAction SilentlyContinue;New-Item -ItemType Directory -Path $To -Force|Out-Null;$robo=Join-Path $env:SystemRoot 'System32\robocopy.exe';if(Test-Path $robo){& $robo $From $To /E /COPY:DAT /DCOPY:T /R:1 /W:1 /XJ /NFL /NDL /NJH /NJS /NP|Out-Null;if($LASTEXITCODE -gt 7){throw ('Last-known-good copy failed with robocopy exit '+$LASTEXITCODE)}}else{Copy-Item -Path (Join-Path $From '*') -Destination $To -Recurse -Force}}
function Save-Lkg([string]$Root,[string]$Explicit){if(-not(Test-Path -LiteralPath $Root)){return $null};$version=VersionText $Root;$dest=$Explicit;if([string]::IsNullOrWhiteSpace($dest)){$dest=Join-Path $updateRoot ('last-known-good\'+$version+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))};Copy-Tree $Root $dest;$meta=[pscustomobject]@{schema=1;version=$version;created_utc=[DateTime]::UtcNow.ToString('o');source=$Root;path=$dest};Write-Utf8 (Join-Path $dest 'YOMI-LKG.json') ($meta|ConvertTo-Json -Depth 5);$parent=Split-Path $dest -Parent;$dirs=@(Get-ChildItem -LiteralPath $parent -Directory -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending);for($i=1;$i -lt $dirs.Count;$i++){Remove-Item -LiteralPath $dirs[$i].FullName -Recurse -Force -ErrorAction SilentlyContinue};return $dest}
function Invoke-BoundedWpfHealthProbe([string]$Exe,[int]$TimeoutMs=45000){
    if(-not(Test-Path -LiteralPath $Exe -PathType Leaf)){return [pscustomobject]@{exit=$null;detail='executable missing';timed_out=$false}}
    $diag=Join-Path $env:TEMP ('YOMI-WPF-SELFTEST-'+[Guid]::NewGuid().ToString('N')+'.txt')
    $oldDiag=$env:YOMI_SELFTEST_DIAGNOSTIC
    $hadDiag=Test-Path Env:\YOMI_SELFTEST_DIAGNOSTIC
    $p=$null
    try{
        $env:YOMI_SELFTEST_DIAGNOSTIC=$diag
        $p=Start-Process -FilePath $Exe -ArgumentList '--self-test' -WorkingDirectory (Split-Path $Exe -Parent) -PassThru
    }finally{
        if($hadDiag){$env:YOMI_SELFTEST_DIAGNOSTIC=$oldDiag}else{Remove-Item Env:\YOMI_SELFTEST_DIAGNOSTIC -ErrorAction SilentlyContinue}
    }
    try{
        if(-not $p.WaitForExit($TimeoutMs)){
            try{$p.Kill()}catch{}
            try{$p.WaitForExit()}catch{}
            return [pscustomobject]@{exit=124;detail=('timeout after '+$TimeoutMs+' ms');timed_out=$true}
        }
        $detail=''
        if(Test-Path -LiteralPath $diag -PathType Leaf){try{$detail=(Get-Content -LiteralPath $diag -Raw -Encoding UTF8).Trim()}catch{}}
        return [pscustomobject]@{exit=[int]$p.ExitCode;detail=$detail;timed_out=$false}
    }finally{
        if($null -ne $p){$p.Dispose()}
        Remove-Item -LiteralPath $diag -Force -ErrorAction SilentlyContinue
    }
}
function Test-Installed([string]$Root,[string]$Version){
    $fail=New-Object Collections.Generic.List[string]
    if(-not(Test-Path -LiteralPath $Root -PathType Container)){$fail.Add('install-root-missing')}
    $actual=VersionText $Root
    if($Version -and $actual -ne $Version){$fail.Add('version-mismatch')}
    foreach($rel in @('runtime\mpv\mpv.exe','runtime\yt-dlp\yt-dlp.exe','app\PriorityRun.exe','app\ArtworkEdgeDetector.exe','app\YomiLauncher.exe','app\music.lua','app\server.ps1','app\controller.ps1','app\update.ps1','app\common.ps1')){
        if(-not(Test-Path -LiteralPath (Join-Path $Root $rel) -PathType Leaf)){$fail.Add('missing:'+ $rel)}
    }
    $parseErrors=0
    foreach($ps in @(Get-ChildItem -LiteralPath (Join-Path $Root 'app') -Filter '*.ps1' -File -ErrorAction SilentlyContinue)){
        try{$tokens=$null;$errors=$null;[void][Management.Automation.Language.Parser]::ParseFile($ps.FullName,[ref]$tokens,[ref]$errors);$parseErrors+=@($errors).Count}catch{$parseErrors++}
    }
    if($parseErrors -gt 0){$fail.Add('powershell-parse-errors:'+ $parseErrors)}
    $wpf=Join-Path $Root 'app\YomiControllerWpf.exe'
    $wpfExit=$null
    if(Test-Path -LiteralPath $wpf -PathType Leaf){
        try{
            $healthProbe=Invoke-BoundedWpfHealthProbe -Exe $wpf
            $wpfExit=$healthProbe.exit
            if($healthProbe.timed_out){$fail.Add('wpf-self-test-timeout')}
            elseif($null -eq $wpfExit){$fail.Add('wpf-self-test-missing')}
            elseif([int]$wpfExit -ne 0){
                if([string]::IsNullOrWhiteSpace([string]$healthProbe.detail)){$fail.Add('wpf-self-test:'+ $wpfExit)}
                else{$fail.Add('wpf-self-test:'+ $wpfExit + ':' + [string]$healthProbe.detail)}
            }
        }catch{$fail.Add('wpf-self-test-launch:'+ $_.Exception.Message)}
    }else{$fail.Add('missing:app\YomiControllerWpf.exe')}
    return [pscustomobject]@{healthy=($fail.Count -eq 0);version=$actual;failures=$fail.ToArray();powershell_parse_errors=$parseErrors;wpf_self_test_exit=$wpfExit}
}
function Restore-Lkg([string]$Root,[string]$Snap){
    if(-not(Test-Path -LiteralPath $Snap -PathType Container)){throw 'Last-known-good snapshot is missing.'}
    $identity=[Security.Principal.WindowsIdentity]::GetCurrent()
    $principal=New-Object Security.Principal.WindowsPrincipal($identity)
    $programFiles=[IO.Path]::GetFullPath($env:ProgramFiles).TrimEnd('\')
    $rootFull=[IO.Path]::GetFullPath($Root).TrimEnd('\')
    $requiresElevation=$rootFull.StartsWith($programFiles,[StringComparison]::OrdinalIgnoreCase)
    if($requiresElevation -and -not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){
        if(-not $PSCommandPath){throw 'Rollback requires an elevated PowerShell process.'}
        $psi=New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $psi.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'" -Rollback -InstallRoot "'+$Root+'" -DataRoot "'+$DataRoot+'" -SnapshotPath "'+$Snap+'"'
        $psi.Verb='runas';$psi.UseShellExecute=$true
        $elevated=New-Object System.Diagnostics.Process;$elevated.StartInfo=$psi
        try{
            if(-not $elevated.Start()){throw 'Could not start elevated rollback.'}
            $rollbackTimeoutMs=3600000
            if(-not $elevated.WaitForExit($rollbackTimeoutMs)){
                try{$elevated.Kill()}catch{};try{$elevated.WaitForExit()}catch{}
                throw 'Elevated rollback exceeded the one-hour safety timeout.'
            }
            if([int]$elevated.ExitCode -ne 0){throw ('Elevated rollback failed with exit '+[int]$elevated.ExitCode)}
            return
        }finally{if($null -ne $elevated){$elevated.Dispose()}}
    }
    Copy-Tree $Snap $Root
    Remove-Item -LiteralPath (Join-Path $Root 'YOMI-LKG.json') -Force -ErrorAction SilentlyContinue
}
if($Prepare){$probe=Test-Package $PackagePath $ExtractRoot $ExpectedVersion $ExpectedPackageHash;$tx=[pscustomobject]@{schema=1;transaction_id=[Guid]::NewGuid().ToString('N');state='PACKAGE_VERIFIED';reason='outer-and-inner-manifest-verified';from_version=(VersionText $InstallRoot);to_version=$probe.version;package_path=$PackagePath;package_sha256=(Hash $PackagePath);staging_path=$ExtractRoot;verified_files=$probe.verified_files;installer=$probe.installer;snapshot_path=$null;created_utc=[DateTime]::UtcNow.ToString('o');updated_utc=[DateTime]::UtcNow.ToString('o')};Write-Tx $tx;$tx|ConvertTo-Json -Depth 8;exit 0}
if($Snapshot){$snap=Save-Lkg $InstallRoot $SnapshotPath;$tx=Read-Json $txFile;if($null -eq $tx){throw 'No prepared update transaction exists.'};$tx.snapshot_path=$snap;$tx.state='LKG_SNAPSHOTTED';$tx.reason='last-known-good-snapshot-complete';Write-Tx $tx;$tx|ConvertTo-Json -Depth 8;exit 0}
if($VerifyInstalled){$tx=Read-Json $txFile;$version=$ExpectedVersion;if([string]::IsNullOrWhiteSpace($version) -and $tx){$version=[string]$tx.to_version};$health=Test-Installed $InstallRoot $version;if($tx){$tx.health=$health;$tx.state=$(if($health.healthy){'HEALTHY'}else{'HEALTH_FAILED'});$tx.reason=$(if($health.healthy){'installed-control-plane-verified'}else{($health.failures -join ',')});Write-Tx $tx};$health|ConvertTo-Json -Depth 8;if($health.healthy){exit 0}else{exit 4}}
if($Rollback){$tx=Read-Json $txFile;$snap=$SnapshotPath;if([string]::IsNullOrWhiteSpace($snap) -and $tx){$snap=[string]$tx.snapshot_path};Restore-Lkg $InstallRoot $snap;$health=Test-Installed $InstallRoot '';if($tx){$tx.state='ROLLED_BACK';$tx.reason='last-known-good-restored';$tx.rollback_health=$health;Write-Tx $tx};if($health.healthy){exit 0}else{exit 5}}
if($Analyze){$tx=Read-Json $txFile;if($null -eq $tx){$tx=[pscustomobject]@{schema=1;state='NONE';reason='no update deployment transaction has been recorded'}};$reports=Join-Path $DataRoot 'reports';New-Item -ItemType Directory -Path $reports -Force|Out-Null;$path=Join-Path $reports ('YOMI-update-deployment-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json');Write-Utf8 $path ($tx|ConvertTo-Json -Depth 12);Write-Host ('UPDATE_STATE='+[string]$tx.state);Write-Host ('REASON='+[string]$tx.reason);Write-Host ('REPORT='+$path);if($CopyReport){try{Set-Clipboard $path}catch{}};if($OpenReport){try{Start-Process notepad.exe ('"'+$path+'"')}catch{}};exit 0}
