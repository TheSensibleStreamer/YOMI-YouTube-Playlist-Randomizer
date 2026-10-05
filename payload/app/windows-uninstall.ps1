$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

function Test-YomiAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if(-not (Test-YomiAdmin)){
    $elevated=$null
    try{
        $psi=New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $psi.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'"'
        $psi.Verb='runas';$psi.UseShellExecute=$true
        $elevated=New-Object System.Diagnostics.Process;$elevated.StartInfo=$psi
        if(-not $elevated.Start()){exit 5}
        $elevationTimeoutMs=7200000
        if(-not $elevated.WaitForExit($elevationTimeoutMs)){
            try{$elevated.Kill()}catch{};try{$elevated.WaitForExit()}catch{}
            exit 6
        }
        exit [int]$elevated.ExitCode
    }catch{ exit 5 }
    finally{if($null -ne $elevated){$elevated.Dispose()}}
}

$installRoot = Split-Path $PSScriptRoot -Parent
$legacyScript = Join-Path $installRoot 'app\uninstall.ps1'
$uninstallKey = 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\YOMI'
if(-not (Test-Path -LiteralPath $legacyScript -PathType Leaf)){
    throw "YOMI uninstaller is missing: $legacyScript"
}

# The original uninstaller predates the compiled WPF controller. Quiesce every
# YOMI-owned executable plus PowerShell processes running installed YOMI scripts
# before handing control to the legacy removal UI.
$rootNeedle = ([IO.Path]::GetFullPath($installRoot).TrimEnd('\') + '\').ToLowerInvariant()
foreach($proc in @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue)){
    if([int]$proc.ProcessId -eq $PID){continue}
    $exe = [string]$proc.ExecutablePath
    $cmd = [string]$proc.CommandLine
    $ownedExe = -not [string]::IsNullOrWhiteSpace($exe) -and $exe.ToLowerInvariant().StartsWith($rootNeedle)
    $installedScript = -not [string]::IsNullOrWhiteSpace($cmd) -and $cmd.ToLowerInvariant().Contains($rootNeedle + 'app\')
    if($ownedExe -or $installedScript){
        try{Stop-Process -Id ([int]$proc.ProcessId) -Force -ErrorAction Stop}catch{}
    }
}
Start-Sleep -Milliseconds 250

$childPsi=New-Object System.Diagnostics.ProcessStartInfo
$childPsi.FileName=Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$childPsi.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+$legacyScript+'" -Elevated'
$childPsi.UseShellExecute=$false
$child=New-Object System.Diagnostics.Process;$child.StartInfo=$childPsi
try{
    if(-not $child.Start()){exit 6}
    $childTimeoutMs=7200000
    if(-not $child.WaitForExit($childTimeoutMs)){
        try{$child.Kill()}catch{};try{$child.WaitForExit()}catch{}
        # Preserve Installed Apps registration; removal did not complete.
        exit 6
    }
    $childExit=[int]$child.ExitCode
}finally{if($null -ne $child){$child.Dispose()}}
if($childExit -ne 0){
    # Failed/cancelled-with-error uninstall: keep Installed Apps registration so
    # the user still has a reliable path to retry removal.
    exit $childExit
}

# The legacy UI historically returns 0 even when the user presses CANCEL.
# Only unregister from Windows after Program Files is actually gone.
if(Test-Path -LiteralPath $installRoot){
    exit 0
}

Remove-Item -LiteralPath $uninstallKey -Recurse -Force -ErrorAction SilentlyContinue
exit 0
