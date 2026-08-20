$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
$config = Get-YomiConfig
$yomiVersion = Get-YomiVersionText

$form = New-Object System.Windows.Forms.Form
$form.Text = "YOMI $yomiVersion - YouTube OBS Music Interface"
$form.StartPosition = 'CenterScreen'
$form.Size = New-Object System.Drawing.Size(1010,850)
$form.MinimumSize = $form.Size
$form.MaximumSize = $form.Size
$form.MaximizeBox = $false
$form.Font = New-Object System.Drawing.Font('Segoe UI',10)
$settingsIcon = Join-Path $InstallRoot 'assets\yomi-settings-v408.ico'
if (Test-Path $settingsIcon) { try { $form.Icon = New-Object System.Drawing.Icon($settingsIcon) } catch {} }

$title = New-Object System.Windows.Forms.Label
$title.Text = 'YOMI'
$title.Font = New-Object System.Drawing.Font('Segoe UI Semibold',24)
$title.Location = New-Object System.Drawing.Point(20,12)
$title.Size = New-Object System.Drawing.Size(100,42)
$form.Controls.Add($title)

$subtitle = New-Object System.Windows.Forms.Label
$subtitle.Text = 'YouTube OBS Music Interface  |  Playlist randomizer, player and modular stream overlay'
$subtitle.Location = New-Object System.Drawing.Point(125,26)
$subtitle.Size = New-Object System.Drawing.Size(780,26)
$subtitle.ForeColor = [System.Drawing.Color]::DimGray
$form.Controls.Add($subtitle)

function Add-Label($parent,$text,$x,$y,$w=180,$h=24) {
    $c=New-Object System.Windows.Forms.Label; $c.Text=$text
    $c.Location=New-Object System.Drawing.Point($x,$y); $c.Size=New-Object System.Drawing.Size($w,$h)
    $c.AutoSize=$false; $c.AutoEllipsis=$true; $c.UseMnemonic=$false
    $parent.Controls.Add($c); return $c
}
function Add-Combo($parent,$x,$y,$w,$items,$selected) {
    $c=New-Object System.Windows.Forms.ComboBox; $c.Location=New-Object System.Drawing.Point($x,$y)
    $c.Size=New-Object System.Drawing.Size($w,28); $c.DropDownStyle='DropDownList'
    [void]$c.Items.AddRange($items); $c.SelectedItem=[string]$selected
    if($c.SelectedIndex -lt 0 -and $c.Items.Count -gt 0){$c.SelectedIndex=0}
    $parent.Controls.Add($c); return $c
}
function Add-Number($parent,$x,$y,$w,$min,$max,$value) {
    $n=New-Object System.Windows.Forms.NumericUpDown; $n.Location=New-Object System.Drawing.Point($x,$y)
    $n.Size=New-Object System.Drawing.Size($w,28); $n.Minimum=[decimal]$min; $n.Maximum=[decimal]$max
    $n.Value=[decimal][Math]::Max($min,[Math]::Min($max,[int]$value)); $parent.Controls.Add($n); return $n
}
function Add-Check($parent,$text,$x,$y,$checked,$w=200) {
    $c=New-Object System.Windows.Forms.CheckBox; $c.Text=$text; $c.Location=New-Object System.Drawing.Point($x,$y)
    $c.Size=New-Object System.Drawing.Size($w,26); $c.Checked=[bool]$checked; $parent.Controls.Add($c); return $c
}
function Add-Text($parent,$x,$y,$w,$text) {
    $c=New-Object System.Windows.Forms.TextBox; $c.Location=New-Object System.Drawing.Point($x,$y)
    $c.Size=New-Object System.Drawing.Size($w,28); $c.Text=[string]$text; $parent.Controls.Add($c); return $c
}
function Hex-Color([string]$hex) {
    try { return [System.Drawing.ColorTranslator]::FromHtml($hex) } catch { return [System.Drawing.Color]::White }
}
function Map-Color([string]$name) {
    switch ($name) {
        'YOMI Cream' { '#F2F0E8' } 'White' { '#FFFFFF' } 'Black' { '#000000' }
        'Near Black' { '#151515' } 'Dark Gray' { '#252525' } 'Gray' { '#8A8A84' }
        'Pink' { '#FF86C8' } 'Lavender' { '#C4A7FF' } 'Purple' { '#9B7BFF' }
        'Cyan' { '#70E1F5' } 'Blue' { '#72A7FF' } 'Green' { '#7BE495' }
        'Yellow' { '#FFD166' } 'Orange' { '#FFA552' } 'Red' { '#FF6B6B' }
        default { '#F2F0E8' }
    }
}
function Color-Name([string]$hex,[string]$fallback='YOMI Cream') {
    foreach($n in @('YOMI Cream','White','Black','Near Black','Dark Gray','Gray','Pink','Lavender','Purple','Cyan','Blue','Green','Yellow','Orange','Red')) {
        if((Map-Color $n) -ieq $hex){return $n}
    }
    return $fallback
}

$directorModuleCatalog=@(
    [PSCustomObject]@{Module='artwork';Label='Artwork';Width=320;Height=180},
    [PSCustomObject]@{Module='video';Label='Video';Width=320;Height=180},
    [PSCustomObject]@{Module='title';Label='Title';Width=900;Height=120},
    [PSCustomObject]@{Module='channel';Label='Channel';Width=900;Height=100},
    [PSCustomObject]@{Module='visualizer';Label='Visualizer';Width=900;Height=180},
    [PSCustomObject]@{Module='progress';Label='Progress';Width=900;Height=100},
    [PSCustomObject]@{Module='stats';Label='Stats';Width=1200;Height=420},
    [PSCustomObject]@{Module='technical';Label='Technical';Width=1000;Height=320},
    [PSCustomObject]@{Module='pipeline';Label='Pipeline';Width=1000;Height=360},
    [PSCustomObject]@{Module='comment';Label='Featured Comment';Width=900;Height=360},
    [PSCustomObject]@{Module='history';Label='History';Width=1200;Height=220},
    [PSCustomObject]@{Module='upnext';Label='Up Next';Width=1000;Height=220},
    [PSCustomObject]@{Module='mission';Label='Mission';Width=1000;Height=320}
)

$tabs=New-Object System.Windows.Forms.TabControl
$tabs.Location=New-Object System.Drawing.Point(20,62); $tabs.Size=New-Object System.Drawing.Size(955,650)
$form.Controls.Add($tabs)
function New-Tab($name){$t=New-Object System.Windows.Forms.TabPage;$t.Text=$name;[void]$tabs.TabPages.Add($t);return $t}

# GENERAL / PLAYER
$general=New-Tab 'General / Player'
Add-Label $general 'YouTube playlist link or playlist ID:' 18 22 320 | Out-Null
$playlist=New-Object System.Windows.Forms.TextBox; $playlist.Location=New-Object System.Drawing.Point(18,48); $playlist.Size=New-Object System.Drawing.Size(890,27); $playlist.Text=[string]$config.playlist; $general.Controls.Add($playlist)
Add-Label $general 'Page preset:' 18 100 95 | Out-Null
$generalPreset=Add-Combo $general 125 96 240 @('Default','Audio-only Player','Efficient Video Player','Quality Video Player','Streamer / OBS','Custom') ([string]$config.general_preset)
$generalPresetHint=Add-Label $general 'Playlist identity is never part of a preset. Changing mode or Player video marks this page Custom.' 385 99 520 42
$generalPresetHint.ForeColor=[System.Drawing.Color]::DimGray
Add-Label $general 'YOMI mode:' 18 150 100 | Out-Null
$mode=Add-Combo $general 125 146 240 @('Player','Streamer / OBS') ([string]$config.app_mode)
$modeHint=Add-Label $general 'Player = lightweight playlist randomizer/player. Streamer / OBS adds the one-source overlay and presentation cache.' 385 149 520 42
$modeHint.ForeColor=[System.Drawing.Color]::DimGray
Add-Label $general 'Player video:' 18 200 100 | Out-Null
$playerQuality=Add-Combo $general 125 196 240 @('Off (audio only)','144p','240p','360p','480p','720p','Best') ([string]$config.player_video_quality)
$playerHint=Add-Label $general 'Audio-only is the lowest-overhead default. Video quality applies only in Player mode and opens an mpv video window.' 385 199 520 44
$playerHint.ForeColor=[System.Drawing.Color]::DimGray
$generalNote=Add-Label $general 'Shuffle Playlist is the normal update action: it reloads the current YouTube playlist, preserves duplicate entries, randomizes the whole list, resets cache mapping and starts from the new track 1.' 18 265 885 58
$generalNote.ForeColor=[System.Drawing.Color]::DimGray
$applyNote=Add-Label $general 'Saving is silent. Browser styling/layout changes refresh automatically. Restart YOMI for player mode, media quality, cache pipeline and generated-visualizer changes.' 18 342 885 48
$applyNote.ForeColor=[System.Drawing.Color]::DarkGoldenrod
$restoreDefaults=New-Object System.Windows.Forms.Button; $restoreDefaults.Text='RESTORE DEFAULT SETTINGS...'; $restoreDefaults.Location=New-Object System.Drawing.Point(18,410); $restoreDefaults.Size=New-Object System.Drawing.Size(250,38); $general.Controls.Add($restoreDefaults)
$restoreHint=Add-Label $general 'Restores YOMI options and output layouts while preserving your playlist, playback history, installed components and Defender choice. Media affected by the restored quality/crop/visualizer settings is rebuilt as needed.' 290 407 615 64
$restoreHint.ForeColor=[System.Drawing.Color]::DimGray

# OBS OVERLAY
$obs=New-Tab 'OBS Overlay'
Add-Label $obs 'Canvas:' 18 24 70 | Out-Null
$canvasPreset=Add-Combo $obs 90 20 180 @('1280 x 720','1920 x 1080','2560 x 1440','3840 x 2160','Custom') ([string]$config.canvas_preset)
Add-Label $obs 'Width:' 290 24 55 | Out-Null
$canvasW=Add-Number $obs 345 20 90 320 7680 ([int]$config.canvas_width)
Add-Label $obs 'Height:' 455 24 60 | Out-Null
$canvasH=Add-Number $obs 515 20 90 240 4320 ([int]$config.canvas_height)
Add-Label $obs 'Corner:' 630 24 65 | Out-Null
$corner=Add-Combo $obs 695 20 190 @('Top Left','Top Right','Bottom Left','Bottom Right') ([string]$config.corner)

Add-Label $obs 'Media size:' 18 75 90 | Out-Null
$mediaPreset=Add-Combo $obs 110 71 160 @('Auto','Compact','Small','Medium','Large','Extra Large','Custom') ([string]$config.media_size_preset)
Add-Label $obs 'Width:' 290 75 55 | Out-Null
$mediaW=Add-Number $obs 345 71 90 40 800 ([int]$config.media_width)
Add-Label $obs 'Height:' 455 75 60 | Out-Null
$mediaH=Add-Number $obs 515 71 90 22 450 ([int]$config.media_height)
Add-Label $obs 'Video zoom:' 630 75 85 | Out-Null
$videoZoom=Add-Number $obs 720 71 80 100 200 ([int][Math]::Round(([double]$config.video_zoom)*100))
Add-Label $obs '%' 805 75 25 | Out-Null

$moduleBox=New-Object System.Windows.Forms.GroupBox; $moduleBox.Text='Overlay modules'; $moduleBox.Location=New-Object System.Drawing.Point(18,120); $moduleBox.Size=New-Object System.Drawing.Size(875,90); $obs.Controls.Add($moduleBox)
$art=Add-Check $moduleBox 'Artwork / thumbnail' 15 28 $config.artwork_enabled 175
$video=Add-Check $moduleBox 'Tiny video' 200 28 $config.video_enabled 120
$titleCheck=Add-Check $moduleBox 'Song title' 330 28 $config.title_enabled 115
$channel=Add-Check $moduleBox 'Channel name' 455 28 $config.channel_enabled 135
$viz=Add-Check $moduleBox 'Retro visualizer' 600 28 $config.visualizer_enabled 150
$smartCrop=Add-Check $moduleBox 'Smart crop black/letterbox borders' 15 57 $config.smart_artwork_crop 280

Add-Label $obs 'Overlay video quality:' 18 235 145 | Out-Null
$overlayVideoQuality=Add-Combo $obs 165 231 190 @('144p (fastest)','240p','360p','480p','720p','Best compatible') ([string]$config.overlay_video_quality)
Add-Label $obs 'Video FPS:' 375 235 95 | Out-Null
$videoFps=Add-Combo $obs 470 231 180 @('30 FPS','60 FPS when available') ([string]$config.video_fps)
Add-Label $obs 'Cache limit MB:' 670 235 110 | Out-Null
$videoCacheLimit=Add-Number $obs 785 231 90 64 8192 ([int]$config.video_cache_limit_mb)
$qualityWarning=Add-Label $obs 'Large-video mode: 720p and Best compatible can take longer to prepare and can use substantially more bandwidth, disk cache and OBS decoding. Long videos may be tens or hundreds of MB. YOMI still enforces the configured cache ceiling and compatibility fallbacks.' 18 278 870 64
$qualityWarning.ForeColor=[System.Drawing.Color]::DarkGoldenrod
$obsHint=Add-Label $obs '60 FPS video is preferred only when YouTube offers it at the selected resolution; YOMI does not fabricate motion by duplicating 30 FPS frames. Disabled classic modules avoid their work unless an enabled Director output needs it. The visualizer begins at the final media pixel; text keeps separate padding.' 18 360 870 72
$obsHint.ForeColor=[System.Drawing.Color]::DimGray
Add-Label $obs 'Page preset:' 18 458 95 | Out-Null
$overlayPreset=Add-Combo $obs 120 454 210 @('Default','Gaming Light','Artwork Radio','Full Strip','Video Showcase','Custom') ([string]$config.overlay_preset)
$overlayPresetHint=Add-Label $obs 'Applies the complete classic-overlay page. Touching any canvas, media, module, crop, quality, FPS, zoom or cache option marks it Custom.' 350 457 540 52
$overlayPresetHint.ForeColor=[System.Drawing.Color]::DimGray

# STYLE
$style=New-Tab 'Text & Style'
Add-Label $style 'Style preset:' 18 24 95 | Out-Null
$stylePreset=Add-Combo $style 120 20 190 @('Default','Classic','Minimal','Retro','Neon','Pastel','Arcade','Custom') ([string]$config.style_preset)
Add-Label $style 'Font:' 335 24 55 | Out-Null
$font=Add-Combo $style 390 20 230 @('Bahnschrift Condensed','Segoe UI','Arial','Arial Black','Georgia','Times New Roman','Trebuchet MS','Verdana','Tahoma','Segoe Print','Segoe Script','Comic Sans MS','Impact','Courier New') ([string]$config.text_font)

Add-Label $style 'Text color:' 18 75 90 | Out-Null
$textColor=Add-Combo $style 120 71 160 @('YOMI Cream','White','Black','Pink','Lavender','Purple','Cyan','Blue','Green','Yellow','Orange','Red') (Color-Name ([string]$config.text_color) 'YOMI Cream')
Add-Label $style 'Outline color:' 310 75 100 | Out-Null
$outlineColor=Add-Combo $style 415 71 160 @('Near Black','Black','Dark Gray','White','Pink','Purple','Blue','Red') (Color-Name ([string]$config.outline_color) 'Near Black')
Add-Label $style 'Outline px:' 610 75 80 | Out-Null
$outlinePx=Add-Number $style 690 71 70 0 12 ([int]$config.text_outline)
Add-Label $style 'Opacity %:' 775 75 80 | Out-Null
$textOpacity=Add-Number $style 855 71 65 20 100 ([int][Math]::Round(([double]$config.text_opacity)*100))

Add-Label $style 'Text size:' 18 125 80 | Out-Null
$textSize=Add-Number $style 120 121 80 10 120 ([int]$config.text_size)
Add-Label $style 'Alignment:' 230 125 80 | Out-Null
$textAlign=Add-Combo $style 310 121 140 @('Auto','Left','Center','Right') ([string]$config.text_alignment)
Add-Label $style 'Title/channel spacing:' 475 125 145 | Out-Null
$textSpacing=Add-Combo $style 625 121 135 @('Tight','Normal','Loose','Extra Loose','Maximum') ([string]$config.title_channel_spacing)
$glow=Add-Check $style 'Subtle text glow' 785 121 $config.text_glow 140

