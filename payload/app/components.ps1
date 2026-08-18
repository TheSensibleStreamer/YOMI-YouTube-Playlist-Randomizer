param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('InstallDeno','RemoveDeno','InstallFfmpeg','RemoveFfmpeg')]
    [string]$Action,
    [switch]$Elevated
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

if (-not $Elevated) {
    $escaped = $PSCommandPath.Replace("'","''")
    $args = "& '$escaped' -Action '$Action' -Elevated"
    try {
        $p = Start-Process powershell.exe -Verb RunAs -ArgumentList @(
            '-NoProfile','-ExecutionPolicy','Bypass','-Command',$args
        ) -Wait -PassThru
        exit $p.ExitCode
    }
    catch { exit 5 }
}

$enginePid = 0
$pidFile = Join-Path $DataRoot 'state\engine.pid'
if (Test-Path $pidFile) {
    try { [void][int]::TryParse((Get-Content $pidFile -Raw).Trim(),[ref]$enginePid) } catch {}
}
if ($enginePid -gt 0 -and (Get-Process -Id $enginePid -ErrorAction SilentlyContinue)) {
    [System.Windows.Forms.MessageBox]::Show(
        'Stop YOMI before changing installed components.',
        'YOMI Components'
    ) | Out-Null
    exit 3
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$cache = Join-Path $DataRoot 'installer-cache'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
$temp = Join-Path $env:TEMP ('YOMI-Component-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp -Force | Out-Null

function Download($uri,$path) {
    Write-Host "Downloading $uri" -ForegroundColor Cyan
    Invoke-WebRequest -Uri $uri -OutFile $path -UseBasicParsing -UserAgent ("YOMI-"+(Get-YomiVersionText)+"-Components")
}

try {
    switch ($Action) {
        'InstallDeno' {
            $zip = Join-Path $cache 'deno-current.zip'
            if (-not (Test-Path $zip) -or (Get-Item $zip).Length -lt 1MB) {
                Download 'https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip' $zip
            }
            $x = Join-Path $temp 'deno'
            Expand-Archive $zip $x -Force
            $exe = Get-ChildItem $x -Filter deno.exe -File -Recurse | Select-Object -First 1
            if (-not $exe) { throw 'deno.exe was not found.' }
            $dst = Join-Path $InstallRoot 'runtime\deno'
            New-Item -ItemType Directory -Path $dst -Force | Out-Null
            Copy-Item $exe.FullName (Join-Path $dst 'deno.exe') -Force
            [System.Windows.Forms.MessageBox]::Show('Deno / YouTube Compatibility installed.','YOMI Components') | Out-Null
        }
        'RemoveDeno' {
            Remove-Item (Join-Path $InstallRoot 'runtime\deno') -Recurse -Force -ErrorAction SilentlyContinue
            [System.Windows.Forms.MessageBox]::Show('Deno removed. YouTube may expose fewer formats without a JavaScript runtime.','YOMI Components') | Out-Null
        }
        'InstallFfmpeg' {
            $zip = Join-Path $cache 'ffmpeg-release-essentials.zip'
            if (-not (Test-Path $zip) -or (Get-Item $zip).Length -lt 10MB) {
                Download 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip' $zip
            }
            $x = Join-Path $temp 'ffmpeg'
            Expand-Archive $zip $x -Force
            $ff = Get-ChildItem $x -Filter ffmpeg.exe -File -Recurse | Select-Object -First 1
            $fp = Get-ChildItem $x -Filter ffprobe.exe -File -Recurse | Select-Object -First 1
            if (-not $ff -or -not $fp) { throw 'FFmpeg executables were not found.' }
            $dst = Join-Path $InstallRoot 'runtime\ffmpeg'
            New-Item -ItemType Directory -Path $dst -Force | Out-Null
            Copy-Item $ff.FullName (Join-Path $dst 'ffmpeg.exe') -Force
            Copy-Item $fp.FullName (Join-Path $dst 'ffprobe.exe') -Force
            [System.Windows.Forms.MessageBox]::Show('FFmpeg Media Tools installed. Visualizer, smart crop and loudness leveling are available.','YOMI Components') | Out-Null
        }
        'RemoveFfmpeg' {
            Remove-Item (Join-Path $InstallRoot 'runtime\ffmpeg') -Recurse -Force -ErrorAction SilentlyContinue
            [System.Windows.Forms.MessageBox]::Show('FFmpeg Media Tools removed. Dependent settings will be unavailable until it is reinstalled.','YOMI Components') | Out-Null
        }
    }
    exit 0
}
catch {
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'YOMI Components') | Out-Null
    exit 1
}
finally {
    Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
}
