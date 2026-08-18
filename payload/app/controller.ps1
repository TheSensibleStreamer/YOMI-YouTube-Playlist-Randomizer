$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms;Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'common.ps1');Initialize-YomiData
$created=$false;$mutex=New-Object System.Threading.Mutex($true,'Local\YOMI_CONTROLLER_V4',[ref]$created);if(-not $created){exit 0}
$pipeName='yomi-v4';$supervisor=Join-Path $PSScriptRoot 'supervisor.ps1';$shuffleScript=Join-Path $PSScriptRoot 'shuffle.ps1';$launcher=Join-Path $PSScriptRoot 'YomiLauncher.exe';$iconPath=Join-Path $InstallRoot 'assets\yomi-v408.ico'
$stateRoot=Join-Path $DataRoot 'state';$enginePidFile=Join-Path $stateRoot 'engine.pid';$serverPidFile=Join-Path $stateRoot 'server.pid';$supervisorPidFile=Join-Path $stateRoot 'supervisor.pid';$engineStatusFile=Join-Path $stateRoot 'engine-status.json';$supervisorStatusFile=Join-Path $stateRoot 'supervisor-status.txt';$currentFile=Join-Path $stateRoot 'current.json';$shuffleStatusFile=Join-Path $stateRoot 'shuffle-status.txt';$shuffleRequestFile=Join-Path $stateRoot 'shuffle-request.txt';$playlistFile=Join-Path $DataRoot 'playlist.txt';$historyFile=Join-Path $stateRoot 'history.jsonl';$metaDir=Join-Path $DataRoot 'cache\meta';$audioDir=Join-Path $DataRoot 'cache\audio';$artDir=Join-Path $DataRoot 'cache\artwork';$videoDir=Join-Path $DataRoot 'cache\video';$vizDir=Join-Path $DataRoot 'cache\visualizer';$gainDir=Join-Path $DataRoot 'cache\gain';$statusDir=Join-Path $DataRoot 'cache\status'
function LivePid($p){if(-not(Test-Path $p)){return 0};$n=0;try{[void][int]::TryParse((Get-Content $p -Raw).Trim(),[ref]$n)}catch{return 0};if($n -gt 0 -and (Get-Process -Id $n -ErrorAction SilentlyContinue)){return $n};return 0}
function Running{return (LivePid $enginePidFile)-gt 0};function Starting{return (LivePid $supervisorPidFile)-gt 0}
function Send-Mpv([object[]]$cmd){$pipe=$null;$writer=$null;try{$pipe=New-Object System.IO.Pipes.NamedPipeClientStream('.',$pipeName,[System.IO.Pipes.PipeDirection]::Out);$pipe.Connect(120);$writer=New-Object System.IO.StreamWriter($pipe,(New-Object System.Text.UTF8Encoding($false)));$writer.AutoFlush=$true;$writer.WriteLine((@{command=$cmd}|ConvertTo-Json -Compress));return $true}catch{return $false}finally{if($writer){$writer.Dispose()};if($pipe){$pipe.Dispose()}}}
function StartEngine{if(Running -or Starting){return};Remove-Item $engineStatusFile -Force -ErrorAction SilentlyContinue;Set-Content $supervisorStatusFile 'Launching YOMI...' -Encoding UTF8;$e=$supervisor.Replace("'","''");Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-Command',("& '"+$e+"'")) -WindowStyle Hidden|Out-Null}
function ForceStop{foreach($f in @($enginePidFile,$serverPidFile,$supervisorPidFile)){$p=LivePid $f;if($p -gt 0 -and $p -ne $PID){Stop-Process -Id $p -Force -ErrorAction SilentlyContinue}};Remove-Item $enginePidFile,$serverPidFile,$supervisorPidFile -Force -ErrorAction SilentlyContinue}
$script:stopping=$false;$script:deadline=[DateTime]::MinValue;$script:shufflePending=$false;$script:shuffleProcess=$null
function BeginStop{if(-not(Running)-and -not(Starting)){$script:stopping=$false;return};$script:stopping=$true;$script:deadline=[DateTime]::UtcNow.AddSeconds(2);[void](Send-Mpv @('quit'))}
function StopForExit{
    if(-not(Running)-and -not(Starting)){return}
    [void](Send-Mpv @('quit'))
    $d=[DateTime]::UtcNow.AddSeconds(2)
    while([DateTime]::UtcNow -lt $d){
        if(-not(Running)-and -not(Starting)){return}
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 60
    }
    ForceStop
}
function StartShuffle{
    Remove-Item $shuffleStatusFile -Force -ErrorAction SilentlyContinue
    $escaped=$shuffleScript.Replace("'","''")
    $script:shuffleProcess=Start-Process powershell.exe -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy','Bypass',
        '-Command',
        ("& '"+$escaped+"'")
    ) -WindowStyle Hidden -PassThru
}
function RequestShuffle{$a=[System.Windows.Forms.MessageBox]::Show("Shuffle Playlist now?`r`n`r`nThis reloads the latest YouTube playlist, preserves duplicate occurrences, creates a new random order, resets cache mapping and restarts from track 1.",'Shuffle Playlist',[System.Windows.Forms.MessageBoxButtons]::YesNo,[System.Windows.Forms.MessageBoxIcon]::Question);if($a -ne [System.Windows.Forms.DialogResult]::Yes){return};if(Running -or Starting){$script:shufflePending=$true;BeginStop}else{StartShuffle}}
function BeginApprovedShuffle{
    if($script:shuffleProcess -or $script:shufflePending){return}
    if(Running -or Starting){
        $script:shufflePending=$true
        BeginStop
    }else{
        StartShuffle
    }
}
$compactSize=New-Object System.Drawing.Size(535,375);$expandedSize=New-Object System.Drawing.Size(1120,680)
$form=New-Object System.Windows.Forms.Form;$form.Text='YOMI 4.2.0 - YouTube OBS Music Interface';$form.StartPosition='CenterScreen';$form.Size=$compactSize;$form.MinimumSize=$compactSize;$form.MaximumSize=$expandedSize;$form.MaximizeBox=$false;$form.Font=New-Object System.Drawing.Font('Segoe UI',10);if(Test-Path $iconPath){try{$form.Icon=New-Object System.Drawing.Icon($iconPath)}catch{}}
$brand=New-Object System.Windows.Forms.Label;$brand.Text='YOMI';$brand.Font=New-Object System.Drawing.Font('Segoe UI Semibold',20);$brand.Location=New-Object System.Drawing.Point(18,10);$brand.Size=New-Object System.Drawing.Size(90,38);$form.Controls.Add($brand)
$modeLabel=New-Object System.Windows.Forms.Label;$modeLabel.Location=New-Object System.Drawing.Point(115,18);$modeLabel.Size=New-Object System.Drawing.Size(390,24);$modeLabel.ForeColor=[System.Drawing.Color]::DimGray;$form.Controls.Add($modeLabel)
$status=New-Object System.Windows.Forms.Label;$status.Location=New-Object System.Drawing.Point(20,52);$status.Size=New-Object System.Drawing.Size(485,23);$status.Font=New-Object System.Drawing.Font('Segoe UI Semibold',10);$form.Controls.Add($status)
$now=New-Object System.Windows.Forms.Label;$now.Location=New-Object System.Drawing.Point(20,78);$now.Size=New-Object System.Drawing.Size(485,45);$now.AutoEllipsis=$true;$form.Controls.Add($now)
function Btn($text,$x,$y,$w){$b=New-Object System.Windows.Forms.Button;$b.Text=$text;$b.Location=New-Object System.Drawing.Point($x,$y);$b.Size=New-Object System.Drawing.Size($w,34);$form.Controls.Add($b);return $b}
$prev=Btn 'Previous' 20 130 100;$pause=Btn 'Pause' 130 130 90;$next=Btn 'Next' 230 130 90;$startStop=Btn 'Stop YOMI' 330 130 175
$shuffle=Btn 'Shuffle Playlist' 20 174 145;$settingsBtn=Btn 'Settings' 175 174 95;$obsBtn=Btn 'OBS Setup' 280 174 105;$dataBtn=Btn 'Data Folder' 395 174 110
$hideBtn=Btn 'Hide to tray' 20 218 120;$exitBtn=Btn 'Exit controller' 150 218 125;$hideComment=Btn 'Hide comment' 285 218 120;$queueBtn=Btn 'Show queue' 415 218 90
$hint=New-Object System.Windows.Forms.Label;$hint.Text='X stops YOMI and exits. Use Hide to tray if you want playback to continue. Hide comment suppresses the current track''s Featured Comment.';$hint.Location=New-Object System.Drawing.Point(20,265);$hint.Size=New-Object System.Drawing.Size(485,48);$hint.ForeColor=[System.Drawing.Color]::DimGray;$form.Controls.Add($hint)