$mediaStyleBox=New-Object System.Windows.Forms.GroupBox; $mediaStyleBox.Text='Media frame'; $mediaStyleBox.Location=New-Object System.Drawing.Point(18,175); $mediaStyleBox.Size=New-Object System.Drawing.Size(875,95); $style.Controls.Add($mediaStyleBox)
$borderOn=Add-Check $mediaStyleBox 'Border' 15 29 $config.media_border_enabled 90
Add-Label $mediaStyleBox 'Width px:' 115 31 70 | Out-Null
$borderPx=Add-Number $mediaStyleBox 185 27 65 0 8 ([int]$config.media_border_px)
Add-Label $mediaStyleBox 'Color:' 275 31 55 | Out-Null
$borderColor=Add-Combo $mediaStyleBox 335 27 150 @('Dark Gray','Near Black','Black','White','Pink','Purple','Cyan','Blue') (Color-Name ([string]$config.media_border_color) 'Dark Gray')
Add-Label $mediaStyleBox 'Corners:' 515 31 70 | Out-Null
$cornerStyle=Add-Combo $mediaStyleBox 590 27 160 @('Square','Soft Rounded','Rounded') ([string]$config.media_corner_style)

$styleHint=Add-Label $style 'The list is intentionally curated instead of dumping hundreds of Windows fonts on you. Loose spacing grows with larger media layouts; Extra Loose and Maximum provide substantially wider separation. Preview changes live before saving.' 18 290 870 52
$styleHint.ForeColor=[System.Drawing.Color]::DimGray

# VISUALIZER
$visual=New-Tab 'Visualizer'
Add-Label $visual 'Page preset:' 18 24 90 | Out-Null
$vizPreset=Add-Combo $visual 115 20 240 @('Default','Monolith Efficiency','Low Overhead Blocks','Broadcast','Smooth 60 FPS','Signal Analyzer','Neon Motion','Custom') ([string]$config.visualizer_preset)

Add-Label $visual 'Activity:' 18 76 70 | Out-Null
$activity=Add-Combo $visual 95 72 145 @('Subtle','Normal','Active','Punchy') ([string]$config.visualizer_activity)
Add-Label $visual 'Opacity %:' 265 76 85 | Out-Null
$opacityPct=Add-Number $visual 350 72 90 5 80 ([int][Math]::Round(([double]$config.visualizer_opacity)*100))
Add-Label $visual 'Pixels:' 470 76 60 | Out-Null
$chunkName=$(if([int]$config.visualizer_internal_width -le 12){'Monolith (12x4)'}elseif([int]$config.visualizer_internal_width -le 16){'Mega Blocks (16x5)'}elseif([int]$config.visualizer_internal_width -le 20){'Giant Blocks (20x6)'}elseif([int]$config.visualizer_internal_width -le 28){'Huge Blocks (28x8)'}elseif([int]$config.visualizer_internal_width -le 40){'Extra Chunky (40x10)'}elseif([int]$config.visualizer_internal_width -le 48){'Chunky (48x12)'}elseif([int]$config.visualizer_internal_width -le 64){'Fine (64x18)'}elseif([int]$config.visualizer_internal_width -le 96){'Extra Fine (96x24)'}elseif([int]$config.visualizer_internal_width -le 128){'Ultra Fine (128x32)'}elseif([int]$config.visualizer_internal_width -le 160){'Microscopic (160x40)'}else{'Maximum Detail (192x48)'})
$chunk=Add-Combo $visual 530 72 190 @('Monolith (12x4)','Mega Blocks (16x5)','Giant Blocks (20x6)','Huge Blocks (28x8)','Extra Chunky (40x10)','Chunky (48x12)','Fine (64x18)','Extra Fine (96x24)','Ultra Fine (128x32)','Microscopic (160x40)','Maximum Detail (192x48)') $chunkName
Add-Label $visual 'Length:' 720 76 65 | Out-Null
$vizLength=Add-Combo $visual 785 72 135 @('Short','Medium','Wide','Extra Wide') $(if([double]$config.visualizer_length_multiplier -le 2.25){'Short'}elseif([double]$config.visualizer_length_multiplier -le 3.25){'Medium'}elseif([double]$config.visualizer_length_multiplier -ge 5.25){'Extra Wide'}else{'Wide'})

Add-Label $visual 'Color mode:' 18 130 90 | Out-Null
$vizColorMode=Add-Combo $visual 115 126 145 @('Solid','Rainbow','Gradient') ([string]$config.visualizer_color_mode)
Add-Label $visual 'Solid color:' 285 130 85 | Out-Null
$vizSolid=Add-Combo $visual 375 126 145 @('Gray','YOMI Cream','White','Pink','Lavender','Purple','Cyan','Blue','Green','Yellow','Orange','Red') (Color-Name ([string]$config.visualizer_solid_color) 'Gray')
Add-Label $visual 'Gradient:' 545 130 75 | Out-Null
$vizGradient=Add-Combo $visual 620 126 145 @('Sunset','Ocean','Pastel','Fire','Forest','Mono') ([string]$config.visualizer_gradient_preset)
Add-Label $visual 'Direction:' 785 130 70 | Out-Null
$gradientOrientation=Add-Combo $visual 855 126 75 @('Horizontal','Vertical') ([string]$config.visualizer_gradient_orientation)

Add-Label $visual 'Bars:' 18 184 60 | Out-Null
$vizDirection=Add-Combo $visual 115 180 145 @('Normal','Mirrored') ([string]$config.visualizer_direction)
Add-Label $visual 'Layer:' 285 184 55 | Out-Null
$vizLayer=Add-Combo $visual 375 180 145 @('Behind text','Above text') ([string]$config.visualizer_layer)
Add-Label $visual 'Shape:' 545 184 65 | Out-Null
$vizShape=Add-Combo $visual 610 180 155 @('Spectrum','Center Mirror','Oscilloscope','Dots','Skyline','Particle Field','Twin Rails') ([string]$config.visualizer_shape)
Add-Label $visual 'Anchor:' 785 184 65 | Out-Null
$vizAnchor=Add-Combo $visual 850 180 80 @('Source','Bottom','Center','Top') ([string]$config.visualizer_vertical_anchor)

Add-Label $visual 'Bar spacing:' 18 238 90 | Out-Null
$vizSpacing=Add-Combo $visual 115 234 145 @('None','Light','Wide') ([string]$config.visualizer_bar_spacing)
Add-Label $visual 'Peak glow:' 285 238 85 | Out-Null
$vizPeakGlow=Add-Combo $visual 375 234 145 @('Off','Subtle','Strong') ([string]$config.visualizer_peak_glow)
Add-Label $visual 'Frequency scale:' 545 238 115 | Out-Null
$vizFrequency=Add-Combo $visual 665 234 160 @('Logarithmic','Linear') ([string]$config.visualizer_frequency_scale)
Add-Label $visual 'Visualizer FPS:' 18 292 105 | Out-Null
$vizFps=Add-Combo $visual 125 288 145 @('30 FPS','60 FPS') ([string]$config.visualizer_fps)
Add-Label $visual 'High-frequency trim %:' 305 292 165 | Out-Null
$vizHighTrim=Add-Number $visual 475 288 80 0 60 ([int]$config.visualizer_high_frequency_trim)
$visualHint=Add-Label $visual 'Monolith, Mega and Giant Blocks stretch very few generated pixels for low preparation cost. Microscopic and Maximum Detail increase resolution. High-frequency trim remaps useful bins across the full width. Generation changes rebuild cached visualizers.' 18 344 875 70
$visualHint.ForeColor=[System.Drawing.Color]::DimGray
$visualCost=Add-Label $visual '' 18 430 875 56
$visualCost.BorderStyle='FixedSingle';$visualCost.ForeColor=[System.Drawing.Color]::DimGray

# PERFORMANCE
$perf=New-Tab 'Performance'
Add-Label $perf 'Page preset:' 18 24 90 | Out-Null
$performancePreset=Add-Combo $perf 115 20 220 @('Default','Gaming','Rapid Cache','Deep Cache','Custom') ([string]$config.performance_preset)
Add-Label $perf 'Background preparation:' 18 76 165 | Out-Null
$perfItems=@('Gaming / Lowest overhead (1 worker)','Balanced (2 workers)','Fast caching (4 workers)','Maximum caching (8 workers)')
$perfSel=$perfItems[0]; if([int]$config.cache_workers -eq 2){$perfSel=$perfItems[1]}elseif([int]$config.cache_workers -ge 8){$perfSel=$perfItems[3]}elseif([int]$config.cache_workers -ge 4){$perfSel=$perfItems[2]}
$performance=Add-Combo $perf 190 72 320 $perfItems $perfSel
Add-Label $perf 'Browser Source FPS:' 535 76 145 | Out-Null
$browserFps=Add-Combo $perf 680 72 130 @('Auto','15 FPS','30 FPS','60 FPS') ([string]$config.browser_fps_mode)
$perfHint=Add-Label $perf 'Default is Balanced / 2 workers with four complete tracks ahead. Maximum / 8 workers can fill a deep cache much faster, but it can hammer CPU, network, disk and Defender at the same time; use it only when the streaming machine has measured headroom.' 18 114 880 52
$perfHint.ForeColor=[System.Drawing.Color]::DimGray
$loudness=Add-Check $perf 'Track loudness leveling (requires FFmpeg Media Tools)' 18 177 $config.loudness_normalization 390
Add-Label $perf 'Complete tracks ready ahead:' 435 181 200 | Out-Null
$prefetch=Add-Number $perf 640 177 80 1 20 ([int]$config.prefetch_ahead)
Add-Label $perf 'Video selection:' 18 232 120 | Out-Null
$videoPreference=Add-Combo $perf 145 228 230 @('Prefer selected maximum','Prefer lowest compatible') ([string]$config.video_preference)
Add-Label $perf 'Audio quality target:' 400 232 145 | Out-Null
$audioQuality=Add-Combo $perf 550 228 215 @('Low (~64 kbps)','Standard (~128 kbps)','High (~160 kbps)','Best available') ([string]$config.audio_quality)

Add-Label $perf 'Audio selection:' 18 280 120 | Out-Null
$audioPreference=Add-Combo $perf 145 276 230 @('Prefer selected maximum','Prefer lowest compatible') ([string]$config.audio_preference)
$audioHint=Add-Label $perf 'Maximum preference chooses the best compatible stream near the selected target. Lowest preference minimizes transfer/cache size and can sound or look noticeably rougher.' 400 274 480 48
$audioHint.ForeColor=[System.Drawing.Color]::DarkGoldenrod
$perfNote=Add-Label $perf 'One ready-ahead number means the whole damn track: audio, metadata, gain and every enabled Streamer/Director media requirement. At 720p/Best, the video-cache ceiling can prevent all 20 from fitting. Player mode caches audio bundles only. Any individual change marks this page Custom.' 18 342 880 72
$perfNote.ForeColor=[System.Drawing.Color]::DimGray
$defenderManaged=Test-Path (Join-Path $DataRoot 'defender-yt-dlp-process-exclusion.txt')
$defenderText=$(if($defenderManaged){'Windows Defender performance option: ENABLED for YOMI yt-dlp.exe only. Uninstall removes the YOMI-managed exclusion.'}else{'Windows Defender performance option: OFF. The first installer screen has the explicit unchecked opt-in below the profile choices.'})
$defenderStatus=Add-Label $perf $defenderText 18 430 880 48
$defenderStatus.ForeColor=$(if($defenderManaged){[System.Drawing.Color]::DarkGreen}else{[System.Drawing.Color]::DarkGoldenrod})

# DIRECTOR MODE
$director=New-Tab 'Director Mode'
$directorOn=Add-Check $director 'Enable Director Mode / modular broadcast engine' 18 18 $config.director_mode 390
Add-Label $director 'Page preset:' 455 22 95 | Out-Null
$directorPreset=Add-Combo $director 555 18 250 @('Default','Pirate Radio','Culture Desk','Control Room','Full Science','Custom') ([string]$config.director_preset)
Add-Label $director 'World / theme:' 18 64 115 | Out-Null
$directorThemes=@('Classic','Pirate Radio','Control Room','Cyberpunk Lab','Record Store','Archive Terminal','Public Access','Museum Label','Arcade','Brutalist Industrial','Spacecraft','Jazz Club')
$directorTheme=Add-Combo $director 140 60 200 $directorThemes ([string]$config.director_theme)
Add-Label $director 'Timeline:' 370 64 75 | Out-Null
$directorTimeline=Add-Combo $director 450 60 180 @('Off','Broadcast Rotation','Rapid Rotation','Cinematic') ([string]$config.director_timeline)
Add-Label $director 'Motion:' 660 64 70 | Out-Null
$directorMotion=Add-Combo $director 730 60 150 @('Off','Subtle','Full') ([string]$config.director_motion)

Add-Label $director 'Palette:' 18 110 105 | Out-Null
$directorPalette=Add-Combo $director 140 106 200 @('Track identity','Theme fixed') ([string]$config.director_palette)
Add-Label $director 'Stats detail:' 370 110 95 | Out-Null
$statsDetail=Add-Combo $director 470 106 160 @('Broadcast','Stats for Nerds','Completely Unhinged') ([string]$config.stats_detail)
Add-Label $director 'Viz shape:' 660 110 80 | Out-Null
$directorVizShape=Add-Combo $director 740 106 165 @('Use global','Spectrum','Center Mirror','Oscilloscope','Dots','Skyline','Particle Field','Twin Rails') ([string]$config.director_visualizer_shape)

$commentOn=Add-Check $director 'Featured Comment (low-priority, relevance-sorted)' 18 158 $config.featured_comment_enabled 390
Add-Label $director 'Maximum characters:' 420 162 155 | Out-Null
$commentChars=Add-Number $director 580 158 80 40 500 ([int]$config.comment_max_chars)
Add-Label $director 'Filter:' 680 162 50 | Out-Null
$commentFilter=Add-Combo $director 735 158 150 @('Basic safety','Strict','Off') ([string]$config.comment_filter_mode)
$commentHint=Add-Label $director 'One top-level comment runs outside essential bundle-worker slots. It is cached, plain-text only, URL-cleaned and hideable from Controller. The local word filter is a useful guard, not infallible moderation. “Featured” means YouTube relevance sorting.' 38 192 840 52
$commentHint.ForeColor=[System.Drawing.Color]::DimGray

$telemetryOn=Add-Check $director 'Broadcast telemetry and ridiculous derived statistics' 18 258 $config.telemetry_enabled 390
$probeOn=Add-Check $director 'Exact FFprobe codec/container/bitrate inspection' 420 258 $config.telemetry_probe_enabled 390
$historyOn=Add-Check $director 'Show persistent broadcast History module' 18 304 $config.history_enabled 330
Add-Label $director 'Keep entries:' 365 308 100 | Out-Null
$historyMax=Add-Number $director 465 304 80 10 1000 ([int]$config.history_max_entries)

$directorHint=Add-Label $director 'Director Mode adds modular Browser Sources, output groups, synchronized scene timelines, theme worlds, history, Up Next, engineering telemetry, mission codes and culture widgets. Build separate modules on Sources or combine them on Groups 1-6. The original /overlay remains the unchanged default.' 18 360 865 70
$directorHint.ForeColor=[System.Drawing.Color]::DimGray
$copySources=New-Object System.Windows.Forms.Button; $copySources.Text='COPY ENABLED SOURCE PACK'; $copySources.Location=New-Object System.Drawing.Point(18,455); $copySources.Size=New-Object System.Drawing.Size(245,38); $director.Controls.Add($copySources)
$openObsGuide=New-Object System.Windows.Forms.Button; $openObsGuide.Text='OPEN OBS SOURCE GUIDE'; $openObsGuide.Location=New-Object System.Drawing.Point(275,455); $openObsGuide.Size=New-Object System.Drawing.Size(225,38); $director.Controls.Add($openObsGuide)
$directorUrlHint=Add-Label $director 'Workflow: enable Director Mode, choose individual sources and/or grouped outputs, Save, then Copy or Open Preview on the same source row. Pipeline-affecting changes restart YOMI automatically.' 18 515 865 52
$directorUrlHint.ForeColor=[System.Drawing.Color]::DimGray

