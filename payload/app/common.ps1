$script:InstallRoot = Split-Path $PSScriptRoot -Parent
$script:DataRoot = Join-Path $env:LOCALAPPDATA 'YOMI'
$script:ConfigPath = Join-Path $script:DataRoot 'config.json'

function Write-YomiUtf8NoBom {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][AllowEmptyString()][string]$Text
    )
    $enc = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path,$Text,$enc)
}

function Initialize-YomiData {
    foreach ($dir in @(
        $script:DataRoot,
        (Join-Path $script:DataRoot 'cache'),
        (Join-Path $script:DataRoot 'cache\audio'),
        (Join-Path $script:DataRoot 'cache\artwork'),
        (Join-Path $script:DataRoot 'cache\video'),
        (Join-Path $script:DataRoot 'cache\visualizer'),
        (Join-Path $script:DataRoot 'cache\meta'),
        (Join-Path $script:DataRoot 'cache\gain'),
        (Join-Path $script:DataRoot 'cache\status'),
        (Join-Path $script:DataRoot 'state'),
        (Join-Path $script:DataRoot 'logs')
    )) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    $defaultPath = Join-Path $PSScriptRoot 'default-config.json'
    if (-not (Test-Path $script:ConfigPath)) {
        Copy-Item $defaultPath $script:ConfigPath -Force
        return
    }

    try {
        $defaults = Get-Content $defaultPath -Raw | ConvertFrom-Json
        $current = Get-Content $script:ConfigPath -Raw | ConvertFrom-Json
        $changed = $false

        foreach ($p in $defaults.PSObject.Properties) {
            if ($null -eq $current.PSObject.Properties[$p.Name]) {
                $current | Add-Member -NotePropertyName $p.Name -NotePropertyValue $p.Value
                $changed = $true
            }
        }

        if ($changed -or [double]$current.version -lt 4.093) {
            $current.version = 4.093
            Write-YomiUtf8NoBom -Path $script:ConfigPath -Text ($current | ConvertTo-Json -Depth 12)
        }
    }
    catch {
        Copy-Item $defaultPath $script:ConfigPath -Force
    }
}

function Get-YomiConfig {
    Initialize-YomiData
    return (Get-Content $script:ConfigPath -Raw | ConvertFrom-Json)
}

function Save-YomiConfig($Config) {
    Initialize-YomiData
    Write-YomiUtf8NoBom -Path $script:ConfigPath -Text ($Config | ConvertTo-Json -Depth 12)
}

function Get-OverlayUrl($Config) {
    return "http://127.0.0.1:$($Config.server_port)/overlay"
}

