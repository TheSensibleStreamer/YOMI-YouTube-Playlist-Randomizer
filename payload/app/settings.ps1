$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
$config = Get-YomiConfig

$form = New-Object System.Windows.Forms.Form
$form.Text = 'YOMI 4.0.9.4 Settings'
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
$subtitle.Text = 'YouTube Playlist Randomizer & Player  |  Optional OBS integration for streamers'
$subtitle.Location = New-Object System.Drawing.Point(125,26)
$subtitle.Size = New-Object System.Drawing.Size(780,26)
$subtitle.ForeColor = [System.Drawing.Color]::DimGray
$form.Controls.Add($subtitle)

function Add-Label($parent,$text,$x,$y,$w=180,$h=24) {
    $c=New-Object System.Windows.Forms.Label; $c.Text=$text
    $c.Location=New-Object System.Drawing.Point($x,$y); $c.Size=New-Object System.Drawing.Size($w,$h)
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

$tabs=New-Object System.Windows.Forms.TabControl
$tabs.Location=New-Object System.Drawing.Point(20,62); $tabs.Size=New-Object System.Drawing.Size(955,650)
$form.Controls.Add($tabs)
function New-Tab($name){$t=New-Object System.Windows.Forms.TabPage;$t.Text=$name;[void]$tabs.TabPages.Add($t);return $t}

# GENERAL / PLAYER
$general=New-Tab 'General / Player'
Add-Label $general 'YouTube playlist link or playlist ID:' 18 22 320 | Out-Null
$playlist=New-Object System.Windows.Forms.TextBox; $playlist.Location=New-Object System.Drawing.Point(18,48); $playlist.Size=New-Object System.Drawing.Size(890,27); $playlist.Text=[string]$config.playlist; $general.Controls.Add($playlist)
Add-Label $general 'YOMI mode:' 18 100 100 | Out-Null
$mode=Add-Combo $general 125 96 240 @('Player','Streamer / OBS') ([string]$config.app_mode)
$modeHint=Add-Label $general 'Player = lightweight playlist randomizer/player. Streamer / OBS adds the one-source overlay and presentation cache.' 385 99 520 42
$modeHint.ForeColor=[System.Drawing.Color]::DimGray
Add-Label $general 'Player video:' 18 150 100 | Out-Null
$playerQuality=Add-Combo $general 125 146 240 @('Off (audio only)','144p','360p','720p','Best') ([string]$config.player_video_quality)
$playerHint=Add-Label $general 'Audio-only is the lowest-overhead default. Video quality applies only in Player mode and opens an mpv video window.' 385 149 520 44
$playerHint.ForeColor=[System.Drawing.Color]::DimGray
$generalNote=Add-Label $general 'Shuffle Playlist is the normal update action: it reloads the current YouTube playlist, preserves duplicate entries, randomizes the whole list, resets cache mapping and starts from the new track 1.' 18 215 885 58
$generalNote.ForeColor=[System.Drawing.Color]::DimGray

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

$obsHint=Add-Label $obs 'Disabled modules are not merely hidden: YOMI does not prepare their cache jobs. Right-side layouts mirror automatically. The visualizer begins directly at the final pixel of the media area; the title/channel keep their own separate padding.' 18 230 870 64
$obsHint.ForeColor=[System.Drawing.Color]::DimGray

# STYLE
$style=New-Tab 'Text & Style'
Add-Label $style 'Style preset:' 18 24 95 | Out-Null
$stylePreset=Add-Combo $style 120 20 190 @('Classic','Minimal','Retro','Neon','Pastel','Arcade','Custom') ([string]$config.style_preset)
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
$textSpacing=Add-Combo $style 625 121 135 @('Tight','Normal','Loose') ([string]$config.title_channel_spacing)
$glow=Add-Check $style 'Subtle text glow' 785 121 $config.text_glow 140

$mediaStyleBox=New-Object System.Windows.Forms.GroupBox; $mediaStyleBox.Text='Media frame'; $mediaStyleBox.Location=New-Object System.Drawing.Point(18,175); $mediaStyleBox.Size=New-Object System.Drawing.Size(875,95); $style.Controls.Add($mediaStyleBox)
$borderOn=Add-Check $mediaStyleBox 'Border' 15 29 $config.media_border_enabled 90
Add-Label $mediaStyleBox 'Width px:' 115 31 70 | Out-Null
$borderPx=Add-Number $mediaStyleBox 185 27 65 0 8 ([int]$config.media_border_px)
Add-Label $mediaStyleBox 'Color:' 275 31 55 | Out-Null
$borderColor=Add-Combo $mediaStyleBox 335 27 150 @('Dark Gray','Near Black','Black','White','Pink','Purple','Cyan','Blue') (Color-Name ([string]$config.media_border_color) 'Dark Gray')
Add-Label $mediaStyleBox 'Corners:' 515 31 70 | Out-Null
$cornerStyle=Add-Combo $mediaStyleBox 590 27 160 @('Square','Soft Rounded','Rounded') ([string]$config.media_corner_style)

$styleHint=Add-Label $style 'The list is intentionally curated instead of dumping hundreds of Windows fonts on you. Preview changes live before saving.' 18 290 870 36
$styleHint.ForeColor=[System.Drawing.Color]::DimGray

# VISUALIZER
$visual=New-Tab 'Visualizer'
Add-Label $visual 'Activity:' 18 24 70 | Out-Null
$activity=Add-Combo $visual 95 20 145 @('Subtle','Normal','Active','Punchy') ([string]$config.visualizer_activity)
Add-Label $visual 'Opacity %:' 265 24 85 | Out-Null
$opacityPct=Add-Number $visual 350 20 90 5 80 ([int][Math]::Round(([double]$config.visualizer_opacity)*100))
Add-Label $visual 'Pixels:' 470 24 60 | Out-Null
$chunk=Add-Combo $visual 530 20 150 @('Extra Chunky','Chunky','Fine') $(if([int]$config.visualizer_internal_width -le 40){'Extra Chunky'}elseif([int]$config.visualizer_internal_width -le 48){'Chunky'}else{'Fine'})
Add-Label $visual 'Length:' 705 24 65 | Out-Null
$vizLength=Add-Combo $visual 770 20 135 @('Short','Medium','Wide','Extra Wide') $(if([double]$config.visualizer_length_multiplier -le 2.25){'Short'}elseif([double]$config.visualizer_length_multiplier -le 3.25){'Medium'}elseif([double]$config.visualizer_length_multiplier -ge 5.25){'Extra Wide'}else{'Wide'})

Add-Label $visual 'Color mode:' 18 78 90 | Out-Null
$vizColorMode=Add-Combo $visual 115 74 145 @('Solid','Rainbow','Gradient') ([string]$config.visualizer_color_mode)
Add-Label $visual 'Solid color:' 285 78 85 | Out-Null
$vizSolid=Add-Combo $visual 375 74 145 @('Gray','YOMI Cream','White','Pink','Lavender','Purple','Cyan','Blue','Green','Yellow','Orange','Red') (Color-Name ([string]$config.visualizer_solid_color) 'Gray')
Add-Label $visual 'Gradient:' 545 78 75 | Out-Null
$vizGradient=Add-Combo $visual 620 74 145 @('Sunset','Ocean','Pastel','Fire','Forest','Mono') ([string]$config.visualizer_gradient_preset)
Add-Label $visual 'Direction:' 785 78 70 | Out-Null
$gradientOrientation=Add-Combo $visual 855 74 75 @('Horizontal','Vertical') ([string]$config.visualizer_gradient_orientation)

Add-Label $visual 'Bars:' 18 132 60 | Out-Null
$vizDirection=Add-Combo $visual 115 128 145 @('Normal','Mirrored') ([string]$config.visualizer_direction)
Add-Label $visual 'Layer:' 285 132 55 | Out-Null
$vizLayer=Add-Combo $visual 375 128 145 @('Behind text','Above text') ([string]$config.visualizer_layer)
$visualHint=Add-Label $visual 'Rainbow and gradients are recolored live in the Browser Source; changing colors does not regenerate cached video. If these controls are gray, install FFmpeg Media Tools in Components.' 18 190 875 48
$visualHint.ForeColor=[System.Drawing.Color]::DimGray

# PERFORMANCE
$perf=New-Tab 'Performance'
Add-Label $perf 'Background preparation:' 18 24 165 | Out-Null
$perfItems=@('Gaming / Lowest overhead (1 worker)','Balanced (2 workers)','Fast caching (4 workers)')
$perfSel=$perfItems[0]; if([int]$config.cache_workers -eq 2){$perfSel=$perfItems[1]}elseif([int]$config.cache_workers -ge 4){$perfSel=$perfItems[2]}
$performance=Add-Combo $perf 190 20 320 $perfItems $perfSel
Add-Label $perf 'Browser Source FPS:' 535 24 145 | Out-Null
$browserFps=Add-Combo $perf 680 20 130 @('Auto','15 FPS','30 FPS') ([string]$config.browser_fps_mode)
$perfHint=Add-Label $perf 'Balanced / 2 workers is the recommended default. It gives YOMI enough preparation headroom to stay smooth without the extra background pressure of Fast caching. Gaming / 1 worker remains available for the absolute lowest overhead.' 18 62 880 52
$perfHint.ForeColor=[System.Drawing.Color]::DimGray
$loudness=Add-Check $perf 'Track loudness leveling (requires FFmpeg Media Tools)' 18 125 $config.loudness_normalization 390
Add-Label $perf 'Prefetch ahead:' 455 129 105 | Out-Null
$prefetch=Add-Number $perf 565 125 80 1 8 ([int]$config.prefetch_ahead)
$perfNote=Add-Label $perf 'Streamer mode prepares complete synchronized bundles. Player mode avoids artwork/video/visualizer jobs entirely. Player video streams through mpv only when you explicitly enable it.' 18 180 880 55
$perfNote.ForeColor=[System.Drawing.Color]::DimGray

# COMPONENTS
$componentsTab=New-Tab 'Components'
$compStatus=New-Object System.Windows.Forms.Label; $compStatus.Location=New-Object System.Drawing.Point(18,20); $compStatus.Size=New-Object System.Drawing.Size(860,190); $compStatus.BorderStyle='FixedSingle'; $componentsTab.Controls.Add($compStatus)
$denoBtn=New-Object System.Windows.Forms.Button; $denoBtn.Location=New-Object System.Drawing.Point(18,230); $denoBtn.Size=New-Object System.Drawing.Size(210,36); $componentsTab.Controls.Add($denoBtn)
$ffmpegBtn=New-Object System.Windows.Forms.Button; $ffmpegBtn.Location=New-Object System.Drawing.Point(240,230); $ffmpegBtn.Size=New-Object System.Drawing.Size(230,36); $componentsTab.Controls.Add($ffmpegBtn)
$refreshComponents=New-Object System.Windows.Forms.Button; $refreshComponents.Text='REFRESH STATUS'; $refreshComponents.Location=New-Object System.Drawing.Point(482,230); $refreshComponents.Size=New-Object System.Drawing.Size(150,36); $componentsTab.Controls.Add($refreshComponents)
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

function Apply-CanvasPreset {
    switch([string]$canvasPreset.SelectedItem){
        '1280 x 720'{$canvasW.Value=1280;$canvasH.Value=720}
        '1920 x 1080'{$canvasW.Value=1920;$canvasH.Value=1080}
        '2560 x 1440'{$canvasW.Value=2560;$canvasH.Value=1440}
        '3840 x 2160'{$canvasW.Value=3840;$canvasH.Value=2160}
    }
}
function Set-Media($w,$h,$t){$mediaW.Value=$w;$mediaH.Value=$h;$textSize.Value=$t}
function Apply-MediaPreset {
    switch([string]$mediaPreset.SelectedItem){
        'Auto'{$w=[int][Math]::Round([int]$canvasW.Value*0.0625);$h=[int][Math]::Round($w*9/16);$t=[int][Math]::Round($w*33/160);Set-Media ([Math]::Max(40,$w)) ([Math]::Max(22,$h)) ([Math]::Max(12,[Math]::Min(72,$t)))}
        'Compact'{Set-Media 80 45 18}'Small'{Set-Media 120 68 26}'Medium'{Set-Media 160 90 33}'Large'{Set-Media 240 135 45}'Extra Large'{Set-Media 320 180 56}
    }
}

$script:ApplyingPreset=$false
function Apply-StylePreset {
    if($script:ApplyingPreset){return}
    $script:ApplyingPreset=$true
    try{
        switch([string]$stylePreset.SelectedItem){
            'Classic'{$font.SelectedItem='Bahnschrift Condensed';$textColor.SelectedItem='YOMI Cream';$outlineColor.SelectedItem='Near Black';$outlinePx.Value=6;$glow.Checked=$false;$borderOn.Checked=$true;$borderColor.SelectedItem='Dark Gray';$cornerStyle.SelectedItem='Square';$vizColorMode.SelectedItem='Solid';$vizSolid.SelectedItem='Gray'}
            'Minimal'{$font.SelectedItem='Segoe UI';$textColor.SelectedItem='White';$outlineColor.SelectedItem='Black';$outlinePx.Value=2;$glow.Checked=$false;$borderOn.Checked=$false;$cornerStyle.SelectedItem='Square';$vizColorMode.SelectedItem='Solid';$vizSolid.SelectedItem='Cyan'}
            'Retro'{$font.SelectedItem='Courier New';$textColor.SelectedItem='YOMI Cream';$outlineColor.SelectedItem='Black';$outlinePx.Value=4;$glow.Checked=$false;$borderOn.Checked=$true;$cornerStyle.SelectedItem='Square';$vizColorMode.SelectedItem='Solid';$vizSolid.SelectedItem='Green'}
            'Neon'{$font.SelectedItem='Arial Black';$textColor.SelectedItem='Cyan';$outlineColor.SelectedItem='Purple';$outlinePx.Value=4;$glow.Checked=$true;$borderOn.Checked=$true;$borderColor.SelectedItem='Purple';$cornerStyle.SelectedItem='Soft Rounded';$vizColorMode.SelectedItem='Gradient';$vizGradient.SelectedItem='Ocean'}
            'Pastel'{$font.SelectedItem='Segoe Print';$textColor.SelectedItem='Pink';$outlineColor.SelectedItem='Purple';$outlinePx.Value=3;$glow.Checked=$true;$borderOn.Checked=$true;$borderColor.SelectedItem='Lavender';$cornerStyle.SelectedItem='Rounded';$vizColorMode.SelectedItem='Gradient';$vizGradient.SelectedItem='Pastel'}
            'Arcade'{$font.SelectedItem='Impact';$textColor.SelectedItem='Yellow';$outlineColor.SelectedItem='Black';$outlinePx.Value=5;$glow.Checked=$true;$borderOn.Checked=$true;$borderColor.SelectedItem='Black';$cornerStyle.SelectedItem='Square';$vizColorMode.SelectedItem='Rainbow'}
        }
    }finally{$script:ApplyingPreset=$false}
    Update-Preview
}

function Get-UiConfig {
    $o=Get-YomiConfig
    $o.app_mode=[string]$mode.SelectedItem; $o.player_video_quality=[string]$playerQuality.SelectedItem
    $o.canvas_preset=[string]$canvasPreset.SelectedItem; $o.canvas_width=[int]$canvasW.Value; $o.canvas_height=[int]$canvasH.Value; $o.corner=[string]$corner.SelectedItem
    $o.media_size_preset=[string]$mediaPreset.SelectedItem; $o.media_width=[int]$mediaW.Value; $o.media_height=[int]$mediaH.Value; $o.video_zoom=[Math]::Round(([double]$videoZoom.Value/100.0),2)
    $o.artwork_enabled=[bool]$art.Checked; $o.smart_artwork_crop=[bool]$smartCrop.Checked; $o.video_enabled=[bool]$video.Checked; $o.title_enabled=[bool]$titleCheck.Checked; $o.channel_enabled=[bool]$channel.Checked; $o.visualizer_enabled=[bool]$viz.Checked
    $o.style_preset=[string]$stylePreset.SelectedItem; $o.text_font=[string]$font.SelectedItem; $o.text_color=Map-Color ([string]$textColor.SelectedItem); $o.outline_color=Map-Color ([string]$outlineColor.SelectedItem); $o.text_outline=[int]$outlinePx.Value; $o.text_opacity=[Math]::Round(([double]$textOpacity.Value/100.0),2); $o.text_size=[int]$textSize.Value; $o.text_alignment=[string]$textAlign.SelectedItem; $o.title_channel_spacing=[string]$textSpacing.SelectedItem; $o.text_glow=[bool]$glow.Checked
    $o.media_border_enabled=[bool]$borderOn.Checked; $o.media_border_px=[int]$borderPx.Value; $o.media_border_color=Map-Color ([string]$borderColor.SelectedItem); $o.media_corner_style=[string]$cornerStyle.SelectedItem
    $o.visualizer_activity=[string]$activity.SelectedItem; $o.visualizer_opacity=[Math]::Round(([double]$opacityPct.Value/100.0),2)
    switch([string]$chunk.SelectedItem){'Chunky'{$o.visualizer_internal_width=48;$o.visualizer_internal_height=12}'Fine'{$o.visualizer_internal_width=64;$o.visualizer_internal_height=18}default{$o.visualizer_internal_width=40;$o.visualizer_internal_height=10}}
    switch([string]$vizLength.SelectedItem){'Short'{$o.visualizer_length_multiplier=2.0}'Medium'{$o.visualizer_length_multiplier=3.0}'Extra Wide'{$o.visualizer_length_multiplier=6.0}default{$o.visualizer_length_multiplier=4.0}}
    $o.visualizer_color_mode=[string]$vizColorMode.SelectedItem; $o.visualizer_solid_color=Map-Color ([string]$vizSolid.SelectedItem); $o.visualizer_gradient_preset=[string]$vizGradient.SelectedItem; $o.visualizer_gradient_orientation=[string]$gradientOrientation.SelectedItem; $o.visualizer_direction=[string]$vizDirection.SelectedItem; $o.visualizer_layer=[string]$vizLayer.SelectedItem
    $o.browser_fps_mode=[string]$browserFps.SelectedItem; $o.loudness_normalization=[bool]$loudness.Checked; $o.prefetch_ahead=[int]$prefetch.Value
    switch([string]$performance.SelectedItem){'Balanced (2 workers)'{$o.performance_mode='Balanced';$o.cache_workers=2;$o.cache_priority='idle'}'Fast caching (4 workers)'{$o.performance_mode='Fast caching';$o.cache_workers=4;$o.cache_priority='below'}default{$o.performance_mode='Gaming / Lowest overhead';$o.cache_workers=1;$o.cache_priority='idle'}}
    return $o
}

function Update-Dependencies {
    $streamer=([string]$mode.SelectedItem -eq 'Streamer / OBS')
    $ff=Test-YomiComponent 'ffmpeg'
    $playerQuality.Enabled=(-not $streamer)
    foreach($c in @($canvasPreset,$canvasW,$canvasH,$corner,$mediaPreset,$mediaW,$mediaH,$videoZoom,$art,$video,$titleCheck,$channel,$font,$textColor,$outlineColor,$outlinePx,$textOpacity,$textSize,$textAlign,$textSpacing,$glow,$borderOn,$borderPx,$borderColor,$cornerStyle,$activity,$opacityPct,$chunk,$vizLength,$vizColorMode,$vizSolid,$vizGradient,$gradientOrientation,$vizDirection,$vizLayer,$browserFps)){ $c.Enabled=$streamer }
    $viz.Enabled=($streamer -and $ff)
    foreach($c in @($activity,$opacityPct,$chunk,$vizLength,$vizColorMode,$vizSolid,$vizGradient,$gradientOrientation,$vizDirection,$vizLayer)) { $c.Enabled=($streamer -and $ff) }
    $smartCrop.Enabled=($streamer -and $ff)
    $loudness.Enabled=$ff
    $copyOverlay.Enabled=$streamer; $obsSetup.Enabled=$streamer
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

            for($i=0;$i -lt $barCount;$i++) {
                $logical=$i
                if([string]$u.visualizer_direction -eq 'Mirrored'){$logical=($barCount-1)-$i}
                $t=[double]$logical/[Math]::Max(1,$barCount-1)
                $wave=0.25+(0.58*[Math]::Abs([Math]::Sin(($logical+2)*0.73)))
                $bh=[Math]::Max(4,[int][Math]::Min($pmh,$pmh*$wave))

                if(-not $right){$bx=$mediaEdge+($i*$barStep)}
                else{$bx=$mediaEdge-(($i+1)*$barStep)}

                $by=$stripY+$pmh-$bh
                $colorT=$t
                if([string]$u.visualizer_gradient_orientation -eq 'Vertical'){$colorT=0.60}
                $vc=Preview-VizColor $u $colorT
                $vc=Preview-AlphaColor $vc ([double]$u.visualizer_opacity)
                $vb=New-Object System.Drawing.SolidBrush($vc)
                $g.FillRectangle($vb,$bx,$by,$barW,$bh)
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

        $lineMult=1.08
        if([string]$u.title_channel_spacing -eq 'Tight'){$lineMult=0.96}
        elseif([string]$u.title_channel_spacing -eq 'Loose'){$lineMult=1.22}

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

$canvasPreset.Add_SelectedIndexChanged({Apply-CanvasPreset;if([string]$mediaPreset.SelectedItem -eq 'Auto'){Apply-MediaPreset};Update-Preview})
$mediaPreset.Add_SelectedIndexChanged({Apply-MediaPreset;Update-Preview})
$stylePreset.Add_SelectedIndexChanged({Apply-StylePreset})
$mode.Add_SelectedIndexChanged({Update-Dependencies;Update-Preview})
foreach($c in @($corner,$font,$textColor,$outlineColor,$textAlign,$textSpacing,$cornerStyle,$vizColorMode,$vizSolid,$vizGradient,$gradientOrientation,$vizDirection,$vizLayer,$browserFps,$performance,$playerQuality,$activity,$chunk,$vizLength)){ $c.Add_SelectedIndexChanged({Update-Preview}) }
foreach($n in @($canvasW,$canvasH,$mediaW,$mediaH,$videoZoom,$outlinePx,$textOpacity,$textSize,$borderPx,$opacityPct,$prefetch)){ $n.Add_ValueChanged({Update-Preview}) }
foreach($c in @($art,$video,$titleCheck,$channel,$viz,$smartCrop,$glow,$borderOn,$loudness)){ $c.Add_CheckedChanged({Update-Preview}) }

$refreshComponents.Add_Click({Update-Components})
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
    $new.version=4.093
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
    $old=Get-YomiConfig; $new=Get-UiConfig; $new.version=4.0; $new.playlist=$playlist.Text.Trim()
    $metrics=Get-YomiLayoutMetrics $new; $new.overlay_width=[int]$metrics.OverlayWidth; $new.overlay_height=[int]$metrics.OverlayHeight; $new.overlay_fps=[int]$metrics.BrowserFps
    $playlistChanged=([string]$old.playlist -ne [string]$new.playlist)
    $vizSigBefore="$($old.visualizer_internal_width)x$($old.visualizer_internal_height)|$($old.visualizer_activity)"; $vizSigAfter="$($new.visualizer_internal_width)x$($new.visualizer_internal_height)|$($new.visualizer_activity)"
    $artSigBefore="$($old.media_width)x$($old.media_height)|$($old.smart_artwork_crop)"; $artSigAfter="$($new.media_width)x$($new.media_height)|$($new.smart_artwork_crop)"
    Save-YomiConfig $new
    if($playlistChanged){Remove-Item (Join-Path $DataRoot 'playlist.txt') -Force -ErrorAction SilentlyContinue;Set-Content (Join-Path $DataRoot 'state\resume-track.txt') '1' -Encoding ASCII}
    if($vizSigBefore -ne $vizSigAfter){Set-Content (Join-Path $DataRoot 'state\visualizer-reset.pending') '1' -Encoding ASCII}
    if($artSigBefore -ne $artSigAfter){Set-Content (Join-Path $DataRoot 'state\artwork-reset.pending') '1' -Encoding ASCII}
    Write-ObsInstructions $new | Out-Null
    [System.Windows.Forms.MessageBox]::Show("Saved.`r`n`r`nEngine/mode changes apply the next time YOMI starts.",'YOMI 4.0.5')|Out-Null
    $config=$new;Update-Dependencies;Update-Preview
})

Update-Components
Update-Dependencies
Update-Preview
Write-ObsInstructions $config | Out-Null
[void]$form.ShowDialog()