# INDIVIDUAL MODULAR SOURCES
$sourcesTab=New-Tab 'Sources'
Add-Label $sourcesTab 'Preset:' 12 13 65 | Out-Null
$sourcesPreset=Add-Combo $sourcesTab 78 9 190 @('Default','Split Essentials','Text Only','Information Desk','Culture Desk','Full Science','Custom') ([string]$config.sources_preset)
$sourcesPresetHint=Add-Label $sourcesTab 'Enable a row to prepare it. Copy and Preview use its current URL.' 285 12 455 28
$sourcesPresetHint.ForeColor=[System.Drawing.Color]::DimGray
$copyEnabledFixed=New-Object System.Windows.Forms.Button;$copyEnabledFixed.Text='COPY ENABLED';$copyEnabledFixed.Location=New-Object System.Drawing.Point(760,7);$copyEnabledFixed.Size=New-Object System.Drawing.Size(135,32);$sourcesTab.Controls.Add($copyEnabledFixed)
Add-Label $sourcesTab 'On' 12 45 30 18 | Out-Null
Add-Label $sourcesTab 'Source' 48 45 140 18 | Out-Null
Add-Label $sourcesTab 'W' 195 45 45 18 | Out-Null
Add-Label $sourcesTab 'H' 260 45 45 18 | Out-Null
Add-Label $sourcesTab 'Browser Source URL' 330 45 250 18 | Out-Null
$sourceControls=@()
$sourceIndex=0
foreach($entry in $directorModuleCatalog){
    $saved=@($config.director_fixed_sources|Where-Object{[string]$_.module -eq [string]$entry.Module})|Select-Object -First 1
    if($null -eq $saved){$saved=[PSCustomObject]@{module=$entry.Module;label=$entry.Label;enabled=$false;width=$entry.Width;height=$entry.Height}}
    $y=70+($sourceIndex*38);$sourceIndex++
    $on=Add-Check $sourcesTab '' 12 $y ([bool]$saved.enabled) 28
    Add-Label $sourcesTab ([string]$entry.Label) 48 ($y+3) 140 | Out-Null
    $sw=Add-Number $sourcesTab 195 $y 55 64 7680 ([int]$saved.width)
    $sh=Add-Number $sourcesTab 260 $y 55 32 4320 ([int]$saved.height)
    $url=Add-Text $sourcesTab 330 $y 360 (Get-DirectorModuleUrl $config ([string]$entry.Module));$url.ReadOnly=$true;$url.BackColor=[System.Drawing.Color]::White
    $copy=New-Object System.Windows.Forms.Button;$copy.Text='COPY';$copy.Location=New-Object System.Drawing.Point(700,$y);$copy.Size=New-Object System.Drawing.Size(75,28);$sourcesTab.Controls.Add($copy)
    $preview=New-Object System.Windows.Forms.Button;$preview.Text='PREVIEW';$preview.Location=New-Object System.Drawing.Point(785,$y);$preview.Size=New-Object System.Drawing.Size(110,28);$sourcesTab.Controls.Add($preview)
    $row=[PSCustomObject]@{Module=[string]$entry.Module;Label=[string]$entry.Label;Enabled=$on;Width=$sw;Height=$sh;Url=$url;Copy=$copy;Preview=$preview}
    $on.Tag=$row;$copy.Tag=$row;$preview.Tag=$row;$sourceControls+=$row
}
$sourcesHint=Add-Label $sourcesTab 'Artwork, video, visualizer and comment can add preparation work. Several Browser Sources share YOMI downloads and clock; several video sources still make OBS decode the video several times.' 12 572 885 36
$sourcesHint.ForeColor=[System.Drawing.Color]::DarkGoldenrod

# MODULAR OUTPUT GROUPS
$outputsTab=New-Tab 'Groups 1-6'
Add-Label $outputsTab 'Page preset:' 16 13 95 | Out-Null
$outputsPreset=Add-Combo $outputsTab 115 9 240 @('Default','Minimal','Split Essentials','Broadcast Desk','Full Studio','Custom') ([string]$config.outputs_preset)
$outputsPresetHint=Add-Label $outputsTab 'A preset fills all six rows. Edit Modules opens an ordered checklist.' 380 12 510 28
$outputsPresetHint.ForeColor=[System.Drawing.Color]::DimGray
Add-Label $outputsTab 'On' 16 55 32 | Out-Null
Add-Label $outputsTab 'ID' 48 55 28 | Out-Null
Add-Label $outputsTab 'Name' 76 55 95 | Out-Null
Add-Label $outputsTab 'Modules (display order)' 180 55 205 | Out-Null
Add-Label $outputsTab 'Layout' 440 55 85 | Out-Null
Add-Label $outputsTab 'Theme' 535 55 95 | Out-Null
Add-Label $outputsTab 'W' 645 55 45 | Out-Null
Add-Label $outputsTab 'H' 700 55 45 | Out-Null
$outputControls=@()
for($oid=1;$oid -le 6;$oid++) {
    $saved=@($config.director_outputs | Where-Object { [int]$_.id -eq $oid }) | Select-Object -First 1
    if($null -eq $saved) {
        $saved=[PSCustomObject]@{id=$oid;enabled=$false;name=("Output "+$oid);modules='title,channel';layout='Horizontal';theme='Global';width=900;height=180}
    }
    $y=85+(($oid-1)*66)
    $on=Add-Check $outputsTab '' 16 $y ([bool]$saved.enabled) 28
    Add-Label $outputsTab ([string]$oid) 50 ($y+3) 25 | Out-Null
    $name=Add-Text $outputsTab 76 $y 98 ([string]$saved.name)
    $mods=Add-Text $outputsTab 180 $y 202 ([string]$saved.modules);$mods.ReadOnly=$true;$mods.BackColor=[System.Drawing.Color]::White
    $edit=New-Object System.Windows.Forms.Button;$edit.Text='EDIT';$edit.Location=New-Object System.Drawing.Point(388,$y);$edit.Size=New-Object System.Drawing.Size(46,28);$outputsTab.Controls.Add($edit)
    $layoutChoice=Add-Combo $outputsTab 440 $y 90 @('Broadcast Strip','Horizontal','Stack','Cards','Terminal','Timeline','Single') ([string]$saved.layout)
    $themeChoice=Add-Combo $outputsTab 535 $y 105 (@('Global')+$directorThemes) ([string]$saved.theme)
    $ow=Add-Number $outputsTab 645 $y 50 64 7680 ([int]$saved.width)
    $oh=Add-Number $outputsTab 700 $y 50 32 4320 ([int]$saved.height)
    $copy=New-Object System.Windows.Forms.Button;$copy.Text='COPY';$copy.Location=New-Object System.Drawing.Point(756,$y);$copy.Size=New-Object System.Drawing.Size(62,28);$outputsTab.Controls.Add($copy)
    $preview=New-Object System.Windows.Forms.Button;$preview.Text='PREVIEW';$preview.Location=New-Object System.Drawing.Point(824,$y);$preview.Size=New-Object System.Drawing.Size(78,28);$outputsTab.Controls.Add($preview)
    $row=[PSCustomObject]@{Id=$oid;Enabled=$on;Name=$name;Modules=$mods;Edit=$edit;Layout=$layoutChoice;Theme=$themeChoice;Width=$ow;Height=$oh;Copy=$copy;Preview=$preview}
    $on.Tag=$row;$edit.Tag=$row;$copy.Tag=$row;$preview.Tag=$row;$outputControls+=$row
}
$outputsHint=Add-Label $outputsTab 'Each enabled row becomes /source/1 through /source/6. Copy and Preview are beside the row. Use Sources when you want exactly one module without consuming an output slot.' 18 500 870 70
$outputsHint.ForeColor=[System.Drawing.Color]::DimGray

# COMPONENTS
$componentsTab=New-Tab 'Components'
$compStatus=New-Object System.Windows.Forms.Label; $compStatus.Location=New-Object System.Drawing.Point(18,20); $compStatus.Size=New-Object System.Drawing.Size(860,190); $compStatus.BorderStyle='FixedSingle'; $componentsTab.Controls.Add($compStatus)
$denoBtn=New-Object System.Windows.Forms.Button; $denoBtn.Location=New-Object System.Drawing.Point(18,230); $denoBtn.Size=New-Object System.Drawing.Size(210,36); $componentsTab.Controls.Add($denoBtn)
$ffmpegBtn=New-Object System.Windows.Forms.Button; $ffmpegBtn.Location=New-Object System.Drawing.Point(240,230); $ffmpegBtn.Size=New-Object System.Drawing.Size(230,36); $componentsTab.Controls.Add($ffmpegBtn)
$refreshComponents=New-Object System.Windows.Forms.Button; $refreshComponents.Text='REFRESH STATUS'; $refreshComponents.Location=New-Object System.Drawing.Point(482,230); $refreshComponents.Size=New-Object System.Drawing.Size(150,36); $componentsTab.Controls.Add($refreshComponents)
$checkUpdates=New-Object System.Windows.Forms.Button; $checkUpdates.Text='CHECK FOR UPDATES'; $checkUpdates.Location=New-Object System.Drawing.Point(644,230); $checkUpdates.Size=New-Object System.Drawing.Size(190,36); $componentsTab.Controls.Add($checkUpdates)
$openData=New-Object System.Windows.Forms.Button; $openData.Text='OPEN DATA FOLDER'; $openData.Location=New-Object System.Drawing.Point(18,285); $openData.Size=New-Object System.Drawing.Size(180,36); $componentsTab.Controls.Add($openData)
$openProgram=New-Object System.Windows.Forms.Button; $openProgram.Text='OPEN PROGRAM FOLDER'; $openProgram.Location=New-Object System.Drawing.Point(210,285); $openProgram.Size=New-Object System.Drawing.Size(210,36); $componentsTab.Controls.Add($openProgram)
$uninstallBtn=New-Object System.Windows.Forms.Button; $uninstallBtn.Text='UNINSTALL YOMI...'; $uninstallBtn.Location=New-Object System.Drawing.Point(432,285); $uninstallBtn.Size=New-Object System.Drawing.Size(175,36); $componentsTab.Controls.Add($uninstallBtn)
$compHint=Add-Label $componentsTab 'Core: mpv + yt-dlp. Deno improves modern YouTube JavaScript challenge compatibility. FFmpeg Media Tools unlock loudness leveling, smart artwork crop and the retro visualizer. Removing an optional component does not delete your playlist/config/cache.' 18 345 865 78
$compHint.ForeColor=[System.Drawing.Color]::DimGray

# PREVIEW
$previewTab=New-Tab 'Live Preview'
$preview=New-Object System.Windows.Forms.Panel; $preview.Location=New-Object System.Drawing.Point(18,18); $preview.Size=New-Object System.Drawing.Size(890,420); $preview.BackColor=[System.Drawing.Color]::Black; $preview.BorderStyle='FixedSingle'; $previewTab.Controls.Add($preview)
$previewInfo=Add-Label $previewTab '' 18 455 885 100
$previewInfo.BorderStyle='FixedSingle'

# bottom buttons
$save=New-Object System.Windows.Forms.Button; $save.Text='SAVE SETTINGS'; $save.Location=New-Object System.Drawing.Point(20,730); $save.Size=New-Object System.Drawing.Size(145,40); $form.Controls.Add($save)
$shuffleBtn=New-Object System.Windows.Forms.Button; $shuffleBtn.Text='SHUFFLE PLAYLIST'; $shuffleBtn.Location=New-Object System.Drawing.Point(175,730); $shuffleBtn.Size=New-Object System.Drawing.Size(170,40); $form.Controls.Add($shuffleBtn)
$controllerBtn=New-Object System.Windows.Forms.Button; $controllerBtn.Text='OPEN CONTROLLER'; $controllerBtn.Location=New-Object System.Drawing.Point(355,730); $controllerBtn.Size=New-Object System.Drawing.Size(155,40); $form.Controls.Add($controllerBtn)
$copyOverlay=New-Object System.Windows.Forms.Button; $copyOverlay.Text='COPY OVERLAY URL'; $copyOverlay.Location=New-Object System.Drawing.Point(520,730); $copyOverlay.Size=New-Object System.Drawing.Size(165,40); $form.Controls.Add($copyOverlay)
$obsSetup=New-Object System.Windows.Forms.Button; $obsSetup.Text='OBS SETUP'; $obsSetup.Location=New-Object System.Drawing.Point(695,730); $obsSetup.Size=New-Object System.Drawing.Size(120,40); $form.Controls.Add($obsSetup)
$readme=New-Object System.Windows.Forms.Button; $readme.Text='README'; $readme.Location=New-Object System.Drawing.Point(825,730); $readme.Size=New-Object System.Drawing.Size(120,40); $form.Controls.Add($readme)
$credit=Add-Label $form 'Created and designed by TheSensibleStreamer  |  Powered by mpv | yt-dlp | FFmpeg  |  Development assistance by ChatGPT' 20 782 955 28
$credit.TextAlign='MiddleCenter'; $credit.ForeColor=[System.Drawing.Color]::DimGray
$saveResetTimer=New-Object System.Windows.Forms.Timer; $saveResetTimer.Interval=1700
$saveResetTimer.Add_Tick({$saveResetTimer.Stop();$save.Text='SAVE SETTINGS'})

$script:ApplyingGeneralPreset=$false
$script:ApplyingOverlayPreset=$false
$script:ApplyingCanvasPreset=$false
$script:ApplyingMediaPreset=$false
$script:ApplyingStylePreset=$false
$script:ApplyingVisualizerPreset=$false
$script:ApplyingPerformancePreset=$false
$script:ApplyingDirectorPreset=$false
$script:ApplyingSourcesPreset=$false
$script:ApplyingOutputsPreset=$false

function Apply-SourcesPreset {
    if($script:ApplyingSourcesPreset){return}
    if([string]$sourcesPreset.SelectedItem -eq 'Custom'){return}
    $script:ApplyingSourcesPreset=$true
    try{
        $enabled=@()
        switch([string]$sourcesPreset.SelectedItem){
            'Split Essentials'{$enabled=@('artwork','video','title','channel','visualizer')}
            'Text Only'{$enabled=@('title','channel','progress','upnext')}
            'Information Desk'{$enabled=@('progress','stats','technical','pipeline','mission')}
            'Culture Desk'{$enabled=@('comment','history','upnext')}
            'Full Science'{$enabled=@($directorModuleCatalog|ForEach-Object{$_.Module})}
        }
        foreach($row in $sourceControls){
            $defaults=@($directorModuleCatalog|Where-Object{$_.Module -eq $row.Module})|Select-Object -First 1
            $row.Enabled.Checked=($enabled -contains $row.Module)
            if($defaults){$row.Width.Value=[int]$defaults.Width;$row.Height.Value=[int]$defaults.Height}
        }
    }finally{$script:ApplyingSourcesPreset=$false}
    Update-Dependencies
}

function Enable-DirectorModulePrerequisites([string]$Modules) {
    $items=@($Modules.ToLowerInvariant().Split(',')|ForEach-Object{$_.Trim()})
    if($items -contains 'comment'){$commentOn.Checked=$true}
    if($items -contains 'history'){$historyOn.Checked=$true}
}

function Show-OutputModulePicker($row) {
    $dialog=New-Object System.Windows.Forms.Form;$dialog.Text="Output $($row.Id) - Modules";$dialog.StartPosition='CenterParent';$dialog.Size=New-Object System.Drawing.Size(470,520);$dialog.MinimumSize=$dialog.Size;$dialog.MaximumSize=$dialog.Size;$dialog.MaximizeBox=$false;$dialog.MinimizeBox=$false;$dialog.Font=$form.Font
    $help=Add-Label $dialog 'Check modules, then use Move Up / Move Down to set their display order.' 14 12 425 42;$help.ForeColor=[System.Drawing.Color]::DimGray
    $list=New-Object System.Windows.Forms.CheckedListBox;$list.Location=New-Object System.Drawing.Point(14,58);$list.Size=New-Object System.Drawing.Size(290,365);$list.CheckOnClick=$true;$dialog.Controls.Add($list)
    $current=@(([string]$row.Modules.Text).Split(',')|ForEach-Object{$_.Trim().ToLowerInvariant()}|Where-Object{$_})
    $all=@($directorModuleCatalog|ForEach-Object{$_.Module})
    $ordered=@($current|Where-Object{$all -contains $_})+@($all|Where-Object{$current -notcontains $_})
    foreach($module in $ordered){$index=$list.Items.Add($module);if($current -contains $module){$list.SetItemChecked($index,$true)}}
    $up=New-Object System.Windows.Forms.Button;$up.Text='MOVE UP';$up.Location=New-Object System.Drawing.Point(320,90);$up.Size=New-Object System.Drawing.Size(115,34);$dialog.Controls.Add($up)
    $down=New-Object System.Windows.Forms.Button;$down.Text='MOVE DOWN';$down.Location=New-Object System.Drawing.Point(320,136);$down.Size=New-Object System.Drawing.Size(115,34);$dialog.Controls.Add($down)
    $ok=New-Object System.Windows.Forms.Button;$ok.Text='USE MODULES';$ok.Location=New-Object System.Drawing.Point(305,390);$ok.Size=New-Object System.Drawing.Size(130,34);$dialog.Controls.Add($ok)
    $cancel=New-Object System.Windows.Forms.Button;$cancel.Text='CANCEL';$cancel.Location=New-Object System.Drawing.Point(305,435);$cancel.Size=New-Object System.Drawing.Size(130,30);$dialog.Controls.Add($cancel)
    $up.Add_Click({$i=$list.SelectedIndex;if($i -le 0){return};$item=$list.Items[$i];$checked=$list.GetItemChecked($i);$list.Items.RemoveAt($i);$list.Items.Insert($i-1,$item);$list.SetItemChecked($i-1,$checked);$list.SelectedIndex=$i-1})
    $down.Add_Click({$i=$list.SelectedIndex;if($i -lt 0 -or $i -ge $list.Items.Count-1){return};$item=$list.Items[$i];$checked=$list.GetItemChecked($i);$list.Items.RemoveAt($i);$list.Items.Insert($i+1,$item);$list.SetItemChecked($i+1,$checked);$list.SelectedIndex=$i+1})
    $ok.Add_Click({$selected=@();for($i=0;$i -lt $list.Items.Count;$i++){if($list.GetItemChecked($i)){$selected+=[string]$list.Items[$i]}};if($selected.Count -lt 1){[System.Windows.Forms.MessageBox]::Show('Choose at least one module.','YOMI Output Modules')|Out-Null;return};$row.Modules.Text=($selected -join ',');if($row.Enabled.Checked){Enable-DirectorModulePrerequisites ([string]$row.Modules.Text)};$dialog.DialogResult=[System.Windows.Forms.DialogResult]::OK;$dialog.Close()})
    $cancel.Add_Click({$dialog.DialogResult=[System.Windows.Forms.DialogResult]::Cancel;$dialog.Close()})
    [void]$dialog.ShowDialog($form);$dialog.Dispose()
}

