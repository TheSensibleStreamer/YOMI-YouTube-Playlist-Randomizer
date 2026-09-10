param([switch]$Elevated)
$ErrorActionPreference='Stop'
Set-StrictMode -Version 2.0

$Product='YOMI 4.2.0.8 R61.97 experimental installer'
$InstallRoot=Join-Path $env:ProgramFiles 'YOMI'
$DataRoot=Join-Path $env:LOCALAPPDATA 'YOMI'
$RepoRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$BaselineZip=Join-Path $RepoRoot 'YOMI-v4.2.0.7.zip'
$SupportRoot=Join-Path $PSScriptRoot 'package\support'
$FocusedRoot=Join-Path $PSScriptRoot 'package\focused-update'
$BaselineSha='f1024ed394f3a0a322dfe38abc9cf23637e695ca13e32b0231f30a8c056b456e'

function Test-Admin {
    $id=[Security.Principal.WindowsIdentity]::GetCurrent()
    $p=New-Object Security.Principal.WindowsPrincipal($id)
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}
function Start-ChildAndWait([string]$File,[string]$Arguments,[bool]$Shell,[string]$Verb){
    $psi=New-Object Diagnostics.ProcessStartInfo
    $psi.FileName=$File
    $psi.Arguments=$Arguments
    $psi.UseShellExecute=$Shell
    if($Verb){$psi.Verb=$Verb}
    if(-not $Shell){$psi.CreateNoWindow=$false}
    $proc=New-Object Diagnostics.Process
    $proc.StartInfo=$psi
    try{
        if(-not $proc.Start()){throw ('Could not start '+$File)}
        if(-not $proc.WaitForExit(1800000)){try{& (Join-Path $env:SystemRoot 'System32\taskkill.exe') /PID $proc.Id /T /F 2>$null|Out-Null}catch{};throw ('Child process exceeded 30-minute safety ceiling: '+$File)}
        return [int]$proc.ExitCode
    }finally{$proc.Dispose()}
}

if(-not(Test-Admin)){
    try{
        $ps=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $args='-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'" -Elevated'
        $code=Start-ChildAndWait $ps $args $true 'runas'
        if($code -eq 0){
            $launcher=Join-Path $InstallRoot 'app\YomiLauncher.exe'
            if(Test-Path -LiteralPath $launcher -PathType Leaf){Start-Process -FilePath $launcher -ArgumentList 'controller' -WorkingDirectory (Split-Path $launcher -Parent)|Out-Null}
        }
        exit $code
    }catch{Write-Host ('R61.97 elevation failed: '+$_.Exception.Message) -ForegroundColor Red;exit 5}
}

