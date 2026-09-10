$script:InstallRoot = Split-Path $PSScriptRoot -Parent
$script:DataRoot = Join-Path $env:LOCALAPPDATA 'YOMI'
$script:ConfigPath = Join-Path $script:DataRoot 'config.json'
$script:ConfigPreviousPath = Join-Path $script:DataRoot 'config.previous.json'
$script:ConfigHistoryRoot = Join-Path $script:DataRoot 'config-history'
$script:ConfigWriteMutexName = 'Local\YOMI_CONFIG_WRITE'
$script:YomiFallbackVersion = '4.2.0.8'
$contractsPath = Join-Path $PSScriptRoot 'contracts.ps1'
if(Test-Path $contractsPath){. $contractsPath}
$script:YomiProductName = 'YOMI - YouTube OBS Music Interface'

function Get-YomiVersionText {
    $versionPath = Join-Path $script:InstallRoot 'VERSION.txt'
    if (Test-Path $versionPath) {
        try {
            $match = [regex]::Match((Get-Content $versionPath -Raw), '\d+(?:\.\d+){1,3}')
            if ($match.Success) { return $match.Value }
        }
        catch {}
    }
    return $script:YomiFallbackVersion
}

function Get-YomiBrowserCacheKey {
    return ((Get-YomiVersionText) -replace '[^0-9]','')
}

function Write-YomiUtf8NoBom {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][AllowEmptyString()][string]$Text
    )
    $enc = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path,$Text,$enc)
}

function Test-YomiJsonFile {
    param([Parameter(Mandatory=$true)][string]$Path)
    if(-not(Test-Path $Path)){return $false}
    try{
        $raw=Get-Content $Path -Raw
        if([string]::IsNullOrWhiteSpace($raw)){return $false}
        $null=$raw|ConvertFrom-Json
        return $true
    }catch{return $false}
}