function Apply-GeneralPreset {
    if($script:ApplyingGeneralPreset){return}
    $script:ApplyingGeneralPreset=$true
    try{
        switch([string]$generalPreset.SelectedItem){
            'Default'{$mode.SelectedItem='Streamer / OBS';$playerQuality.SelectedItem='Off (audio only)'}
            'Audio-only Player'{$mode.SelectedItem='Player';$playerQuality.SelectedItem='Off (audio only)'}
            'Efficient Video Player'{$mode.SelectedItem='Player';$playerQuality.SelectedItem='240p'}
            'Quality Video Player'{$mode.SelectedItem='Player';$playerQuality.SelectedItem='720p'}
            'Streamer / OBS'{$mode.SelectedItem='Streamer / OBS';$playerQuality.SelectedItem='Off (audio only)'}
        }
    }finally{$script:ApplyingGeneralPreset=$false}
    Update-Dependencies;Update-Preview
}

function Apply-CanvasPreset {
    if($script:ApplyingCanvasPreset){return}
    $script:ApplyingCanvasPreset=$true
    try{
        switch([string]$canvasPreset.SelectedItem){
            '1280 x 720'{$canvasW.Value=1280;$canvasH.Value=720}
            '1920 x 1080'{$canvasW.Value=1920;$canvasH.Value=1080}
            '2560 x 1440'{$canvasW.Value=2560;$canvasH.Value=1440}
            '3840 x 2160'{$canvasW.Value=3840;$canvasH.Value=2160}
        }
    }finally{$script:ApplyingCanvasPreset=$false}
}
function Set-Media($w,$h,$t){$mediaW.Value=$w;$mediaH.Value=$h;$textSize.Value=$t}
function Apply-MediaPreset {
    if($script:ApplyingMediaPreset){return}
    $script:ApplyingMediaPreset=$true
    try{
        switch([string]$mediaPreset.SelectedItem){
            'Auto'{$w=[int][Math]::Round([int]$canvasW.Value*0.0625);$h=[int][Math]::Round($w*9/16);$t=[int][Math]::Round($w*33/160);Set-Media ([Math]::Max(40,$w)) ([Math]::Max(22,$h)) ([Math]::Max(12,[Math]::Min(72,$t)))}
            'Compact'{Set-Media 80 45 18}'Small'{Set-Media 120 68 26}'Medium'{Set-Media 160 90 33}'Large'{Set-Media 240 135 45}'Extra Large'{Set-Media 320 180 56}
        }
    }finally{$script:ApplyingMediaPreset=$false}
}

function Apply-OverlayPreset {
    if($script:ApplyingOverlayPreset){return}
    if([string]$overlayPreset.SelectedItem -eq 'Custom'){return}
    $script:ApplyingOverlayPreset=$true
    try{
        # Every curated classic-overlay bundle keeps the proven 160x90 joined
        # media geometry. The presets vary workload and presentation instead of
        # quietly moving a user's source around the canvas.
        $canvasPreset.SelectedItem='2560 x 1440';$corner.SelectedItem='Top Left'
        $mediaPreset.SelectedItem='Medium';$videoZoom.Value=125
        $smartCrop.Checked=$true;$titleCheck.Checked=$true;$channel.Checked=$true
        $overlayVideoQuality.SelectedItem='144p (fastest)';$videoFps.SelectedItem='30 FPS';$videoCacheLimit.Value=512
        switch([string]$overlayPreset.SelectedItem){
            'Default'{$art.Checked=$true;$video.Checked=$true;$viz.Checked=$true}
            'Gaming Light'{$art.Checked=$false;$video.Checked=$false;$viz.Checked=$false;$videoCacheLimit.Value=256}
            'Artwork Radio'{$art.Checked=$true;$video.Checked=$false;$viz.Checked=$true;$videoCacheLimit.Value=256}
            'Full Strip'{$art.Checked=$true;$video.Checked=$true;$viz.Checked=$true}
            'Video Showcase'{$art.Checked=$true;$video.Checked=$true;$viz.Checked=$true;$overlayVideoQuality.SelectedItem='360p';$videoFps.SelectedItem='60 FPS when available';$videoCacheLimit.Value=1024}
        }
    }finally{$script:ApplyingOverlayPreset=$false}
    Update-Dependencies;Update-QualityWarning;Update-Preview
}

function Apply-StylePreset {
    if($script:ApplyingStylePreset){return}
    $script:ApplyingStylePreset=$true
    try{
        switch([string]$stylePreset.SelectedItem){
            'Default'{$font.SelectedItem='Bahnschrift Condensed';$textColor.SelectedItem='YOMI Cream';$outlineColor.SelectedItem='Near Black';$outlinePx.Value=6;$textOpacity.Value=100;$textSize.Value=33;$textAlign.SelectedItem='Auto';$textSpacing.SelectedItem='Normal';$glow.Checked=$false;$borderOn.Checked=$true;$borderPx.Value=2;$borderColor.SelectedItem='Dark Gray';$cornerStyle.SelectedItem='Square'}
            'Classic'{$font.SelectedItem='Bahnschrift Condensed';$textColor.SelectedItem='YOMI Cream';$outlineColor.SelectedItem='Near Black';$outlinePx.Value=6;$glow.Checked=$false;$borderOn.Checked=$true;$borderColor.SelectedItem='Dark Gray';$cornerStyle.SelectedItem='Square'}
            'Minimal'{$font.SelectedItem='Segoe UI';$textColor.SelectedItem='White';$outlineColor.SelectedItem='Black';$outlinePx.Value=2;$glow.Checked=$false;$borderOn.Checked=$false;$cornerStyle.SelectedItem='Square'}
            'Retro'{$font.SelectedItem='Courier New';$textColor.SelectedItem='YOMI Cream';$outlineColor.SelectedItem='Black';$outlinePx.Value=4;$glow.Checked=$false;$borderOn.Checked=$true;$cornerStyle.SelectedItem='Square'}
            'Neon'{$font.SelectedItem='Arial Black';$textColor.SelectedItem='Cyan';$outlineColor.SelectedItem='Purple';$outlinePx.Value=4;$glow.Checked=$true;$borderOn.Checked=$true;$borderColor.SelectedItem='Purple';$cornerStyle.SelectedItem='Soft Rounded'}
            'Pastel'{$font.SelectedItem='Segoe Print';$textColor.SelectedItem='Pink';$outlineColor.SelectedItem='Purple';$outlinePx.Value=3;$glow.Checked=$true;$borderOn.Checked=$true;$borderColor.SelectedItem='Lavender';$cornerStyle.SelectedItem='Rounded'}
            'Arcade'{$font.SelectedItem='Impact';$textColor.SelectedItem='Yellow';$outlineColor.SelectedItem='Black';$outlinePx.Value=5;$glow.Checked=$true;$borderOn.Checked=$true;$borderColor.SelectedItem='Black';$cornerStyle.SelectedItem='Square'}
        }
    }finally{$script:ApplyingStylePreset=$false}
    Update-Preview
}

function Get-VisualizerPixelSize([string]$Name) {
    switch($Name){
        'Monolith (12x4)'{return [PSCustomObject]@{Width=12;Height=4;Class='minimum generation cost'}}
        'Mega Blocks (16x5)'{return [PSCustomObject]@{Width=16;Height=5;Class='extremely low generation cost'}}
        'Giant Blocks (20x6)'{return [PSCustomObject]@{Width=20;Height=6;Class='very low generation cost'}}
        'Huge Blocks (28x8)'{return [PSCustomObject]@{Width=28;Height=8;Class='low generation cost'}}
        'Chunky (48x12)'{return [PSCustomObject]@{Width=48;Height=12;Class='moderate detail'}}
        'Fine (64x18)'{return [PSCustomObject]@{Width=64;Height=18;Class='fine detail'}}
        'Extra Fine (96x24)'{return [PSCustomObject]@{Width=96;Height=24;Class='high detail'}}
        'Ultra Fine (128x32)'{return [PSCustomObject]@{Width=128;Height=32;Class='very high detail'}}
        'Microscopic (160x40)'{return [PSCustomObject]@{Width=160;Height=40;Class='extreme detail'}}
        'Maximum Detail (192x48)'{return [PSCustomObject]@{Width=192;Height=48;Class='maximum supported detail'}}
        default{return [PSCustomObject]@{Width=40;Height=10;Class='default balanced detail'}}
    }
}

function Update-VisualizerResolutionInfo {
    $r=Get-VisualizerPixelSize ([string]$chunk.SelectedItem)
    $pixels=[int]$r.Width*[int]$r.Height
    $fps=$(if([string]$vizFps.SelectedItem -match '^60'){60}else{30})
    $samplesPerSecond=$pixels*$fps
    $relative=[int][Math]::Round(($samplesPerSecond/12000.0)*100)
    $visualCost.Text="Generated source: $($r.Width)x$($r.Height) = $pixels pixels/frame x $fps FPS = $samplesPerSecond samples/sec ($relative% of Default 40x10 @ 30).  Profile: $($r.Class)."
}

function Apply-VisualizerPreset {
    if($script:ApplyingVisualizerPreset){return}
    $script:ApplyingVisualizerPreset=$true
    try{
        switch([string]$vizPreset.SelectedItem){
            'Default'{$activity.SelectedItem='Active';$opacityPct.Value=30;$chunk.SelectedItem='Extra Chunky (40x10)';$vizLength.SelectedItem='Wide';$vizColorMode.SelectedItem='Solid';$vizSolid.SelectedItem='Gray';$gradientOrientation.SelectedItem='Horizontal';$vizDirection.SelectedItem='Normal';$vizLayer.SelectedItem='Behind text';$vizShape.SelectedItem='Spectrum';$vizAnchor.SelectedItem='Source';$vizSpacing.SelectedItem='None';$vizPeakGlow.SelectedItem='Off';$vizFrequency.SelectedItem='Logarithmic';$vizFps.SelectedItem='30 FPS';$vizHighTrim.Value=20}
            'Monolith Efficiency'{$activity.SelectedItem='Normal';$opacityPct.Value=30;$chunk.SelectedItem='Monolith (12x4)';$vizLength.SelectedItem='Medium';$vizColorMode.SelectedItem='Solid';$vizSolid.SelectedItem='Gray';$vizShape.SelectedItem='Spectrum';$vizAnchor.SelectedItem='Source';$vizSpacing.SelectedItem='None';$vizPeakGlow.SelectedItem='Off';$vizFrequency.SelectedItem='Logarithmic';$vizFps.SelectedItem='30 FPS';$vizHighTrim.Value=28}
            'Low Overhead Blocks'{$activity.SelectedItem='Normal';$opacityPct.Value=28;$chunk.SelectedItem='Giant Blocks (20x6)';$vizLength.SelectedItem='Medium';$vizColorMode.SelectedItem='Solid';$vizSolid.SelectedItem='Gray';$vizShape.SelectedItem='Spectrum';$vizAnchor.SelectedItem='Source';$vizSpacing.SelectedItem='None';$vizPeakGlow.SelectedItem='Off';$vizFrequency.SelectedItem='Logarithmic';$vizFps.SelectedItem='30 FPS';$vizHighTrim.Value=25}
            'Broadcast'{$activity.SelectedItem='Active';$opacityPct.Value=38;$chunk.SelectedItem='Chunky (48x12)';$vizLength.SelectedItem='Wide';$vizColorMode.SelectedItem='Gradient';$vizGradient.SelectedItem='Sunset';$gradientOrientation.SelectedItem='Horizontal';$vizDirection.SelectedItem='Normal';$vizLayer.SelectedItem='Behind text';$vizShape.SelectedItem='Spectrum';$vizAnchor.SelectedItem='Bottom';$vizSpacing.SelectedItem='Light';$vizPeakGlow.SelectedItem='Subtle';$vizFrequency.SelectedItem='Logarithmic';$vizFps.SelectedItem='30 FPS';$vizHighTrim.Value=20}
            'Smooth 60 FPS'{$activity.SelectedItem='Active';$opacityPct.Value=34;$chunk.SelectedItem='Fine (64x18)';$vizLength.SelectedItem='Wide';$vizColorMode.SelectedItem='Solid';$vizSolid.SelectedItem='Cyan';$vizShape.SelectedItem='Spectrum';$vizAnchor.SelectedItem='Bottom';$vizSpacing.SelectedItem='Light';$vizPeakGlow.SelectedItem='Subtle';$vizFrequency.SelectedItem='Logarithmic';$vizFps.SelectedItem='60 FPS';$vizHighTrim.Value=20}
            'Signal Analyzer'{$activity.SelectedItem='Active';$opacityPct.Value=42;$chunk.SelectedItem='Maximum Detail (192x48)';$vizLength.SelectedItem='Extra Wide';$vizColorMode.SelectedItem='Gradient';$vizGradient.SelectedItem='Ocean';$gradientOrientation.SelectedItem='Vertical';$vizDirection.SelectedItem='Normal';$vizLayer.SelectedItem='Above text';$vizShape.SelectedItem='Oscilloscope';$vizAnchor.SelectedItem='Center';$vizSpacing.SelectedItem='None';$vizPeakGlow.SelectedItem='Subtle';$vizFrequency.SelectedItem='Linear';$vizFps.SelectedItem='60 FPS';$vizHighTrim.Value=0}
            'Neon Motion'{$activity.SelectedItem='Punchy';$opacityPct.Value=48;$chunk.SelectedItem='Extra Fine (96x24)';$vizLength.SelectedItem='Extra Wide';$vizColorMode.SelectedItem='Gradient';$vizGradient.SelectedItem='Ocean';$gradientOrientation.SelectedItem='Horizontal';$vizDirection.SelectedItem='Mirrored';$vizLayer.SelectedItem='Above text';$vizShape.SelectedItem='Particle Field';$vizAnchor.SelectedItem='Center';$vizSpacing.SelectedItem='Light';$vizPeakGlow.SelectedItem='Strong';$vizFrequency.SelectedItem='Logarithmic';$vizFps.SelectedItem='60 FPS';$vizHighTrim.Value=16}
        }
    }finally{$script:ApplyingVisualizerPreset=$false}
    Update-Dependencies;Update-VisualizerResolutionInfo;Update-Preview
}

function Apply-PerformancePreset {
    if($script:ApplyingPerformancePreset){return}
    $script:ApplyingPerformancePreset=$true
    try{
        switch([string]$performancePreset.SelectedItem){
            'Default'{$performance.SelectedItem='Balanced (2 workers)';$browserFps.SelectedItem='Auto';$loudness.Checked=$true;$prefetch.Value=4;$videoPreference.SelectedItem='Prefer selected maximum';$audioQuality.SelectedItem='Best available';$audioPreference.SelectedItem='Prefer selected maximum'}
            'Gaming'{$performance.SelectedItem='Gaming / Lowest overhead (1 worker)';$browserFps.SelectedItem='Auto';$loudness.Checked=$true;$prefetch.Value=2;$videoPreference.SelectedItem='Prefer lowest compatible';$audioQuality.SelectedItem='High (~160 kbps)';$audioPreference.SelectedItem='Prefer selected maximum'}
            'Rapid Cache'{$performance.SelectedItem='Fast caching (4 workers)';$browserFps.SelectedItem='Auto';$loudness.Checked=$true;$prefetch.Value=8;$videoPreference.SelectedItem='Prefer selected maximum';$audioQuality.SelectedItem='Best available';$audioPreference.SelectedItem='Prefer selected maximum'}
            'Deep Cache'{$performance.SelectedItem='Maximum caching (8 workers)';$browserFps.SelectedItem='Auto';$loudness.Checked=$true;$prefetch.Value=20;$videoPreference.SelectedItem='Prefer selected maximum';$audioQuality.SelectedItem='Best available';$audioPreference.SelectedItem='Prefer selected maximum'}
        }
    }finally{$script:ApplyingPerformancePreset=$false}
    Update-Preview
}

