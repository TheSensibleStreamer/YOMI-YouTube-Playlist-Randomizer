param([switch]$Elevated)
$ErrorActionPreference='Stop'
Set-StrictMode -Version 2.0

$Product='YOMI 4.2.0.8 Compact Controller Calibration R61.97'
$InstallRoot=Join-Path $env:ProgramFiles 'YOMI'
$App=Join-Path $InstallRoot 'app'
$DataRoot=Join-Path $env:LOCALAPPDATA 'YOMI'
$PayloadRoot=$PSScriptRoot
$PatchRelaunchMarker=Join-Path $DataRoot 'state\patch-relaunch.pending'
$InstallResultPath=Join-Path $DataRoot 'state\focused-install-result.json'
$PromotedNames=@('YomiControllerWpf.exe','YomiControllerWpf.cs','YomiControllerWpf.xaml','YomiDesign.xaml','music.lua','PriorityRun.exe','PriorityRun.cs','YomiUpdateHost.ps1','server.ps1','supervisor.ps1','contracts.ps1','YomiDiagnosticBundle.ps1','FOCUSED-BUILD.txt')

function Is-Admin {
    $id=[Security.Principal.WindowsIdentity]::GetCurrent()
    $principal=New-Object Security.Principal.WindowsPrincipal($id)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}
