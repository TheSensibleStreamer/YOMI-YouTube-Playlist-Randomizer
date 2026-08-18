$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
$config = Get-YomiConfig
$stateRoot=Join-Path $DataRoot 'state';$supervisorPidFile=Join-Path $stateRoot 'supervisor.pid';$serverPidFile=Join-Path $stateRoot 'server.pid';$enginePidFile=Join-Path $stateRoot 'engine.pid';$supervisorStatus=Join-Path $stateRoot 'supervisor-status.txt'
function Set-StartStatus([string]$Text){try{Set-Content $supervisorStatus $Text -Encoding UTF8}catch{}}
function Write-SupervisorLog([string]$Text){try{Add-Content (Join-Path $DataRoot 'logs\supervisor.log') ((Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')+' '+$Text) -Encoding UTF8}catch{}}
Set-Content $supervisorPidFile $PID -Encoding ASCII;Set-StartStatus ("Starting YOMI "+(Get-YomiVersionText)+"...")
Remove-Item (Join-Path $DataRoot 'cache\status\*.failed') -Force -ErrorAction SilentlyContinue;Remove-Item (Join-Path $DataRoot 'state\current.json') -Force -ErrorAction SilentlyContinue;Remove-Item (Join-Path $DataRoot 'state\engine-status.json') -Force -ErrorAction SilentlyContinue
$vizReset=Join-Path $DataRoot 'state\visualizer-reset.pending';if(Test-Path $vizReset){Get-ChildItem (Join-Path $DataRoot 'cache\visualizer') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Remove-Item $vizReset -Force -ErrorAction SilentlyContinue}
$videoReset=Join-Path $DataRoot 'state\video-reset.pending';if(Test-Path $videoReset){Get-ChildItem (Join-Path $DataRoot 'cache\video') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\telemetry') -Filter '*.video.json' -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\status') -Filter 'track-*.video*' -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Remove-Item $videoReset -Force -ErrorAction SilentlyContinue}
$audioReset=Join-Path $DataRoot 'state\audio-reset.pending';if(Test-Path $audioReset){foreach($name in @('audio','meta','gain','visualizer')){Get-ChildItem (Join-Path $DataRoot ('cache\'+$name)) -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue};Get-ChildItem (Join-Path $DataRoot 'cache\telemetry') -Filter '*.audio.json' -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\status') -Filter 'track-*.audio*' -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Remove-Item $audioReset -Force -ErrorAction SilentlyContinue}
$artReset=Join-Path $DataRoot 'state\artwork-reset.pending';if(Test-Path $artReset){Get-ChildItem (Join-Path $DataRoot 'cache\artwork') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Remove-Item $artReset -Force -ErrorAction SilentlyContinue}
$created=$false;$mutex=New-Object System.Threading.Mutex($true,'Local\YOMI_V4',[ref]$created);if(-not $created){Remove-Item $supervisorPidFile -Force -ErrorAction SilentlyContinue;exit 0}
$server=$null;$mpvProcess=$null
try{
 Write-SupervisorLog ('START mode='+[string]$config.app_mode)
 $playlist=Join-Path $DataRoot 'playlist.txt';if(-not(Test-Path $playlist)){Set-StartStatus 'Reading and shuffling the playlist for the first time...';& (Join-Path $PSScriptRoot 'shuffle.ps1');if($LASTEXITCODE -ne 0 -or -not(Test-Path $playlist)){Add-Type -AssemblyName System.Windows.Forms;[System.Windows.Forms.MessageBox]::Show('The playlist is not ready. Open YOMI Settings and save a playlist first.','YOMI')|Out-Null;exit 1}}
 $streamer=([string]$config.app_mode -eq 'Streamer / OBS')
 if($streamer){Set-StartStatus 'Starting OBS overlay server...';$serverScript=Join-Path $PSScriptRoot 'server.ps1';$serverOut=Join-Path $DataRoot 'logs\server.log';$serverErr=Join-Path $DataRoot 'logs\server-error.log';$escaped=$serverScript.Replace("'","''");$server=Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-Command',("& '"+$escaped+"'")) -WindowStyle Hidden -RedirectStandardOutput $serverOut -RedirectStandardError $serverErr -PassThru;Set-Content $serverPidFile $server.Id -Encoding ASCII;try{$server.PriorityClass=[System.Diagnostics.ProcessPriorityClass]::Normal}catch{};Write-SupervisorLog ('SERVER START PID '+$server.Id)}else{Set-StartStatus 'Player mode - OBS server disabled for lower overhead.';Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue}
 Set-StartStatus 'Starting player...';$mpv=Join-Path $InstallRoot 'runtime\mpv\mpv.exe';$lua=Join-Path $PSScriptRoot 'music.lua';$mpvLog=Join-Path $DataRoot 'logs\mpv.log';$yt=Join-Path $InstallRoot 'runtime\yt-dlp\yt-dlp.exe';$denoDir=Join-Path $InstallRoot 'runtime\deno';if(Test-Path (Join-Path $denoDir 'deno.exe')){$env:PATH=$denoDir+';'+$env:PATH}
 $args=@('--no-config','--idle=yes','--audio-display=no','--gapless-audio=yes','--replaygain=no','--volume=100','--volume-max=100','--input-ipc-server=\\.\pipe\yomi-v4',("--log-file=`""+$mpvLog+"`""),("--script=`""+$lua+"`""))
 $videoQuality=[string]$config.player_video_quality
 if($streamer -or $videoQuality -eq 'Off (audio only)'){$args+=@('--force-window=no','--no-video')}else{
  $args+=@('--force-window=yes','--hwdec=auto-safe',("--script-opts=ytdl_hook-ytdl_path="+$yt))
  $videoCap=0;switch($videoQuality){'144p'{$videoCap=144}'240p'{$videoCap=240}'360p'{$videoCap=360}'480p'{$videoCap=480}'720p'{$videoCap=720}}
  $preferLow=([string]$config.video_preference -eq 'Prefer lowest compatible')
  $baseSelector='bestvideo';if($preferLow){$baseSelector='worstvideo'}
  $heightFilter='';if($videoCap -gt 0){$heightFilter="[height<=$videoCap]"}
  $prefer60=([string]$config.video_fps -match '^60')
  if($prefer60){$videoFmt="${baseSelector}${heightFilter}[fps>30][fps<=60][vcodec^=avc][ext=mp4]/${baseSelector}${heightFilter}[fps>30][fps<=60][ext=mp4]/${baseSelector}${heightFilter}[fps<=60][vcodec^=avc][ext=mp4]/${baseSelector}${heightFilter}[fps<=60][ext=mp4]"}
  else{$videoFmt="${baseSelector}${heightFilter}[fps<=30][vcodec^=avc][ext=mp4]/${baseSelector}${heightFilter}[fps<=30][ext=mp4]"}
  $audioCap=0;switch([string]$config.audio_quality){'Low (~64 kbps)'{$audioCap=64}'Standard (~128 kbps)'{$audioCap=128}'High (~160 kbps)'{$audioCap=160}}
  $preferLowAudio=([string]$config.audio_preference -eq 'Prefer lowest compatible')
  if($audioCap -gt 0){if($preferLowAudio){$audioFmt="worstaudio[abr<=$audioCap]/worstaudio"}else{$audioFmt="bestaudio[abr<=$audioCap]/bestaudio"}}
  else{if($preferLowAudio){$audioFmt='worstaudio/bestaudio'}else{$audioFmt='bestaudio/best'}}
  $fmt="($videoFmt)+($audioFmt)/best"
  $args+=("--ytdl-format="+$fmt)
 }
 $env:YOMI_INSTALL_ROOT=$InstallRoot;$mpvProcess=Start-Process -FilePath $mpv -ArgumentList $args -PassThru;Set-Content $enginePidFile $mpvProcess.Id -Encoding ASCII;try{$mpvProcess.PriorityClass=[System.Diagnostics.ProcessPriorityClass]::Normal}catch{};Set-StartStatus 'Player started; preparing the first playable track...';Write-SupervisorLog ('MPV START PID '+$mpvProcess.Id);$mpvProcess.WaitForExit();Write-SupervisorLog ('MPV EXIT '+$mpvProcess.ExitCode)
}catch{Set-StartStatus ('Startup error: '+$_.Exception.Message);Write-SupervisorLog ('ERROR '+$_.Exception.Message);try{Add-Type -AssemblyName System.Windows.Forms;[System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'YOMI')|Out-Null}catch{}}
finally{if($mpvProcess -and -not $mpvProcess.HasExited){Stop-Process -Id $mpvProcess.Id -Force -ErrorAction SilentlyContinue};if($server -and -not $server.HasExited){Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue};Remove-Item $enginePidFile,$serverPidFile,$supervisorPidFile -Force -ErrorAction SilentlyContinue;Set-StartStatus 'Stopped';try{$mutex.ReleaseMutex()}catch{};$mutex.Dispose();Write-SupervisorLog 'STOP'}