function Apply-DirectorPreset {
    if($script:ApplyingDirectorPreset){return}
    if([string]$directorPreset.SelectedItem -eq 'Custom'){return}
    $script:ApplyingDirectorPreset=$true
    try{
        # Establish complete values first so switching from an extravagant preset
        # back to a restrained one never leaves an expensive hidden toggle behind.
        $directorOn.Checked=$true;$directorTheme.SelectedItem='Classic';$directorTimeline.SelectedItem='Broadcast Rotation';$directorMotion.SelectedItem='Subtle';$directorPalette.SelectedItem='Track identity';$statsDetail.SelectedItem='Broadcast';$directorVizShape.SelectedItem='Use global'
        $commentOn.Checked=$false;$commentChars.Value=220;$commentFilter.SelectedItem='Basic safety';$telemetryOn.Checked=$false;$probeOn.Checked=$false;$historyOn.Checked=$false;$historyMax.Value=100
        switch([string]$directorPreset.SelectedItem){
            'Default'{$directorOn.Checked=$false}
            'Pirate Radio'{$directorTheme.SelectedItem='Pirate Radio';$directorTimeline.SelectedItem='Rapid Rotation';$directorMotion.SelectedItem='Full';$commentOn.Checked=$true;$historyOn.Checked=$true}
            'Culture Desk'{$directorTheme.SelectedItem='Record Store';$directorTimeline.SelectedItem='Cinematic';$directorMotion.SelectedItem='Subtle';$commentOn.Checked=$true;$historyOn.Checked=$true}
            'Control Room'{$directorTheme.SelectedItem='Control Room';$directorPalette.SelectedItem='Theme fixed';$statsDetail.SelectedItem='Stats for Nerds';$telemetryOn.Checked=$true;$probeOn.Checked=$true;$historyOn.Checked=$true}
            'Full Science'{$directorTheme.SelectedItem='Cyberpunk Lab';$directorTimeline.SelectedItem='Rapid Rotation';$directorMotion.SelectedItem='Full';$statsDetail.SelectedItem='Completely Unhinged';$directorVizShape.SelectedItem='Particle Field';$commentOn.Checked=$true;$telemetryOn.Checked=$true;$probeOn.Checked=$true;$historyOn.Checked=$true}
        }
    }finally{$script:ApplyingDirectorPreset=$false}
    Update-Dependencies;Update-Preview
}

function Set-OutputRow($row,[bool]$enabled,[string]$name,[string]$modules,[string]$layout,[string]$theme,[int]$width,[int]$height) {
    $row.Enabled.Checked=$enabled;$row.Name.Text=$name;$row.Modules.Text=$modules;$row.Layout.SelectedItem=$layout;$row.Theme.SelectedItem=$theme;$row.Width.Value=$width;$row.Height.Value=$height
}

function Apply-OutputsPreset {
    if($script:ApplyingOutputsPreset){return}
    if([string]$outputsPreset.SelectedItem -eq 'Custom'){return}
    $script:ApplyingOutputsPreset=$true
    try{
        $rows=@($outputControls)
        switch([string]$outputsPreset.SelectedItem){
            'Default'{
                Set-OutputRow ($rows[0]) $true 'Now Playing' 'artwork,video,title,channel,visualizer' 'Broadcast Strip' 'Global' 2560 90
                Set-OutputRow ($rows[1]) $false 'Information' 'stats,technical,progress' 'Cards' 'Global' 900 260
                Set-OutputRow ($rows[2]) $false 'Culture' 'comment,history,upnext' 'Stack' 'Global' 900 420
                Set-OutputRow ($rows[3]) $false 'Engineering' 'technical,pipeline,mission' 'Terminal' 'Archive Terminal' 900 420
                Set-OutputRow ($rows[4]) $false 'Director Timeline' 'artwork,video,title,channel,visualizer,comment,stats,technical,progress,history,upnext,mission' 'Timeline' 'Global' 1920 1080
                Set-OutputRow ($rows[5]) $false 'Custom' 'title,channel' 'Horizontal' 'Global' 900 180
            }
            'Minimal'{
                Set-OutputRow ($rows[0]) $true 'Now Playing' 'title,channel' 'Horizontal' 'Global' 900 180
                for($i=1;$i -lt 6;$i++){Set-OutputRow ($rows[$i]) $false ("Output "+($i+1)) 'title,channel' 'Horizontal' 'Global' 900 180}
            }
            'Split Essentials'{
                Set-OutputRow ($rows[0]) $true 'Artwork' 'artwork' 'Single' 'Global' 320 180
                Set-OutputRow ($rows[1]) $true 'Video' 'video' 'Single' 'Global' 320 180
                Set-OutputRow ($rows[2]) $true 'Title Card' 'title,channel' 'Stack' 'Global' 900 180
                Set-OutputRow ($rows[3]) $true 'Visualizer' 'visualizer' 'Single' 'Global' 900 180
                Set-OutputRow ($rows[4]) $true 'Culture' 'comment,history,upnext' 'Stack' 'Global' 900 420
                Set-OutputRow ($rows[5]) $true 'Stats Lab' 'stats,technical,pipeline,mission' 'Cards' 'Control Room' 1200 420
            }
            'Broadcast Desk'{
                Set-OutputRow ($rows[0]) $true 'Now Playing' 'artwork,video,title,channel,visualizer' 'Broadcast Strip' 'Global' 2560 90
                Set-OutputRow ($rows[1]) $true 'Information' 'stats,technical,progress' 'Cards' 'Global' 900 260
                Set-OutputRow ($rows[2]) $true 'Culture' 'comment,history,upnext' 'Stack' 'Global' 900 420
                Set-OutputRow ($rows[3]) $true 'Engineering' 'technical,pipeline,mission' 'Terminal' 'Archive Terminal' 900 420
                Set-OutputRow ($rows[4]) $false 'Director Timeline' 'artwork,video,title,channel,visualizer,comment,stats,technical,progress,history,upnext,mission' 'Timeline' 'Global' 1920 1080
                Set-OutputRow ($rows[5]) $false 'Custom' 'title,channel' 'Horizontal' 'Global' 900 180
            }
            'Full Studio'{
                Set-OutputRow ($rows[0]) $true 'Now Playing' 'artwork,video,title,channel,visualizer' 'Broadcast Strip' 'Global' 2560 90
                Set-OutputRow ($rows[1]) $true 'Telemetry Wall' 'stats,technical,pipeline,mission' 'Cards' 'Control Room' 1200 500
                Set-OutputRow ($rows[2]) $true 'Culture Desk' 'comment,history,upnext' 'Stack' 'Record Store' 900 500
                Set-OutputRow ($rows[3]) $true 'Engineering' 'technical,pipeline,mission' 'Terminal' 'Archive Terminal' 900 420
                Set-OutputRow ($rows[4]) $true 'Director Timeline' 'artwork,video,title,channel,visualizer,comment,stats,technical,progress,history,upnext,mission' 'Timeline' 'Global' 1920 1080
                Set-OutputRow ($rows[5]) $true 'Mission Control' 'upnext,progress,stats,mission' 'Horizontal' 'Spacecraft' 1600 260
            }
        }
    }finally{$script:ApplyingOutputsPreset=$false}
    Update-Preview
}

function Get-UiConfig {
    $o=Get-YomiConfig
    $o.general_preset=[string]$generalPreset.SelectedItem; $o.app_mode=[string]$mode.SelectedItem; $o.player_video_quality=[string]$playerQuality.SelectedItem
    $o.video_preference=[string]$videoPreference.SelectedItem; $o.audio_quality=[string]$audioQuality.SelectedItem; $o.audio_preference=[string]$audioPreference.SelectedItem
    $o.overlay_preset=[string]$overlayPreset.SelectedItem; $o.canvas_preset=[string]$canvasPreset.SelectedItem; $o.canvas_width=[int]$canvasW.Value; $o.canvas_height=[int]$canvasH.Value; $o.corner=[string]$corner.SelectedItem
    $o.media_size_preset=[string]$mediaPreset.SelectedItem; $o.media_width=[int]$mediaW.Value; $o.media_height=[int]$mediaH.Value; $o.video_zoom=[Math]::Round(([double]$videoZoom.Value/100.0),2)
    $o.overlay_video_quality=[string]$overlayVideoQuality.SelectedItem; $o.video_fps=[string]$videoFps.SelectedItem; $o.video_cache_limit_mb=[int]$videoCacheLimit.Value
    $o.artwork_enabled=[bool]$art.Checked; $o.smart_artwork_crop=[bool]$smartCrop.Checked; $o.video_enabled=[bool]$video.Checked; $o.title_enabled=[bool]$titleCheck.Checked; $o.channel_enabled=[bool]$channel.Checked; $o.visualizer_enabled=[bool]$viz.Checked
    $o.style_preset=[string]$stylePreset.SelectedItem; $o.text_font=[string]$font.SelectedItem; $o.text_color=Map-Color ([string]$textColor.SelectedItem); $o.outline_color=Map-Color ([string]$outlineColor.SelectedItem); $o.text_outline=[int]$outlinePx.Value; $o.text_opacity=[Math]::Round(([double]$textOpacity.Value/100.0),2); $o.text_size=[int]$textSize.Value; $o.text_alignment=[string]$textAlign.SelectedItem; $o.title_channel_spacing=[string]$textSpacing.SelectedItem; $o.text_glow=[bool]$glow.Checked
    $o.media_border_enabled=[bool]$borderOn.Checked; $o.media_border_px=[int]$borderPx.Value; $o.media_border_color=Map-Color ([string]$borderColor.SelectedItem); $o.media_corner_style=[string]$cornerStyle.SelectedItem
    $o.visualizer_preset=[string]$vizPreset.SelectedItem; $o.visualizer_activity=[string]$activity.SelectedItem; $o.visualizer_opacity=[Math]::Round(([double]$opacityPct.Value/100.0),2)
    $vizPixels=Get-VisualizerPixelSize ([string]$chunk.SelectedItem);$o.visualizer_internal_width=[int]$vizPixels.Width;$o.visualizer_internal_height=[int]$vizPixels.Height
    switch([string]$vizLength.SelectedItem){'Short'{$o.visualizer_length_multiplier=2.0}'Medium'{$o.visualizer_length_multiplier=3.0}'Extra Wide'{$o.visualizer_length_multiplier=6.0}default{$o.visualizer_length_multiplier=4.0}}
    $o.visualizer_color_mode=[string]$vizColorMode.SelectedItem; $o.visualizer_solid_color=Map-Color ([string]$vizSolid.SelectedItem); $o.visualizer_gradient_preset=[string]$vizGradient.SelectedItem; $o.visualizer_gradient_orientation=[string]$gradientOrientation.SelectedItem; $o.visualizer_direction=[string]$vizDirection.SelectedItem; $o.visualizer_layer=[string]$vizLayer.SelectedItem
    $o.visualizer_shape=[string]$vizShape.SelectedItem; $o.visualizer_vertical_anchor=[string]$vizAnchor.SelectedItem; $o.visualizer_bar_spacing=[string]$vizSpacing.SelectedItem; $o.visualizer_peak_glow=[string]$vizPeakGlow.SelectedItem; $o.visualizer_frequency_scale=[string]$vizFrequency.SelectedItem; $o.visualizer_high_frequency_trim=[int]$vizHighTrim.Value; $o.visualizer_fps=[string]$vizFps.SelectedItem
    $o.performance_preset=[string]$performancePreset.SelectedItem; $o.browser_fps_mode=[string]$browserFps.SelectedItem; $o.loudness_normalization=[bool]$loudness.Checked; $o.prefetch_ahead=[int]$prefetch.Value; $o.video_prefetch_ahead=[int]$prefetch.Value
    $o.director_preset=[string]$directorPreset.SelectedItem; $o.director_mode=[bool]$directorOn.Checked; $o.director_theme=[string]$directorTheme.SelectedItem; $o.director_timeline=[string]$directorTimeline.SelectedItem; $o.director_motion=[string]$directorMotion.SelectedItem; $o.director_palette=[string]$directorPalette.SelectedItem; $o.director_visualizer_shape=[string]$directorVizShape.SelectedItem; $o.stats_detail=[string]$statsDetail.SelectedItem
    $o.featured_comment_enabled=[bool]$commentOn.Checked; $o.comment_max_chars=[int]$commentChars.Value; $o.comment_filter_mode=[string]$commentFilter.SelectedItem
    $o.telemetry_enabled=[bool]$telemetryOn.Checked; $o.telemetry_probe_enabled=[bool]$probeOn.Checked
    $o.history_enabled=[bool]$historyOn.Checked; $o.history_max_entries=[int]$historyMax.Value
    $o.sources_preset=[string]$sourcesPreset.SelectedItem
    $fixed=@()
    foreach($row in $sourceControls){
        $fixed += [PSCustomObject]@{module=[string]$row.Module;label=[string]$row.Label;enabled=[bool]$row.Enabled.Checked;width=[int]$row.Width.Value;height=[int]$row.Height.Value}
    }
    $o.director_fixed_sources=$fixed
    $outs=@()
    foreach($row in $outputControls) {
        $outs += [PSCustomObject]@{
            id=[int]$row.Id
            enabled=[bool]$row.Enabled.Checked
            name=[string]$row.Name.Text.Trim()
            modules=[string]$row.Modules.Text.Trim().ToLowerInvariant()
            layout=[string]$row.Layout.SelectedItem
            theme=[string]$row.Theme.SelectedItem
            width=[int]$row.Width.Value
            height=[int]$row.Height.Value
        }
    }
    $o.outputs_preset=[string]$outputsPreset.SelectedItem;$o.director_outputs=$outs
    switch([string]$performance.SelectedItem){'Balanced (2 workers)'{$o.performance_mode='Balanced';$o.cache_workers=2;$o.cache_priority='idle'}'Fast caching (4 workers)'{$o.performance_mode='Fast caching';$o.cache_workers=4;$o.cache_priority='below'}'Maximum caching (8 workers)'{$o.performance_mode='Maximum caching';$o.cache_workers=8;$o.cache_priority='below'}default{$o.performance_mode='Gaming / Lowest overhead';$o.cache_workers=1;$o.cache_priority='idle'}}
    return $o
}

function Get-YomiEngineConfigSignature($c) {
    $fixed=@($c.director_fixed_sources|Sort-Object module|ForEach-Object{"$($_.module):$([bool]$_.enabled)"})
    $outputs=@($c.director_outputs|Sort-Object id|ForEach-Object{"$($_.id):$([bool]$_.enabled):$($_.modules)"})
    $projection=[ordered]@{
        app_mode=[string]$c.app_mode;player_video_quality=[string]$c.player_video_quality
        overlay_video_quality=[string]$c.overlay_video_quality;video_preference=[string]$c.video_preference;video_fps=[string]$c.video_fps
        audio_quality=[string]$c.audio_quality;audio_preference=[string]$c.audio_preference;loudness_normalization=[bool]$c.loudness_normalization
        artwork_enabled=[bool]$c.artwork_enabled;video_enabled=[bool]$c.video_enabled;visualizer_enabled=[bool]$c.visualizer_enabled
        smart_artwork_crop=[bool]$c.smart_artwork_crop;media_width=[int]$c.media_width;media_height=[int]$c.media_height
        visualizer_internal_width=[int]$c.visualizer_internal_width;visualizer_internal_height=[int]$c.visualizer_internal_height
        visualizer_activity=[string]$c.visualizer_activity;visualizer_frequency_scale=[string]$c.visualizer_frequency_scale;visualizer_fps=[string]$c.visualizer_fps
        cache_workers=[int]$c.cache_workers;prefetch_ahead=[int]$c.prefetch_ahead;cache_priority=[string]$c.cache_priority
        director_mode=[bool]$c.director_mode;fixed_sources=$fixed;grouped_outputs=$outputs
        featured_comment_enabled=[bool]$c.featured_comment_enabled;comment_max_chars=[int]$c.comment_max_chars;comment_filter_mode=[string]$c.comment_filter_mode
        telemetry_enabled=[bool]$c.telemetry_enabled;telemetry_probe_enabled=[bool]$c.telemetry_probe_enabled
        history_enabled=[bool]$c.history_enabled;history_max_entries=[int]$c.history_max_entries
    }
    return ($projection|ConvertTo-Json -Depth 8 -Compress)
}

