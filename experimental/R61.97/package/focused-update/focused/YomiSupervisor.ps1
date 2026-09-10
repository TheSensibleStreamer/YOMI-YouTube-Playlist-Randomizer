param([switch]$ExplicitPlay,[switch]$Prewarm)
$ErrorActionPreference = 'Stop'
# R61.95 DENSE CONTROLLER / AUDIO-FIRST STARTUP; OBS ownership protocol remains R61.94
$script:R15BootstrapDataRoot=Join-Path $env:LOCALAPPDATA 'YOMI'
function Write-R15BootstrapFailure([string]$Message){
    try{
        $r15State=Join-Path $script:R15BootstrapDataRoot 'state'
        $r15Logs=Join-Path $script:R15BootstrapDataRoot 'logs'
        New-Item -ItemType Directory -Path $r15State,$r15Logs -Force|Out-Null
        $r15Message='Startup error before audio engine: '+$Message
        Set-Content -LiteralPath (Join-Path $r15State 'supervisor-status.txt') -Value $r15Message -Encoding UTF8
        Add-Content -LiteralPath (Join-Path $r15Logs 'supervisor.log') -Value ((Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')+' BOOTSTRAP ERROR '+$Message) -Encoding UTF8
    }catch{}
}
try{
    . (Join-Path $PSScriptRoot 'common.ps1')
    Initialize-YomiData
}catch{
    Write-R15BootstrapFailure $_.Exception.Message
    exit 1
}
# DEV13.49 CONFIG TRANSACTION STARTUP ACTIVATION
$startupAdvisories=@()
$transactionTool=Join-Path $PSScriptRoot 'config-transaction.ps1'
if(Test-Path -LiteralPath $transactionTool){
    try{
        . $transactionTool
        $activation=Apply-YomiPendingConfigAtStartup
        if($activation -and [string]$activation.action -eq 'CONFLICT'){
            $quarantine=Join-Path $DataRoot 'state\quarantine'
            New-Item -ItemType Directory -Path $quarantine -Force|Out-Null
            $stamp=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
            foreach($staleName in @('config-pending.json','config-pending-conflict.json')){
                $stalePath=Join-Path $DataRoot ('state\'+$staleName)
                if(Test-Path -LiteralPath $stalePath){Move-Item -LiteralPath $stalePath -Destination (Join-Path $quarantine ($stamp+'-'+$staleName)) -Force}
            }
            $startupAdvisories+=('A stale pending Settings transaction conflicted with committed settings. It was preserved under state\quarantine and removed from startup authority.')
        }
    }catch{
        $startupAdvisories+=('Settings change-control check failed; committed settings will be used: '+$_.Exception.Message)
    }
}
try{
    $config = Get-YomiConfig
}catch{
    $startupAdvisories+=('Committed settings could not be read; factory runtime defaults will be used for this launch: '+$_.Exception.Message)
    try{$config=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'default-config.json') -Raw -Encoding UTF8|ConvertFrom-Json}catch{Write-R15BootstrapFailure ('No usable YOMI configuration is available: '+$_.Exception.Message);exit 1}
}
# DEV13.51 RECOVERY REPLAY STARTUP COMPILER
$recoveryTool=Join-Path $PSScriptRoot 'recovery-replay.ps1'
if(Test-Path -LiteralPath $recoveryTool){
    try{
        . $recoveryTool
        $recoveryResult=Invoke-YomiRecoveryReplay -PrepareStartup -Policy ([string]$config.recovery_policy)
        if($recoveryResult -and [string]$recoveryResult.action -eq 'BLOCK'){
            $startupAdvisories+=('Recovery found contradictory durable state; recovery was left untouched and audio startup will continue from committed state.')
        }
    }catch{
        $startupAdvisories+=('Recovery preparation failed; audio startup will continue without applying recovery: '+$_.Exception.Message)
    }
}
# DEV13.50 STATE COHERENCE STARTUP FENCE
Remove-Item (Join-Path $DataRoot 'state\state-coherence.json') -Force -ErrorAction SilentlyContinue
$stateRoot=Join-Path $DataRoot 'state';$supervisorPidFile=Join-Path $stateRoot 'supervisor.pid';$serverPidFile=Join-Path $stateRoot 'server.pid';$enginePidFile=Join-Path $stateRoot 'engine.pid';$supervisorStatus=Join-Path $stateRoot 'supervisor-status.txt'
function Set-StartStatus([string]$Text){try{Set-Content $supervisorStatus $Text -Encoding UTF8}catch{}}
function Rotate-YomiLog([string]$Path,[int64]$MaxBytes=4194304,[int]$Keep=3){
    try{
        if(-not(Test-Path $Path) -or (Get-Item $Path).Length -lt $MaxBytes){return}
        for($n=$Keep;$n -ge 1;$n--){
            $src=if($n -eq 1){$Path}else{"$Path."+($n-1)}
            $dst="$Path.$n"
            if(Test-Path $dst){Remove-Item $dst -Force -ErrorAction SilentlyContinue}
            if(Test-Path $src){Move-Item $src $dst -Force}
        }
    }catch{}
}
function Write-SupervisorLog([string]$Text){
    try{
        $path=Join-Path $DataRoot 'logs\supervisor.log'
        Rotate-YomiLog $path
        Add-Content $path ((Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')+' '+$Text) -Encoding UTF8
    }catch{}
}
Set-Content $supervisorPidFile $PID -Encoding ASCII;Set-StartStatus ("Starting YOMI "+(Get-YomiVersionText)+"...")
Remove-Item (Join-Path $DataRoot 'cache\status\*.failed') -Force -ErrorAction SilentlyContinue;Remove-Item (Join-Path $DataRoot 'state\current.json') -Force -ErrorAction SilentlyContinue;Remove-Item (Join-Path $DataRoot 'state\queue-runtime.json') -Force -ErrorAction SilentlyContinue;Remove-Item (Join-Path $DataRoot 'state\browser-clients.json') -Force -ErrorAction SilentlyContinue;Remove-Item (Join-Path $DataRoot 'state\runtime-lease.json') -Force -ErrorAction SilentlyContinue;Remove-Item (Join-Path $DataRoot 'state\engine-status.json') -Force -ErrorAction SilentlyContinue
$vizReset=Join-Path $DataRoot 'state\visualizer-reset.pending';if(Test-Path $vizReset){Get-ChildItem (Join-Path $DataRoot 'cache\visualizer') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\objects\visualizer') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Remove-Item $vizReset -Force -ErrorAction SilentlyContinue}
$videoReset=Join-Path $DataRoot 'state\video-reset.pending';if(Test-Path $videoReset){Get-ChildItem (Join-Path $DataRoot 'cache\video') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\objects\video') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\telemetry') -Filter '*.video.json' -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\status') -Filter 'track-*.video*' -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Remove-Item $videoReset -Force -ErrorAction SilentlyContinue}
$audioReset=Join-Path $DataRoot 'state\audio-reset.pending';if(Test-Path $audioReset){foreach($name in @('audio','meta','gain','visualizer')){Get-ChildItem (Join-Path $DataRoot ('cache\'+$name)) -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot ('cache\objects\'+$name)) -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue};Get-ChildItem (Join-Path $DataRoot 'cache\telemetry') -Filter '*.audio.json' -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\status') -Filter 'track-*.audio*' -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Remove-Item $audioReset -Force -ErrorAction SilentlyContinue}
$oracleReset=Join-Path $DataRoot 'state\reset-oracle-learning.pending';if(Test-Path $oracleReset){Get-ChildItem (Join-Path $DataRoot 'cache\capabilities') -File -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Remove-Item $oracleReset -Force -ErrorAction SilentlyContinue;Write-SupervisorLog 'ORACLE LEARNING RESET'}
$gainReset=Join-Path $DataRoot 'state\gain-reset.pending';if(Test-Path $gainReset){Get-ChildItem (Join-Path $DataRoot 'cache\gain') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\objects\gain') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Remove-Item $gainReset -Force -ErrorAction SilentlyContinue}
$artReset=Join-Path $DataRoot 'state\artwork-reset.pending';if(Test-Path $artReset){Get-ChildItem (Join-Path $DataRoot 'cache\artwork') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\objects\artwork') -Force -ErrorAction SilentlyContinue|Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;Remove-Item $artReset -Force -ErrorAction SilentlyContinue}
$created=$false;$mutex=New-Object System.Threading.Mutex($true,'Local\YOMI_V4',[ref]$created);if(-not $created){Set-StartStatus 'Another YOMI supervisor already owns playback startup.';Write-SupervisorLog 'MUTEX BUSY another supervisor owns Local\YOMI_V4';Remove-Item $supervisorPidFile -Force -ErrorAction SilentlyContinue;exit 2}
$safeModeOnce=Join-Path $stateRoot 'safe-mode.once'
$crashCountFile=Join-Path $stateRoot 'rapid-start-failures.txt'
$plannedStopFile=Join-Path $stateRoot 'planned-stop.pending'
Remove-Item $plannedStopFile -Force -ErrorAction SilentlyContinue
$rapidFailures=0
if(Test-Path $crashCountFile){[void][int]::TryParse((Get-Content $crashCountFile -Raw).Trim(),[ref]$rapidFailures)}
$safeMode=(Test-Path $safeModeOnce) -or ($rapidFailures -ge 3)
if(Test-Path $safeModeOnce){Remove-Item $safeModeOnce -Force -ErrorAction SilentlyContinue}
$env:YOMI_SAFE_MODE=$(if($safeMode){'1'}else{'0'})

$runtimeId=[Guid]::NewGuid().ToString('N')
$env:YOMI_RUNTIME_ID=$runtimeId
$env:YOMI_SUPERVISOR_PID=[string]$PID
$env:YOMI_SERVER_AUTHORITY='supervisor'
$runtimeInstanceFile=Join-Path $stateRoot 'runtime-instance.json'
$obsRepairRequestFile=Join-Path $stateRoot 'obs-repair-request.json'
$obsRepairResultFile=Join-Path $stateRoot 'obs-repair-result.json'
Remove-Item $obsRepairRequestFile,$obsRepairResultFile -Force -ErrorAction SilentlyContinue

function Test-YomiLoopbackPortOccupied([int]$Port){
    $client=$null
    try{
        $client=New-Object Net.Sockets.TcpClient
        $pending=$client.BeginConnect('127.0.0.1',$Port,$null,$null)
        if(-not $pending.AsyncWaitHandle.WaitOne(180)){return $false}
        try{$client.EndConnect($pending)}catch{return $false}
        return [bool]$client.Connected
    }catch{return $false}
    finally{if($client){try{$client.Close()}catch{}}}
}

function Get-YomiOverlayHealth([int]$Port,[int]$ExpectedPid=0){
    $url=('http://127.0.0.1:'+([int]$Port)+'/v1/health')
    try{
        $response=Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 1
        if($response.StatusCode -ne 200){return [PSCustomObject]@{State='absent';Owned=$false;Conflict=$false;Pid=0;Detail=('HTTP '+$response.StatusCode)}}
        $health=$null
        try{$health=$response.Content|ConvertFrom-Json -ErrorAction Stop}catch{return [PSCustomObject]@{State='conflict';Owned=$false;Conflict=$true;Pid=0;Detail='Listener answered without R61.94 ownership JSON.'}}
        $listenerPid=0;[void][int]::TryParse([string]$health.pid,[ref]$listenerPid)
        $supervisor=0;[void][int]::TryParse([string]$health.supervisor_pid,[ref]$supervisor)
        $identityOk=([string]$health.revision -eq 'R61.94') -and ([string]$health.authority -eq 'supervisor') -and ([string]$health.runtime_id -eq $runtimeId) -and ($supervisor -eq $PID)
        if($identityOk -and ($ExpectedPid -le 0 -or $listenerPid -eq $ExpectedPid)){
            return [PSCustomObject]@{State='owned';Owned=$true;Conflict=$false;Pid=$listenerPid;Detail='current runtime generation'}
        }
        $detail=('listener identity mismatch revision='+[string]$health.revision+' authority='+[string]$health.authority+' runtime='+[string]$health.runtime_id+' supervisor='+[string]$supervisor+' pid='+[string]$listenerPid)
        return [PSCustomObject]@{State='conflict';Owned=$false;Conflict=$true;Pid=$listenerPid;Detail=$detail}
    }catch{
        # With no expected supervisor child, an occupied TCP port that does not speak the
        # R61.94 health protocol is still a conflict. Never interpret it as permission to spawn.
        if($ExpectedPid -le 0 -and (Test-YomiLoopbackPortOccupied -Port $Port)){
            return [PSCustomObject]@{State='conflict';Owned=$false;Conflict=$true;Pid=0;Detail='TCP listener occupies the configured port but did not provide generation-verifiable YOMI health.'}
        }
        return [PSCustomObject]@{State='absent';Owned=$false;Conflict=$false;Pid=0;Detail=$_.Exception.Message}
    }
}

function Write-YomiObsRepairResult([string]$RequestId,[string]$Status,[string]$Detail,[int]$ServerPid=0){
    try{
        $result=[ordered]@{schema=1;revision='R61.94';runtime_id=$runtimeId;supervisor_pid=$PID;request_id=$RequestId;status=$Status;detail=$Detail;server_pid=$ServerPid;utc=[DateTime]::UtcNow.ToString('o')}
        Write-YomiUtf8NoBom -Path $obsRepairResultFile -Text ($result|ConvertTo-Json -Depth 4 -Compress)
    }catch{}
}
function Get-YomiProcessProvenance {
    param(
        [Parameter(Mandatory=$true)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory=$true)][string]$Role,
        [string]$IntendedPath=''
    )
    $path=''
    try{$path=[string]$Process.MainModule.FileName}catch{}
    if([string]::IsNullOrWhiteSpace($path) -and -not [string]::IsNullOrWhiteSpace($IntendedPath)){
        try{$path=(Resolve-Path $IntendedPath -ErrorAction Stop).Path}catch{$path=$IntendedPath}
    }
    $started=$null
    try{$started=$Process.StartTime.ToUniversalTime()}catch{}
    $hash=''
    if($path -and (Test-Path $path -PathType Leaf)){
        try{$hash=(Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant()}catch{}
    }
    [PSCustomObject][ordered]@{
        role=$Role
        pid=[int]$Process.Id
        executable_path=$path
        executable_sha256=$hash
        start_utc=$(if($started){$started.ToString('o')}else{''})
        start_ticks_utc=$(if($started){[int64]$started.Ticks}else{0})
    }
}

try{
    $runtimeContracts=$null
    try{$runtimeContracts=Get-YomiContractSnapshot}catch{}
    $selfProcess=Get-Process -Id $PID -ErrorAction Stop
    $runtimeRecord=[ordered]@{
        schema=4
        runtime_id=$runtimeId
        supervisor_pid=$PID
        started_utc=[DateTime]::UtcNow.ToString('o')
        state='starting'
        safe_mode=$safeMode
        rapid_failures=$rapidFailures
        contracts=$runtimeContracts
        processes=[ordered]@{
            supervisor=(Get-YomiProcessProvenance -Process $selfProcess -Role 'supervisor' -IntendedPath (Join-Path $PSHOME 'powershell.exe'))
        }
    }
    Write-YomiUtf8NoBom -Path $runtimeInstanceFile -Text ($runtimeRecord|ConvertTo-Json -Depth 8 -Compress)
}catch{}
if($safeMode){
    Set-StartStatus 'Safe Mode: audio-only, one worker, presentation modules disabled.'
    Write-SupervisorLog ('SAFE MODE active rapid_failures='+$rapidFailures)
}
$server=$null;$mpvProcess=$null
try{
 Write-SupervisorLog ('START mode='+[string]$config.app_mode)
 foreach($startupAdvisory in @($startupAdvisories)){Write-SupervisorLog ('STARTUP ADVISORY '+[string]$startupAdvisory)}
 $startupAdvisoryCount=@($startupAdvisories).Count
 if($startupAdvisoryCount -gt 0){Set-StartStatus ('Playback continuing with '+[string]$startupAdvisoryCount+' startup advisory item(s).')}
 $playlist=Join-Path $DataRoot 'playlist.txt';if(-not(Test-Path $playlist)){Set-StartStatus 'Reading and shuffling the playlist for the first time...';& (Join-Path $PSScriptRoot 'shuffle.ps1');if($LASTEXITCODE -ne 0 -or -not(Test-Path $playlist)){Add-Type -AssemblyName System.Windows.Forms;[System.Windows.Forms.MessageBox]::Show('The playlist is not ready. Open YOMI Settings and save a playlist first.','YOMI')|Out-Null;exit 1}}
  foreach($logName in @('mpv.log','server.log','server-error.log')){Rotate-YomiLog (Join-Path $DataRoot ('logs\'+$logName))}
 $preflightScript=Join-Path $PSScriptRoot 'preflight.ps1'
 function Invoke-YomiStartupPreflight([switch]$UserVisible){
  if(-not(Test-Path $preflightScript)){return}
  try{
   if($UserVisible){Set-StartStatus 'Running startup preflight...'}
   $preflightArgs=@('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',('"' + $preflightScript + '"'),'-Quiet')
   if($safeMode){$preflightArgs+='-SafeMode'}
   $preflightProc=Start-Process powershell.exe -ArgumentList $preflightArgs -Wait -PassThru -NoNewWindow
   if($preflightProc.ExitCode -ne 0){
    $preflightSummary='Startup preflight reported a problem; audio continues.'
    try{$pf=Get-Content (Join-Path $stateRoot 'preflight.json') -Raw|ConvertFrom-Json;$preflightSummary=('Startup preflight: '+[string]$pf.failures+' blocking check(s), '+[string]$pf.warnings+' warning(s). Audio continues.')}catch{}
    if($UserVisible){Set-StartStatus $preflightSummary}
    Write-SupervisorLog ('PREFLIGHT ADVISORY '+$preflightSummary)
   }else{Write-SupervisorLog 'PREFLIGHT PASS'}
  }catch{
   if($UserVisible){Set-StartStatus ('Startup preflight could not run: '+$_.Exception.Message)}
   Write-SupervisorLog ('PREFLIGHT ERROR ADVISORY '+$_.Exception.Message)
  }
 }
 if($env:YOMI_PREFLIGHT_ONLY -eq '1'){Invoke-YomiStartupPreflight -UserVisible;Set-StartStatus 'Preflight-only startup test complete.';Write-SupervisorLog 'PREFLIGHT ONLY EXIT';exit 0}
 $streamer=([string]$config.app_mode -eq 'Streamer / OBS') -and -not $safeMode
 Set-StartStatus 'Starting player...';$mpv=Join-Path $InstallRoot 'runtime\mpv\mpv.exe';$lua=Join-Path $PSScriptRoot 'music.lua';$mpvLog=Join-Path $DataRoot 'logs\mpv.log';$yt=Join-Path $InstallRoot 'runtime\yt-dlp\yt-dlp.exe';$denoDir=Join-Path $InstallRoot 'runtime\deno';if(Test-Path (Join-Path $denoDir 'deno.exe')){$env:PATH=$denoDir+';'+$env:PATH}
 $args=@('--no-config','--idle=yes','--audio-display=no','--gapless-audio=yes','--replaygain=no','--volume=100','--volume-max=100','--input-ipc-server=\\.\pipe\yomi-v4',("--log-file=`""+$mpvLog+"`""),("--script=`""+$lua+"`""))
 $videoQuality=$(if($safeMode){'Off (audio only)'}else{[string]$config.player_video_quality})
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
 if($env:YOMI_ENGINE_PROBE_ONLY -eq '1'){
 $probeProcess=$null
 try{
  if(-not(Test-Path -LiteralPath $mpv)){throw 'mpv.exe is missing.'}
  $probeProcess=Start-Process -FilePath $mpv -ArgumentList '--version' -PassThru -WindowStyle Hidden
  if(-not $probeProcess.WaitForExit(10000)){try{$probeProcess.Kill()}catch{};throw 'mpv --version probe timed out.'}
  if([int]$probeProcess.ExitCode -ne 0){throw ('mpv --version probe exited '+$probeProcess.ExitCode+'.')}
  Set-StartStatus 'Engine probe complete.'
  Write-SupervisorLog 'ENGINE PROBE ONLY EXIT'
  exit 0
 }finally{if($probeProcess){$probeProcess.Dispose()}}
}
$env:YOMI_INSTALL_ROOT=$InstallRoot;if($ExplicitPlay){$env:YOMI_EXPLICIT_PLAY='1'}else{Remove-Item Env:YOMI_EXPLICIT_PLAY -ErrorAction SilentlyContinue};if($Prewarm){$env:YOMI_PREWARM='1'}else{Remove-Item Env:YOMI_PREWARM -ErrorAction SilentlyContinue};$mpvStarted=[DateTime]::UtcNow;$mpvProcess=Start-Process -FilePath $mpv -ArgumentList $args -PassThru;Set-Content $enginePidFile $mpvProcess.Id -Encoding ASCII
try{
    $runtimeRecord.engine_pid=$mpvProcess.Id
    $runtimeRecord.engine_started_utc=$mpvProcess.StartTime.ToUniversalTime().ToString('o')
    if($null -eq $runtimeRecord.processes){$runtimeRecord['processes']=[ordered]@{}}
    $runtimeRecord.processes['engine']=Get-YomiProcessProvenance -Process $mpvProcess -Role 'engine' -IntendedPath $mpv
    Write-YomiUtf8NoBom -Path $runtimeInstanceFile -Text ($runtimeRecord|ConvertTo-Json -Depth 8 -Compress)
}catch{}
try{$mpvProcess.PriorityClass=[System.Diagnostics.ProcessPriorityClass]::Normal}catch{};Set-StartStatus $(if($safeMode){'Safe Mode player started; preparing audio...'}else{'Player started; preparing the first playable track...'});Write-SupervisorLog ('MPV START PID '+$mpvProcess.Id); $watchdogStatusFile=Join-Path $stateRoot 'watchdog-status.json'
 # R61.95 AUDIO-FIRST STARTUP: mpv owns the critical path. OBS health and advisory
 # preflight deliberately trail engine launch so cached audio is never held behind them.
 $audioFirstDeadline=[DateTime]::UtcNow.AddMilliseconds(1600)
 while([DateTime]::UtcNow -lt $audioFirstDeadline){
  try{
   $engineStatusPath=Join-Path $stateRoot 'engine-status.json'
   if(Test-Path $engineStatusPath){
    $engineBoot=Get-Content $engineStatusPath -Raw -Encoding UTF8|ConvertFrom-Json
    $enginePhase=[string]$engineBoot.phase
    if($enginePhase -eq 'playing' -or $enginePhase -eq 'paused'){break}
   }
  }catch{}
  Start-Sleep -Milliseconds 50
 }
 if($streamer){try{
  Set-StartStatus 'Starting OBS overlay server...'
  $serverScript=Join-Path $PSScriptRoot 'server.ps1'
  $serverOut=Join-Path $DataRoot 'logs\server.log'
  $serverErr=Join-Path $DataRoot 'logs\server-error.log'
  $escaped=$serverScript.Replace("'","''")
  $healthUrl=('http://127.0.0.1:'+([int]$config.server_port)+'/v1/health')
  $serverReady=$false

  $preexisting=Get-YomiOverlayHealth -Port ([int]$config.server_port)
  if($preexisting.Conflict){
   $server=$null
   Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue
   Set-StartStatus 'OBS overlay port is owned by another runtime; audio startup continues.'
   Write-SupervisorLog ('SERVER OWNERSHIP CONFLICT before launch: '+$preexisting.Detail)
  }else{
   $server=Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-Command',("& '"+$escaped+"'")) -WindowStyle Hidden -RedirectStandardOutput $serverOut -RedirectStandardError $serverErr -PassThru
   Set-Content $serverPidFile $server.Id -Encoding ASCII
   try{$server.PriorityClass=[System.Diagnostics.ProcessPriorityClass]::Normal}catch{}
   Write-SupervisorLog ('SERVER START PID '+$server.Id+' runtime='+$runtimeId)
   $serverReady=$false
   $serverDeadline=[DateTime]::UtcNow.AddSeconds(5)
   $lastHealth=$null
   while([DateTime]::UtcNow -lt $serverDeadline -and -not $serverReady){
    if($server.HasExited){break}
    $lastHealth=Get-YomiOverlayHealth -Port ([int]$config.server_port) -ExpectedPid $server.Id
    if($lastHealth.Owned){$serverReady=$true;break}
    if($lastHealth.Conflict){break}
    Start-Sleep -Milliseconds 120
   }
   if(-not $serverReady){
    if($lastHealth -and $lastHealth.Conflict){
     Set-StartStatus 'OBS overlay ownership conflict; audio startup continues.'
     Write-SupervisorLog ('SERVER OWNERSHIP CONFLICT initial readiness: '+$lastHealth.Detail)
     if($server -and -not $server.HasExited){Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue}
     $server=$null
     Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue
    }else{
     Set-StartStatus 'OBS overlay is still warming; starting audio now.'
     Write-SupervisorLog ('SERVER DEGRADED initial readiness on port '+([int]$config.server_port)+'; audio startup continues.')
     if(-not $server -or $server.HasExited){Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue}
    }
   }
   if($serverReady){Write-SupervisorLog ('SERVER HEALTH READY PID '+$server.Id+' generation verified')}
  }
  try{
    $runtimeRecord.state='running'
    $runtimeRecord.server_pid=$(if($serverReady -and $server){$server.Id}else{0})
    $runtimeRecord.server_authority='supervisor'
    $runtimeRecord.server_ready_utc=$(if($serverReady){[DateTime]::UtcNow.ToString('o')}else{''})
    if($serverReady -and $server){
     if($null -eq $runtimeRecord.processes){$runtimeRecord['processes']=[ordered]@{}}
     $runtimeRecord.processes['server']=Get-YomiProcessProvenance -Process $server -Role 'server' -IntendedPath (Join-Path $PSHOME 'powershell.exe')
    }
    Write-YomiUtf8NoBom -Path $runtimeInstanceFile -Text ($runtimeRecord|ConvertTo-Json -Depth 8 -Compress)
  }catch{}
 }catch{
  $server=$null
  Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue
  Set-StartStatus ('OBS overlay unavailable; audio startup continues: '+$_.Exception.Message)
  Write-SupervisorLog ('SERVER DEGRADED '+$_.Exception.Message)
 }}else{
  Set-StartStatus 'Player mode - OBS server disabled for lower overhead.'
  Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue
 }

 Invoke-YomiStartupPreflight

 $serverRestartCount=0
 $serverRestartWindow=[DateTime]::UtcNow
 $nextServerRestartAttempt=[DateTime]::MinValue
 $serverHealthFailures=0
 $leaseWarnings=0
 $lastWatchdogState=''
 $lastWatchdogMessage=''
 $lastWatchdogWrite=[DateTime]::MinValue

 function Write-WatchdogStatus([string]$State,[string]$Message=''){
  try{
   $now=[DateTime]::UtcNow
   if($State -eq $script:lastWatchdogState -and $Message -eq $script:lastWatchdogMessage -and ($now-$script:lastWatchdogWrite).TotalSeconds -lt 10){return}
   $leaseAge=-1
   $leaseFile=Join-Path $stateRoot 'runtime-lease.json'
   if(Test-Path $leaseFile){
    $leaseInfo=Get-Item $leaseFile -ErrorAction SilentlyContinue
    if($leaseInfo){$leaseAge=[Math]::Max(0,([DateTime]::UtcNow-$leaseInfo.LastWriteTimeUtc).TotalSeconds)}
   }
   $obj=[ordered]@{
    schema=1
    runtime_id=$runtimeId
    state=$State
    message=$Message
    updated_utc=[DateTime]::UtcNow.ToString('o')
    engine_pid=$(if($mpvProcess){$mpvProcess.Id}else{0})
    server_pid=$(if($server -and -not $server.HasExited){$server.Id}else{0})
    server_authority='supervisor'
    server_restarts=$serverRestartCount
    server_health_failures=$serverHealthFailures
    runtime_lease_age_seconds=$leaseAge
    safe_mode=$safeMode
   }
   Write-YomiUtf8NoBom -Path $watchdogStatusFile -Text ($obj|ConvertTo-Json -Depth 5 -Compress)
   $script:lastWatchdogState=$State
   $script:lastWatchdogMessage=$Message
   $script:lastWatchdogWrite=$now
  }catch{}
 }

 function Restart-YomiOverlayServerWatchdog([bool]$BypassCircuit=$false,[string]$Reason='watchdog') {
  if(-not $streamer){return $false}
  $now=[DateTime]::UtcNow
  $expectedPid=$(if($script:server -and -not $script:server.HasExited){$script:server.Id}else{0})
  $observed=Get-YomiOverlayHealth -Port ([int]$config.server_port) -ExpectedPid $expectedPid
  if($observed.Owned){
   $script:serverHealthFailures=0
   return $true
  }
  if($observed.Conflict){
   $script:nextServerRestartAttempt=$now.AddSeconds(30)
   Write-SupervisorLog ('SERVER OWNERSHIP CONFLICT '+$observed.Detail+' reason='+$Reason)
   Write-WatchdogStatus 'overlay-conflict' ('Port '+([int]$config.server_port)+' is occupied by a listener outside this runtime generation; YOMI will not terminate it.')
   return $false
  }
  if(($now-$serverRestartWindow).TotalSeconds -gt 120){
   $script:serverRestartWindow=$now
   $script:serverRestartCount=0
  }
  if(-not $BypassCircuit -and $serverRestartCount -ge 3){
   $script:nextServerRestartAttempt=$now.AddMinutes(2)
   Write-SupervisorLog 'SERVER WATCHDOG circuit open after 3 restart attempts'
   Write-WatchdogStatus 'overlay-degraded' 'Overlay server restart circuit open; audio engine continues.'
   return $false
  }

  $script:serverRestartCount++
  Write-SupervisorLog ('SERVER WATCHDOG restart attempt '+$serverRestartCount+' reason='+$Reason)
  try{
   # Only terminate the process object this supervisor itself started. Never kill a PID merely
   # because it owns the configured port or appears in a stale server.pid file.
   if($script:server -and -not $script:server.HasExited){Stop-Process -Id $script:server.Id -Force -ErrorAction SilentlyContinue}
   Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue
   $prelaunch=Get-YomiOverlayHealth -Port ([int]$config.server_port)
   if($prelaunch.Conflict){
    Write-SupervisorLog ('SERVER OWNERSHIP CONFLICT before restart: '+$prelaunch.Detail)
    Write-WatchdogStatus 'overlay-conflict' 'Configured OBS port is occupied by another listener; no process was killed.'
    $script:nextServerRestartAttempt=[DateTime]::UtcNow.AddSeconds(30)
    return $false
   }
   $script:server=Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-Command',("& '"+$escaped+"'")) -WindowStyle Hidden -RedirectStandardOutput $serverOut -RedirectStandardError $serverErr -PassThru
   Set-Content $serverPidFile $script:server.Id -Encoding ASCII
   try{$server.PriorityClass=[System.Diagnostics.ProcessPriorityClass]::Normal}catch{}

   $deadline=[DateTime]::UtcNow.AddSeconds(5)
   $ready=$false
   $lastObserved=$null
   while([DateTime]::UtcNow -lt $deadline -and -not $ready){
    if($script:server.HasExited){break}
    $lastObserved=Get-YomiOverlayHealth -Port ([int]$config.server_port) -ExpectedPid $script:server.Id
    if($lastObserved.Owned){$ready=$true;break}
    if($lastObserved.Conflict){break}
    Start-Sleep -Milliseconds 150
   }
   if(-not $ready){
    if($lastObserved -and $lastObserved.Conflict){
     if($script:server -and -not $script:server.HasExited){Stop-Process -Id $script:server.Id -Force -ErrorAction SilentlyContinue}
     $script:server=$null
     Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue
     Write-SupervisorLog ('SERVER OWNERSHIP CONFLICT during restart: '+$lastObserved.Detail)
     Write-WatchdogStatus 'overlay-conflict' 'Another listener won the port; the supervisor-owned child was stopped and no foreign process was touched.'
     $script:nextServerRestartAttempt=[DateTime]::UtcNow.AddSeconds(30)
     return $false
    }
    throw 'Overlay server restart did not reach generation-verified /v1/health.'
   }

   $script:serverHealthFailures=0
   Write-SupervisorLog ('SERVER WATCHDOG recovered PID '+$script:server.Id+' generation verified')
   Write-WatchdogStatus 'healthy' 'Overlay server recovered automatically.'
   try{
    $runtimeRecord.server_pid=$script:server.Id
    $runtimeRecord.server_authority='supervisor'
    $runtimeRecord.server_restart_count=$serverRestartCount
    $runtimeRecord.server_recovered_utc=[DateTime]::UtcNow.ToString('o')
    if($null -eq $runtimeRecord.processes){$runtimeRecord['processes']=[ordered]@{}}
    $runtimeRecord.processes['server']=Get-YomiProcessProvenance -Process $script:server -Role 'server' -IntendedPath (Join-Path $PSHOME 'powershell.exe')
    Write-YomiUtf8NoBom -Path $runtimeInstanceFile -Text ($runtimeRecord|ConvertTo-Json -Depth 8 -Compress)
   }catch{}
   return $true
  }catch{
   Write-SupervisorLog ('SERVER WATCHDOG restart failed: '+$_.Exception.Message)
   Write-WatchdogStatus 'overlay-degraded' $_.Exception.Message
   return $false
  }
 }

 function Consume-YomiObsRepairRequest {
  if(-not(Test-Path $obsRepairRequestFile)){return}
  $request=$null
  try{$request=Get-Content $obsRepairRequestFile -Raw -Encoding UTF8|ConvertFrom-Json}catch{}
  Remove-Item $obsRepairRequestFile -Force -ErrorAction SilentlyContinue
  if($null -eq $request){return}
  $requestId=[string]$request.request_id
  $requestPort=0;[void][int]::TryParse([string]$request.port,[ref]$requestPort)
  $requestSupervisor=0;[void][int]::TryParse([string]$request.supervisor_pid,[ref]$requestSupervisor)
  if([string]$request.runtime_id -ne $runtimeId -or $requestSupervisor -ne $PID -or $requestPort -ne [int]$config.server_port){
   Write-SupervisorLog ('OBS REPAIR REQUEST rejected id='+$requestId+' runtime/port/supervisor mismatch')
   Write-YomiObsRepairResult $requestId 'rejected' 'Request does not belong to this runtime generation.' 0
   return
  }
  $expectedPid=$(if($script:server -and -not $script:server.HasExited){$script:server.Id}else{0})
  $health=Get-YomiOverlayHealth -Port $requestPort -ExpectedPid $expectedPid
  if($health.Owned){
   Write-SupervisorLog ('OBS REPAIR REQUEST id='+$requestId+' already healthy')
   Write-YomiObsRepairResult $requestId 'already-healthy' 'Generation-verified server is already healthy.' $health.Pid
   return
  }
  if($health.Conflict){
   Write-SupervisorLog ('OBS REPAIR REQUEST id='+$requestId+' conflict '+$health.Detail)
   Write-YomiObsRepairResult $requestId 'conflict' $health.Detail $health.Pid
   Write-WatchdogStatus 'overlay-conflict' 'Repair request observed a foreign/stale listener; no process was terminated.'
   return
  }
  $ok=Restart-YomiOverlayServerWatchdog -BypassCircuit $true -Reason ('controller-request '+$requestId)
  $post=Get-YomiOverlayHealth -Port $requestPort -ExpectedPid $(if($script:server -and -not $script:server.HasExited){$script:server.Id}else{0})
  if($ok -and $post.Owned){Write-YomiObsRepairResult $requestId 'recovered' 'Supervisor restarted the generation-owned OBS server.' $post.Pid}
  else{Write-YomiObsRepairResult $requestId 'degraded' $post.Detail $post.Pid}
 }

 Write-WatchdogStatus 'healthy' 'Supervisor watchdog armed.'
 try{
  $runtimeRecord.state='running'
  $runtimeRecord.engine_pid=$mpvProcess.Id
  $runtimeRecord.watchdog='armed'
  if($streamer -and $server -and -not $server.HasExited){
   $owned=Get-YomiOverlayHealth -Port ([int]$config.server_port) -ExpectedPid $server.Id
   $runtimeRecord.server_pid=$(if($owned.Owned){$server.Id}else{0})
  }
  $runtimeRecord.server_authority='supervisor'
  Write-YomiUtf8NoBom -Path $runtimeInstanceFile -Text ($runtimeRecord|ConvertTo-Json -Depth 6 -Compress)
 }catch{}

 while(-not $mpvProcess.HasExited){
  Start-Sleep -Seconds 2

  if($streamer){
   Consume-YomiObsRepairRequest
   $expectedPid=$(if($server -and -not $server.HasExited){$server.Id}else{0})
   $overlayHealth=Get-YomiOverlayHealth -Port ([int]$config.server_port) -ExpectedPid $expectedPid
   $serverDead=($null -eq $server) -or $server.HasExited
   if($overlayHealth.Owned){
    $serverHealthFailures=0
    $serverDead=$false
   }elseif($overlayHealth.Conflict){
    $serverHealthFailures=0
    $serverDead=$false
    $nextServerRestartAttempt=[DateTime]::UtcNow.AddSeconds(30)
    Write-WatchdogStatus 'overlay-conflict' 'Configured OBS port is occupied outside this runtime generation; no foreign process will be killed.'
   }else{
    $serverHealthFailures++
    if($serverHealthFailures -ge 3){
     Write-SupervisorLog ('SERVER WATCHDOG unhealthy probes='+$serverHealthFailures)
     $serverDead=$true
    }
   }

   if($serverDead -and [DateTime]::UtcNow -ge $nextServerRestartAttempt){
    [void](Restart-YomiOverlayServerWatchdog -BypassCircuit $false -Reason 'periodic watchdog')
   }
  }

  $leaseFile=Join-Path $stateRoot 'runtime-lease.json'
  if(Test-Path $leaseFile){
   try{
    $leaseAge=([DateTime]::UtcNow-(Get-Item $leaseFile).LastWriteTimeUtc).TotalSeconds
    if($leaseAge -gt 20){
     $leaseWarnings++
     if($leaseWarnings -eq 1 -or ($leaseWarnings % 15) -eq 0){
      Write-SupervisorLog ('ENGINE LEASE STALE '+[Math]::Round($leaseAge,1)+' sec; process remains alive')
     }
     Write-WatchdogStatus 'engine-stalled' ('Runtime lease stale '+[Math]::Round($leaseAge,1)+' sec; watchdog is observing, not force-killing playback.')
    }else{
     $leaseWarnings=0
     if(-not $streamer){Write-WatchdogStatus 'healthy' ''}
     elseif($server -and -not $server.HasExited){
      $leaseOverlay=Get-YomiOverlayHealth -Port ([int]$config.server_port) -ExpectedPid $server.Id
      if($leaseOverlay.Owned){Write-WatchdogStatus 'healthy' ''}
     }
    }
   }catch{}
  }
 }
 Write-SupervisorLog ('MPV EXIT '+$mpvProcess.ExitCode)
 $runSeconds=([DateTime]::UtcNow-$mpvStarted).TotalSeconds
 $planned=Test-Path $plannedStopFile
 Remove-Item $plannedStopFile -Force -ErrorAction SilentlyContinue
 if($planned -or $runSeconds -ge 20){
  Set-Content $crashCountFile '0' -Encoding ASCII
 }else{
  $rapidFailures++
  Set-Content $crashCountFile ([string]$rapidFailures) -Encoding ASCII
  Write-SupervisorLog ('RAPID EXIT count='+$rapidFailures+' seconds='+[Math]::Round($runSeconds,1))
 }
}catch{
 if(-not $mpvProcess){
  $rapidFailures++
  Set-Content $crashCountFile ([string]$rapidFailures) -Encoding ASCII
  Write-SupervisorLog ('STARTUP FAILURE count='+$rapidFailures)
 }
 Set-StartStatus ('Startup error: '+$_.Exception.Message)
 Write-SupervisorLog ('ERROR '+$_.Exception.Message)
 try{Add-Type -AssemblyName System.Windows.Forms;[System.Windows.Forms.MessageBox]::Show($_.Exception.Message,'YOMI')|Out-Null}catch{}
}
finally{if($mpvProcess -and -not $mpvProcess.HasExited){Stop-Process -Id $mpvProcess.Id -Force -ErrorAction SilentlyContinue};if($server -and -not $server.HasExited){Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue};Remove-Item $enginePidFile,$serverPidFile,$supervisorPidFile,(Join-Path $stateRoot 'runtime-lease.json'),(Join-Path $stateRoot 'watchdog-status.json'),$obsRepairRequestFile -Force -ErrorAction SilentlyContinue;Set-StartStatus 'Stopped';try{$mutex.ReleaseMutex()}catch{};$mutex.Dispose();try{$runtimeRecord.state='stopped';$runtimeRecord.stopped_utc=[DateTime]::UtcNow.ToString('o');Write-YomiUtf8NoBom -Path $runtimeInstanceFile -Text ($runtimeRecord|ConvertTo-Json -Compress)}catch{};Write-SupervisorLog 'STOP'}
