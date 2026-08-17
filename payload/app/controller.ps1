$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms;Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'common.ps1');Initialize-YomiData
$created=$false;$mutex=New-Object System.Threading.Mutex($true,'Local\YOMI_CONTROLLER_V4',[ref]$created);if(-not $created){exit 0}
$pipeName='yomi-v4';$supervisor=Join-Path $PSScriptRoot 'supervisor.ps1';$shuffleScript=Join-Path $PSScriptRoot 'shuffle.ps1';$launcher=Join-Path $PSScriptRoot 'YomiLauncher.exe';$iconPath=Join-Path $InstallRoot 'assets\yomi-v408.ico'
$stateRoot=Join-Path $DataRoot 'state';$enginePidFile=Join-Path $stateRoot 'engine.pid';$serverPidFile=Join-Path $stateRoot 'server.pid';$supervisorPidFile=Join-Path $stateRoot 'supervisor.pid';$engineStatusFile=Join-Path $stateRoot 'engine-status.json';$supervisorStatusFile=Join-Path $stateRoot 'supervisor-status.txt';$currentFile=Join-Path $stateRoot 'current.json';$shuffleStatusFile=Join-Path $stateRoot 'shuffle-status.txt';$shuffleRequestFile=Join-Path $stateRoot 'shuffle-request.txt'
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
$form=New-Object System.Windows.Forms.Form;$form.Text='YOMI 4.0.9.4 Controller';$form.StartPosition='CenterScreen';$form.Size=New-Object System.Drawing.Size(535,330);$form.MinimumSize=$form.Size;$form.MaximumSize=$form.Size;$form.MaximizeBox=$false;$form.Font=New-Object System.Drawing.Font('Segoe UI',10);if(Test-Path $iconPath){try{$form.Icon=New-Object System.Drawing.Icon($iconPath)}catch{}}
$brand=New-Object System.Windows.Forms.Label;$brand.Text='YOMI';$brand.Font=New-Object System.Drawing.Font('Segoe UI Semibold',20);$brand.Location=New-Object System.Drawing.Point(18,10);$brand.Size=New-Object System.Drawing.Size(90,38);$form.Controls.Add($brand)
$modeLabel=New-Object System.Windows.Forms.Label;$modeLabel.Location=New-Object System.Drawing.Point(115,18);$modeLabel.Size=New-Object System.Drawing.Size(390,24);$modeLabel.ForeColor=[System.Drawing.Color]::DimGray;$form.Controls.Add($modeLabel)
$status=New-Object System.Windows.Forms.Label;$status.Location=New-Object System.Drawing.Point(20,52);$status.Size=New-Object System.Drawing.Size(485,23);$status.Font=New-Object System.Drawing.Font('Segoe UI Semibold',10);$form.Controls.Add($status)
$now=New-Object System.Windows.Forms.Label;$now.Location=New-Object System.Drawing.Point(20,78);$now.Size=New-Object System.Drawing.Size(485,45);$now.AutoEllipsis=$true;$form.Controls.Add($now)
function Btn($text,$x,$y,$w){$b=New-Object System.Windows.Forms.Button;$b.Text=$text;$b.Location=New-Object System.Drawing.Point($x,$y);$b.Size=New-Object System.Drawing.Size($w,34);$form.Controls.Add($b);return $b}
$prev=Btn 'Previous' 20 130 100;$pause=Btn 'Pause' 130 130 90;$next=Btn 'Next' 230 130 90;$startStop=Btn 'Stop YOMI' 330 130 175
$shuffle=Btn 'Shuffle Playlist' 20 174 145;$settingsBtn=Btn 'Settings' 175 174 95;$obsBtn=Btn 'OBS Setup' 280 174 105;$dataBtn=Btn 'Data Folder' 395 174 110
$hideBtn=Btn 'Hide to tray' 20 218 120;$exitBtn=Btn 'Exit controller' 150 218 125
$hint=New-Object System.Windows.Forms.Label;$hint.Text='X stops YOMI and exits. Use Hide to tray if you want playback to continue.';$hint.Location=New-Object System.Drawing.Point(285,218);$hint.Size=New-Object System.Drawing.Size(220,55);$hint.ForeColor=[System.Drawing.Color]::DimGray;$form.Controls.Add($hint)
$tray=New-Object System.Windows.Forms.NotifyIcon;$tray.Text='YOMI';$tray.Visible=$true;if(Test-Path $iconPath){try{$tray.Icon=New-Object System.Drawing.Icon($iconPath)}catch{$tray.Icon=[System.Drawing.SystemIcons]::Application}}else{$tray.Icon=[System.Drawing.SystemIcons]::Application}
$menu=New-Object System.Windows.Forms.ContextMenuStrip;$miShow=$menu.Items.Add('Show controller');$miPause=$menu.Items.Add('Play / Pause');$miPrev=$menu.Items.Add('Previous');$miNext=$menu.Items.Add('Next');$miShuffle=$menu.Items.Add('Shuffle Playlist...');[void]$menu.Items.Add('-');$miSettings=$menu.Items.Add('Settings');$miStop=$menu.Items.Add('Stop YOMI');$miExit=$menu.Items.Add('Exit controller');$tray.ContextMenuStrip=$menu
$prev.Add_Click({[void](Send-Mpv @('script-message','yomi-prev'))});$next.Add_Click({[void](Send-Mpv @('script-message','yomi-next'))});$pause.Add_Click({[void](Send-Mpv @('cycle','pause'))});$shuffle.Add_Click({RequestShuffle});$settingsBtn.Add_Click({Start-Process $launcher -ArgumentList 'settings'});$obsBtn.Add_Click({$c=Get-YomiConfig;if([string]$c.app_mode -ne 'Streamer / OBS'){return};$p=Write-ObsInstructions $c;Start-Process notepad.exe ('"'+$p+'"')});$dataBtn.Add_Click({Start-Process explorer.exe $DataRoot});$hideBtn.Add_Click({$form.Hide()});$startStop.Add_Click({if(Running -or Starting){BeginStop}else{StartEngine}});$exitBtn.Add_Click({$script:allowClose=$true;StopForExit;$tray.Visible=$false;$form.Close()})
$miShow.Add_Click({$form.Show();$form.WindowState='Normal';$form.Activate()});$miPause.Add_Click({[void](Send-Mpv @('cycle','pause'))});$miPrev.Add_Click({[void](Send-Mpv @('script-message','yomi-prev'))});$miNext.Add_Click({[void](Send-Mpv @('script-message','yomi-next'))});$miShuffle.Add_Click({RequestShuffle});$miSettings.Add_Click({Start-Process $launcher -ArgumentList 'settings'});$miStop.Add_Click({BeginStop});$miExit.Add_Click({$script:allowClose=$true;StopForExit;$tray.Visible=$false;$form.Close()});$tray.Add_DoubleClick({$form.Show();$form.WindowState='Normal';$form.Activate()})
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
 $modeDetail='One-source OBS overlay'
 if([string]$c.app_mode -eq 'Player'){$modeDetail=[string]$c.player_video_quality}
 $modeLabel.Text=([string]$c.app_mode)+'  |  '+$modeDetail
 $obsBtn.Enabled=([string]$c.app_mode -eq 'Streamer / OBS')
 $running=Running;$starting=Starting;if(-not $running -and -not $starting){$status.Text='Stopped';$now.Text='YOMI is not playing.';$pause.Enabled=$false;$prev.Enabled=$false;$next.Enabled=$false;$startStop.Text='Start YOMI';$tray.Text='YOMI - Stopped';return};$startStop.Text='Stop YOMI'
 if(-not $running){$pause.Enabled=$false;$prev.Enabled=$false;$next.Enabled=$false;$status.Text='Starting...';if(Test-Path $supervisorStatusFile){try{$now.Text=(Get-Content $supervisorStatusFile -Raw).Trim()}catch{}};$tray.Text='YOMI - Starting';return}
 $pause.Enabled=$true;$prev.Enabled=$true;$next.Enabled=$true;$engine=$null;$track=$null;if(Test-Path $engineStatusFile){try{$engine=Get-Content $engineStatusFile -Raw|ConvertFrom-Json}catch{}};if(Test-Path $currentFile){try{$track=Get-Content $currentFile -Raw|ConvertFrom-Json}catch{}}
 if($engine){switch([string]$engine.phase){'paused'{$status.Text='Paused';$pause.Text='Play';$tray.Text='YOMI - Paused'}'playing'{$status.Text='Playing';$pause.Text='Pause';$tray.Text='YOMI - Playing'}'error'{$status.Text='Playback error';$tray.Text='YOMI - Error'}default{$status.Text='Preparing...';$pause.Text='Pause';$tray.Text='YOMI - Preparing'}};if($engine.message){$now.Text=[string]$engine.message}}else{$status.Text='Preparing...';$now.Text='Preparing the first playable track...'}
 if($track -and $engine -and ([string]$engine.phase -in @('playing','paused'))){$name=[string]$track.title;if($track.channel){$name+='  -  '+[string]$track.channel};$now.Text=$name}
});$timer.Start();StartEngine;$form.Add_Shown({$form.Activate()});try{[void]$form.ShowDialog()}finally{$timer.Stop();$timer.Dispose();$tray.Visible=$false;$tray.Dispose();try{$mutex.ReleaseMutex()}catch{};$mutex.Dispose()}