$queuePanel=New-Object System.Windows.Forms.Panel;$queuePanel.Location=New-Object System.Drawing.Point(530,10);$queuePanel.Size=New-Object System.Drawing.Size(560,620);$queuePanel.Visible=$false;$form.Controls.Add($queuePanel)
$queueTitle=New-Object System.Windows.Forms.Label;$queueTitle.Text='PLAYLIST NAVIGATOR';$queueTitle.Font=New-Object System.Drawing.Font('Segoe UI Semibold',14);$queueTitle.Location=New-Object System.Drawing.Point(8,8);$queueTitle.Size=New-Object System.Drawing.Size(250,32);$queuePanel.Controls.Add($queueTitle)
$queueStatus=New-Object System.Windows.Forms.Label;$queueStatus.Text='Previous plays + next 20 positions';$queueStatus.Location=New-Object System.Drawing.Point(270,14);$queueStatus.Size=New-Object System.Drawing.Size(275,24);$queueStatus.TextAlign='MiddleRight';$queueStatus.ForeColor=[System.Drawing.Color]::DimGray;$queuePanel.Controls.Add($queueStatus)
$queueGrid=New-Object System.Windows.Forms.DataGridView;$queueGrid.Location=New-Object System.Drawing.Point(8,48);$queueGrid.Size=New-Object System.Drawing.Size(544,500);$queueGrid.ReadOnly=$true;$queueGrid.AllowUserToAddRows=$false;$queueGrid.AllowUserToDeleteRows=$false;$queueGrid.AllowUserToResizeRows=$false;$queueGrid.MultiSelect=$false;$queueGrid.SelectionMode='FullRowSelect';$queueGrid.RowHeadersVisible=$false;$queueGrid.AutoSizeRowsMode='None';$queueGrid.RowTemplate.Height=24;$queueGrid.BackgroundColor=[System.Drawing.Color]::White;$queueGrid.BorderStyle='FixedSingle';$queuePanel.Controls.Add($queueGrid)
[void]$queueGrid.Columns.Add('Position','POSITION');[void]$queueGrid.Columns.Add('Number','#');[void]$queueGrid.Columns.Add('Title','TITLE');[void]$queueGrid.Columns.Add('Channel','CHANNEL');[void]$queueGrid.Columns.Add('Cache','CACHE')
$queueGrid.Columns['Position'].Width=84;$queueGrid.Columns['Number'].Width=48;$queueGrid.Columns['Title'].Width=205;$queueGrid.Columns['Channel'].Width=120;$queueGrid.Columns['Cache'].Width=68
$playSelected=New-Object System.Windows.Forms.Button;$playSelected.Text='PLAY SELECTED';$playSelected.Location=New-Object System.Drawing.Point(8,560);$playSelected.Size=New-Object System.Drawing.Size(155,38);$queuePanel.Controls.Add($playSelected)
$queueHint=New-Object System.Windows.Forms.Label;$queueHint.Text='Double-click a row or select it and press Play Selected. Uncached jumps prepare normally.';$queueHint.Location=New-Object System.Drawing.Point(178,558);$queueHint.Size=New-Object System.Drawing.Size(370,48);$queueHint.ForeColor=[System.Drawing.Color]::DimGray;$queuePanel.Controls.Add($queueHint)