function Test-YomiRuntimeRunning {
    $stateRoot=Join-Path $DataRoot 'state'
    foreach($name in @('engine.pid','supervisor.pid')){
        $path=Join-Path $stateRoot $name;if(-not(Test-Path $path)){continue}
        $pidValue=0;try{[void][int]::TryParse((Get-Content $path -Raw).Trim(),[ref]$pidValue)}catch{}
        if($pidValue -gt 0 -and (Get-Process -Id $pidValue -ErrorAction SilentlyContinue)){return $true}
    }
    return $false
}

function Request-YomiControlledRestart {
    $stateRoot=Join-Path $DataRoot 'state';New-Item -ItemType Directory -Path $stateRoot -Force|Out-Null
    Set-Content (Join-Path $stateRoot 'restart-request.txt') 'settings-engine-change' -Encoding ASCII
    Start-Process (Join-Path $PSScriptRoot 'YomiLauncher.exe') -ArgumentList 'controller'
}

function Update-QualityWarning {
    $q=[string]$overlayVideoQuality.SelectedItem
    $show=([string]$mode.SelectedItem -eq 'Streamer / OBS') -and $video.Checked -and ($q -eq '720p' -or $q -eq 'Best compatible')
    $qualityWarning.Visible=$show
    if($show){$obsHint.Location=New-Object System.Drawing.Point(18,360)}
    else{$obsHint.Location=New-Object System.Drawing.Point(18,292)}
}

function Update-Dependencies {
    $streamer=([string]$mode.SelectedItem -eq 'Streamer / OBS')
    $ff=Test-YomiComponent 'ffmpeg'
    $playerQuality.Enabled=(-not $streamer)
    foreach($c in @($overlayPreset,$canvasPreset,$canvasW,$canvasH,$corner,$mediaPreset,$mediaW,$mediaH,$videoZoom,$overlayVideoQuality,$videoFps,$videoCacheLimit,$art,$video,$titleCheck,$channel,$font,$textColor,$outlineColor,$outlinePx,$textOpacity,$textSize,$textAlign,$textSpacing,$glow,$borderOn,$borderPx,$borderColor,$cornerStyle,$vizPreset,$activity,$opacityPct,$chunk,$vizLength,$vizColorMode,$vizSolid,$vizGradient,$gradientOrientation,$vizDirection,$vizLayer,$vizShape,$vizAnchor,$vizSpacing,$vizPeakGlow,$vizFrequency,$vizFps,$vizHighTrim,$browserFps)){ $c.Enabled=$streamer }
    $viz.Enabled=($streamer -and $ff)
    foreach($c in @($vizPreset,$activity,$opacityPct,$chunk,$vizLength,$vizColorMode,$vizSolid,$vizGradient,$gradientOrientation,$vizDirection,$vizLayer,$vizShape,$vizAnchor,$vizSpacing,$vizPeakGlow,$vizFrequency,$vizFps,$vizHighTrim)) { $c.Enabled=($streamer -and $ff) }
    $vizActive=($streamer -and $ff)
    $colorMode=[string]$vizColorMode.SelectedItem
    $vizSolid.Enabled=($vizActive -and $colorMode -eq 'Solid')
    $vizGradient.Enabled=($vizActive -and $colorMode -eq 'Gradient')
    $gradientOrientation.Enabled=($vizActive -and ($colorMode -eq 'Gradient' -or $colorMode -eq 'Rainbow'))
    $smartCrop.Enabled=($streamer -and $ff)
    $loudness.Enabled=$ff
    $directorOn.Enabled=$streamer
    $directorPreset.Enabled=$streamer
    $directorActive=($streamer -and $directorOn.Checked)
    foreach($c in @($directorTheme,$directorTimeline,$directorMotion,$directorPalette,$directorVizShape,$statsDetail,$commentOn,$telemetryOn,$historyOn,$copySources,$openObsGuide)){ $c.Enabled=$directorActive }
    $commentChars.Enabled=($directorActive -and $commentOn.Checked)
    $commentFilter.Enabled=($directorActive -and $commentOn.Checked)
    $probeOn.Enabled=($directorActive -and $telemetryOn.Checked -and $ff)
    $historyMax.Enabled=($directorActive -and $historyOn.Checked)
    $sourcesPreset.Enabled=$directorActive
    $anyFixed=$false
    foreach($row in $sourceControls){
        $supported=($row.Module -ne 'visualizer' -or $ff)
        $row.Enabled.Enabled=($directorActive -and $supported)
        $active=($directorActive -and $supported -and $row.Enabled.Checked)
        $row.Width.Enabled=$active;$row.Height.Enabled=$active;$row.Url.Enabled=$directorActive;$row.Copy.Enabled=$active;$row.Preview.Enabled=$active
        if($active){$anyFixed=$true}
    }
    $copyEnabledFixed.Enabled=($directorActive -and $anyFixed)
    foreach($row in $outputControls) {
        foreach($c in @($row.Enabled,$row.Name,$row.Modules,$row.Edit,$row.Layout,$row.Theme,$row.Width,$row.Height)) { $c.Enabled=$directorActive }
        $row.Copy.Enabled=($directorActive -and $row.Enabled.Checked);$row.Preview.Enabled=($directorActive -and $row.Enabled.Checked)
    }
    $outputsPreset.Enabled=$directorActive
    $copyOverlay.Enabled=$streamer; $obsSetup.Enabled=$streamer
    Update-QualityWarning
}

function Update-Components {
    $mp=Get-YomiComponentInfo 'mpv'; $yt=Get-YomiComponentInfo 'ytdlp'; $de=Get-YomiComponentInfo 'deno'; $ff=Get-YomiComponentInfo 'ffmpeg'
    function Line($label,$x,$required){if($x.Installed){$v=$x.Version;if($v.Length -gt 55){$v=$v.Substring(0,55)};return "$label : INSTALLED  |  $($x.SizeMB) MB  |  $v"}elseif($required){return "$label : MISSING - repair/reinstall YOMI"}else{return "$label : Not installed"}}
    $compStatus.Text=(Line 'mpv (core)' $mp $true)+"`r`n"+(Line 'yt-dlp (core)' $yt $true)+"`r`n"+(Line 'Deno / YouTube Compatibility' $de $false)+"`r`n"+(Line 'FFmpeg Media Tools' $ff $false)+"`r`n`r`nOptional components can be added or removed later."
    if($de.Installed){$denoBtn.Text='REMOVE DENO'}else{$denoBtn.Text='INSTALL DENO'}
    if($ff.Installed){$ffmpegBtn.Text='REMOVE FFMPEG MEDIA TOOLS'}else{$ffmpegBtn.Text='INSTALL FFMPEG MEDIA TOOLS'}
    Update-Dependencies
}

function Preview-AlphaColor([System.Drawing.Color]$c,[double]$opacity) {
    $a=[Math]::Max(0,[Math]::Min(255,[int][Math]::Round(255*$opacity)))
    return [System.Drawing.Color]::FromArgb($a,$c.R,$c.G,$c.B)
}

function Preview-MixColor([System.Drawing.Color]$a,[System.Drawing.Color]$b,[double]$t) {
    $t=[Math]::Max(0,[Math]::Min(1,$t))
    return [System.Drawing.Color]::FromArgb(
        255,
        [int][Math]::Round($a.R+(($b.R-$a.R)*$t)),
        [int][Math]::Round($a.G+(($b.G-$a.G)*$t)),
        [int][Math]::Round($a.B+(($b.B-$a.B)*$t))
    )
}

function Preview-VizColor($u,[double]$t) {
    if([string]$u.visualizer_color_mode -eq 'Rainbow') {
        $colors=@('#FF6B6B','#FFD166','#7BE495','#70E1F5','#72A7FF','#C4A7FF','#FF86C8')
        $p=$t*($colors.Count-1)
        $i=[Math]::Min($colors.Count-2,[Math]::Max(0,[int][Math]::Floor($p)))
        $a=Hex-Color $colors[$i]
        $b=Hex-Color $colors[$i+1]
        return (Preview-MixColor $a $b ($p-$i))
    }

    if([string]$u.visualizer_color_mode -eq 'Gradient') {
        $sets=@{
            'Sunset'=@('#FF6B6B','#FFA552','#C4A7FF')
            'Ocean'=@('#72A7FF','#70E1F5','#9B7BFF')
            'Pastel'=@('#FF86C8','#C4A7FF','#70E1F5')
            'Fire'=@('#FF6B6B','#FFA552','#FFD166')
            'Forest'=@('#2E7D32','#7BE495','#FFD166')
            'Mono'=@('#F2F0E8','#8A8A84','#252525')
        }
        $set=$sets[[string]$u.visualizer_gradient_preset]
        if($null -eq $set){$set=$sets['Sunset']}
        if($t -lt 0.5) {
            $a=Hex-Color $set[0]
            $b=Hex-Color $set[1]
            return (Preview-MixColor $a $b ($t*2))
        }
        $a=Hex-Color $set[1]
        $b=Hex-Color $set[2]
        return (Preview-MixColor $a $b (($t-0.5)*2))
    }

    return (Hex-Color ([string]$u.visualizer_solid_color))
}

function Preview-DrawText(
    [System.Drawing.Graphics]$g,
    [string]$text,
    [System.Drawing.Font]$font,
    [System.Drawing.Rectangle]$rect,
    [System.Drawing.Color]$textColor,
    [System.Drawing.Color]$outlineColor,
    [int]$outlinePx,
    [bool]$glow,
    [string]$alignment
) {
    $fmt=New-Object System.Drawing.StringFormat
    if($alignment -eq 'Right'){$fmt.Alignment=[System.Drawing.StringAlignment]::Far}
    elseif($alignment -eq 'Center'){$fmt.Alignment=[System.Drawing.StringAlignment]::Center}
    else{$fmt.Alignment=[System.Drawing.StringAlignment]::Near}
    $fmt.LineAlignment=[System.Drawing.StringAlignment]::Near
    $fmt.Trimming=[System.Drawing.StringTrimming]::EllipsisCharacter
    $fmt.FormatFlags=[System.Drawing.StringFormatFlags]::NoWrap

    if($glow) {
        $gb=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(50,$textColor.R,$textColor.G,$textColor.B))
        foreach($dx in @(-3,0,3)) {
            foreach($dy in @(-3,0,3)) {
                if($dx -eq 0 -and $dy -eq 0){continue}
                $r=New-Object System.Drawing.Rectangle($rect.X+$dx,$rect.Y+$dy,$rect.Width,$rect.Height)
                $g.DrawString($text,$font,$gb,$r,$fmt)
            }
        }
        $gb.Dispose()
    }

    if($outlinePx -gt 0) {
        $ob=New-Object System.Drawing.SolidBrush($outlineColor)
        foreach($dx in @(-$outlinePx,0,$outlinePx)) {
            foreach($dy in @(-$outlinePx,0,$outlinePx)) {
                if($dx -eq 0 -and $dy -eq 0){continue}
                $r=New-Object System.Drawing.Rectangle($rect.X+$dx,$rect.Y+$dy,$rect.Width,$rect.Height)
                $g.DrawString($text,$font,$ob,$r,$fmt)
            }
        }
        $ob.Dispose()
    }

    $tb=New-Object System.Drawing.SolidBrush($textColor)
    $g.DrawString($text,$font,$tb,$rect,$fmt)
    $tb.Dispose()
    $fmt.Dispose()
}

function Update-Preview {
    try{
        $u=Get-UiConfig
        $m=Get-YomiLayoutMetrics $u
        $previewInfo.Text="Overlay close-up  |  Canvas $($m.CanvasWidth)x$($m.CanvasHeight), $($u.corner)  |  Browser $($m.OverlayWidth)x$($m.OverlayHeight) @ $($m.BrowserFps) FPS`r`nMedia $($m.MediaWidth)x$($m.MediaHeight)  |  Font $($u.text_font), $($u.text_size)px  |  $($u.performance_mode), $($u.cache_workers) workers`r`nPreview uses generic sample content only. It never reads your playlist, current track or cache."
        $preview.Invalidate()
    }catch{}
}

