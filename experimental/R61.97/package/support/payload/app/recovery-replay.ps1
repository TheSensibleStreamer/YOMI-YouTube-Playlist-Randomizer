param(
    [switch]$Analyze,
    [switch]$PrepareStartup,
    [string]$Policy = '',
    [switch]$CopyReport,
    [switch]$OpenReport
)

$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$stateRoot=Join-Path $DataRoot 'state'
$checkpointPath=Join-Path $stateRoot 'recovery-checkpoint.json'
$cleanPath=Join-Path $stateRoot 'recovery-clean-shutdown.json'
$planPath=Join-Path $stateRoot 'recovery-plan.json'
$historyRoot=Join-Path $stateRoot 'recovery-history'
$sessionPath=Join-Path $stateRoot 'session.json'
$orderPath=Join-Path $stateRoot 'session-order.json'
$journalHeadPath=Join-Path $stateRoot 'session-journal-head.json'
$runtimePath=Join-Path $stateRoot 'runtime-instance.json'
$resumePath=Join-Path $stateRoot 'resume-track.txt'
$configConflictPath=Join-Path $stateRoot 'config-pending-conflict.json'

function Read-JsonSafe([string]$Path){if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){return $null};try{return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8|ConvertFrom-Json)}catch{return $null}}
function Write-AtomicJson([string]$Path,$Object){if([string]::IsNullOrWhiteSpace($Path)){throw 'Atomic JSON target path is empty.'};$Path=[IO.Path]::GetFullPath($Path);$dir=[IO.Path]::GetDirectoryName($Path);New-Item -ItemType Directory -Path $dir -Force|Out-Null;$tmp=$Path+'.tmp.'+[Guid]::NewGuid().ToString('N');$backup=$Path+'.replace-backup';$text=$Object|ConvertTo-Json -Depth 14;Write-YomiUtf8NoBom -Path $tmp -Text $text;try{if(Test-Path -LiteralPath $Path){Remove-Item -LiteralPath $backup -Force -ErrorAction SilentlyContinue;[IO.File]::Replace($tmp,$Path,$backup,$true);Remove-Item -LiteralPath $backup -Force -ErrorAction SilentlyContinue}else{[IO.File]::Move($tmp,$Path)}}finally{Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue}}
function Live-PidFile([string]$Name){$p=Join-Path $stateRoot $Name;if(-not(Test-Path $p)){return 0};$n=0;try{[void][int]::TryParse((Get-Content $p -Raw).Trim(),[ref]$n)}catch{};if($n -gt 0 -and (Get-Process -Id $n -ErrorAction SilentlyContinue)){return $n};return 0}
function Add-Finding([Collections.Generic.List[object]]$List,[string]$Level,[string]$Code,[string]$Message){$List.Add([pscustomobject]@{level=$Level;code=$Code;message=$Message})}
function Read-Resume(){if(-not(Test-Path $resumePath)){return 0};$n=0;try{[void][int]::TryParse((Get-Content $resumePath -Raw).Trim(),[ref]$n)}catch{};return $n}
function Get-Order($obj){$vals=New-Object Collections.Generic.List[int];if($obj -and $obj.order){foreach($v in @($obj.order)){$n=0;if([int]::TryParse([string]$v,[ref]$n) -and $n -gt 0){$vals.Add($n)}}};return $vals.ToArray()}
function Test-UniquePositive([int[]]$Values){if($Values.Count -lt 1){return $false};return (@($Values|Select-Object -Unique).Count -eq $Values.Count -and @($Values|Where-Object{$_ -le 0}).Count -eq 0)}
# Recovery episode retention is bounded: maximum 8 tiny state capsules.
function Preserve-RecoveryEpisode([string]$Id){New-Item -ItemType Directory -Path $historyRoot -Force|Out-Null;$dir=Join-Path $historyRoot $Id;New-Item -ItemType Directory -Path $dir -Force|Out-Null;foreach($name in @('current.json','queue-runtime.json','state-coherence.json','runtime-instance.json','engine-status.json','recovery-checkpoint.json','session-journal-head.json','session-order.json')){$src=Join-Path $stateRoot $name;if(Test-Path -LiteralPath $src -PathType Leaf){Copy-Item -LiteralPath $src -Destination (Join-Path $dir $name) -Force -ErrorAction SilentlyContinue}};$dirs=@(Get-ChildItem -LiteralPath $historyRoot -Directory -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending);for($i=8;$i -lt $dirs.Count;$i++){Remove-Item -LiteralPath $dirs[$i].FullName -Recurse -Force -ErrorAction SilentlyContinue};return $dir}