function Save-YomiConfigHistorySnapshot {
    param([Parameter(Mandatory=$true)][string]$SourcePath,[string]$IncomingJson='')

    if(-not(Test-YomiJsonFile $SourcePath)){return}
    try{
        $currentText=Get-Content $SourcePath -Raw
        if(-not [string]::IsNullOrWhiteSpace($IncomingJson) -and $currentText -ceq $IncomingJson){return}

        New-Item -ItemType Directory -Path $script:ConfigHistoryRoot -Force|Out-Null
        $bytes=[System.Text.Encoding]::UTF8.GetBytes($currentText)
        $sha=[Security.Cryptography.SHA256]::Create()
        try{$hash=([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()}
        finally{$sha.Dispose()}

        $latest=Get-ChildItem $script:ConfigHistoryRoot -Filter 'config-*.json' -File -ErrorAction SilentlyContinue|
            Sort-Object LastWriteTimeUtc -Descending|Select-Object -First 1
        if($latest){
            try{
                $latestHash=(Get-FileHash $latest.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                if($latestHash -eq $hash){return}
            }catch{}
        }

        $stamp=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
        $dest=Join-Path $script:ConfigHistoryRoot ("config-$stamp-$($hash.Substring(0,12)).json")
        Write-YomiUtf8NoBom -Path $dest -Text $currentText

        $history=@(Get-ChildItem $script:ConfigHistoryRoot -Filter 'config-*.json' -File -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending)
        if($history.Count -gt 8){
            $history|Select-Object -Skip 8|Remove-Item -Force -ErrorAction SilentlyContinue
        }
    }catch{}
}

function Invoke-YomiAtomicReplace {
    param(
        [Parameter(Mandatory=$true)][string]$Source,
        [Parameter(Mandatory=$true)][string]$Destination
    )

    $Source=[System.IO.Path]::GetFullPath($Source)
    $Destination=[System.IO.Path]::GetFullPath($Destination)

    if(-not(Test-Path -LiteralPath $Destination)){
        [System.IO.File]::Move($Source,$Destination)
        return
    }

    $backup=$Destination+'.replace-backup.'+$PID+'.'+[Guid]::NewGuid().ToString('N')
    $replaced=$false
    try{
        [System.IO.File]::Replace($Source,$Destination,$backup,$true)
        $replaced=$true
    }
    finally{
        if($replaced){
            Remove-Item -LiteralPath $backup -Force -ErrorAction SilentlyContinue
        }
    }
}

function Write-YomiConfigAtomic {
    param([Parameter(Mandatory=$true)]$Config)

    $json=$Config|ConvertTo-Json -Depth 12
    $null=$json|ConvertFrom-Json

    $mutex=New-Object System.Threading.Mutex($false,$script:ConfigWriteMutexName)
    $locked=$false
    $temp=$script:ConfigPath+'.tmp.'+$PID+'.'+[Guid]::NewGuid().ToString('N')
    $previousTemp=$script:ConfigPreviousPath+'.tmp.'+$PID+'.'+[Guid]::NewGuid().ToString('N')
    try{
        try{
            $locked=$mutex.WaitOne([TimeSpan]::FromSeconds(10))
        }catch [System.Threading.AbandonedMutexException]{
            $locked=$true
        }
        if(-not $locked){throw 'Timed out waiting for the YOMI config write lock.'}

        Write-YomiUtf8NoBom -Path $temp -Text $json
        if(-not(Test-YomiJsonFile $temp)){throw 'Temporary config failed parse-back validation.'}

        if(Test-YomiJsonFile $script:ConfigPath){
            Save-YomiConfigHistorySnapshot -SourcePath $script:ConfigPath -IncomingJson $json
            Copy-Item $script:ConfigPath $previousTemp -Force
            if(-not(Test-YomiJsonFile $previousTemp)){throw 'Previous-good config backup failed validation.'}
            if(Test-Path $script:ConfigPreviousPath){
                Invoke-YomiAtomicReplace -Source $previousTemp -Destination $script:ConfigPreviousPath
            }else{
                [System.IO.File]::Move($previousTemp,$script:ConfigPreviousPath)
            }
        }

        if(Test-Path $script:ConfigPath){
            Invoke-YomiAtomicReplace -Source $temp -Destination $script:ConfigPath
        }else{
            [System.IO.File]::Move($temp,$script:ConfigPath)
        }
    }
    finally{
        Remove-Item $temp,$previousTemp -Force -ErrorAction SilentlyContinue
        if($locked){try{$mutex.ReleaseMutex()}catch{}}
        $mutex.Dispose()
    }
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
        (Join-Path $script:DataRoot 'cache\comments'),
        (Join-Path $script:DataRoot 'cache\telemetry'),
        (Join-Path $script:DataRoot 'cache\objects'),
        (Join-Path $script:DataRoot 'cache\objects\audio'),
        (Join-Path $script:DataRoot 'cache\objects\meta'),
        (Join-Path $script:DataRoot 'cache\objects\gain'),
        (Join-Path $script:DataRoot 'cache\objects\artwork'),
        (Join-Path $script:DataRoot 'cache\objects\video'),
        (Join-Path $script:DataRoot 'cache\objects\visualizer'),
        (Join-Path $script:DataRoot 'cache\capabilities'),
        (Join-Path $script:DataRoot 'cache\objects\receipts'),
        (Join-Path $script:DataRoot 'cache\objects\quarantine'),
        (Join-Path $script:DataRoot 'state'),
        (Join-Path $script:DataRoot 'logs')
    )) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    $defaultPath = Join-Path $PSScriptRoot 'default-config.json'
    if (-not (Test-Path $script:ConfigPath)) {
        $fresh=Get-Content $defaultPath -Raw|ConvertFrom-Json
        Write-YomiConfigAtomic $fresh
        return
    }

    try {
        $defaults = Get-Content $defaultPath -Raw | ConvertFrom-Json
        $current = Get-Content $script:ConfigPath -Raw | ConvertFrom-Json

        $supportedSchema=2
        if($script:YomiContractVersions){$supportedSchema=[int]$script:YomiContractVersions.config_schema}
        $actualSchema=1
        if($null -ne $current.PSObject.Properties['config_schema_version']){
            $actualSchema=[int]$current.config_schema_version
        }
        if($actualSchema -gt $supportedSchema){
            $notice=Join-Path $script:DataRoot 'state\config-incompatible.txt'
            Write-YomiUtf8NoBom -Path $notice -Text ("Config schema $actualSchema is newer than supported schema $supportedSchema. File preserved without modification.")
            throw "Config schema $actualSchema is newer than this YOMI build supports ($supportedSchema)."
        }

        $changed = $false

        $originalVersion = [string]$current.version
        $targetVersion = Get-YomiVersionText
        $hadGeneralPreset = $null -ne $current.PSObject.Properties['general_preset']
        $hadOverlayPreset = $null -ne $current.PSObject.Properties['overlay_preset']
        $hadVisualizerPreset = $null -ne $current.PSObject.Properties['visualizer_preset']
        $hadPerformancePreset = $null -ne $current.PSObject.Properties['performance_preset']
        $hadDirectorPreset = $null -ne $current.PSObject.Properties['director_preset']
        $hadOutputsPreset = $null -ne $current.PSObject.Properties['outputs_preset']

        foreach ($p in $defaults.PSObject.Properties) {
            if ($null -eq $current.PSObject.Properties[$p.Name]) {
                $current | Add-Member -NotePropertyName $p.Name -NotePropertyValue $p.Value
                $changed = $true
            }
        }

        # Existing installations predate some or all page-preset trackers. Their
        # current values are real custom settings, not the new factory presets.
        if (-not $hadGeneralPreset) {
            $current.general_preset = 'Custom'
            $changed = $true
        }
        if (-not $hadOverlayPreset) {
            $current.overlay_preset = 'Custom'
            $changed = $true
        }
        if (-not $hadVisualizerPreset) {
            $current.visualizer_preset = 'Custom'
            $changed = $true
        }
        if (-not $hadPerformancePreset) {
            $current.performance_preset = 'Custom'
            $changed = $true
        }
        if (-not $hadDirectorPreset) {
            $current.director_preset = 'Custom'
            $changed = $true
        }
        if (-not $hadOutputsPreset) {
            $current.outputs_preset = 'Custom'
            $changed = $true
        }

        # The first 4.1 Director prototype shipped Output 1 as a loose 180px
        # Horizontal row. Migrate only that exact untouched prototype preset;
        # never trample a user's customized output.
        $firstOutput = @($current.director_outputs | Where-Object { [int]$_.id -eq 1 }) | Select-Object -First 1
        if ($originalVersion -eq '4.1' -and $null -ne $firstOutput -and
            [string]$firstOutput.name -eq 'Now Playing' -and
            [string]$firstOutput.modules -eq 'artwork,video,title,channel,visualizer' -and
            [string]$firstOutput.layout -eq 'Horizontal' -and
            [int]$firstOutput.width -eq 2560 -and [int]$firstOutput.height -eq 180) {
            $firstOutput.layout = 'Broadcast Strip'
            $firstOutput.height = 90
            $changed = $true
        }

        # "Use global" is visually identical to the old Spectrum default on
        # migration, while allowing the expanded Visualizer tab to control both
        # the classic overlay and Director outputs unless explicitly overridden.
        if ($originalVersion -eq '4.1' -and [string]$current.director_visualizer_shape -eq 'Spectrum') {
            $current.director_visualizer_shape = 'Use global'
            $changed = $true
        }

        # 4.2 replaces the misleading split audio/video look-ahead controls
        # with one complete-bundle distance. Keep the old property synchronized
        # for downgrade/readability, although the 4.2 engine no longer uses it.
        if ($originalVersion -ne $targetVersion) {
            $current.video_prefetch_ahead = [int]$current.prefetch_ahead
            $changed = $true
        }

        if ($changed -or $originalVersion -ne $targetVersion) {
            $current.version = $targetVersion
            $current.config_schema_version = $supportedSchema
            Write-YomiConfigAtomic $current
            Remove-Item (Join-Path $script:DataRoot 'state\config-incompatible.txt') -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        # A valid user config is never overwritten merely because migration
        # logic itself encountered an unexpected condition.
        if(Test-YomiJsonFile $script:ConfigPath){
            try{Write-YomiUtf8NoBom -Path (Join-Path $script:DataRoot 'state\config-recovery.txt') -Text ('Migration warning: '+$_.Exception.Message)}catch{}
            return
        }

        # Preserve the broken bytes for diagnosis, then recover the last known
        # parseable config. Factory defaults are the final fallback only.
        if(Test-Path $script:ConfigPath){
            $stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
            Copy-Item $script:ConfigPath (Join-Path $script:DataRoot ('config.corrupt-'+$stamp+'.json')) -Force -ErrorAction SilentlyContinue
        }

        if(Test-YomiJsonFile $script:ConfigPreviousPath){
            $recovered=Get-Content $script:ConfigPreviousPath -Raw|ConvertFrom-Json
            Write-YomiConfigAtomic $recovered
            try{Write-YomiUtf8NoBom -Path (Join-Path $script:DataRoot 'state\config-recovery.txt') -Text 'Recovered config.previous.json after current config parse failure.'}catch{}
        }else{
            $fresh=Get-Content $defaultPath -Raw|ConvertFrom-Json
            Write-YomiConfigAtomic $fresh
            try{Write-YomiUtf8NoBom -Path (Join-Path $script:DataRoot 'state\config-recovery.txt') -Text 'Current config was invalid and no previous-good copy existed; factory defaults were restored.'}catch{}
        }
    }
}

function Get-YomiConfig {
    Initialize-YomiData
    return (Get-Content $script:ConfigPath -Raw | ConvertFrom-Json)
}

function Save-YomiConfig($Config) {
    Initialize-YomiData
    $supportedSchema=2
    if($script:YomiContractVersions){$supportedSchema=[int]$script:YomiContractVersions.config_schema}
    $actualSchema=if($null -ne $Config.PSObject.Properties['config_schema_version']){[int]$Config.config_schema_version}else{1}
    Assert-YomiSchemaCompatible -Name 'Config' -Actual $actualSchema -Supported $supportedSchema
    $Config.config_schema_version=$supportedSchema
    Write-YomiConfigAtomic $Config
    Remove-Item (Join-Path $script:DataRoot 'state\config-recovery.txt') -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $script:DataRoot 'state\config-incompatible.txt') -Force -ErrorAction SilentlyContinue
}

function Get-OverlayUrl($Config) {
    return "http://127.0.0.1:$($Config.server_port)/overlay?v=$(Get-YomiBrowserCacheKey)"
}

function Get-DirectorOutputUrl($Config,[int]$OutputId) {
    return "http://127.0.0.1:$($Config.server_port)/source/$OutputId?v=$(Get-YomiBrowserCacheKey)"
}

function Get-DirectorModuleUrl($Config,[string]$Module) {
    $safe = ([string]$Module).Trim().ToLowerInvariant()
    return "http://127.0.0.1:$($Config.server_port)/source/$safe?v=$(Get-YomiBrowserCacheKey)"
}

function Get-DirectorFixedSource($Config,[string]$Module) {
    $safe = ([string]$Module).Trim().ToLowerInvariant()
    return @($Config.director_fixed_sources | Where-Object {
        ([string]$_.module).Trim().ToLowerInvariant() -eq $safe
    }) | Select-Object -First 1
}

function Clear-YomiCache {
    Initialize-YomiData
    foreach ($name in @('audio','artwork','video','visualizer','meta','gain','status','comments','telemetry','objects','capabilities')) {
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
    if ($mode -eq '60 FPS') { return 60 }

    if ([string]$Config.app_mode -ne 'Streamer / OBS') { return 15 }
    $modules = @()
    if ([bool]$Config.director_mode) {
        foreach ($source in @($Config.director_fixed_sources)) {
            if ([bool]$source.enabled) {
                $modules += ([string]$source.module).Trim().ToLowerInvariant()
            }
        }
        foreach ($output in @($Config.director_outputs)) {
            if ([bool]$output.enabled) {
                $modules += @(([string]$output.modules).ToLowerInvariant().Split(',') | ForEach-Object { $_.Trim() })
            }
        }
    }
    $videoOn = [bool]$Config.video_enabled -or $modules -contains 'video'
    $vizOn = [bool]$Config.visualizer_enabled -or $modules -contains 'visualizer'
    if (($videoOn -and [string]$Config.video_fps -match '^60') -or ($vizOn -and [string]$Config.visualizer_fps -match '^60')) { return 60 }
    if ($videoOn -or $vizOn) { return 30 }
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

    $spacingScale = [Math]::Max(1.0,[Math]::Min(3.0,([double]$mh / [Math]::Max(1.0,([double]$textSize * 2.7)))))
    $textGap = [int][Math]::Round($textSize * 0.12)
    switch ([string]$Config.title_channel_spacing) {
        'Tight' { $textGap = 0 }
        'Loose' { $textGap = [int][Math]::Round($textSize * (0.45 + (0.22 * ($spacingScale - 1.0)))) }
        'Extra Loose' { $textGap = [int][Math]::Round($textSize * (0.90 + (0.38 * ($spacingScale - 1.0)))) }
        'Maximum' { $textGap = [int][Math]::Round($textSize * (1.50 + (0.62 * ($spacingScale - 1.0)))) }
    }

    $textHeight = 1
    if ($lines -gt 0) {
        $textHeight = [int][Math]::Ceiling(($lines * $textSize) + ([Math]::Max(0,$lines-1) * $textGap) + 6)
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
YOMI $(Get-YomiVersionText) - YOUTUBE OBS MUSIC INTERFACE
========================================
YouTube playlist randomizer, player and modular stream overlay

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

    if ([bool]$Config.director_mode) {
        $text += @"


DIRECTOR MODE / MODULAR SOURCES
===============================
1. In Settings, enable the individual sources and/or grouped outputs you want.
2. Save. YOMI automatically restarts only when the media pipeline must reload.
3. In OBS, create one Browser Source for each enabled URL below.
4. Copy the matching width and height exactly, then place the source anywhere.

Every source uses the same YOMI playback clock and one shared download cache.
Adding the same video to several Browser Sources does not redownload it, but it
does make OBS decode that video once per video source.

ENABLED INDIVIDUAL SOURCES
--------------------------
"@

        $fixedCount = 0
        foreach ($source in @($Config.director_fixed_sources)) {
            if (-not [bool]$source.enabled) { continue }
            $fixedCount++
            $label = if ($source.label) { [string]$source.label } else { [string]$source.module }
            $text += "`r`n$label`r`n"
            $text += "URL: $(Get-DirectorModuleUrl $Config ([string]$source.module))`r`n"
            $text += "Browser: $([int]$source.width) x $([int]$source.height)`r`n"
        }
        if ($fixedCount -eq 0) {
            $text += "`r`n(none enabled - use Settings > Sources)`r`n"
        }

        $text += @"

ENABLED GROUPED OUTPUTS
-----------------------
"@

        $outputCount = 0
        foreach ($output in @($Config.director_outputs)) {
            if (-not [bool]$output.enabled) { continue }
            $outputCount++
            $text += "`r`nOutput $($output.id) - $($output.name)`r`n"
            $text += "URL: $(Get-DirectorOutputUrl $Config ([int]$output.id))`r`n"
            $text += "Browser: $($output.width) x $($output.height)`r`n"
            $text += "Modules: $($output.modules)`r`n"
        }
        if ($outputCount -eq 0) {
            $text += "`r`n(none enabled - use Settings > Groups 1-6)`r`n"
        }
    }

    Set-Content $path $text -Encoding UTF8
    return $path
}