$preview.Add_Paint({
    param($sender,$e)

    try{
        $u=Get-UiConfig
        $m=Get-YomiLayoutMetrics $u
        $g=$e.Graphics
        $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.TextRenderingHint=[System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

        $bg=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(20,20,20))
        $g.FillRectangle($bg,0,0,$sender.Width,$sender.Height)
        $bg.Dispose()

        # Transparent-canvas checkerboard.
        $cell=18
        $a=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(39,39,39))
        $b=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(48,48,48))
        for($yy=28;$yy -lt 270;$yy+=$cell) {
            for($xx=18;$xx -lt ($sender.Width-18);$xx+=$cell) {
                $index=(([int](($xx-18)/$cell))+([int](($yy-28)/$cell))) % 2
                if($index -eq 0){$g.FillRectangle($a,$xx,$yy,$cell,$cell)}
                else{$g.FillRectangle($b,$xx,$yy,$cell,$cell)}
            }
        }
        $a.Dispose()
        $b.Dispose()

        $capFont=New-Object System.Drawing.Font('Segoe UI Semibold',9)
        $capBrush=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Gainsboro)
        $g.DrawString('OVERLAY CLOSE-UP',$capFont,$capBrush,18,6)

        if([string]$u.app_mode -ne 'Streamer / OBS') {
            $f1=New-Object System.Drawing.Font('Segoe UI Semibold',18)
            $f2=New-Object System.Drawing.Font('Segoe UI',10)
            $w=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
            $d=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Silver)
            $g.DrawString('Player mode',$f1,$w,42,96)
            $g.DrawString('OBS overlay disabled — configure player-video quality in General.',$f2,$d,42,138)
            $w.Dispose();$d.Dispose();$f1.Dispose();$f2.Dispose();$capBrush.Dispose();$capFont.Dispose()
            return
        }

        $mh=[Math]::Max(20,[double]$m.MediaHeight)
        $mw=[Math]::Max(20,[double]$m.MediaWidth)
        $scale=[Math]::Min(2.0,[Math]::Max(0.50,126.0/$mh))
        $pmh=[int][Math]::Round($mh*$scale)
        $pmw=[int][Math]::Round($mw*$scale)

        $borderPx=0
        if([bool]$u.media_border_enabled){
            $borderPx=[Math]::Max(1,[int][Math]::Round([double]$u.media_border_px*$scale))
        }

        $artOn=[bool]$u.artwork_enabled
        $vidOn=[bool]$u.video_enabled
        $vizOn=[bool]$u.visualizer_enabled
        $right=([string]$u.corner).EndsWith('Right')

        $count=0
        if($artOn){$count++}
        if($vidOn){$count++}
        $mediaExtent=$count*$pmw
        if($artOn -and $vidOn -and $borderPx -gt 0){$mediaExtent-=$borderPx}

        $stripY=[Math]::Max(54,[int](145-($pmh/2)))
        if(($stripY+$pmh) -gt 258){$stripY=258-$pmh}

        $margin=34
        $gap=[Math]::Max(7,[int][Math]::Round(14*$scale))
        $textWidth=[Math]::Max(220,$sender.Width-(2*$margin)-$mediaExtent-$gap-20)

        if(-not $right) {
            $mediaX=$margin
            $textX=$mediaX+$mediaExtent+$gap
            $mediaEdge=$mediaX+$mediaExtent
        } else {
            $mediaX=$sender.Width-$margin-$mediaExtent
            $textX=$mediaX-$gap-$textWidth
            $mediaEdge=$mediaX
        }

        $borderColor=Hex-Color ([string]$u.media_border_color)
        $artFill=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(68,78,96))
        $videoFill=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(40,56,56))
        $borderBrush=New-Object System.Drawing.SolidBrush($borderColor)

        # Media placeholders with the configured frame visibly on top.
        if(-not $right) {
            $x=$mediaX
            if($artOn) {
                $g.FillRectangle($artFill,$x,$stripY,$pmw,$pmh)
                if($borderPx -gt 0){
                    $pen=New-Object System.Drawing.Pen($borderColor,$borderPx)
                    $g.DrawRectangle($pen,$x,$stripY,$pmw-1,$pmh-1)
                    $pen.Dispose()
                }
                $x+=$pmw
                if($vidOn -and $borderPx -gt 0){$x-=$borderPx}
            }
            if($vidOn) {
                $g.FillRectangle($videoFill,$x,$stripY,$pmw,$pmh)
                if($borderPx -gt 0){
                    $pen=New-Object System.Drawing.Pen($borderColor,$borderPx)
                    $g.DrawRectangle($pen,$x,$stripY,$pmw-1,$pmh-1)
                    $pen.Dispose()
                }
            }
        } else {
            $x=$mediaX+$mediaExtent
            if($artOn) {
                $x-=$pmw
                $g.FillRectangle($artFill,$x,$stripY,$pmw,$pmh)
                if($borderPx -gt 0){
                    $pen=New-Object System.Drawing.Pen($borderColor,$borderPx)
                    $g.DrawRectangle($pen,$x,$stripY,$pmw-1,$pmh-1)
                    $pen.Dispose()
                }
                if($vidOn -and $borderPx -gt 0){$x+=$borderPx}
            }
            if($vidOn) {
                $x-=$pmw
                $g.FillRectangle($videoFill,$x,$stripY,$pmw,$pmh)
                if($borderPx -gt 0){
                    $pen=New-Object System.Drawing.Pen($borderColor,$borderPx)
                    $g.DrawRectangle($pen,$x,$stripY,$pmw-1,$pmh-1)
                    $pen.Dispose()
                }
            }
        }

        $artFill.Dispose();$videoFill.Dispose();$borderBrush.Dispose()

        # Small neutral labels so the two media modules are obvious.
        $moduleFont=New-Object System.Drawing.Font('Segoe UI Semibold',[single][Math]::Max(8,[Math]::Min(13,$pmh*0.13)))
        $moduleBrush=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(205,245,245,245))
        if(-not $right) {
            $x=$mediaX
            if($artOn){$g.DrawString('ART',$moduleFont,$moduleBrush,$x+8,$stripY+8);$x+=$pmw;if($vidOn -and $borderPx -gt 0){$x-=$borderPx}}
            if($vidOn){$g.DrawString('VIDEO',$moduleFont,$moduleBrush,$x+8,$stripY+8)}
        } else {
            $x=$mediaX+$mediaExtent
            if($artOn){$x-=$pmw;$g.DrawString('ART',$moduleFont,$moduleBrush,$x+8,$stripY+8);if($vidOn -and $borderPx -gt 0){$x+=$borderPx}}
            if($vidOn){$x-=$pmw;$g.DrawString('VIDEO',$moduleFont,$moduleBrush,$x+8,$stripY+8)}
        }
        $moduleBrush.Dispose();$moduleFont.Dispose()

        # Visualizer: first visible bar starts at the media edge, independently
        # of text padding. This mirrors the real overlay architecture.
        $drawViz={
            if(-not $vizOn){return}
            $barCount=28
            $vizWidth=[Math]::Min([Math]::Max(120,[int](260*$scale)),$textWidth+$gap)
            $barStep=[Math]::Max(3,[int]($vizWidth/$barCount))
            $barW=[Math]::Max(2,$barStep-2)
            if([string]$u.visualizer_bar_spacing -eq 'Light'){$barW=[Math]::Max(2,$barStep-3)}
            elseif([string]$u.visualizer_bar_spacing -eq 'Wide'){$barW=[Math]::Max(1,[int][Math]::Floor($barStep/2))}
            $shape=[string]$u.visualizer_shape
            $anchor=[string]$u.visualizer_vertical_anchor

            for($i=0;$i -lt $barCount;$i++) {
                $logical=$i
                if([string]$u.visualizer_direction -eq 'Mirrored'){$logical=($barCount-1)-$i}
                $t=[double]$logical/[Math]::Max(1,$barCount-1)
                $wave=0.25+(0.58*[Math]::Abs([Math]::Sin(($logical+2)*0.73)))
                $bh=[Math]::Max(4,[int][Math]::Min($pmh,$pmh*$wave))

                if(-not $right){$bx=$mediaEdge+($i*$barStep)}
                else{$bx=$mediaEdge-(($i+1)*$barStep)}

                $by=$stripY+$pmh-$bh
                if($anchor -eq 'Top'){$by=$stripY}
                elseif($anchor -eq 'Center' -or $shape -eq 'Center Mirror' -or $shape -eq 'Twin Rails'){$by=$stripY+[int](($pmh-$bh)/2)}
                $colorT=$t
                if([string]$u.visualizer_gradient_orientation -eq 'Vertical'){$colorT=0.60}
                $vc=Preview-VizColor $u $colorT
                $vc=Preview-AlphaColor $vc ([double]$u.visualizer_opacity)
                $vb=New-Object System.Drawing.SolidBrush($vc)
                if([string]$u.visualizer_peak_glow -ne 'Off'){
                    $ga=35;if([string]$u.visualizer_peak_glow -eq 'Strong'){$ga=70}
                    $gb=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb($ga,$vc.R,$vc.G,$vc.B))
                    $g.FillRectangle($gb,$bx-2,$by-2,$barW+4,$bh+4);$gb.Dispose()
                }
                if($shape -eq 'Dots'){
                    for($dy=$by;$dy -lt ($by+$bh);$dy+=5){$g.FillRectangle($vb,$bx,$dy,$barW,[Math]::Min(2,($by+$bh)-$dy))}
                }
                elseif($shape -eq 'Oscilloscope'){
                    $oy=$stripY+[int]($pmh/2)-[int]([Math]::Sin(($logical+2)*0.73)*$bh*0.42)
                    $g.FillEllipse($vb,$bx,$oy,[Math]::Max(2,$barW),[Math]::Max(2,$barW))
                }
                elseif($shape -eq 'Particle Field'){
                    for($p=0;$p -lt 3;$p++){$py=$by+(($logical*7+$p*11)%[Math]::Max(1,$bh));$g.FillRectangle($vb,$bx+(($p*3)%[Math]::Max(1,$barW)),$py,2,2)}
                }
                elseif($shape -eq 'Twin Rails'){
                    $cy=$stripY+[int]($pmh/2);$rail=[Math]::Max(1,[int]($bh/2));$g.FillRectangle($vb,$bx,$cy-$rail,$barW,2);$g.FillRectangle($vb,$bx,$cy+$rail-2,$barW,2)
                }
                else{$g.FillRectangle($vb,$bx,$by,$barW,$bh)}
                $vb.Dispose()
            }
        }

        if([string]$u.visualizer_layer -eq 'Behind text'){& $drawViz}

        # Generic sample text only. Never read playback state or cached metadata.
        $fontSize=[single][Math]::Max(11,[Math]::Min(31,[double]$u.text_size*$scale))
        try{$font=New-Object System.Drawing.Font([string]$u.text_font,$fontSize,[System.Drawing.FontStyle]::Regular)}
        catch{$font=New-Object System.Drawing.Font('Segoe UI',$fontSize,[System.Drawing.FontStyle]::Regular)}

        $align=[string]$u.text_alignment
        if($align -eq 'Auto'){
            if($right){$align='Right'}else{$align='Left'}
        }

        $spacingScale=[Math]::Max(1.0,[Math]::Min(3.0,([double]$u.media_height/[Math]::Max(1.0,([double]$u.text_size*2.7)))))
        $lineMult=1.08
        if([string]$u.title_channel_spacing -eq 'Tight'){$lineMult=0.96}
        elseif([string]$u.title_channel_spacing -eq 'Loose'){$lineMult=1.22+(0.22*($spacingScale-1.0))}
        elseif([string]$u.title_channel_spacing -eq 'Extra Loose'){$lineMult=1.50+(0.32*($spacingScale-1.0))}
        elseif([string]$u.title_channel_spacing -eq 'Maximum'){$lineMult=1.85+(0.45*($spacingScale-1.0))}

        $lineH=[Math]::Max(13,[int][Math]::Round($fontSize*$lineMult))
        $textY=$stripY+4
        $titleRect=New-Object System.Drawing.Rectangle($textX,$textY,$textWidth,[int]($fontSize*1.6))
        $channelRect=New-Object System.Drawing.Rectangle($textX,$textY+$lineH,$textWidth,[int]($fontSize*1.6))

        $textColor=Preview-AlphaColor (Hex-Color ([string]$u.text_color)) ([double]$u.text_opacity)
        $outlineColor=Hex-Color ([string]$u.outline_color)
        $previewOutline=[Math]::Max(0,[Math]::Min(4,[int][Math]::Round([double]$u.text_outline*$scale*0.55)))

        if([bool]$u.title_enabled){
            Preview-DrawText $g 'Artist — Track Title' $font $titleRect $textColor $outlineColor $previewOutline ([bool]$u.text_glow) $align
        }
        if([bool]$u.channel_enabled){
            Preview-DrawText $g 'YouTube Channel' $font $channelRect $textColor $outlineColor $previewOutline ([bool]$u.text_glow) $align
        }

        if([string]$u.visualizer_layer -eq 'Above text'){& $drawViz}
        $font.Dispose()

        # Separate mini-map for canvas/corner placement.
        $mapX=28;$mapY=305;$mapW=210;$mapH=88
        $mapBrush=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(8,8,8))
        $mapPen=New-Object System.Drawing.Pen([System.Drawing.Color]::DimGray,1)
        $g.FillRectangle($mapBrush,$mapX,$mapY,$mapW,$mapH)
        $g.DrawRectangle($mapPen,$mapX,$mapY,$mapW,$mapH)
        $mapBrush.Dispose();$mapPen.Dispose()

        $miniW=[Math]::Max(26,[Math]::Min(96,[int](($m.OverlayWidth/[double]$m.CanvasWidth)*$mapW)))
        $miniH=[Math]::Max(5,[Math]::Min(18,[int](($m.OverlayHeight/[double]$m.CanvasHeight)*$mapH)))
        $miniX=$mapX
        $miniY=$mapY
        if($m.IsRight){$miniX=$mapX+$mapW-$miniW}
        if($m.IsBottom){$miniY=$mapY+$mapH-$miniH}
        $accent=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(125,205,205,205))
        $g.FillRectangle($accent,$miniX,$miniY,$miniW,$miniH)
        $accent.Dispose()

        $infoFont=New-Object System.Drawing.Font('Segoe UI',9)
        $infoBrush=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Silver)
        $g.DrawString(("Canvas placement: "+[string]$u.corner),$infoFont,$infoBrush,260,316)
        $g.DrawString(("Media "+$m.MediaWidth+"x"+$m.MediaHeight+"  |  Browser "+$m.BrowserFps+" FPS"),$infoFont,$infoBrush,260,344)
        $g.DrawString('Generic sample only — playlist and cache are never read.',$infoFont,$infoBrush,260,372)
        $infoBrush.Dispose();$infoFont.Dispose()

        $capBrush.Dispose();$capFont.Dispose()
    }catch{
        # A preview paint problem must never make Settings unusable.
    }
})

$generalPreset.Add_SelectedIndexChanged({Apply-GeneralPreset})
$overlayPreset.Add_SelectedIndexChanged({Apply-OverlayPreset})
$canvasPreset.Add_SelectedIndexChanged({Apply-CanvasPreset;if([string]$mediaPreset.SelectedItem -eq 'Auto'){Apply-MediaPreset};Update-Preview})
$mediaPreset.Add_SelectedIndexChanged({Apply-MediaPreset;Update-Preview})
$stylePreset.Add_SelectedIndexChanged({Apply-StylePreset})
$vizPreset.Add_SelectedIndexChanged({Apply-VisualizerPreset})
$performancePreset.Add_SelectedIndexChanged({Apply-PerformancePreset})
$directorPreset.Add_SelectedIndexChanged({Apply-DirectorPreset})
$sourcesPreset.Add_SelectedIndexChanged({Apply-SourcesPreset})
$outputsPreset.Add_SelectedIndexChanged({Apply-OutputsPreset})
$mode.Add_SelectedIndexChanged({Update-Dependencies;Update-Preview})
foreach($c in @($corner,$font,$textColor,$outlineColor,$textAlign,$textSpacing,$cornerStyle,$vizColorMode,$vizSolid,$vizGradient,$gradientOrientation,$vizDirection,$vizLayer,$vizShape,$vizAnchor,$vizSpacing,$vizPeakGlow,$vizFrequency,$vizFps,$browserFps,$performance,$playerQuality,$videoPreference,$videoFps,$audioQuality,$audioPreference,$activity,$chunk,$vizLength,$directorTheme,$directorTimeline,$directorMotion,$directorPalette,$directorVizShape,$statsDetail,$commentFilter)){ $c.Add_SelectedIndexChanged({Update-Preview}) }
$overlayVideoQuality.Add_SelectedIndexChanged({Update-QualityWarning;Update-Preview})
foreach($n in @($canvasW,$canvasH,$mediaW,$mediaH,$videoZoom,$outlinePx,$textOpacity,$textSize,$borderPx,$opacityPct,$prefetch,$videoCacheLimit,$vizHighTrim,$commentChars,$historyMax)){ $n.Add_ValueChanged({Update-Preview}) }
foreach($c in @($art,$titleCheck,$channel,$viz,$smartCrop,$glow,$borderOn,$loudness)){ $c.Add_CheckedChanged({Update-Preview}) }
$video.Add_CheckedChanged({Update-QualityWarning;Update-Preview})
$directorOn.Add_CheckedChanged({Update-Dependencies;Update-Preview})
$commentOn.Add_CheckedChanged({Update-Dependencies})
$telemetryOn.Add_CheckedChanged({Update-Dependencies})
$historyOn.Add_CheckedChanged({Update-Dependencies})
$vizColorMode.Add_SelectedIndexChanged({Update-Dependencies})
$chunk.Add_SelectedIndexChanged({Update-VisualizerResolutionInfo})
$vizFps.Add_SelectedIndexChanged({Update-VisualizerResolutionInfo})
foreach($row in $outputControls) {
    $row.Enabled.Add_CheckedChanged({param($sender,$e)if($sender.Checked){Enable-DirectorModulePrerequisites ([string]$sender.Tag.Modules.Text)};Update-Dependencies;Update-Preview})
    $row.Layout.Add_SelectedIndexChanged({Update-Preview})
    $row.Theme.Add_SelectedIndexChanged({Update-Preview})
    $row.Edit.Add_Click({param($sender,$e)Show-OutputModulePicker $sender.Tag})
    $row.Copy.Add_Click({param($sender,$e)$row=$sender.Tag;[System.Windows.Forms.Clipboard]::SetText((Get-DirectorOutputUrl (Get-UiConfig) ([int]$row.Id)));$sender.Text='COPIED'})
    $row.Preview.Add_Click({param($sender,$e)Start-Process (Get-DirectorOutputUrl (Get-UiConfig) ([int]$sender.Tag.Id))})
}
foreach($row in $sourceControls){
    $row.Enabled.Add_CheckedChanged({param($sender,$e)if($sender.Checked){Enable-DirectorModulePrerequisites ([string]$sender.Tag.Module)};Update-Dependencies})
    $row.Copy.Add_Click({param($sender,$e)$row=$sender.Tag;[System.Windows.Forms.Clipboard]::SetText((Get-DirectorModuleUrl (Get-UiConfig) ([string]$row.Module)));$sender.Text='COPIED'})
    $row.Preview.Add_Click({param($sender,$e)Start-Process (Get-DirectorModuleUrl (Get-UiConfig) ([string]$sender.Tag.Module))})
}