function Invoke-YomiRecoveryReplay {
    param([switch]$PrepareStartup,[string]$Policy='')
    if([string]::IsNullOrWhiteSpace($Policy)){try{$Policy=[string](Get-YomiConfig).recovery_policy}catch{$Policy='Validated resume'}}
    if([string]::IsNullOrWhiteSpace($Policy)){$Policy='Validated resume'}
    $findings=New-Object 'Collections.Generic.List[object]'
    $id=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
    $session=Read-JsonSafe $sessionPath;$orderState=Read-JsonSafe $orderPath;$checkpoint=Read-JsonSafe $checkpointPath;$clean=Read-JsonSafe $cleanPath;$head=Read-JsonSafe $journalHeadPath;$priorRuntime=Read-JsonSafe $runtimePath;$configConflict=Read-JsonSafe $configConflictPath
    $order=@(Get-Order $orderState);$rawOrderCount=if($orderState -and $orderState.order){@($orderState.order).Count}else{0};$orderValid=(Test-UniquePositive $order) -and $order.Count -eq $rawOrderCount
    $sessionId=if($session){[string]$session.session_id}else{''};$orderSession=if($orderState){[string]$orderState.session_id}else{''};$checkpointSession=if($checkpoint){[string]$checkpoint.session_id}else{''};$priorRuntimeId=if($priorRuntime){[string]$priorRuntime.runtime_id}else{''};$checkpointRuntime=if($checkpoint){[string]$checkpoint.runtime_id}else{''}
    $orderRevision=if($orderState){[int]$orderState.revision}else{-1};$headRevision=if($head){[int]$head.revision}else{-1}
    $checkpointOccurrence=if($checkpoint){[int]$checkpoint.occurrence}else{0};$resume=Read-Resume
    $checkpointValid=$checkpoint -and $orderValid -and $checkpointOccurrence -gt 0 -and ($order -contains $checkpointOccurrence) -and -not [string]::IsNullOrWhiteSpace($sessionId) -and $checkpointSession -eq $sessionId -and ($orderSession -eq '' -or $orderSession -eq $sessionId) -and ([string]::IsNullOrWhiteSpace($priorRuntimeId) -or [string]::IsNullOrWhiteSpace($checkpointRuntime) -or $checkpointRuntime -eq $priorRuntimeId)
    $cleanValid=$checkpointValid -and $clean -and $checkpoint -and [string]$clean.runtime_id -eq [string]$checkpoint.runtime_id -and [string]$clean.session_id -eq [string]$checkpoint.session_id -and [int64]$clean.checkpoint_sequence -eq [int64]$checkpoint.checkpoint_sequence
    $resumeValid=$orderValid -and $resume -gt 0 -and ($order -contains $resume)
    $durableConflict=$false

    if(Live-PidFile 'engine.pid'){Add-Finding $findings 'ERROR' 'ENGINE_ALREADY_LIVE' 'Recovery replay refuses to mutate startup state while an engine process is already live.';$durableConflict=$true}
    if($configConflict){Add-Finding $findings 'ERROR' 'CONFIG_TRANSACTION_CONFLICT' 'A staged configuration transaction has an unresolved compare-and-swap conflict.';$durableConflict=$true}
    if(-not $session){Add-Finding $findings 'ERROR' 'SESSION_MISSING' 'session.json is missing or unreadable.';$durableConflict=$true}
    if(-not $orderValid){Add-Finding $findings 'ERROR' 'ORDER_INVALID' 'session-order.json does not contain a unique positive occurrence order.';$durableConflict=$true}
    if($sessionId -and $orderSession -and $sessionId -ne $orderSession){Add-Finding $findings 'ERROR' 'SESSION_ORDER_IDENTITY_CONFLICT' 'session.json and session-order.json identify different sessions.';$durableConflict=$true}
    if($head -and $head.session_id -and $sessionId -and [string]$head.session_id -ne $sessionId){Add-Finding $findings 'ERROR' 'LEDGER_SESSION_CONFLICT' 'Mutation-ledger head belongs to a different session.';$durableConflict=$true}
    if($headRevision -gt $orderRevision -and $orderRevision -ge 0){Add-Finding $findings 'ERROR' 'LEDGER_AHEAD_OF_ORDER' "Mutation-ledger head revision $headRevision is ahead of durable order revision $orderRevision.";$durableConflict=$true}
    elseif($headRevision -ge 0 -and $orderRevision -ge 0 -and $headRevision -lt $orderRevision){Add-Finding $findings 'WARN' 'LEDGER_HEAD_LAG' "Mutation-ledger head revision $headRevision lags durable order revision $orderRevision; existing Aegis repair may reconcile a crash-lagging head."}
    if($checkpoint -and -not $checkpointValid){Add-Finding $findings 'WARN' 'CHECKPOINT_REJECTED' 'Last playback checkpoint does not match the durable session/order/runtime evidence.'}
    if($clean -and -not $cleanValid){Add-Finding $findings 'WARN' 'CLEAN_MARKER_REJECTED' 'Clean-shutdown marker does not match the last committed recovery checkpoint.'}

    # Let the pre-existing Aegis engine reconcile only the one class it already
    # proves safe: a crash-lagging mutation-ledger head. Then re-read evidence.
    if($PrepareStartup -and -not $durableConflict -and -not $cleanValid -and $Policy -notin @('Off','Observe only') -and $headRevision -ge 0 -and $orderRevision -ge 0 -and $headRevision -lt $orderRevision){
        $aegis=Join-Path $PSScriptRoot 'aegis-repair.ps1'
        if(Test-Path -LiteralPath $aegis){try{& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $aegis -Mode RepairSafe|Out-Null;$head=Read-JsonSafe $journalHeadPath;$headRevision=if($head){[int]$head.revision}else{-1};if($headRevision -eq $orderRevision){Add-Finding $findings 'INFO' 'LEDGER_HEAD_RECONCILED' 'Existing Aegis safe repair reconciled the crash-lagging mutation-ledger head.'}}catch{Add-Finding $findings 'WARN' 'AEGIS_REPAIR_UNAVAILABLE' $_.Exception.Message}}
    }

    $state='';$action='NONE';$reason='';$selected=0
    if($Policy -eq 'Off'){$state='OBSERVE';$reason='recovery-policy-off'}
    elseif($durableConflict){$state='BLOCKED';$action='REFUSE_START';$reason='durable-authority-conflict'}
    elseif($cleanValid){$state='CLEAN';$selected=if($resumeValid){$resume}elseif($checkpointValid){$checkpointOccurrence}else{$order[0]};$reason='matching-clean-shutdown-marker'}
    elseif($Policy -eq 'Observe only'){$state='OBSERVE';$selected=if($checkpointValid){$checkpointOccurrence}elseif($resumeValid){$resume}else{0};$reason='unclean-exit-observed-no-mutation'}
    elseif($Policy -eq 'Strict replay'){
        if(-not $checkpointValid){$state='BLOCKED';$action='REFUSE_START';$reason='strict-replay-requires-valid-checkpoint'}
        elseif($headRevision -ge 0 -and $orderRevision -ge 0 -and $headRevision -ne $orderRevision){$state='BLOCKED';$action='REFUSE_START';$reason='strict-replay-requires-ledger-order-agreement'}
        else{$state='RESUMED';$action='RESTORE_COMMITTED_OCCURRENCE';$selected=$checkpointOccurrence;$reason='validated-unclean-checkpoint'}
    }else{
        if($checkpointValid){$state='RESUMED';$action='RESTORE_COMMITTED_OCCURRENCE';$selected=$checkpointOccurrence;$reason='validated-unclean-checkpoint'}
        elseif($resumeValid){$state='FALLBACK';$action='USE_DURABLE_RESUME';$selected=$resume;$reason='checkpoint-unavailable-durable-resume-valid'}
        elseif($orderValid){$state='FALLBACK';$action='USE_FIRST_DURABLE_ORDER_OCCURRENCE';$selected=$order[0];$reason='checkpoint-and-resume-unavailable'}
        else{$state='BLOCKED';$action='REFUSE_START';$reason='no-provable-resume-authority'}
    }

    $episode=$null
    if($PrepareStartup -and -not $cleanValid){$episode=Preserve-RecoveryEpisode $id}
    if($PrepareStartup -and $state -notin @('BLOCKED','CONFLICT') -and $Policy -notin @('Off','Observe only') -and $selected -gt 0){Write-YomiUtf8NoBom -Path $resumePath -Text ([string]$selected)}
    if($PrepareStartup){Remove-Item -LiteralPath $cleanPath -Force -ErrorAction SilentlyContinue}

    $plan=[ordered]@{
        schema=1;id=$id;generated_utc=[DateTime]::UtcNow.ToString('o');policy=$Policy;prepare_startup=[bool]$PrepareStartup;
        state=$state;action=$action;reason=$reason;clean_shutdown=[bool]$cleanValid;unclean_shutdown=[bool](-not $cleanValid);
        session_id=$sessionId;prior_runtime_id=$priorRuntimeId;checkpoint_runtime_id=$checkpointRuntime;
        checkpoint_valid=[bool]$checkpointValid;checkpoint_sequence=if($checkpoint){[int64]$checkpoint.checkpoint_sequence}else{-1};checkpoint_occurrence=$checkpointOccurrence;
        resume_before=$resume;resume_occurrence=$selected;resume_valid=[bool]$resumeValid;
        order_revision=$orderRevision;ledger_head_revision=$headRevision;order_count=$order.Count;
        presentation_epoch=if($checkpoint){[int]$checkpoint.presentation_epoch}else{-1};state_sequence=if($checkpoint){[int64]$checkpoint.state_sequence}else{-1};
        recovery_episode=$episode;findings=$findings.ToArray()
    }
    Write-AtomicJson $planPath $plan
    if($PrepareStartup -and $state -eq 'BLOCKED'){return [pscustomobject]@{action='BLOCK';plan=$plan}}
    return [pscustomobject]@{action=$action;plan=$plan}
}