$script:queueVisible=$false;$script:playlistStamp=0L;$script:playlistLines=@();$script:metaCache=@{};$script:nextQueueRefresh=[DateTime]::MinValue
function Refresh-PlaylistIndex {
    if(-not(Test-Path $playlistFile)){$script:playlistLines=@();return}
    $stamp=(Get-Item $playlistFile -ErrorAction SilentlyContinue).LastWriteTimeUtc.Ticks
    if($stamp -ne $script:playlistStamp){$script:playlistStamp=$stamp;$script:playlistLines=@(Get-Content $playlistFile | Where-Object {$_ -match '^https?://' });$script:metaCache=@{}}
}
function Get-TrackMeta([int]$index) {
    $path=Join-Path $metaDir ("track-$index.info.json")
    if(-not(Test-Path $path)){return [PSCustomObject]@{title="Track $index (metadata pending)";channel=''}}
    $stamp=(Get-Item $path -ErrorAction SilentlyContinue).LastWriteTimeUtc.Ticks;$key=[string]$index;$cached=$script:metaCache[$key]
    if($cached -and $cached.Stamp -eq $stamp){return $cached.Value}
    try{$raw=Get-Content $path -Raw|ConvertFrom-Json;$value=[PSCustomObject]@{title=if($raw.title){[string]$raw.title}else{"Track $index"};channel=if($raw.channel){[string]$raw.channel}elseif($raw.uploader){[string]$raw.uploader}else{''}}}
    catch{$value=[PSCustomObject]@{title="Track $index (metadata pending)";channel=''}}
    $script:metaCache[$key]=[PSCustomObject]@{Stamp=$stamp;Value=$value};return $value
}
function Config-WantsModule($config,[string]$module) {
    if([string]$config.app_mode -ne 'Streamer / OBS'){return $false}
    if($module -eq 'visualizer' -and -not(Test-YomiComponent 'ffmpeg')){return $false}
    switch($module){'artwork'{if([bool]$config.artwork_enabled){return $true}}'video'{if([bool]$config.video_enabled){return $true}}'visualizer'{if([bool]$config.visualizer_enabled){return $true}}}
    if([bool]$config.director_mode){foreach($output in @($config.director_outputs)){if([bool]$output.enabled -and @(([string]$output.modules).ToLowerInvariant().Split(',')|ForEach-Object{$_.Trim()}) -contains $module){return $true}}}
    return $false
}
function Get-CacheLabel([int]$index,$config) {
    $audioReady=(Test-Path (Join-Path $audioDir "track-$index.audio")) -and (Test-Path (Join-Path $metaDir "track-$index.info.json")) -and (Test-Path (Join-Path $gainDir "track-$index.gain"))
    if(-not $audioReady){return 'WAITING'}
    if(Config-WantsModule $config 'artwork'){$ok=$false;foreach($ext in @('jpg','jpeg','png','webp')){if(Test-Path (Join-Path $artDir "track-$index.$ext")){$ok=$true;break}};if(-not $ok){$ok=Test-Path (Join-Path $statusDir "track-$index.artwork.failed")};if(-not $ok){return 'BUILDING'}}
    if(Config-WantsModule $config 'video'){$ok=(Test-Path (Join-Path $videoDir "track-$index.mp4")) -or (Test-Path (Join-Path $statusDir "track-$index.video.failed"));if(-not $ok){return 'BUILDING'}}
    if(Config-WantsModule $config 'visualizer'){$ok=(Test-Path (Join-Path $vizDir "track-$index.mp4")) -or (Test-Path (Join-Path $statusDir "track-$index.visualizer.failed"));if(-not $ok){return 'BUILDING'}}
    return 'READY'
}
function Add-QueueRow([string]$position,[int]$index,[string]$title,[string]$channel,[string]$cache,[System.Drawing.Color]$color) {
    $rowIndex=$queueGrid.Rows.Add($position,$index,$title,$channel,$cache);$row=$queueGrid.Rows[$rowIndex];$row.Tag=$index;$row.DefaultCellStyle.BackColor=$color
}
function Next-QueueCandidate([int]$from,[int]$count) {
    $index=$from;for($tries=0;$tries -lt $count;$tries++){$index=($index%$count)+1;if(-not(Test-Path (Join-Path $statusDir "track-$index.audio.permanent"))){return $index}}
    return 0
}
function Refresh-QueueView([int]$currentIndex,$config) {
    if(-not $script:queueVisible){return};Refresh-PlaylistIndex;$count=$script:playlistLines.Count;if($count -lt 1){$queueGrid.Rows.Clear();$queueStatus.Text='Playlist is not ready';return}
    if($currentIndex -lt 1 -or $currentIndex -gt $count){$currentIndex=1}
    $selectedIndex=0;if($queueGrid.SelectedRows.Count -gt 0){$selectedIndex=[int]$queueGrid.SelectedRows[0].Tag};$firstVisible=-1;try{$firstVisible=$queueGrid.FirstDisplayedScrollingRowIndex}catch{}
    $queueGrid.SuspendLayout();try{
        $queueGrid.Rows.Clear();$history=@()
        if(Test-Path $historyFile){foreach($line in @(Get-Content $historyFile -Tail 30 -ErrorAction SilentlyContinue)){try{$entry=$line|ConvertFrom-Json;if([int]$entry.index -gt 0){$history+=$entry}}catch{}}}
        if($history.Count -gt 0 -and [int]$history[-1].index -eq $currentIndex){$history=@($history|Select-Object -First ([Math]::Max(0,$history.Count-1)))}
        $history=@($history|Select-Object -Last 20)
        foreach($entry in $history){Add-QueueRow 'PREVIOUS' ([int]$entry.index) ([string]$entry.title) ([string]$entry.channel) (Get-CacheLabel ([int]$entry.index) $config) ([System.Drawing.Color]::FromArgb(242,242,242))}
        $meta=Get-TrackMeta $currentIndex;$currentLabel=if(Running){'PLAYING'}else{'RESUME'};Add-QueueRow $currentLabel $currentIndex ([string]$meta.title) ([string]$meta.channel) $currentLabel ([System.Drawing.Color]::FromArgb(214,245,220))
        $cursor=$currentIndex;for($offset=1;$offset -le [Math]::Min(20,$count-1);$offset++){$index=Next-QueueCandidate $cursor $count;if($index -lt 1 -or $index -eq $currentIndex){break};$cursor=$index;$meta=Get-TrackMeta $index;$position=if($offset -eq 1){'UP NEXT'}else{"AHEAD +$offset"};Add-QueueRow $position $index ([string]$meta.title) ([string]$meta.channel) (Get-CacheLabel $index $config) ([System.Drawing.Color]::White)}
        $queueStatus.Text="Track $currentIndex of $count  |  double-click to jump"
        $queueGrid.ClearSelection();$target=if($selectedIndex -gt 0){$selectedIndex}else{$currentIndex};foreach($row in $queueGrid.Rows){if([int]$row.Tag -eq $target){$row.Selected=$true;break}}
        if($firstVisible -ge 0 -and $firstVisible -lt $queueGrid.Rows.Count){$queueGrid.FirstDisplayedScrollingRowIndex=$firstVisible}else{foreach($row in $queueGrid.Rows){if([int]$row.Tag -eq $currentIndex){$queueGrid.FirstDisplayedScrollingRowIndex=[Math]::Max(0,$row.Index-4);break}}}
    }finally{$queueGrid.ResumeLayout()}
}
function Current-QueueIndex {
    if(Test-Path $currentFile){try{$s=Get-Content $currentFile -Raw|ConvertFrom-Json;if([int]$s.index -gt 0){return [int]$s.index}}catch{}}
    $resume=Join-Path $stateRoot 'resume-track.txt';if(Test-Path $resume){$n=0;if([int]::TryParse((Get-Content $resume -Raw).Trim(),[ref]$n)){return $n}}
    return 1
}
function Jump-ToQueueSelection {
    if($queueGrid.SelectedRows.Count -lt 1){return};$index=[int]$queueGrid.SelectedRows[0].Tag;if($index -lt 1){return}
    if(Running){[void](Send-Mpv @('script-message','yomi-jump',[string]$index))}else{Set-Content (Join-Path $stateRoot 'resume-track.txt') ([string]$index) -Encoding ASCII;StartEngine}
}
function Set-QueueVisible([bool]$visible) {
    $script:queueVisible=$visible;$queuePanel.Visible=$visible
    if($visible){$queueBtn.Text='Hide queue';$form.Size=$expandedSize;$script:nextQueueRefresh=[DateTime]::MinValue;Refresh-QueueView (Current-QueueIndex) (Get-YomiConfig)}
    else{$queueBtn.Text='Show queue';$form.Size=$compactSize}
}
$tray=New-Object System.Windows.Forms.NotifyIcon;$tray.Text='YOMI';$tray.Visible=$true;if(Test-Path $iconPath){try{$tray.Icon=New-Object System.Drawing.Icon($iconPath)}catch{$tray.Icon=[System.Drawing.SystemIcons]::Application}}else{$tray.Icon=[System.Drawing.SystemIcons]::Application}
$menu=New-Object System.Windows.Forms.ContextMenuStrip;$miShow=$menu.Items.Add('Show controller');$miPause=$menu.Items.Add('Play / Pause');$miPrev=$menu.Items.Add('Previous');$miNext=$menu.Items.Add('Next');$miHideComment=$menu.Items.Add('Hide current Featured Comment');$miShuffle=$menu.Items.Add('Shuffle Playlist...');[void]$menu.Items.Add('-');$miSettings=$menu.Items.Add('Settings');$miStop=$menu.Items.Add('Stop YOMI');$miExit=$menu.Items.Add('Exit controller');$tray.ContextMenuStrip=$menu
$prev.Add_Click({[void](Send-Mpv @('script-message','yomi-prev'))});$next.Add_Click({[void](Send-Mpv @('script-message','yomi-next'))});$pause.Add_Click({[void](Send-Mpv @('cycle','pause'))});$hideComment.Add_Click({[void](Send-Mpv @('script-message','yomi-hide-comment'))});$shuffle.Add_Click({RequestShuffle});$settingsBtn.Add_Click({Start-Process $launcher -ArgumentList 'settings'});$obsBtn.Add_Click({$c=Get-YomiConfig;if([string]$c.app_mode -ne 'Streamer / OBS'){return};$p=Write-ObsInstructions $c;Start-Process notepad.exe ('"'+$p+'"')});$dataBtn.Add_Click({Start-Process explorer.exe $DataRoot});$hideBtn.Add_Click({$form.Hide()});$queueBtn.Add_Click({Set-QueueVisible (-not $script:queueVisible)});$playSelected.Add_Click({Jump-ToQueueSelection});$queueGrid.Add_CellDoubleClick({param($sender,$e)if($e.RowIndex -ge 0){$queueGrid.Rows[$e.RowIndex].Selected=$true;Jump-ToQueueSelection}});$startStop.Add_Click({if(Running -or Starting){BeginStop}else{StartEngine}});$exitBtn.Add_Click({$script:allowClose=$true;StopForExit;$tray.Visible=$false;$form.Close()})
$miShow.Add_Click({$form.Show();$form.WindowState='Normal';$form.Activate()});$miPause.Add_Click({[void](Send-Mpv @('cycle','pause'))});$miPrev.Add_Click({[void](Send-Mpv @('script-message','yomi-prev'))});$miNext.Add_Click({[void](Send-Mpv @('script-message','yomi-next'))});$miHideComment.Add_Click({[void](Send-Mpv @('script-message','yomi-hide-comment'))});$miShuffle.Add_Click({RequestShuffle});$miSettings.Add_Click({Start-Process $launcher -ArgumentList 'settings'});$miStop.Add_Click({BeginStop});$miExit.Add_Click({$script:allowClose=$true;StopForExit;$tray.Visible=$false;$form.Close()});$tray.Add_DoubleClick({$form.Show();$form.WindowState='Normal';$form.Activate()})
$script:allowClose=$false
$form.Add_FormClosing({
    param($sender,$e)
    if($e.CloseReason -eq [System.Windows.Forms.CloseReason]::UserClosing -and -not $script:allowClose){
        $script:allowClose=$true
        StopForExit
        $tray.Visible=$false
    }
})
$timer=New-Object System.Windows.Forms.Timer;$timer.Interval=350;$timer.Add_Tick({
 if(Test-Path $shuffleRequestFile){
    try{
        Remove-Item $shuffleRequestFile -Force -ErrorAction SilentlyContinue
        BeginApprovedShuffle
    }catch{}
 }
 if($script:stopping){if(-not(Running)-and -not(Starting)){$script:stopping=$false;if($script:shufflePending){$script:shufflePending=$false;StartShuffle}}elseif([DateTime]::UtcNow -ge $script:deadline){ForceStop;$script:stopping=$false;if($script:shufflePending){$script:shufflePending=$false;StartShuffle}}}
 if($script:shuffleProcess){if($script:shuffleProcess.HasExited){$code=$script:shuffleProcess.ExitCode;$script:shuffleProcess.Dispose();$script:shuffleProcess=$null;if($code -eq 0){StartEngine}else{$status.Text='Shuffle failed';if(Test-Path $shuffleStatusFile){try{$now.Text=(Get-Content $shuffleStatusFile -Raw).Trim()}catch{$now.Text='Shuffle Playlist failed.'}}else{$now.Text='Shuffle Playlist failed.'}}}else{$status.Text='Shuffling playlist...';if(Test-Path $shuffleStatusFile){try{$now.Text=(Get-Content $shuffleStatusFile -Raw).Trim()}catch{}};return}}
 $c=Get-YomiConfig
 if($script:queueVisible -and [DateTime]::UtcNow -ge $script:nextQueueRefresh){$script:nextQueueRefresh=[DateTime]::UtcNow.AddMilliseconds(1500);Refresh-QueueView (Current-QueueIndex) $c}
 $modeDetail='One-source OBS overlay'
 if([bool]$c.director_mode){$modeDetail='OBS + Director Mode'}
 if([string]$c.app_mode -eq 'Player'){$modeDetail=[string]$c.player_video_quality}
 $modeLabel.Text=([string]$c.app_mode)+'  |  '+$modeDetail
 $obsBtn.Enabled=([string]$c.app_mode -eq 'Streamer / OBS')
 $running=Running;$starting=Starting;if(-not $running -and -not $starting){$status.Text='Stopped';$now.Text='YOMI is not playing.';$pause.Enabled=$false;$prev.Enabled=$false;$next.Enabled=$false;$startStop.Text='Start YOMI';$tray.Text='YOMI - Stopped';return};$startStop.Text='Stop YOMI'
 if(-not $running){$pause.Enabled=$false;$prev.Enabled=$false;$next.Enabled=$false;$status.Text='Starting...';if(Test-Path $supervisorStatusFile){try{$now.Text=(Get-Content $supervisorStatusFile -Raw).Trim()}catch{}};$tray.Text='YOMI - Starting';return}
 $pause.Enabled=$true;$prev.Enabled=$true;$next.Enabled=$true;$engine=$null;$track=$null;if(Test-Path $engineStatusFile){try{$engine=Get-Content $engineStatusFile -Raw|ConvertFrom-Json}catch{}};if(Test-Path $currentFile){try{$track=Get-Content $currentFile -Raw|ConvertFrom-Json}catch{}}
 if($engine){switch([string]$engine.phase){'paused'{$status.Text='Paused';$pause.Text='Play';$tray.Text='YOMI - Paused'}'playing'{$status.Text='Playing';$pause.Text='Pause';$tray.Text='YOMI - Playing'}'error'{$status.Text='Playback error';$tray.Text='YOMI - Error'}default{$status.Text='Preparing...';$pause.Text='Pause';$tray.Text='YOMI - Preparing'}};if($engine.message){$now.Text=[string]$engine.message}}else{$status.Text='Preparing...';$now.Text='Preparing the first playable track...'}
 if($track -and $engine -and ([string]$engine.phase -in @('playing','paused'))){$name=[string]$track.title;if($track.channel){$name+='  -  '+[string]$track.channel};$now.Text=$name}
});$timer.Start();StartEngine;$form.Add_Shown({$form.Activate();Start-Process (Join-Path $PSScriptRoot 'YomiLauncher.exe') -ArgumentList 'update-auto'});try{[void]$form.ShowDialog()}finally{$timer.Stop();$timer.Dispose();$tray.Visible=$false;$tray.Dispose();try{$mutex.ReleaseMutex()}catch{};$mutex.Dispose()}