# Page presets are intentionally honest: touching any underlying field turns
# that page into Custom. Programmatic preset application is guarded so it keeps
# the selected preset name while it fills the controls.
foreach($c in @($mode,$playerQuality)) {
    $c.Add_SelectedIndexChanged({if(-not $script:ApplyingGeneralPreset -and [string]$generalPreset.SelectedItem -ne 'Custom'){$generalPreset.SelectedItem='Custom'}})
}
foreach($c in @($canvasPreset,$corner,$mediaPreset,$overlayVideoQuality,$videoFps)) {
    $c.Add_SelectedIndexChanged({if(-not $script:ApplyingOverlayPreset -and [string]$overlayPreset.SelectedItem -ne 'Custom'){$overlayPreset.SelectedItem='Custom'}})
}
foreach($n in @($canvasW,$canvasH,$mediaW,$mediaH,$videoZoom,$videoCacheLimit)) {
    $n.Add_ValueChanged({if(-not $script:ApplyingOverlayPreset -and [string]$overlayPreset.SelectedItem -ne 'Custom'){$overlayPreset.SelectedItem='Custom'}})
}
foreach($c in @($art,$video,$titleCheck,$channel,$viz,$smartCrop)) {
    $c.Add_CheckedChanged({if(-not $script:ApplyingOverlayPreset -and [string]$overlayPreset.SelectedItem -ne 'Custom'){$overlayPreset.SelectedItem='Custom'}})
}
foreach($c in @($font,$textColor,$outlineColor,$textAlign,$textSpacing,$cornerStyle,$borderColor)) {
    $c.Add_SelectedIndexChanged({if(-not $script:ApplyingStylePreset -and [string]$stylePreset.SelectedItem -ne 'Custom'){$stylePreset.SelectedItem='Custom'}})
}
$canvasW.Add_ValueChanged({if(-not $script:ApplyingCanvasPreset -and [string]$canvasPreset.SelectedItem -ne 'Custom'){$canvasPreset.SelectedItem='Custom'}})
$canvasH.Add_ValueChanged({if(-not $script:ApplyingCanvasPreset -and [string]$canvasPreset.SelectedItem -ne 'Custom'){$canvasPreset.SelectedItem='Custom'}})
$mediaW.Add_ValueChanged({if(-not $script:ApplyingMediaPreset -and [string]$mediaPreset.SelectedItem -ne 'Custom'){$mediaPreset.SelectedItem='Custom'}})
$mediaH.Add_ValueChanged({if(-not $script:ApplyingMediaPreset -and [string]$mediaPreset.SelectedItem -ne 'Custom'){$mediaPreset.SelectedItem='Custom'}})
foreach($n in @($outlinePx,$textOpacity,$textSize,$borderPx)) {
    $n.Add_ValueChanged({if(-not $script:ApplyingStylePreset -and [string]$stylePreset.SelectedItem -ne 'Custom'){$stylePreset.SelectedItem='Custom'}})
}
foreach($c in @($glow,$borderOn)) {
    $c.Add_CheckedChanged({if(-not $script:ApplyingStylePreset -and [string]$stylePreset.SelectedItem -ne 'Custom'){$stylePreset.SelectedItem='Custom'}})
}
foreach($c in @($activity,$chunk,$vizLength,$vizColorMode,$vizSolid,$vizGradient,$gradientOrientation,$vizDirection,$vizLayer,$vizShape,$vizAnchor,$vizSpacing,$vizPeakGlow,$vizFrequency,$vizFps)) {
    $c.Add_SelectedIndexChanged({if(-not $script:ApplyingVisualizerPreset -and [string]$vizPreset.SelectedItem -ne 'Custom'){$vizPreset.SelectedItem='Custom'}})
}
foreach($n in @($opacityPct,$vizHighTrim)) {
    $n.Add_ValueChanged({if(-not $script:ApplyingVisualizerPreset -and [string]$vizPreset.SelectedItem -ne 'Custom'){$vizPreset.SelectedItem='Custom'}})
}
foreach($c in @($performance,$browserFps,$videoPreference,$audioQuality,$audioPreference)) {
    $c.Add_SelectedIndexChanged({if(-not $script:ApplyingPerformancePreset -and [string]$performancePreset.SelectedItem -ne 'Custom'){$performancePreset.SelectedItem='Custom'}})
}
$prefetch.Add_ValueChanged({if(-not $script:ApplyingPerformancePreset -and [string]$performancePreset.SelectedItem -ne 'Custom'){$performancePreset.SelectedItem='Custom'}})
$loudness.Add_CheckedChanged({if(-not $script:ApplyingPerformancePreset -and [string]$performancePreset.SelectedItem -ne 'Custom'){$performancePreset.SelectedItem='Custom'}})
foreach($c in @($directorTheme,$directorTimeline,$directorMotion,$directorPalette,$statsDetail,$directorVizShape,$commentFilter)) {
    $c.Add_SelectedIndexChanged({if(-not $script:ApplyingDirectorPreset -and [string]$directorPreset.SelectedItem -ne 'Custom'){$directorPreset.SelectedItem='Custom'}})
}
foreach($n in @($commentChars,$historyMax)) {
    $n.Add_ValueChanged({if(-not $script:ApplyingDirectorPreset -and [string]$directorPreset.SelectedItem -ne 'Custom'){$directorPreset.SelectedItem='Custom'}})
}
foreach($c in @($directorOn,$commentOn,$telemetryOn,$probeOn,$historyOn)) {
    $c.Add_CheckedChanged({if(-not $script:ApplyingDirectorPreset -and [string]$directorPreset.SelectedItem -ne 'Custom'){$directorPreset.SelectedItem='Custom'}})
}
foreach($row in $sourceControls){
    $row.Enabled.Add_CheckedChanged({if(-not $script:ApplyingSourcesPreset -and [string]$sourcesPreset.SelectedItem -ne 'Custom'){$sourcesPreset.SelectedItem='Custom'}})
    $row.Width.Add_ValueChanged({if(-not $script:ApplyingSourcesPreset -and [string]$sourcesPreset.SelectedItem -ne 'Custom'){$sourcesPreset.SelectedItem='Custom'}})
    $row.Height.Add_ValueChanged({if(-not $script:ApplyingSourcesPreset -and [string]$sourcesPreset.SelectedItem -ne 'Custom'){$sourcesPreset.SelectedItem='Custom'}})
}
foreach($row in $outputControls) {
    $row.Enabled.Add_CheckedChanged({if(-not $script:ApplyingOutputsPreset -and [string]$outputsPreset.SelectedItem -ne 'Custom'){$outputsPreset.SelectedItem='Custom'}})
    $row.Name.Add_TextChanged({if(-not $script:ApplyingOutputsPreset -and [string]$outputsPreset.SelectedItem -ne 'Custom'){$outputsPreset.SelectedItem='Custom'}})
    $row.Modules.Add_TextChanged({if(-not $script:ApplyingOutputsPreset -and [string]$outputsPreset.SelectedItem -ne 'Custom'){$outputsPreset.SelectedItem='Custom'}})
    $row.Layout.Add_SelectedIndexChanged({if(-not $script:ApplyingOutputsPreset -and [string]$outputsPreset.SelectedItem -ne 'Custom'){$outputsPreset.SelectedItem='Custom'}})
    $row.Theme.Add_SelectedIndexChanged({if(-not $script:ApplyingOutputsPreset -and [string]$outputsPreset.SelectedItem -ne 'Custom'){$outputsPreset.SelectedItem='Custom'}})
    $row.Width.Add_ValueChanged({if(-not $script:ApplyingOutputsPreset -and [string]$outputsPreset.SelectedItem -ne 'Custom'){$outputsPreset.SelectedItem='Custom'}})
    $row.Height.Add_ValueChanged({if(-not $script:ApplyingOutputsPreset -and [string]$outputsPreset.SelectedItem -ne 'Custom'){$outputsPreset.SelectedItem='Custom'}})
}

$refreshComponents.Add_Click({Update-Components})
$checkUpdates.Add_Click({Start-Process (Join-Path $PSScriptRoot 'YomiLauncher.exe') -ArgumentList 'update'})
$denoBtn.Add_Click({
    $a = 'InstallDeno'
    if (Test-YomiComponent 'deno') { $a = 'RemoveDeno' }
    Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'components.ps1'),'-Action',$a)
})
$ffmpegBtn.Add_Click({
    $a = 'InstallFfmpeg'
    if (Test-YomiComponent 'ffmpeg') { $a = 'RemoveFfmpeg' }
    Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'components.ps1'),'-Action',$a)
})
$openData.Add_Click({Start-Process explorer.exe $DataRoot})
$openProgram.Add_Click({Start-Process explorer.exe $InstallRoot})
$uninstallBtn.Add_Click({
    $answer=[System.Windows.Forms.MessageBox]::Show(
        "Open the YOMI uninstaller?`r`n`r`nThe uninstaller will ask whether to remove everything or keep your playlist/config for a future reinstall.",
        'Uninstall YOMI',
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )
    if($answer -ne [System.Windows.Forms.DialogResult]::Yes){return}

    $rootUninstaller=Join-Path $InstallRoot 'Uninstall YOMI.cmd'
    $scriptUninstaller=Join-Path $InstallRoot 'app\uninstall.ps1'

    if(Test-Path $rootUninstaller){
        Start-Process $rootUninstaller
        $form.Close()
        return
    }

    # Defensive fallback to the real uninstall engine.
    if(Test-Path $scriptUninstaller){
        $escaped=$scriptUninstaller.Replace("'","''")
        Start-Process powershell.exe -ArgumentList @(
            '-NoProfile',
            '-ExecutionPolicy','Bypass',
            '-Command',
            ("& '"+$escaped+"'")
        )
        $form.Close()
        return
    }

    [System.Windows.Forms.MessageBox]::Show(
        "The YOMI uninstall engine could not be found.`r`n`r`nExpected:`r`n$scriptUninstaller",
        'YOMI',
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    )|Out-Null
})
$controllerBtn.Add_Click({Start-Process (Join-Path $PSScriptRoot 'YomiLauncher.exe') -ArgumentList 'controller'})
$readme.Add_Click({Start-Process notepad.exe ('"'+(Join-Path $InstallRoot 'README-EASY.txt')+'"')})
$copyOverlay.Add_Click({[System.Windows.Forms.Clipboard]::SetText((Get-OverlayUrl (Get-UiConfig)));[System.Windows.Forms.MessageBox]::Show('Overlay URL copied.','YOMI')|Out-Null})
$obsSetup.Add_Click({$p=Write-ObsInstructions (Get-UiConfig);Start-Process notepad.exe ('"'+$p+'"')})
$copySources.Add_Click({
    $p=Write-ObsInstructions (Get-UiConfig)
    [System.Windows.Forms.Clipboard]::SetText((Get-Content $p -Raw))
    [System.Windows.Forms.MessageBox]::Show('The enabled source pack, URLs and exact OBS dimensions were copied.','YOMI Director Mode')|Out-Null
})
$openObsGuide.Add_Click({$p=Write-ObsInstructions (Get-UiConfig);Start-Process notepad.exe ('"'+$p+'"')})
$copyEnabledFixed.Add_Click({
    $c=Get-UiConfig;$lines=@('YOMI ENABLED INDIVIDUAL SOURCES','')
    foreach($source in @($c.director_fixed_sources)){
        if(-not [bool]$source.enabled){continue}
        $lines+="$($source.label): $(Get-DirectorModuleUrl $c ([string]$source.module))  |  $($source.width)x$($source.height)"
    }
    if($lines.Count -eq 2){$lines+='(none enabled)'}
    [System.Windows.Forms.Clipboard]::SetText(($lines -join "`r`n"));$copyEnabledFixed.Text='COPIED'
})
$restoreDefaults.Add_Click({
    $answer=[System.Windows.Forms.MessageBox]::Show(
        "Restore factory settings?`r`n`r`nThis resets YOMI options and output layouts. Your playlist, playback history, installed components and Defender choice stay intact. Media affected by quality, crop or visualizer defaults will rebuild on the next start.",
        'Restore Default Settings',
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )
    if($answer -ne [System.Windows.Forms.DialogResult]::Yes){return}

    $defaults=Get-Content (Join-Path $PSScriptRoot 'default-config.json') -Raw | ConvertFrom-Json
    $defaults.playlist=$playlist.Text.Trim()
    $defaults.version=$yomiVersion
    Save-YomiConfig $defaults
    $stateRoot=Join-Path $DataRoot 'state';New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
    foreach($flag in @('audio-reset.pending','artwork-reset.pending','video-reset.pending','visualizer-reset.pending')){Set-Content (Join-Path $stateRoot $flag) '1' -Encoding ASCII}
    Write-ObsInstructions $defaults | Out-Null
    if(Test-YomiRuntimeRunning){Request-YomiControlledRestart}
    Start-Process (Join-Path $PSScriptRoot 'YomiLauncher.exe') -ArgumentList 'settings'
    $form.Close()
})
$shuffleBtn.Add_Click({
    $answer=[System.Windows.Forms.MessageBox]::Show(
        "Shuffle Playlist now?`r`n`r`nYOMI will reload the latest contents from YouTube, preserve every playlist occurrence, randomize the list, reset track-number cache and restart from track 1.",
        'Shuffle Playlist',
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Question
    )

    if($answer -ne [System.Windows.Forms.DialogResult]::Yes){return}

    # Save the current Settings UI first so a newly edited playlist URL is
    # exactly what Shuffle Playlist reloads.
    $new=Get-UiConfig
    $new.version=$yomiVersion
    $new.playlist=$playlist.Text.Trim()
    $metrics=Get-YomiLayoutMetrics $new
    $new.overlay_width=[int]$metrics.OverlayWidth
    $new.overlay_height=[int]$metrics.OverlayHeight
    $new.overlay_fps=[int]$metrics.BrowserFps
    Save-YomiConfig $new

    $stateRoot=Join-Path $DataRoot 'state'
    New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
    Set-Content (Join-Path $stateRoot 'shuffle-request.txt') 'approved' -Encoding ASCII

    # If Controller is already running its timer sees the request. If it is not,
    # launching it makes the same request get consumed immediately.
    Start-Process (Join-Path $PSScriptRoot 'YomiLauncher.exe') -ArgumentList 'controller'
})

$save.Add_Click({
    $old=Get-YomiConfig; $new=Get-UiConfig; $new.version=$yomiVersion; $new.playlist=$playlist.Text.Trim()
    $engineChanged=((Get-YomiEngineConfigSignature $old) -ne (Get-YomiEngineConfigSignature $new));$runtimeWasRunning=Test-YomiRuntimeRunning
    $metrics=Get-YomiLayoutMetrics $new; $new.overlay_width=[int]$metrics.OverlayWidth; $new.overlay_height=[int]$metrics.OverlayHeight; $new.overlay_fps=[int]$metrics.BrowserFps
    $playlistChanged=([string]$old.playlist -ne [string]$new.playlist)
    $vizSigBefore="$($old.visualizer_internal_width)x$($old.visualizer_internal_height)|$($old.visualizer_activity)|$($old.visualizer_frequency_scale)|$($old.visualizer_fps)"; $vizSigAfter="$($new.visualizer_internal_width)x$($new.visualizer_internal_height)|$($new.visualizer_activity)|$($new.visualizer_frequency_scale)|$($new.visualizer_fps)"
    $artSigBefore="$($old.media_width)x$($old.media_height)|$($old.smart_artwork_crop)"; $artSigAfter="$($new.media_width)x$($new.media_height)|$($new.smart_artwork_crop)"
    $videoSigBefore="$($old.overlay_video_quality)|$($old.video_preference)|$($old.video_fps)"; $videoSigAfter="$($new.overlay_video_quality)|$($new.video_preference)|$($new.video_fps)"
    $audioSigBefore="$($old.audio_quality)|$($old.audio_preference)"; $audioSigAfter="$($new.audio_quality)|$($new.audio_preference)"
    $commentSigBefore="$($old.featured_comment_enabled)|$($old.comment_max_chars)|$($old.comment_filter_mode)"; $commentSigAfter="$($new.featured_comment_enabled)|$($new.comment_max_chars)|$($new.comment_filter_mode)"
    Save-YomiConfig $new
    if($playlistChanged){Remove-Item (Join-Path $DataRoot 'playlist.txt') -Force -ErrorAction SilentlyContinue;Set-Content (Join-Path $DataRoot 'state\resume-track.txt') '1' -Encoding ASCII}
    if($vizSigBefore -ne $vizSigAfter){Set-Content (Join-Path $DataRoot 'state\visualizer-reset.pending') '1' -Encoding ASCII}
    if($artSigBefore -ne $artSigAfter){Set-Content (Join-Path $DataRoot 'state\artwork-reset.pending') '1' -Encoding ASCII}
    if($videoSigBefore -ne $videoSigAfter){Set-Content (Join-Path $DataRoot 'state\video-reset.pending') '1' -Encoding ASCII}
    if($audioSigBefore -ne $audioSigAfter){Set-Content (Join-Path $DataRoot 'state\audio-reset.pending') '1' -Encoding ASCII}
    if($commentSigBefore -ne $commentSigAfter){Get-ChildItem (Join-Path $DataRoot 'cache\comments') -Force -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\status') -Filter 'track-*.comment.*' -Force -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue}
    Write-ObsInstructions $new | Out-Null
    $saveResetTimer.Stop()
    if($engineChanged -and $runtimeWasRunning){$save.Text='SAVED - RESTARTING';Request-YomiControlledRestart}else{$save.Text='SAVED'}
    $saveResetTimer.Start()
    $config=$new;Update-Dependencies;Update-Preview
})

Update-Components
Update-Dependencies
Update-VisualizerResolutionInfo
Update-Preview
Write-ObsInstructions $config | Out-Null
$form.Add_Shown({Start-Process (Join-Path $PSScriptRoot 'YomiLauncher.exe') -ArgumentList 'update-auto'})
try{[void]$form.ShowDialog()}finally{$saveResetTimer.Stop();$saveResetTimer.Dispose()}