if($MyInvocation.InvocationName -ne '.') {
    $result=Invoke-YomiRecoveryReplay -PrepareStartup:$PrepareStartup -Policy $Policy
    $plan=$result.plan
    Write-Host '===== YOMI RECOVERY REPLAY ====='
    Write-Host ("State: {0}  Policy: {1}  Action: {2}" -f $plan.state,$plan.policy,$plan.action)
    Write-Host ("Reason: {0}" -f $plan.reason)
    Write-Host ("Session: {0}  Order R{1}  Resume: O{2}" -f $plan.session_id,$plan.order_revision,$plan.resume_occurrence)
    Write-Host ("Checkpoint: valid={0} seq={1} occurrence={2} epoch={3}" -f $plan.checkpoint_valid,$plan.checkpoint_sequence,$plan.checkpoint_occurrence,$plan.presentation_epoch)
    foreach($f in @($plan.findings)){Write-Host ("{0,-5} {1}: {2}" -f $f.level,$f.code,$f.message)}
    Write-Host "Plan: $planPath"
    if($CopyReport){try{Set-Clipboard $planPath}catch{}}
    if($OpenReport){try{Start-Process notepad.exe ('"'+$planPath+'"')}catch{}}
    if($PrepareStartup -and $plan.state -eq 'BLOCKED'){exit 3}
}