function Clear-YomiCache {
    Initialize-YomiData
    foreach ($name in @('audio','artwork','video','visualizer','meta','gain','status')) {
        $path = Join-Path $script:DataRoot ("cache\" + $name)
        Get-ChildItem $path -Force -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
    Remove-Item (Join-Path $script:DataRoot 'state\current.json') -Force -ErrorAction SilentlyContinue
}

function Test-YomiComponent([string]$Name) {
    switch ($Name.ToLowerInvariant()) {
        'mpv'    { return (Test-Path (Join-Path $script:InstallRoot 'runtime\mpv\mpv.exe')) }
        'ytdlp'  { return (Test-Path (Join-Path $script:InstallRoot 'runtime\yt-dlp\yt-dlp.exe')) }
        'deno'   { return (Test-Path (Join-Path $script:InstallRoot 'runtime\deno\deno.exe')) }
        'ffmpeg' { return (Test-Path (Join-Path $script:InstallRoot 'runtime\ffmpeg\ffmpeg.exe')) }
        default  { return $false }
    }
}

function Get-YomiDirSizeMB([string]$Path) {
    if (-not (Test-Path $Path)) { return 0 }
    $sum = (Get-ChildItem $Path -File -Recurse -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
    if ($null -eq $sum) { return 0 }
    return [Math]::Round(([double]$sum / 1MB),1)
}

function Get-YomiComponentInfo([string]$Name) {
    $key = $Name.ToLowerInvariant()
    $path = ''
    $exe = ''
    switch ($key) {
        'mpv'    { $path=Join-Path $script:InstallRoot 'runtime\mpv'; $exe=Join-Path $path 'mpv.exe' }
        'ytdlp'  { $path=Join-Path $script:InstallRoot 'runtime\yt-dlp'; $exe=Join-Path $path 'yt-dlp.exe' }
        'deno'   { $path=Join-Path $script:InstallRoot 'runtime\deno'; $exe=Join-Path $path 'deno.exe' }
        'ffmpeg' { $path=Join-Path $script:InstallRoot 'runtime\ffmpeg'; $exe=Join-Path $path 'ffmpeg.exe' }
    }

    $installed = ($exe -and (Test-Path $exe))
    $version = ''
    if ($installed) {
        try {
            if ($key -eq 'ffmpeg') {
                $line = (& $exe -version 2>$null | Select-Object -First 1)
                if ($line -match '^ffmpeg version\s+([^\s]+)') { $version = $matches[1] } else { $version = [string]$line }
            }
            else {
                $line = (& $exe --version 2>$null | Select-Object -First 1)
                $version = [string]$line
            }
        } catch {}
    }

    [PSCustomObject]@{
        Name = $Name
        Installed = [bool]$installed
        Version = $version
        SizeMB = [double](Get-YomiDirSizeMB $path)
        Path = $path
    }
}

function Get-YomiResolvedFps($Config) {
    $mode = [string]$Config.browser_fps_mode
    if ($mode -eq '15 FPS') { return 15 }
    if ($mode -eq '30 FPS') { return 30 }
    if ([bool]$Config.visualizer_enabled -and ([string]$Config.app_mode -eq 'Streamer / OBS')) { return 30 }
    return 15
}

function Get-YomiLayoutMetrics($Config) {
    $cw = [Math]::Max(320,[int]$Config.canvas_width)
    $ch = [Math]::Max(240,[int]$Config.canvas_height)
    $mw = [Math]::Max(20,[int]$Config.media_width)
    $mh = [Math]::Max(20,[int]$Config.media_height)
    $textSize = [Math]::Max(8,[int]$Config.text_size)

    $streamer = ([string]$Config.app_mode -eq 'Streamer / OBS')
    $artOn = $streamer -and [bool]$Config.artwork_enabled
    $vidOn = $streamer -and [bool]$Config.video_enabled
    $vizOn = $streamer -and [bool]$Config.visualizer_enabled
    $titleOn = $streamer -and [bool]$Config.title_enabled
    $channelOn = $streamer -and [bool]$Config.channel_enabled

    $borderPx = 0
    if ([bool]$Config.media_border_enabled) { $borderPx = [Math]::Max(0,[int]$Config.media_border_px) }

    $mediaExtent = 0
    if ($artOn) { $mediaExtent += $mw }
    if ($vidOn) { $mediaExtent += $mw }
    if ($artOn -and $vidOn -and $borderPx -gt 0) { $mediaExtent -= $borderPx }

    $lines = 0
    if ($titleOn) { $lines++ }
    if ($channelOn) { $lines++ }

    $lineMultiplier = 1.08
    switch ([string]$Config.title_channel_spacing) {
        'Tight' { $lineMultiplier = 0.96 }
        'Loose' { $lineMultiplier = 1.22 }
    }

    $textHeight = 1
    if ($lines -gt 0) {
        $textHeight = [int][Math]::Ceiling(($lines * $textSize * $lineMultiplier) + 6)
    }

    $visualHeight = 1
    if ($artOn -or $vidOn -or $vizOn) { $visualHeight = $mh }
    $overlayHeight = [Math]::Max(24,[Math]::Max($visualHeight,$textHeight))

    $vizMultiplier = 4.0
    try { $vizMultiplier = [double]$Config.visualizer_length_multiplier } catch {}
    $vizMultiplier = [Math]::Max(1.0,[Math]::Min(10.0,$vizMultiplier))
    $wantedVizWidth = [Math]::Max(80,[int][Math]::Round($mw * $vizMultiplier))
    $availableViz = [Math]::Max(80,$cw - $mediaExtent)
    $vizWidth = [Math]::Min($wantedVizWidth,$availableViz)

    $corner = [string]$Config.corner
    $isRight = $corner -match 'Right$'
    $isBottom = $corner -match '^Bottom'
    $mainY = 0
    if ($isBottom) { $mainY = [Math]::Max(0,$ch - $overlayHeight) }

    if ($isRight) { $vizX = $cw - $mediaExtent - $vizWidth } else { $vizX = $mediaExtent }
    if ($vizX -lt 0) { $vizX = 0 }

    [PSCustomObject]@{
        CanvasWidth = $cw
        CanvasHeight = $ch
        MediaWidth = $mw
        MediaHeight = $mh
        OverlayWidth = $cw
        OverlayHeight = $overlayHeight
        MainX = 0
        MainY = $mainY
        VisualizerWidth = $vizWidth
        VisualizerHeight = $overlayHeight
        VisualizerX = [int]$vizX
        VisualizerY = 0
        MediaExtent = [int]$mediaExtent
        IsRight = [bool]$isRight
        IsBottom = [bool]$isBottom
        BrowserFps = [int](Get-YomiResolvedFps $Config)
    }
}

function Write-ObsInstructions($Config) {
    $path = Join-Path $script:DataRoot 'OBS-SETUP.txt'
    $m = Get-YomiLayoutMetrics $Config

    $text = @"
YOMI 4.0 - OBS SETUP
====================
YouTube Playlist Randomizer & Player
Optional OBS integration for streamers

YOMI uses ONE Browser Source for artwork, tiny video, title, channel and visualizer.

URL:
$(Get-OverlayUrl $Config)

Browser Source width:  $($m.OverlayWidth)
Browser Source height: $($m.OverlayHeight)
Browser Source FPS:    $($m.BrowserFps)

OBS Transform Position:
X: $($m.MainX)
Y: $($m.MainY)

Canvas selected in YOMI:
$($m.CanvasWidth) x $($m.CanvasHeight)

Corner:
$($Config.corner)

No second visualizer source is needed.
"@

    Set-Content $path $text -Encoding UTF8
    return $path
}
