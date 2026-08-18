$ErrorActionPreference='SilentlyContinue'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
$config=Get-YomiConfig
$out=New-Object System.Collections.Generic.List[string]

$out.Add('===== YOMI 4.2.0 DIAGNOSTICS =====')
$out.Add('')
$out.Add("Program: $InstallRoot")
$out.Add("Data: $DataRoot")
$out.Add("Mode: $($config.app_mode)")
$out.Add("Player video: $($config.player_video_quality)")
$out.Add("Video selection: $($config.video_preference) / $($config.video_fps)")
$out.Add("Audio: $($config.audio_quality) / $($config.audio_preference)")
$out.Add("Overlay video: $($config.overlay_video_quality) / limit $($config.video_cache_limit_mb) MB")
$out.Add("Performance: $($config.performance_mode) / $($config.cache_workers) worker(s) / $($config.prefetch_ahead) complete track(s) ahead / Browser $((Get-YomiResolvedFps $config)) FPS")
$out.Add("Canvas: $($config.canvas_width)x$($config.canvas_height)  Corner: $($config.corner)")
$out.Add("Classic modules: Art=$($config.artwork_enabled) Video=$($config.video_enabled) Viz=$($config.visualizer_enabled)")
$out.Add("Visualizer: $($config.visualizer_shape) / $($config.visualizer_fps) / trim $($config.visualizer_high_frequency_trim)% / anchor $($config.visualizer_vertical_anchor) / spacing $($config.visualizer_bar_spacing) / glow $($config.visualizer_peak_glow) / frequency $($config.visualizer_frequency_scale)")
$out.Add("Director: Enabled=$($config.director_mode) Theme=$($config.director_theme) Timeline=$($config.director_timeline) Motion=$($config.director_motion)")
$out.Add("Director data: Comment=$($config.featured_comment_enabled) Telemetry=$($config.telemetry_enabled) Probe=$($config.telemetry_probe_enabled) History=$($config.history_enabled)")

$out.Add('')
$out.Add('===== DIRECTOR OUTPUTS =====')
foreach($o in @($config.director_outputs)) {
    $out.Add("$($o.id): Enabled=$($o.enabled) $($o.width)x$($o.height) $($o.layout) [$($o.modules)] URL=$(Get-DirectorOutputUrl $config ([int]$o.id))")
}

$out.Add('')
$out.Add('===== COMPONENTS =====')
foreach($n in @('mpv','ytdlp','deno','ffmpeg')) {
    $i=Get-YomiComponentInfo $n
    $out.Add("$n : Installed=$($i.Installed)  Size=$($i.SizeMB)MB  Version=$($i.Version)")
}

$out.Add('')
$out.Add('===== APP PROCESSES =====')
Get-CimInstance Win32_Process |
    Where-Object {$_.CommandLine -like '*YOMI*' -or $_.ExecutablePath -like "$InstallRoot*"} |
    Select-Object -First 24 |
    ForEach-Object {$out.Add("$($_.Name) PID $($_.ProcessId)")}

$out.Add('')
$out.Add('===== CACHE COUNTS / SIZES =====')
foreach($d in @('audio','artwork','video','visualizer','meta','gain','status','comments','telemetry')) {
    $path=Join-Path $DataRoot "cache\$d"
    $count=@(Get-ChildItem $path -File -ErrorAction SilentlyContinue).Count
    $size=Get-YomiDirSizeMB $path
    $out.Add(("{0,-12} {1,5} files  {2,9} MB" -f $d,$count,$size))
}

$statePath=Join-Path $DataRoot 'state\current.json'
$out.Add('')
$out.Add('===== CURRENT BROADCAST STATE =====')
if(Test-Path $statePath) {
    try {
        $s=Get-Content $statePath -Raw | ConvertFrom-Json
        $out.Add("Track $($s.index)/$($s.playlist_count): $($s.title)")
        $out.Add("Media bytes: audio=$($s.media.audio_bytes) art=$($s.media.artwork_bytes) video=$($s.media.video_bytes) viz=$($s.media.visualizer_bytes)")
        $out.Add("Pipeline: ready=$($s.pipeline.bundle_ready) cacheHit=$($s.pipeline.cache_hit) active=$($s.pipeline.active_jobs) queued=$($s.pipeline.queued_jobs) ahead=$($s.pipeline.ready_ahead)")
        $out.Add("Comment present: $([bool]$s.comment.text)  Audio probe present: $([bool]$s.telemetry.audio)  Video probe present: $([bool]$s.telemetry.video)")
    } catch {$out.Add('(current state could not be parsed)')}
} else {$out.Add('(no current state yet)')}

$mpvLog=Join-Path $DataRoot 'logs\mpv.log'
$out.Add('')
$out.Add('===== MPV LOG - LAST 45 USEFUL LINES =====')
if(Test-Path $mpvLog) {
    Get-Content $mpvLog -Tail 350 |
        Where-Object {$_ -match '(?i)YOMI|error|fail|403|track|video|artwork|visualizer|comment|probe|director'} |
        Select-Object -Last 45 |
        ForEach-Object {
            $clean=$_ -replace 'https?://\S+','<URL REMOVED>'
            if($clean.Length -gt 260){$clean=$clean.Substring(0,260)+'...'}
            $out.Add($clean)
        }
} else {$out.Add('(no mpv log yet)')}

$text=$out -join "`r`n"
try{Set-Clipboard -Value $text}catch{}
$text
Write-Host ''
Write-Host 'Diagnostics copied to clipboard.' -ForegroundColor Green