New-Item -ItemType Directory -Path $DataRoot -Force|Out-Null
$Log=Join-Path $DataRoot 'install-experimental-r61.97.log'
try{Start-Transcript -Path $Log -Force|Out-Null}catch{}
$Work=Join-Path $env:TEMP ('YOMI-R61-97-CUMULATIVE-'+[Guid]::NewGuid().ToString('N'))
$Candidate=Join-Path $env:ProgramFiles ('YOMI.r6197-candidate-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
$Backup=Join-Path $env:ProgramFiles ('YOMI.r6197-rollback-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
$SwapStarted=$false
$HadOriginalAtEntry=Test-Path -LiteralPath $InstallRoot -PathType Container
$FreshBaselineInstalled=$false
$mutex=$null

function Sha256([string]$Path){return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Assert-Hash([string]$Path,[string]$Expected,[string]$Label){
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw ($Label+' is missing: '+$Path)}
    $actual=Sha256 $Path
    if($actual -ne $Expected){throw ($Label+' SHA-256 mismatch. Expected '+$Expected+' but found '+$actual)}
}
function Resolve-Csc {
    foreach($p in @((Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),(Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'))){if(Test-Path -LiteralPath $p -PathType Leaf){return $p}}
    throw '.NET Framework C# compiler (csc.exe) was not found.'
}
function Get-WpfReferences {
    $names=@('PresentationFramework','PresentationCore','WindowsBase','System.Xaml','WindowsFormsIntegration','System.Windows.Forms','System.Drawing','System.Web.Extensions','System.Xml','System','System.Core')
    $seen=@{};$queue=@()
    foreach($name in $names){
        Add-Type -AssemblyName $name -ErrorAction Stop
        $a=[AppDomain]::CurrentDomain.GetAssemblies()|Where-Object{$_.GetName().Name -eq $name}|Select-Object -First 1
        if($null -eq $a){throw ('Required framework assembly did not load: '+$name)}
        if(-not $seen.ContainsKey($a.FullName)){$seen[$a.FullName]=$a;$queue+=,$a}
    }
    while($queue.Count -gt 0){
        $a=$queue[0];if($queue.Count -eq 1){$queue=@()}else{$queue=@($queue[1..($queue.Count-1)])}
        foreach($rn in @($a.GetReferencedAssemblies())){
            if($rn.Name -eq 'mscorlib' -or $seen.ContainsKey($rn.FullName)){continue}
            try{$d=[Reflection.Assembly]::Load($rn)}catch{continue}
            if($d -and $d.Location -and -not $seen.ContainsKey($d.FullName)){$seen[$d.FullName]=$d;$queue+=,$d}
        }
    }
    return @($seen.Values|ForEach-Object{$_.Location}|Where-Object{$_}|Sort-Object -Unique)
}
function Assert-PowerShellParseTree([string]$Root){
    foreach($f in @(Get-ChildItem -LiteralPath $Root -Filter '*.ps1' -File -Recurse)){
        $tokens=$null;$errors=$null
        [void][Management.Automation.Language.Parser]::ParseFile($f.FullName,[ref]$tokens,[ref]$errors)
        if($errors -and $errors.Count -gt 0){throw ('PowerShell 5.1 parse failed: '+$f.FullName+' :: '+$errors[0].Message)}
    }
}
function Assert-Foundation([string]$Expanded){
    $manifestPath=Join-Path $Expanded 'support-manifest.json'
    if(-not(Test-Path -LiteralPath $manifestPath -PathType Leaf)){throw 'Foundation manifest is missing.'}
    $m=[IO.File]::ReadAllText($manifestPath)|ConvertFrom-Json
    if([int]$m.schema -ne 1 -or [string]$m.revision -ne 'R61.97-support'){throw 'Foundation manifest identity mismatch.'}
    $seen=@{}
    foreach($item in @($m.files)){
        $rel=[string]$item.path
        if($rel.Contains('\') -or $rel.Contains(':') -or [IO.Path]::IsPathRooted($rel) -or $rel.Contains('../')){throw ('Unsafe foundation path: '+$rel)}
        $full=Join-Path $Expanded ($rel.Replace('/','\'))
        if(-not(Test-Path -LiteralPath $full -PathType Leaf)){throw ('Foundation file missing: '+$rel)}
        if([int64](Get-Item -LiteralPath $full).Length -ne [int64]$item.bytes){throw ('Foundation byte count mismatch: '+$rel)}
        if((Sha256 $full) -ne ([string]$item.sha256).ToLowerInvariant()){throw ('Foundation hash mismatch: '+$rel)}
        $seen[$rel.ToLowerInvariant()]=$true
    }
    $actual=@(Get-ChildItem -LiteralPath (Join-Path $Expanded 'payload') -File -Recurse)
    if($actual.Count -ne $seen.Count){throw ('Foundation exact-file-set mismatch: manifest '+$seen.Count+', payload '+$actual.Count)}
    foreach($f in $actual){
        $rel=$f.FullName.Substring($Expanded.Length).TrimStart('\').Replace('\','/').ToLowerInvariant()
        if(-not $seen.ContainsKey($rel)){throw ('Unmanifested foundation file: '+$rel)}
    }
}

function Assert-FocusedPackage([string]$Root){
    $manifestPath=Join-Path $Root 'package-manifest.json'
    if(-not(Test-Path -LiteralPath $manifestPath -PathType Leaf)){throw 'R61.97 focused package manifest is missing.'}
    $m=[IO.File]::ReadAllText($manifestPath)|ConvertFrom-Json
    if([int]$m.schema -ne 1 -or [string]$m.revision -ne 'R61.97' -or [string]$m.base_revision -ne 'R61.96.2'){throw 'R61.97 focused package identity mismatch.'}
    $seen=@{}
    foreach($item in @($m.files)){
        $rel=[string]$item.path
        if($rel.Contains('\') -or $rel.Contains(':') -or [IO.Path]::IsPathRooted($rel) -or $rel.Contains('../')){throw ('Unsafe focused package path: '+$rel)}
        $full=Join-Path $Root ($rel.Replace('/','\'))
        if(-not(Test-Path -LiteralPath $full -PathType Leaf)){throw ('Focused package file missing: '+$rel)}
        if([int64](Get-Item -LiteralPath $full).Length -ne [int64]$item.bytes){throw ('Focused package byte count mismatch: '+$rel)}
        if((Sha256 $full) -ne ([string]$item.sha256).ToLowerInvariant()){throw ('Focused package hash mismatch: '+$rel)}
        $seen[$rel.ToLowerInvariant()]=$true
    }
    $actual=@(Get-ChildItem -LiteralPath $Root -File -Recurse|Where-Object{$_.Name -ne 'package-manifest.json'})
    if($actual.Count -ne $seen.Count){throw ('Focused package exact-file-set mismatch: manifest '+$seen.Count+', files '+$actual.Count)}
    foreach($f in $actual){
        $rel=$f.FullName.Substring($Root.Length).TrimStart('\').Replace('\','/').ToLowerInvariant()
        if(-not $seen.ContainsKey($rel)){throw ('Unmanifested focused package file: '+$rel)}
    }
}
function Invoke-Robocopy([string]$Source,[string]$Destination){
    New-Item -ItemType Directory -Path $Destination -Force|Out-Null
    & (Join-Path $env:SystemRoot 'System32\robocopy.exe') $Source $Destination /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /XJ /NFL /NDL /NJH /NJS /NP | Out-Null
    $rc=[int]$LASTEXITCODE
    if($rc -gt 7){throw ('robocopy failed with exit code '+$rc+' copying '+$Source+' to '+$Destination)}
}
function Compile-WinExe([string]$Source,[string]$Output,[string]$Manifest,[string]$Icon){
    $csc=Resolve-Csc;$refs=@(Get-WpfReferences)
    $args=@('/nologo','/noconfig','/codepage:65001','/target:winexe','/platform:anycpu','/optimize+','/debug-',('/out:"'+$Output+'"'))
    if($Manifest){$args+=('/win32manifest:"'+$Manifest+'"')}
    if($Icon -and (Test-Path -LiteralPath $Icon -PathType Leaf)){$args+=('/win32icon:"'+$Icon+'"')}
    foreach($ref in $refs){$args+=('/reference:"'+$ref+'"')}
    $args+=('"'+$Source+'"')
    $out=& $csc @args 2>&1
    if($LASTEXITCODE -ne 0){throw ('C# compile failed for '+$Source+"`r`n"+($out -join "`r`n"))}
    if(-not(Test-Path -LiteralPath $Output -PathType Leaf)){throw ('Compiler returned success without output: '+$Output)}
}
function Compile-ConsoleExe([string]$Source,[string]$Output,[string[]]$Refs){
    $csc=Resolve-Csc
    $args=@('/nologo','/noconfig','/codepage:65001','/target:exe','/platform:anycpu','/optimize+','/debug-',('/out:"'+$Output+'"'))
    foreach($ref in @($Refs)){if($ref){$args+=('/reference:"'+$ref+'"')}}
    $args+=('"'+$Source+'"')
    $out=& $csc @args 2>&1
    if($LASTEXITCODE -ne 0){throw ('C# compile failed for '+$Source+"`r`n"+($out -join "`r`n"))}
    if(-not(Test-Path -LiteralPath $Output -PathType Leaf)){throw ('Compiler returned success without output: '+$Output)}
}
function Assert-ObsServerCompile([string]$Path){
    $lines=[IO.File]::ReadAllLines($Path)
    $startMarker="`$source = @'";$endMarker="'@";$start=-1;$finish=-1
    for($i=0;$i -lt $lines.Length;$i++){
        if($start -lt 0 -and $lines[$i].Trim() -ceq $startMarker){$start=$i;continue}
        if($start -ge 0 -and $lines[$i].Trim() -ceq $endMarker){$finish=$i;break}
    }
    if($start -lt 0 -or $finish -le ($start+1)){throw 'OBS server embedded C# source block was not found.'}
    $csharp=[string]::Join([Environment]::NewLine,@($lines[($start+1)..($finish-1)]))
    if([string]::IsNullOrWhiteSpace($csharp)){throw 'OBS server embedded C# source block was empty.'}
    try{$types=@(Add-Type -TypeDefinition $csharp -Language CSharp -PassThru -ErrorAction Stop)}catch{throw ('OBS server embedded C# compile failed: '+$_.Exception.Message)}
    $type=$types|Where-Object{$_.Name -match '^YomiObsHttpServerR\d+$'}|Select-Object -First 1
    if($null -eq $type){throw 'Compiled OBS server type was not discovered.'}
}
function Assert-WpfSelfTest([string]$AppRoot){
    $exe=Join-Path $AppRoot 'YomiControllerWpf.exe'
    $diag=Join-Path $Work 'foundation-wpf-selftest.log'
    Remove-Item -LiteralPath $diag -Force -ErrorAction SilentlyContinue
    $old=$env:YOMI_SELFTEST_DIAGNOSTIC
    try{
        $env:YOMI_SELFTEST_DIAGNOSTIC=$diag
        $psi=New-Object Diagnostics.ProcessStartInfo
        $psi.FileName=$exe;$psi.Arguments='--self-test';$psi.WorkingDirectory=$AppRoot;$psi.UseShellExecute=$false;$psi.CreateNoWindow=$true
        $p=New-Object Diagnostics.Process;$p.StartInfo=$psi
        try{
            if(-not $p.Start()){throw 'Foundation WPF self-test could not start.'}
            if(-not $p.WaitForExit(20000)){try{& (Join-Path $env:SystemRoot 'System32\taskkill.exe') /PID $p.Id /T /F 2>$null|Out-Null}catch{};throw 'Foundation WPF self-test exceeded 20 seconds.'}
            if([int]$p.ExitCode -ne 0){$detail='';if(Test-Path -LiteralPath $diag){$detail=[IO.File]::ReadAllText($diag)};throw ('Foundation WPF self-test failed: '+$detail)}
        }finally{$p.Dispose()}
    }finally{if($null -eq $old){Remove-Item Env:YOMI_SELFTEST_DIAGNOSTIC -ErrorAction SilentlyContinue}else{$env:YOMI_SELFTEST_DIAGNOSTIC=$old}}
}
function Stop-YomiRuntime {
    $appPrefix=((Join-Path $InstallRoot 'app').TrimEnd('\')+'\').ToLowerInvariant()
    $installPrefix=($InstallRoot.TrimEnd('\')+'\').ToLowerInvariant()
    $exeNames=@('yomicontrollerwpf.exe','mpv.exe','ffmpeg.exe','ffprobe.exe','yt-dlp.exe','priorityrun.exe','yomilauncher.exe')
    foreach($proc in @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue)){
        try{
            $name=([string]$proc.Name).ToLowerInvariant();$exe=([string]$proc.ExecutablePath).ToLowerInvariant();$cmd=([string]$proc.CommandLine).ToLowerInvariant()
            $owned=(($exeNames -contains $name) -and $exe -and $exe.StartsWith($installPrefix)) -or (($name -match '^(powershell|pwsh)\.exe$') -and ($cmd.Contains($appPrefix+'supervisor.ps1') -or $cmd.Contains($appPrefix+'server.ps1')))
            if($owned -and [int]$proc.ProcessId -ne $PID){try{& (Join-Path $env:SystemRoot 'System32\taskkill.exe') /PID ([int]$proc.ProcessId) /T /F 2>$null|Out-Null}catch{}}
        }catch{}
    }
    Start-Sleep -Milliseconds 500
}
function Restore-OuterBackup {
    Stop-YomiRuntime
    if(Test-Path -LiteralPath $InstallRoot -PathType Container){Remove-Item -LiteralPath $InstallRoot -Recurse -Force}
    if(Test-Path -LiteralPath $Backup -PathType Container){Move-Item -LiteralPath $Backup -Destination $InstallRoot}
}

try{
    $createdNew=$false
    $mutex=New-Object Threading.Mutex($true,'Global\YOMI-R61-97-CUMULATIVE-INSTALL',[ref]$createdNew)
    if(-not $createdNew){throw 'Another R61.97 installation is already running.'}
    New-Item -ItemType Directory -Path $Work -Force|Out-Null

    Write-Host ''
    Write-Host 'YOMI 4.2.0.8 - R61.97 EXPERIMENTAL INSTALLER' -ForegroundColor Cyan
    Write-Host 'Preparing installation...' -ForegroundColor DarkGray
    Write-Host ''

    Assert-Hash $BaselineZip $BaselineSha 'YOMI base package'
    Assert-Foundation $SupportRoot
    Assert-FocusedPackage $FocusedRoot
    Write-Host '  Package verification: PASS' -ForegroundColor Green

    $versionPath=Join-Path $InstallRoot 'VERSION.txt'
    if(-not(Test-Path -LiteralPath $InstallRoot -PathType Container)){
        Write-Host '  No YOMI installation found; installing required components...' -ForegroundColor Yellow
        $baselineDir=Join-Path $Work 'public-baseline'
        Expand-Archive -LiteralPath $BaselineZip -DestinationPath $baselineDir -Force
        $baselineInstaller=Get-ChildItem -LiteralPath $baselineDir -Filter 'install.ps1' -File -Recurse|Where-Object{$_.FullName -match '[\\/]installer[\\/]install\.ps1$'}|Select-Object -First 1
        if($null -eq $baselineInstaller){throw 'Required installer was not found inside YOMI-v4.2.0.7.zip.'}
        $ps=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $code=Start-ChildAndWait $ps ('-NoProfile -ExecutionPolicy Bypass -File "'+$baselineInstaller.FullName+'"') $false ''
        if($code -ne 0){throw ('Required YOMI installer failed with exit code '+$code)}
        $FreshBaselineInstalled=$true
        if(-not(Test-Path -LiteralPath $InstallRoot -PathType Container)){throw 'Required YOMI installer returned success but Program Files\YOMI is missing.'}
    }

    if(-not(Test-Path -LiteralPath $versionPath -PathType Leaf)){throw 'Existing YOMI installation has no VERSION.txt; refusing to replace an unrecognized tree.'}
    $version=([IO.File]::ReadAllText($versionPath)).Trim()
    if($version -ne '4.2.0.7' -and $version -ne '4.2.0.8'){throw ('Unsupported existing YOMI version: '+$version)}
    Write-Host ('  Existing YOMI installation detected: '+$version) -ForegroundColor Green

    Assert-PowerShellParseTree (Join-Path $SupportRoot 'payload\app')
    Assert-PowerShellParseTree $FocusedRoot
    Write-Host '  Script validation: PASS' -ForegroundColor Green

    Invoke-Robocopy $InstallRoot $Candidate
    Invoke-Robocopy (Join-Path $SupportRoot 'payload') $Candidate
    $candidateApp=Join-Path $Candidate 'app'

    $updateDir=$FocusedRoot
    $updateInstaller=Join-Path $FocusedRoot 'install.ps1'
    if(-not(Test-Path -LiteralPath $updateInstaller -PathType Leaf)){throw 'R61.97 focused installer is missing.'}
    foreach($pair in @(
        @('YomiControllerWpf.cs','YomiControllerWpf.cs'),@('YomiControllerWpf.xaml','YomiControllerWpf.xaml'),@('YomiDesign.xaml','YomiDesign.xaml'),
        @('music.lua','music.lua'),@('PriorityRun.cs','PriorityRun.cs'),@('YomiUpdateHost.ps1','YomiUpdateHost.ps1'),
        @('YomiObsServer.ps1','server.ps1'),@('YomiSupervisor.ps1','supervisor.ps1'),@('contracts.ps1','contracts.ps1'),
        @('YomiDiagnosticBundle.ps1','YomiDiagnosticBundle.ps1')
    )){Copy-Item -LiteralPath (Join-Path $updateDir ('focused\'+$pair[0])) -Destination (Join-Path $candidateApp $pair[1]) -Force}
    [IO.File]::WriteAllText((Join-Path $candidateApp 'FOCUSED-BUILD.txt'),"YOMI 4.2.0.8 Focused Media Player`r`nCompact Controller Calibration R61.97`r`n",(New-Object Text.UTF8Encoding($false)))

    foreach($needed in @('YomiControllerWpf.cs','YomiControllerWpf.xaml','YomiDesign.xaml','YomiControllerWpf.manifest','YomiLauncher.cs','PriorityRun.cs','ArtworkEdgeDetector.cs','server.ps1','supervisor.ps1','music.lua')){
        if(-not(Test-Path -LiteralPath (Join-Path $candidateApp $needed) -PathType Leaf)){throw ('Candidate runtime file missing: '+$needed)}
    }
    Assert-PowerShellParseTree $candidateApp
    Assert-ObsServerCompile (Join-Path $candidateApp 'server.ps1')
    Write-Host '  Pre-install validation: PASS' -ForegroundColor Green

    $refs=@(Get-WpfReferences)
    Compile-ConsoleExe (Join-Path $candidateApp 'PriorityRun.cs') (Join-Path $candidateApp 'PriorityRun.exe') $refs
    $probe=& (Join-Path $candidateApp 'PriorityRun.exe') below $env:ComSpec /d /c exit 0 2>&1
    if($LASTEXITCODE -ne 0){throw ('Candidate PriorityRun Job Object self-test failed: '+($probe -join ' '))}
    Compile-ConsoleExe (Join-Path $candidateApp 'ArtworkEdgeDetector.cs') (Join-Path $candidateApp 'ArtworkEdgeDetector.exe') @('System.Drawing.dll')
    Compile-WinExe (Join-Path $candidateApp 'YomiLauncher.cs') (Join-Path $candidateApp 'YomiLauncher.exe') '' ''
    Compile-WinExe (Join-Path $candidateApp 'YomiControllerWpf.cs') (Join-Path $candidateApp 'YomiControllerWpf.exe') (Join-Path $candidateApp 'YomiControllerWpf.manifest') (Join-Path $candidateApp 'yomi.ico')
    Assert-WpfSelfTest $candidateApp
    Write-Host '  Application validation: PASS' -ForegroundColor Green

    Stop-YomiRuntime
    if(Test-Path -LiteralPath $Backup){Remove-Item -LiteralPath $Backup -Recurse -Force}
    Move-Item -LiteralPath $InstallRoot -Destination $Backup
    $SwapStarted=$true
    try{Move-Item -LiteralPath $Candidate -Destination $InstallRoot}catch{Move-Item -LiteralPath $Backup -Destination $InstallRoot; $SwapStarted=$false; throw}
    Write-Host '  Application files installed: PASS' -ForegroundColor Green

    $ps=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $code=Start-ChildAndWait $ps ('-NoProfile -ExecutionPolicy Bypass -File "'+$updateInstaller+'" -Elevated') $false ''
    if($code -ne 0){throw ('R61.97 final installation step failed with exit code '+$code)}

    $focused=Join-Path $InstallRoot 'app\FOCUSED-BUILD.txt'
    if(-not(Test-Path -LiteralPath $focused -PathType Leaf)){throw 'R61.97 installation verification file is missing.'}
    $identity=[IO.File]::ReadAllText($focused)
    if($identity -notmatch 'R61\.97'){throw 'R61.97 installation verification failed.'}
    foreach($pair in @(
        @((Join-Path $updateDir 'focused\YomiControllerWpf.cs'),(Join-Path $InstallRoot 'app\YomiControllerWpf.cs')),
        @((Join-Path $updateDir 'focused\YomiControllerWpf.xaml'),(Join-Path $InstallRoot 'app\YomiControllerWpf.xaml')),
        @((Join-Path $updateDir 'focused\YomiDesign.xaml'),(Join-Path $InstallRoot 'app\YomiDesign.xaml')),
        @((Join-Path $updateDir 'focused\music.lua'),(Join-Path $InstallRoot 'app\music.lua'))
    )){if((Sha256 $pair[0]) -ne (Sha256 $pair[1])){throw ('Final promoted byte verification failed: '+$pair[1])}}
    Write-Host '  Final verification: PASS' -ForegroundColor Green

    if(Test-Path -LiteralPath $Backup -PathType Container){Remove-Item -LiteralPath $Backup -Recurse -Force}
    $SwapStarted=$false
    Write-Host ''
    Write-Host 'R61.97 INSTALL COMPLETE' -ForegroundColor Green
    Write-Host ('Log: '+$Log) -ForegroundColor DarkGray
    exit 0
}catch{
    $message=$_.Exception.Message
    Write-Host ''
    Write-Host ('R61.97 INSTALL FAILED: '+$message) -ForegroundColor Red
    if($SwapStarted -and (Test-Path -LiteralPath $Backup -PathType Container)){
        try{Restore-OuterBackup;Write-Host 'Previous YOMI installation restored.' -ForegroundColor Yellow}catch{Write-Host ('OUTER ROLLBACK WARNING: '+$_.Exception.Message) -ForegroundColor Red}
    }elseif($FreshBaselineInstalled){
        Write-Host 'A working YOMI installation remains in place; R61.97 was not installed.' -ForegroundColor Yellow
    }else{
        Write-Host 'Existing Program Files YOMI was not replaced.' -ForegroundColor Yellow
    }
    Write-Host ('Log: '+$Log) -ForegroundColor DarkGray
    exit 1
}finally{
    if(Test-Path -LiteralPath $Candidate -PathType Container){Remove-Item -LiteralPath $Candidate -Recurse -Force -ErrorAction SilentlyContinue}
    if(Test-Path -LiteralPath $Work -PathType Container){Remove-Item -LiteralPath $Work -Recurse -Force -ErrorAction SilentlyContinue}
    if($mutex){try{$mutex.ReleaseMutex()}catch{};try{$mutex.Dispose()}catch{}}
    try{Stop-Transcript|Out-Null}catch{}
}
