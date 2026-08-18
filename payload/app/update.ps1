param([switch]$Manual)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms

$installRoot = Split-Path $PSScriptRoot -Parent
$dataRoot = Join-Path $env:LOCALAPPDATA 'YOMI'
$stateRoot = Join-Path $dataRoot 'state'
$updateRoot = Join-Path $dataRoot 'updates'
$lastCheckFile = Join-Path $stateRoot 'last-update-check.txt'
$manifestUri = 'https://raw.githubusercontent.com/TheSensibleStreamer/YOMI-YouTube-Playlist-Randomizer/main/update.json'

New-Item -ItemType Directory -Path $stateRoot,$updateRoot -Force | Out-Null

if (-not $Manual -and (Test-Path $lastCheckFile)) {
    try {
        $last = [DateTime]::Parse((Get-Content $lastCheckFile -Raw).Trim())
        if (((Get-Date) - $last).TotalHours -lt 24) { exit 0 }
    }
    catch {}
}

# Record the automatic attempt before network access so an offline machine is
# never hammered every time Controller starts. Manual checks always bypass it.
Set-Content $lastCheckFile ((Get-Date).ToString('o')) -Encoding ASCII

function Get-VersionNumber([string]$Text) {
    $match = [regex]::Match($Text,'\d+(?:\.\d+){1,3}')
    if (-not $match.Success) { return [version]'0.0' }
    try { return [version]$match.Value }
    catch { return [version]'0.0' }
}

function Show-UpdateMessage([string]$Text,[System.Windows.Forms.MessageBoxIcon]$Icon) {
    if ($Manual) {
        [System.Windows.Forms.MessageBox]::Show(
            $Text,
            'YOMI Update',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            $Icon
        ) | Out-Null
    }
}

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $versionFile = Join-Path $installRoot 'VERSION.txt'
    $currentText = if (Test-Path $versionFile) { Get-Content $versionFile -Raw } else { '0.0' }
    $current = Get-VersionNumber $currentText
    $headers = @{ 'User-Agent' = ("YOMI-"+$current.ToString()+"-Updater") }
    $manifest = Invoke-RestMethod -Uri $manifestUri -Headers $headers -UseBasicParsing -TimeoutSec 20

    $latest = Get-VersionNumber ([string]$manifest.version)

    if ($latest -le $current) {
        Show-UpdateMessage "YOMI $current is current.`r`n`r`nNo newer public build is available." ([System.Windows.Forms.MessageBoxIcon]::Information)
        exit 0
    }

    $notes = [string]$manifest.summary
    if ([string]::IsNullOrWhiteSpace($notes)) { $notes = 'A newer public YOMI build is available.' }
    $answer = [System.Windows.Forms.MessageBox]::Show(
        "YOMI $latest is available. You have $current.`r`n`r`n$notes`r`n`r`nDownload, verify and open the updater now? Your playlist and settings are preserved by the normal installer.",
        'YOMI Update Available',
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Information
    )
    if ($answer -ne [System.Windows.Forms.DialogResult]::Yes) { exit 0 }

    $packageName = [string]$manifest.package_name
    if ([string]::IsNullOrWhiteSpace($packageName) -or $packageName -notmatch '^YOMI-v[0-9.]+\.zip$') {
        throw 'The public update manifest contains an invalid package name.'
    }
    $packageUri = [string]$manifest.package_url
    $expectedHash = ([string]$manifest.sha256).Trim().ToLowerInvariant()
    if ($packageUri -notlike 'https://raw.githubusercontent.com/TheSensibleStreamer/YOMI-YouTube-Playlist-Randomizer/*') {
        throw 'The public update manifest points outside the official YOMI repository.'
    }
    if ($expectedHash -notmatch '^[a-f0-9]{64}$') { throw 'The public update manifest contains an invalid SHA-256 value.' }

    $packagePath = Join-Path $updateRoot $packageName
    $downloading = $packagePath + '.downloading'
    Remove-Item $downloading -Force -ErrorAction SilentlyContinue
    Invoke-WebRequest -Uri $packageUri -Headers $headers -UseBasicParsing -TimeoutSec 120 -OutFile $downloading
    $actualHash = (Get-FileHash $downloading -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash) {
        Remove-Item $downloading -Force -ErrorAction SilentlyContinue
        throw "Update integrity check failed. Expected $expectedHash but received $actualHash."
    }
    Move-Item $downloading $packagePath -Force

    $extractRoot = Join-Path $updateRoot ('ready-' + $latest.ToString())
    Remove-Item $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
    Expand-Archive -Path $packagePath -DestinationPath $extractRoot -Force
    $installer = Get-ChildItem $extractRoot -Filter 'INSTALL YOMI.cmd' -File -Recurse | Select-Object -First 1
    if (-not $installer) { throw 'The verified update package does not contain INSTALL YOMI.cmd.' }

    Start-Process $installer.FullName
}
catch {
    Show-UpdateMessage ("YOMI could not check for or prepare the update.`r`n`r`n" + $_.Exception.Message) ([System.Windows.Forms.MessageBoxIcon]::Warning)
    exit 1
}