if(-not(Is-Admin)){
    $child=$null
    try{
        $psi=New-Object Diagnostics.ProcessStartInfo
        $psi.FileName=(Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe')
        $psi.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'" -Elevated'
        $psi.Verb='runas'
        $psi.UseShellExecute=$true
        $child=New-Object Diagnostics.Process
        $child.StartInfo=$psi
        if(-not $child.Start()){throw 'Could not start elevated YOMI patch.'}
        if(-not $child.WaitForExit(900000)){throw 'YOMI patch exceeded the 15-minute safety ceiling.'}
        $code=[int]$child.ExitCode
        $relaunchRequired=Test-Path -LiteralPath $PatchRelaunchMarker -PathType Leaf
        if($code -eq 0 -or $relaunchRequired){
            $launcher=Join-Path $App 'YomiLauncher.exe'
            if(Test-Path -LiteralPath $launcher -PathType Leaf){Start-Process -FilePath $launcher -ArgumentList 'controller' -WorkingDirectory $App|Out-Null}
            if($relaunchRequired){Remove-Item -LiteralPath $PatchRelaunchMarker -Force -ErrorAction SilentlyContinue}
        }
        exit $code
    }catch{Write-Host ('YOMI patch elevation failed: '+$_.Exception.Message) -ForegroundColor Red;exit 5}
    finally{if($child){$child.Dispose()}}
}

New-Item -ItemType Directory -Path $DataRoot -Force|Out-Null
$Log=Join-Path $DataRoot 'quick-patch-r61-91.log'
try{Start-Transcript -Path $Log -Force|Out-Null}catch{}
$Work=Join-Path $env:TEMP ('YOMI-R61-97-COMPACT-CONTROLLER-'+[Guid]::NewGuid().ToString('N'))
$Build=Join-Path $Work 'build'
$Backup=Join-Path $DataRoot ('updates\r61-97-before-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6))
$PreExisting=@{}
foreach($name in $PromotedNames){$PreExisting[$name]=Test-Path -LiteralPath (Join-Path $App $name) -PathType Leaf}
$RuntimeStoppedForPatch=$false
$PromotionStarted=$false

function Resolve-Csc {
    foreach($candidate in @(
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
    )){if(Test-Path -LiteralPath $candidate){return $candidate}}
    throw '.NET Framework C# compiler (csc.exe) was not found.'
}
function Get-WpfReferenceClosure {
    $rootNames=@('PresentationFramework','PresentationCore','WindowsBase','System.Xaml','WindowsFormsIntegration','System.Windows.Forms','System.Drawing','System.Web.Extensions','System.Xml','System','System.Core')
    $seen=@{};$queue=@()
    foreach($name in $rootNames){
        Add-Type -AssemblyName $name -ErrorAction Stop
        $assembly=[AppDomain]::CurrentDomain.GetAssemblies()|Where-Object{$_.GetName().Name -eq $name}|Select-Object -First 1
        if($null -eq $assembly){throw ('Required framework assembly did not load: '+$name)}
        if(-not $seen.ContainsKey($assembly.FullName)){$seen[$assembly.FullName]=$assembly;$queue+=$assembly}
    }
    while($queue.Count -gt 0){
        $assembly=$queue[0]
        if($queue.Count -eq 1){$queue=@()}else{$queue=@($queue[1..($queue.Count-1)])}
        foreach($referenceName in @($assembly.GetReferencedAssemblies())){
            if($referenceName.Name -eq 'mscorlib' -or $seen.ContainsKey($referenceName.FullName)){continue}
            try{$dependency=[Reflection.Assembly]::Load($referenceName)}catch{continue}
            if($dependency -and -not [string]::IsNullOrWhiteSpace([string]$dependency.Location) -and -not $seen.ContainsKey($dependency.FullName)){$seen[$dependency.FullName]=$dependency;$queue+=$dependency}
        }
    }
    $refs=@();foreach($assembly in $seen.Values){if(-not [string]::IsNullOrWhiteSpace([string]$assembly.Location)){$refs+=[string]$assembly.Location}}
    return @($refs|Sort-Object -Unique)
}
function Invoke-TaskKillTree([int]$Id){
    if($Id -le 0 -or $Id -eq $PID){return}
    try{& (Join-Path $env:SystemRoot 'System32\taskkill.exe') /PID $Id /T /F 2>$null|Out-Null}catch{}
}
function Invoke-TaskKillSingle([int]$Id){
    if($Id -le 0 -or $Id -eq $PID){return}
    try{& (Join-Path $env:SystemRoot 'System32\taskkill.exe') /PID $Id /F 2>$null|Out-Null}catch{}
}
function Get-YomiOwnedRuntimeProcesses {
    # R61.85: installer/update processes are intentionally NOT runtime-owned merely because
    # they descend from the controller. Limit ownership to exact runtime executables and the
    # exact supervisor/server scripts. This prevents pre-promotion shutdown from killing the
    # updater that is performing the transaction.
    $installPrefix=($InstallRoot.TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar).ToLowerInvariant()
    $appPrefix=($App.TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar).ToLowerInvariant()
    $runtimeExeNames=@('yomicontrollerwpf.exe','mpv.exe','ffmpeg.exe','ffprobe.exe','yt-dlp.exe','priorityrun.exe')
    $owned=@()
    foreach($proc in @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue)){
        try{
            $name=([string]$proc.Name).ToLowerInvariant()
            $exePath=([string]$proc.ExecutablePath).ToLowerInvariant()
            $cmd=[string]$proc.CommandLine
            $ownedExe=($runtimeExeNames -contains $name) -and (-not [string]::IsNullOrWhiteSpace($exePath)) -and $exePath.StartsWith($installPrefix)
            $ownedScript=$false
            if($name -match '^(powershell|pwsh)\.exe$' -and -not [string]::IsNullOrWhiteSpace($cmd)){
                $normalizedCmd=$cmd.ToLowerInvariant()
                $ownedScript=($normalizedCmd.Contains($appPrefix+'supervisor.ps1') -or $normalizedCmd.Contains($appPrefix+'server.ps1'))
            }
            if($ownedExe -or $ownedScript){$owned+=,$proc}
        }catch{}
    }
    return @($owned)
}
function Test-YomiSupervisorProcess($Proc){
    try{
        $name=([string]$Proc.Name).ToLowerInvariant()
        if($name -notmatch '^(powershell|pwsh)\.exe$'){return $false}
        $cmd=([string]$Proc.CommandLine).ToLowerInvariant()
        $appPrefix=($App.TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar).ToLowerInvariant()
        return $cmd.Contains($appPrefix+'supervisor.ps1')
    }catch{return $false}
}
function Stop-YomiOwnedProcess($Proc){
    if($null -eq $Proc){return}
    $id=[int]$Proc.ProcessId
    if($id -le 0 -or $id -eq $PID){return}
    $name=([string]$Proc.Name).ToLowerInvariant()
    # The controller may be the parent of YomiUpdateHost -> elevated install.ps1. Never tree-kill
    # it. Supervisor owns only playback/server children, so its tree is safe and desirable.
    if(Test-YomiSupervisorProcess $Proc){Invoke-TaskKillTree $id}
    else{Invoke-TaskKillSingle $id}
}
function Stop-YomiRuntimeForPatch {
    $state=Join-Path $DataRoot 'state'
    $pidValues=@{}
    foreach($name in @('supervisor.pid','engine.pid','server.pid','controller.pid')){
        $path=Join-Path $state $name
        if(Test-Path -LiteralPath $path){
            $raw=[string](Get-Content -LiteralPath $path -ErrorAction SilentlyContinue|Select-Object -First 1)
            $id=0;if([int]::TryParse($raw.Trim(),[ref]$id)){$pidValues[$name]=$id}
        }
    }
    # Stop the supervisor tree first because it is the sole owner of mpv/server. Stop the
    # controller as a SINGLE process last, preserving updater descendants launched from it.
    if($pidValues.ContainsKey('supervisor.pid')){Invoke-TaskKillTree ([int]$pidValues['supervisor.pid'])}
    foreach($name in @('engine.pid','server.pid')){if($pidValues.ContainsKey($name)){Invoke-TaskKillSingle ([int]$pidValues[$name])}}
    if($pidValues.ContainsKey('controller.pid')){Invoke-TaskKillSingle ([int]$pidValues['controller.pid'])}

    foreach($proc in @(Get-YomiOwnedRuntimeProcesses)){try{Stop-YomiOwnedProcess $proc}catch{}}
    Start-Sleep -Milliseconds 500
    $survivors=@(Get-YomiOwnedRuntimeProcesses)
    if($survivors.Count -gt 0){
        foreach($proc in $survivors){try{Stop-YomiOwnedProcess $proc}catch{}}
        Start-Sleep -Milliseconds 350
        $survivors=@(Get-YomiOwnedRuntimeProcesses)
    }
    if($survivors.Count -gt 0){
        $detail=(@($survivors|ForEach-Object{([string]$_.Name)+' PID '+([string]$_.ProcessId)}) -join ', ')
        throw ('Owned YOMI runtime processes survived the pre-promotion stop transaction: '+$detail)
    }
    foreach($name in @('supervisor.pid','engine.pid','server.pid','controller.pid','controller-runtime-owner.json')){Remove-Item -LiteralPath (Join-Path $state $name) -Force -ErrorAction SilentlyContinue}
}
function Compile-QuickController {
    $csc=Resolve-Csc;$refs=@(Get-WpfReferenceClosure)
    $source=Join-Path $PayloadRoot 'focused\YomiControllerWpf.cs'
    $manifest=Join-Path $App 'YomiControllerWpf.manifest'
    $icon=Join-Path $App 'yomi.ico'
    $exe=Join-Path $Build 'YomiControllerWpf.exe'
    $args=@('/nologo','/noconfig','/codepage:65001','/target:winexe','/platform:anycpu','/optimize+','/debug-',('/win32manifest:"'+$manifest+'"'),('/out:"'+$exe+'"'))
    foreach($ref in $refs){$args+=('/reference:"'+$ref+'"')}
    if(Test-Path -LiteralPath $icon){$args+=('/win32icon:"'+$icon+'"')}
    $args+=('"'+$source+'"')
    $out=& $csc @args 2>&1
    if($LASTEXITCODE -ne 0){throw ("Controller compile failed:`r`n"+($out -join "`r`n"))}
    if(-not(Test-Path -LiteralPath $exe -PathType Leaf)){throw 'Controller compiler returned success without an output EXE.'}
    return $exe
}
function Compile-PriorityRunner {
    $csc=Resolve-Csc;$refs=@(Get-WpfReferenceClosure)
    $source=Join-Path $PayloadRoot 'focused\PriorityRun.cs'
    $exe=Join-Path $Build 'PriorityRun.exe'
    $args=@('/nologo','/noconfig','/codepage:65001','/target:exe','/platform:anycpu','/optimize+','/debug-',('/out:"'+$exe+'"'))
    foreach($ref in $refs){$args+=('/reference:"'+$ref+'"')}
    $args+=('"'+$source+'"')
    $out=& $csc @args 2>&1
    if($LASTEXITCODE -ne 0){throw ("PriorityRun compile failed:`r`n"+($out -join "`r`n"))}
    if(-not(Test-Path -LiteralPath $exe -PathType Leaf)){throw 'PriorityRun compiler returned success without an output EXE.'}
    return $exe
}
function Assert-PriorityRunnerRuntime([string]$Path){
    $probeOut=& $Path below $env:ComSpec /d /c exit 0 2>&1
    if($LASTEXITCODE -ne 0){throw ("PriorityRun Windows Job Object self-test failed:`r`n"+($probeOut -join "`r`n"))}
}
function Assert-Xml([string]$Path){$xml=New-Object Xml.XmlDocument;$xml.PreserveWhitespace=$true;$xml.Load($Path)}
function Assert-Ascii([string]$Path){foreach($b in [IO.File]::ReadAllBytes($Path)){if($b -ge 128){throw ('PowerShell 5.1 ASCII contract failed: '+$Path)}}}
function Assert-PowerShellParse([string]$Path){
    $tokens=$null;$errors=$null
    [void][Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors)
    if($errors -and $errors.Count -gt 0){throw ('PowerShell parse failed for '+$Path+': '+$errors[0].Message)}
}
function Assert-NoReservedAutomaticVariableAssignment([string]$Path){
    $tokens=$null;$errors=$null
    $ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors)
    if($errors -and $errors.Count -gt 0){throw ('PowerShell parse failed for '+$Path+': '+$errors[0].Message)}
    $reserved=@('host','pid','psversiontable','pshome','psscriptroot','pscommandpath')
    $bad=@($ast.FindAll({
        param($node)
        if($node -isnot [Management.Automation.Language.AssignmentStatementAst]){return $false}
        if($node.Left -isnot [Management.Automation.Language.VariableExpressionAst]){return $false}
        return $reserved -contains $node.Left.VariablePath.UserPath.ToLowerInvariant()
    },$true))
    if($bad.Count -gt 0){
        $detail=@($bad|ForEach-Object{('$'+$_.Left.VariablePath.UserPath+' line '+$_.Extent.StartLineNumber)}) -join ', '
        throw ('Reserved PowerShell automatic-variable assignment in '+$Path+': '+$detail)
    }
}
function Assert-NoHardcodedPayloadHashContracts {
    $source=[IO.File]::ReadAllText($PSCommandPath)
    $pattern='(?i)(?<![0-9a-f])[0-9a-f]{64}(?![0-9a-f])'
    if([Text.RegularExpressions.Regex]::IsMatch($source,$pattern)){throw 'Installer contains a forbidden hardcoded SHA-256 literal; package-manifest.json must be the single payload-integrity authority.'}
}
function Assert-PayloadManifestIntegrity {
    $manifestPath=Join-Path $PayloadRoot 'package-manifest.json'
    if(-not(Test-Path -LiteralPath $manifestPath -PathType Leaf)){throw 'Package manifest is missing.'}
    try{$manifest=([IO.File]::ReadAllText($manifestPath)|ConvertFrom-Json)}catch{throw ('Package manifest JSON is invalid: '+$_.Exception.Message)}
    if([int]$manifest.schema -ne 1){throw ('Unsupported package manifest schema: '+[string]$manifest.schema)}
    if([string]$manifest.product -ne $Product){throw ('Package manifest product mismatch: '+[string]$manifest.product)}
    $seen=@{}
    foreach($item in @($manifest.files)){
        $rel=[string]$item.path
        if([string]::IsNullOrWhiteSpace($rel)){throw 'Package manifest contains an empty path.'}
        if($rel.Contains('\') -or $rel.Contains(':') -or [IO.Path]::IsPathRooted($rel)){throw ('Unsafe package manifest path: '+$rel)}
        $parts=@($rel.Split('/'))
        if($parts.Count -lt 1 -or @($parts|Where-Object{$_ -eq '' -or $_ -eq '.' -or $_ -eq '..'}).Count -gt 0){throw ('Non-canonical package manifest path: '+$rel)}
        $fold=$rel.ToLowerInvariant()
        if($seen.ContainsKey($fold)){throw ('Duplicate package manifest path: '+$rel)}
        $seen[$fold]=$true
        $full=Join-Path $PayloadRoot ($rel.Replace('/','\'))
        if(-not(Test-Path -LiteralPath $full -PathType Leaf)){throw ('Manifested payload file missing: '+$rel)}
        $actualBytes=(Get-Item -LiteralPath $full).Length
        if([int64]$item.bytes -ne [int64]$actualBytes){throw ('Payload byte-count mismatch: '+$rel)}
        $expectedHash=([string]$item.sha256).ToLowerInvariant()
        if($expectedHash -notmatch '^[0-9a-f]{64}$'){throw ('Invalid manifest SHA-256: '+$rel)}
        $actualHash=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
        if($actualHash -ne $expectedHash){throw ('Payload SHA-256 mismatch: '+$rel)}
    }
    if($seen.Count -lt 1){throw 'Package manifest contains no payload files.'}
}
function Assert-SameFile([string]$Expected,[string]$Actual,[string]$Label){
    if(-not(Test-Path -LiteralPath $Actual -PathType Leaf)){throw ('Promoted file missing: '+$Label)}
    $a=(Get-FileHash -LiteralPath $Expected -Algorithm SHA256).Hash
    $b=(Get-FileHash -LiteralPath $Actual -Algorithm SHA256).Hash
    if($a -ne $b){throw ('Promoted file verification failed for '+$Label)}
}
function Write-InstallResult([string]$Status,[string]$Detail,[bool]$UpdateLaneRegistered,[string]$BackupPath){
    try{
        $parent=Split-Path -Parent $InstallResultPath
        if($parent){New-Item -ItemType Directory -Path $parent -Force|Out-Null}
        $record=[ordered]@{
            revision='R61.97';status=$Status;detail=$Detail;update_lane_registered=$UpdateLaneRegistered;
            backup=$BackupPath;utc=[DateTime]::UtcNow.ToString('o')
        }
        [IO.File]::WriteAllText($InstallResultPath,($record|ConvertTo-Json -Depth 4 -Compress),(New-Object Text.UTF8Encoding($false)))
    }catch{}
}
function Assert-ObsServerCompile([string]$Path){
    $lines=[IO.File]::ReadAllLines($Path)
    $startMarker="`$source = @'"
    $endMarker="'@"
    $start=-1;$finish=-1
    for($i=0;$i -lt $lines.Length;$i++){
        if($start -lt 0 -and $lines[$i].Trim() -ceq $startMarker){$start=$i;continue}
        if($start -ge 0 -and $lines[$i].Trim() -ceq $endMarker){$finish=$i;break}
    }
    if($start -lt 0 -or $finish -le ($start+1)){throw 'OBS server embedded C# source block was not found.'}
    $csharp=[string]::Join([Environment]::NewLine,@($lines[($start+1)..($finish-1)]))
    if([string]::IsNullOrWhiteSpace($csharp)){throw 'OBS server embedded C# source block was empty.'}
    $compiledTypes=@()
    try{$compiledTypes=@(Add-Type -TypeDefinition $csharp -Language CSharp -PassThru -ErrorAction Stop)}catch{throw ('OBS server C# compile failed: '+$_.Exception.Message)}
    try{
        $type=$compiledTypes|Where-Object{$_.Name -match '^YomiObsHttpServerR\d+$'}|Select-Object -First 1
        if($null -eq $type){throw 'compiled OBS server type was not discovered'}
        $method=$type.GetMethod('NormalizeFocusedMediaRoutes',[Reflection.BindingFlags]'NonPublic,Static')
        if($null -eq $method){throw 'media-route normalization method is missing'}
        $fixture='{"index":218,"artwork":"C:\\cache\\track-218.jpg","video":"C:\\cache\\track-218.mp4","visualizer":""}'
        $normalized=[string]$method.Invoke($null,@($fixture))
        if($normalized.IndexOf('/media/artwork/218',[StringComparison]::Ordinal)-lt 0 -or $normalized.IndexOf('/media/video/218',[StringComparison]::Ordinal)-lt 0){throw 'focused filesystem media paths were not normalized to HTTP routes'}
        $renderer=$type.GetMethod('OverlayHtml',[Reflection.BindingFlags]'NonPublic,Static')
        if($null -eq $renderer){throw 'embedded OBS renderer method is missing'}
        $rendererArgs=New-Object 'object[]' 1;$rendererArgs[0]=$null;$html=[string]$renderer.Invoke($null,$rendererArgs)
        if([string]::IsNullOrWhiteSpace($html)){throw 'embedded OBS renderer returned empty HTML'}
    }catch{throw ('OBS server contract self-test failed: '+$_.Exception.Message)}
}
function Assert-WpfRuntimeLoad([string]$ControllerExe){
    foreach($name in @('YomiControllerWpf.xaml','YomiDesign.xaml')){Copy-Item -LiteralPath (Join-Path $PayloadRoot ('focused\'+$name)) -Destination (Join-Path $Build $name) -Force}
    $diag=Join-Path $Build 'wpf-runtime-probe.log';Remove-Item -LiteralPath $diag -Force -ErrorAction SilentlyContinue
    $oldDiag=$env:YOMI_SELFTEST_DIAGNOSTIC
    try{
        $env:YOMI_SELFTEST_DIAGNOSTIC=$diag
        $psi=New-Object Diagnostics.ProcessStartInfo
        $psi.FileName=$ControllerExe;$psi.Arguments='--self-test';$psi.WorkingDirectory=$Build;$psi.UseShellExecute=$false;$psi.CreateNoWindow=$true
        $proc=New-Object Diagnostics.Process;$proc.StartInfo=$psi
        if(-not $proc.Start()){throw 'WPF runtime XAML probe could not start.'}
        if(-not $proc.WaitForExit(20000)){try{Invoke-TaskKillTree ([int]$proc.Id)}catch{};throw 'WPF runtime XAML probe exceeded 20 seconds.'}
        $code=[int]$proc.ExitCode;$proc.Dispose()
        if($code -ne 0){
            $detail='';if(Test-Path -LiteralPath $diag -PathType Leaf){try{$detail=[IO.File]::ReadAllText($diag)}catch{}}
            if([string]::IsNullOrWhiteSpace($detail)){$detail='self-test exited '+$code}
            throw ("Staged WPF/controller self-test failed before promotion:`r`n"+$detail)
        }
    }finally{if($null -eq $oldDiag){Remove-Item Env:YOMI_SELFTEST_DIAGNOSTIC -ErrorAction SilentlyContinue}else{$env:YOMI_SELFTEST_DIAGNOSTIC=$oldDiag}}
}
function Register-YomiUpdateLane {
    $updateHostPath=Join-Path $App 'YomiUpdateHost.ps1'
    $controller=Join-Path $App 'YomiControllerWpf.exe'
    $powershell=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $classes='HKCU:\Software\Classes'
    $ext=Join-Path $classes '.yomiupdate'
    $type=Join-Path $classes 'YOMI.UpdatePackage'
    $iconKey=Join-Path $type 'DefaultIcon'
    $commandKey=Join-Path $type 'shell\open\command'
    $paths=@($ext,$type,$iconKey,$commandKey)
    $snapshots=@()
    foreach($path in $paths){
        $exists=Test-Path -LiteralPath $path
        $value=$null
        if($exists){try{$value=(Get-Item -LiteralPath $path).GetValue('')}catch{}}
        $snapshots+=,[PSCustomObject]@{Path=$path;Exists=$exists;Value=$value}
    }
    try{
        New-Item -Path $ext -Force|Out-Null;Set-Item -Path $ext -Value 'YOMI.UpdatePackage'
        New-Item -Path $type -Force|Out-Null;Set-Item -Path $type -Value 'YOMI verified update package'
        New-Item -Path $iconKey -Force|Out-Null;Set-Item -Path $iconKey -Value ('"'+$controller+'",0')
        New-Item -Path $commandKey -Force|Out-Null
        Set-Item -Path $commandKey -Value ('"'+$powershell+'" -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "'+$updateHostPath+'" "%1"')
        return $true
    }catch{
        foreach($snapshot in @($snapshots|Sort-Object { $_.Path.Length } -Descending)){
            try{
                if(-not $snapshot.Exists){Remove-Item -LiteralPath $snapshot.Path -Recurse -Force -ErrorAction SilentlyContinue}
                else{
                    New-Item -Path $snapshot.Path -Force|Out-Null
                    Set-Item -Path $snapshot.Path -Value $snapshot.Value
                }
            }catch{}
        }
        throw
    }
}

try{
    Write-Host ''
    Write-Host 'YOMI 4.2.0.8 - COMPACT CONTROLLER CALIBRATION R61.97' -ForegroundColor Cyan
    Write-Host 'Validate -> compile -> real WPF load -> stop owned runtime -> backup -> promote -> verify -> optional updater registration.' -ForegroundColor DarkGray
    Write-Host ''

    foreach($required in @(
        (Join-Path $App 'YomiControllerWpf.exe'),(Join-Path $App 'YomiControllerWpf.manifest'),(Join-Path $App 'YomiLauncher.exe'),
        (Join-Path $PayloadRoot 'focused\YomiControllerWpf.cs'),(Join-Path $PayloadRoot 'focused\YomiControllerWpf.xaml'),
        (Join-Path $PayloadRoot 'focused\YomiDesign.xaml'),(Join-Path $PayloadRoot 'focused\music.lua'),(Join-Path $PayloadRoot 'focused\PriorityRun.cs'),(Join-Path $PayloadRoot 'focused\YomiUpdateHost.ps1'),(Join-Path $PayloadRoot 'focused\YomiObsServer.ps1'),(Join-Path $PayloadRoot 'focused\YomiSupervisor.ps1'),(Join-Path $PayloadRoot 'focused\contracts.ps1'),(Join-Path $PayloadRoot 'focused\YomiDiagnosticBundle.ps1'),
        (Join-Path $PayloadRoot 'package-manifest.json')
    )){if(-not(Test-Path -LiteralPath $required -PathType Leaf)){throw ('Required file missing: '+$required)}}

    Assert-PayloadManifestIntegrity
    Write-Host '  Manifest-driven payload integrity: PASS' -ForegroundColor Green
    New-Item -ItemType Directory -Path $Build -Force|Out-Null
    Assert-Xml (Join-Path $PayloadRoot 'focused\YomiControllerWpf.xaml');Assert-Xml (Join-Path $PayloadRoot 'focused\YomiDesign.xaml')
    Assert-Ascii $PSCommandPath;Assert-Ascii (Join-Path $PayloadRoot 'focused\YomiUpdateHost.ps1');Assert-Ascii (Join-Path $PayloadRoot 'focused\YomiObsServer.ps1');Assert-Ascii (Join-Path $PayloadRoot 'focused\YomiSupervisor.ps1');Assert-Ascii (Join-Path $PayloadRoot 'focused\contracts.ps1');Assert-Ascii (Join-Path $PayloadRoot 'focused\YomiDiagnosticBundle.ps1')
    Assert-PowerShellParse $PSCommandPath;Assert-PowerShellParse (Join-Path $PayloadRoot 'focused\YomiUpdateHost.ps1');Assert-PowerShellParse (Join-Path $PayloadRoot 'focused\YomiObsServer.ps1');Assert-PowerShellParse (Join-Path $PayloadRoot 'focused\YomiSupervisor.ps1');Assert-PowerShellParse (Join-Path $PayloadRoot 'focused\contracts.ps1');Assert-PowerShellParse (Join-Path $PayloadRoot 'focused\YomiDiagnosticBundle.ps1')
    Assert-NoReservedAutomaticVariableAssignment $PSCommandPath
    Assert-NoReservedAutomaticVariableAssignment (Join-Path $PayloadRoot 'focused\YomiUpdateHost.ps1')
    Assert-NoReservedAutomaticVariableAssignment (Join-Path $PayloadRoot 'focused\YomiObsServer.ps1')
    Assert-NoReservedAutomaticVariableAssignment (Join-Path $PayloadRoot 'focused\YomiSupervisor.ps1')
    Assert-NoReservedAutomaticVariableAssignment (Join-Path $PayloadRoot 'focused\contracts.ps1')
    Assert-NoReservedAutomaticVariableAssignment (Join-Path $PayloadRoot 'focused\YomiDiagnosticBundle.ps1')
    Assert-ObsServerCompile (Join-Path $PayloadRoot 'focused\YomiObsServer.ps1')
    Write-Host '  XML + PowerShell 5.1 encoding + automatic-variable + OBS server compile gates: PASS' -ForegroundColor Green

    # R61.97 STREAMLINED RELEASE GATES: source-token/anchor policing is intentionally retired.
    # Structural/behavioral authority is the real compiler, staged WPF self-test, and transactional promotion.
    $newPriorityRunner=Compile-PriorityRunner
    Write-Host '  PriorityRun compile: PASS' -ForegroundColor Green
    Assert-PriorityRunnerRuntime $newPriorityRunner
    Write-Host '  PriorityRun suspended-child Job Object self-test: PASS' -ForegroundColor Green

    $newExe=Compile-QuickController
    Write-Host '  Controller compile: PASS' -ForegroundColor Green
    Assert-WpfRuntimeLoad $newExe
    Write-Host '  Real staged WPF load + semantic self-test: PASS' -ForegroundColor Green

    Stop-YomiRuntimeForPatch
    $RuntimeStoppedForPatch=$true
    New-Item -ItemType Directory -Path (Split-Path -Parent $PatchRelaunchMarker) -Force|Out-Null
    Set-Content -LiteralPath $PatchRelaunchMarker -Value 'runtime-stopped' -Encoding ASCII
    Write-Host '  Existing YOMI runtime stopped after every pre-promotion gate: PASS' -ForegroundColor Green

    New-Item -ItemType Directory -Path $Backup -Force|Out-Null
    foreach($name in $PromotedNames){
        if([bool]$PreExisting[$name]){
            $src=Join-Path $App $name
            $backupFile=Join-Path $Backup $name
            Copy-Item -LiteralPath $src -Destination $backupFile -Force
            Assert-SameFile $src $backupFile ('rollback snapshot '+$name)
        }
    }
    Write-Host '  Rollback snapshot byte verification: PASS' -ForegroundColor Green
    $PromotionStarted=$true

    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\YomiControllerWpf.cs') -Destination (Join-Path $App 'YomiControllerWpf.cs') -Force
    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\YomiControllerWpf.xaml') -Destination (Join-Path $App 'YomiControllerWpf.xaml') -Force
    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\YomiDesign.xaml') -Destination (Join-Path $App 'YomiDesign.xaml') -Force
    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\music.lua') -Destination (Join-Path $App 'music.lua') -Force
    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\PriorityRun.cs') -Destination (Join-Path $App 'PriorityRun.cs') -Force
    Copy-Item -LiteralPath $newPriorityRunner -Destination (Join-Path $App 'PriorityRun.exe') -Force
    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\YomiUpdateHost.ps1') -Destination (Join-Path $App 'YomiUpdateHost.ps1') -Force
    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\YomiObsServer.ps1') -Destination (Join-Path $App 'server.ps1') -Force
    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\YomiSupervisor.ps1') -Destination (Join-Path $App 'supervisor.ps1') -Force
    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\contracts.ps1') -Destination (Join-Path $App 'contracts.ps1') -Force
    Copy-Item -LiteralPath (Join-Path $PayloadRoot 'focused\YomiDiagnosticBundle.ps1') -Destination (Join-Path $App 'YomiDiagnosticBundle.ps1') -Force
    Copy-Item -LiteralPath $newExe -Destination (Join-Path $App 'YomiControllerWpf.exe') -Force
    $focusedBuildText="YOMI 4.2.0.8 Focused Media Player`r`nCompact Controller Calibration R61.97`r`n"
    [IO.File]::WriteAllText((Join-Path $App 'FOCUSED-BUILD.txt'),$focusedBuildText,(New-Object Text.UTF8Encoding($false)))

    foreach($pair in @(
        @((Join-Path $PayloadRoot 'focused\YomiControllerWpf.cs'),(Join-Path $App 'YomiControllerWpf.cs'),'controller source'),
        @((Join-Path $PayloadRoot 'focused\YomiControllerWpf.xaml'),(Join-Path $App 'YomiControllerWpf.xaml'),'controller XAML'),
        @((Join-Path $PayloadRoot 'focused\YomiDesign.xaml'),(Join-Path $App 'YomiDesign.xaml'),'design XAML'),
        @((Join-Path $PayloadRoot 'focused\music.lua'),(Join-Path $App 'music.lua'),'playback spine'),
        @((Join-Path $PayloadRoot 'focused\PriorityRun.cs'),(Join-Path $App 'PriorityRun.cs'),'priority runner source'),
        @($newPriorityRunner,(Join-Path $App 'PriorityRun.exe'),'priority runner executable'),
        @((Join-Path $PayloadRoot 'focused\YomiUpdateHost.ps1'),(Join-Path $App 'YomiUpdateHost.ps1'),'update host'),
        @((Join-Path $PayloadRoot 'focused\YomiObsServer.ps1'),(Join-Path $App 'server.ps1'),'OBS server'),
        @((Join-Path $PayloadRoot 'focused\YomiSupervisor.ps1'),(Join-Path $App 'supervisor.ps1'),'runtime supervisor'),
        @((Join-Path $PayloadRoot 'focused\contracts.ps1'),(Join-Path $App 'contracts.ps1'),'schema contracts'),
        @((Join-Path $PayloadRoot 'focused\YomiDiagnosticBundle.ps1'),(Join-Path $App 'YomiDiagnosticBundle.ps1'),'diagnostic bundle'),
        @($newExe,(Join-Path $App 'YomiControllerWpf.exe'),'controller executable')
    )){Assert-SameFile $pair[0] $pair[1] $pair[2]}
    if([IO.File]::ReadAllText((Join-Path $App 'FOCUSED-BUILD.txt')) -cne $focusedBuildText){throw 'Promoted focused-build identity verification failed.'}
    Write-Host '  Promoted file byte verification + focused identity: PASS' -ForegroundColor Green

    $updateLaneRegistered=$false
    try{
        $updateLaneRegistered=[bool](Register-YomiUpdateLane)
        if($updateLaneRegistered){Write-Host '  Persistent .yomiupdate lane: PASS' -ForegroundColor Green}
    }catch{
        Write-Host ('  Persistent .yomiupdate lane: WARN - '+$_.Exception.Message) -ForegroundColor Yellow
        Write-Host '  YOMI itself is promoted and verified; future updates can still use the setup EXE.' -ForegroundColor DarkYellow
    }
    Remove-Item -LiteralPath $PatchRelaunchMarker -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $DataRoot 'state\planned-stop.pending') -Force -ErrorAction SilentlyContinue
    Write-InstallResult 'success' 'promotion verified' $updateLaneRegistered $Backup

    Write-Host '  Promotion transaction: PASS' -ForegroundColor Green
    Write-Host ''
    Write-Host 'R61.97 UPDATE COMPLETE' -ForegroundColor Green
    Write-Host ('Backup: '+$Backup) -ForegroundColor DarkGray
    exit 0
}catch{
    $failureMessage=$_.Exception.Message
    Write-Host ''
    Write-Host ('R61.97 UPDATE FAILED: '+$failureMessage) -ForegroundColor Red
    try{
        if($PromotionStarted -and (Test-Path -LiteralPath $Backup -PathType Container)){
            Stop-YomiRuntimeForPatch
            foreach($name in $PromotedNames){
                $src=Join-Path $Backup $name
                $dst=Join-Path $App $name
                if([bool]$PreExisting[$name]){
                    if(-not(Test-Path -LiteralPath $src -PathType Leaf)){throw ('Rollback backup missing pre-existing file: '+$name)}
                    Copy-Item -LiteralPath $src -Destination $dst -Force
                    Assert-SameFile $src $dst ('rollback restore '+$name)
                }else{
                    Remove-Item -LiteralPath $dst -Force -ErrorAction SilentlyContinue
                    if(Test-Path -LiteralPath $dst -PathType Leaf){throw ('Rollback could not remove newly introduced file: '+$name)}
                }
            }
            New-Item -ItemType Directory -Path (Split-Path -Parent $PatchRelaunchMarker) -Force|Out-Null
            Set-Content -LiteralPath $PatchRelaunchMarker -Value 'rollback-restored' -Encoding ASCII
            Write-InstallResult 'rollback-restored' $failureMessage $false $Backup
            Write-Host 'Previous controller restored. The non-elevated parent will relaunch it automatically.' -ForegroundColor Yellow
        }elseif($RuntimeStoppedForPatch){
            # Nothing was promoted. The old installation is still intact, but it was intentionally stopped.
            New-Item -ItemType Directory -Path (Split-Path -Parent $PatchRelaunchMarker) -Force|Out-Null
            Set-Content -LiteralPath $PatchRelaunchMarker -Value 'existing-install-relaunch' -Encoding ASCII
            Write-InstallResult 'post-stop-pre-promotion-failure' $failureMessage $false $Backup
            Write-Host 'Installed files were not promoted; the non-elevated parent will relaunch the existing controller.' -ForegroundColor Yellow
        }else{
            Write-InstallResult 'pre-promotion-failure' $failureMessage $false ''
        }
    }catch{
        $rollbackMessage=$_.Exception.Message
        Remove-Item -LiteralPath $PatchRelaunchMarker -Force -ErrorAction SilentlyContinue
        Write-InstallResult 'rollback-warning' ($failureMessage+' | rollback: '+$rollbackMessage) $false $Backup
        Write-Host ('Rollback warning: '+$rollbackMessage) -ForegroundColor Red
        Write-Host 'Automatic relaunch is suppressed because rollback could not be verified.' -ForegroundColor Red
    }
    Write-Host ('Log: '+$Log) -ForegroundColor DarkGray
    exit 1
}finally{
    try{Remove-Item -LiteralPath $Work -Recurse -Force -ErrorAction SilentlyContinue}catch{}
    try{Stop-Transcript|Out-Null}catch{}
}
