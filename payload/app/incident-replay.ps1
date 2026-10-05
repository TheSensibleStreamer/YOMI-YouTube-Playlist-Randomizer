param(
    [string]$OutputPath='',
    [int]$MaxEvents=2000,
    [switch]$Copy
)

$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
$stateRoot=Join-Path $DataRoot 'state'
$eventsPath=Join-Path $stateRoot 'events.jsonl'
$journalPath=Join-Path $stateRoot 'session-journal.jsonl'

if([string]::IsNullOrWhiteSpace($OutputPath)){
    $incidentRoot=Join-Path $DataRoot 'incidents'
    New-Item -ItemType Directory -Path $incidentRoot -Force|Out-Null
    $OutputPath=Join-Path $incidentRoot ('YOMI-incident-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json')
}

function Parse-JsonLines([string]$Target,[int]$Limit){
    $result=New-Object System.Collections.Generic.List[System.Object]
    if(-not(Test-Path $Target)){return $result}
    foreach($line in @(Get-Content $Target -Tail $Limit -ErrorAction SilentlyContinue)){
        if([string]::IsNullOrWhiteSpace($line)){continue}
        try{$result.Add(($line|ConvertFrom-Json))}catch{}
    }
    return $result
}
function Aegis-Hash32([string]$Text,[uint64]$Seed){
    [uint64]$h=$Seed
    foreach($b in [System.Text.Encoding]::UTF8.GetBytes([string]$Text)){
        $h=(($h*65599)+[uint64]$b+17)%4294967296
    }
    return ('{0:x8}' -f [uint32]$h)
}
function Aegis-IdentityHash([string]$Text){
    return (Aegis-Hash32 $Text 2166136261)+(Aegis-Hash32 $Text 2246822519)
}
function Aegis-JournalPayload($Entry){
    return (@(
        [string]$Entry.schema,
        [string]$Entry.session_id,
        [string]$Entry.runtime_id,
        [string]$Entry.sequence,
        [string]$Entry.revision,
        [string]$Entry.work_generation,
        [string]$Entry.action,
        [string]$Entry.current_occurrence,
        [string]$Entry.active_count,
        [string]$Entry.registry_count,
        [string]$Entry.order_hash,
        [string]$Entry.previous_hash,
        [string]$Entry.unix
    ) -join [char]31)
}

$events=Parse-JsonLines $eventsPath ([Math]::Max(100,$MaxEvents))
$journal=Parse-JsonLines $journalPath 500

$findings=New-Object System.Collections.Generic.List[System.Object]
function Finding([string]$Severity,[string]$Code,[string]$Message,$Evidence=$null){
    $findings.Add([PSCustomObject][ordered]@{severity=$Severity;code=$Code;message=$Message;evidence=$Evidence})
}

$runtimeGroups=@($events|Where-Object{$_.runtime_id}|Group-Object runtime_id)
foreach($group in $runtimeGroups){
    $lastSeq=-1
    foreach($e in @($group.Group|Sort-Object {[int64]$_.seq})){
        $seq=[int64]$e.seq
        if($lastSeq -ge 0 -and $seq -le $lastSeq){Finding 'WARN' 'EVENT_SEQ_NON_MONOTONIC' "Runtime $($group.Name) event sequence did not increase." @{previous=$lastSeq;current=$seq}}
        $lastSeq=$seq
    }
}

$starts=@($events|Where-Object{$_.type -eq 'RUNTIME_START'}).Count
$stops=@($events|Where-Object{$_.type -eq 'RUNTIME_STOP'}).Count
if($starts -gt $stops+1){Finding 'WARN' 'UNCLEAN_RUNTIME_EXIT' "$starts runtime starts but only $stops clean stops appear in the retained flight recorder."}

$playStart=@($events|Where-Object{$_.type -eq 'PLAYBACK_START'}).Count
$playEnd=@($events|Where-Object{$_.type -eq 'PLAYBACK_END'}).Count
if($playStart -gt $playEnd+1){Finding 'INFO' 'PLAYBACK_OPEN' 'The retained recorder ends with playback that may still be active.' @{starts=$playStart;ends=$playEnd}}

$domains=@($events|Where-Object{$_.type -match 'CIRCUIT|DISK|DEGRADED|FAIL'}|Select-Object -Last 100)
if($domains.Count -gt 0){Finding 'INFO' 'FAILURE_SIGNAL_HISTORY' "$($domains.Count) recent failure/degradation signals retained."}

$lastRevision=-1
foreach($j in @($journal)){
    $rev=0
    if($null -ne $j.PSObject.Properties['revision']){$rev=[int64]$j.revision}
    if($rev -lt $lastRevision){Finding 'WARN' 'ORDER_REVISION_REGRESSION' 'Session mutation journal revision moved backwards.' @{previous=$lastRevision;current=$rev}}
    if($rev -gt $lastRevision){$lastRevision=$rev}
}

$ledgerRows=@($journal|Where-Object{[int]$_.schema -eq 2})
$legacyLedgerRows=@($journal|Where-Object{[int]$_.schema -lt 2})
if($legacyLedgerRows.Count -gt 0){Finding 'INFO' 'LEGACY_MUTATION_JOURNAL' "$($legacyLedgerRows.Count) retained pre-Aegis mutation record(s) are not hash chained."}

$previousLedger=$null
foreach($entry in $ledgerRows){
    $computed=Aegis-IdentityHash (Aegis-JournalPayload $entry)
    if($computed -ne [string]$entry.entry_hash){
        Finding 'ERROR' 'MUTATION_LEDGER_HASH_MISMATCH' "Mutation ledger record $($entry.sequence) failed its consistency hash." @{expected=$computed;recorded=$entry.entry_hash}
    }
    if($previousLedger){
        if([int64]$entry.sequence -ne ([int64]$previousLedger.sequence+1)){
            Finding 'ERROR' 'MUTATION_LEDGER_SEQUENCE_GAP' "Mutation ledger sequence jumped from $($previousLedger.sequence) to $($entry.sequence)."
        }
        if([string]$entry.previous_hash -ne [string]$previousLedger.entry_hash){
            Finding 'ERROR' 'MUTATION_LEDGER_CHAIN_BREAK' "Mutation ledger record $($entry.sequence) does not reference the preceding retained record."
        }
    }
    $previousLedger=$entry
}

$ledgerHead=$null
$ledgerHeadPath=Join-Path $stateRoot 'session-journal-head.json'
if(Test-Path $ledgerHeadPath){try{$ledgerHead=Get-Content $ledgerHeadPath -Raw|ConvertFrom-Json}catch{Finding 'WARN' 'MUTATION_LEDGER_HEAD_PARSE' 'Mutation ledger head is unreadable.'}}
if($ledgerHead -and $ledgerRows.Count -gt 0){
    $tail=$ledgerRows[-1]
    if([int64]$ledgerHead.sequence -gt [int64]$tail.sequence){
        Finding 'ERROR' 'MUTATION_LEDGER_HEAD_AHEAD' 'Ledger head claims a sequence newer than the durable journal tail.' @{head=$ledgerHead.sequence;tail=$tail.sequence}
    }elseif([int64]$ledgerHead.sequence -lt [int64]$tail.sequence){
        Finding 'WARN' 'MUTATION_LEDGER_HEAD_LAG' 'Journal tail is newer than the persisted head; this is compatible with a crash between append and head commit.' @{head=$ledgerHead.sequence;tail=$tail.sequence}
    }elseif([string]$ledgerHead.entry_hash -ne [string]$tail.entry_hash){
        Finding 'ERROR' 'MUTATION_LEDGER_HEAD_HASH_MISMATCH' 'Ledger head and journal tail disagree at the same sequence.'
    }
}

$orderState=$null
$orderStatePath=Join-Path $stateRoot 'session-order.json'
if(Test-Path $orderStatePath){try{$orderState=Get-Content $orderStatePath -Raw|ConvertFrom-Json}catch{Finding 'WARN' 'ORDER_STATE_PARSE' 'session-order.json is unreadable.'}}
if($orderState -and $ledgerRows.Count -gt 0){
    $tail=$ledgerRows[-1]
    if([string]$orderState.session_id -eq [string]$tail.session_id){
        $currentOrderHash=Aegis-IdentityHash ((@($orderState.order|ForEach-Object{[string][int]$_}) -join ','))
        if([int64]$orderState.revision -ne [int64]$tail.revision){
            Finding 'ERROR' 'MUTATION_LEDGER_STATE_REVISION_DIVERGENCE' 'Persisted mutable order revision differs from the mutation-ledger tail.' @{order_revision=$orderState.revision;ledger_revision=$tail.revision}
        }
        if($currentOrderHash -ne [string]$tail.order_hash){
            Finding 'ERROR' 'MUTATION_LEDGER_STATE_HASH_DIVERGENCE' 'Persisted mutable order bytes describe a different occurrence order than the mutation-ledger tail.' @{order_hash=$currentOrderHash;ledger_hash=$tail.order_hash}
        }
    }
}

$runtime=$null;$lease=$null;$queue=$null;$watchdog=$null
foreach($pair in @(
    @('runtime','runtime-instance.json'),@('lease','runtime-lease.json'),
    @('queue','queue-runtime.json'),@('watchdog','watchdog-status.json')
)){
    $file=Join-Path $stateRoot $pair[1]
    if(Test-Path $file){
        try{
            $value=Get-Content $file -Raw|ConvertFrom-Json
            Set-Variable -Name $pair[0] -Value $value -Scope Local
        }catch{Finding 'WARN' 'STATE_PARSE' "Could not parse $($pair[1])."}
    }
}
if($runtime -and $lease -and [string]$runtime.runtime_id -ne [string]$lease.runtime_id){Finding 'ERROR' 'RUNTIME_LEASE_EPOCH_MISMATCH' 'Runtime record and engine lease have different runtime IDs.'}
if($runtime -and $queue -and [string]$runtime.runtime_id -ne [string]$queue.runtime_id){Finding 'ERROR' 'RUNTIME_QUEUE_EPOCH_MISMATCH' 'Runtime record and queue projection have different runtime IDs.'}
if($queue -and [string]$queue.buffer_health -eq 'AT_RISK'){Finding 'WARN' 'BUFFER_AT_RISK' 'Latest queue projection reports AT_RISK buffer health.'}
if($watchdog -and [string]$watchdog.state -ne 'healthy'){Finding 'WARN' 'WATCHDOG_DEGRADED' ([string]$watchdog.message) $watchdog}

$eventTypeCounts=[ordered]@{}
foreach($g in @($events|Group-Object type|Sort-Object Count -Descending)){if($g.Name){$eventTypeCounts[$g.Name]=[int]$g.Count}}

$contractSnapshot=$null
try{$contractSnapshot=Get-YomiContractSnapshot}catch{}
$queueSummary=$null
if($queue){
    $queueSummary=[ordered]@{
        runtime_id=$queue.runtime_id;session_id=$queue.session_id;current_index=$queue.current_index;
        current_order_slot=$queue.current_order_slot;buffer_health=$queue.buffer_health;
        ready_ahead=$queue.ready_ahead;target_ahead=$queue.target_ahead;ready_time_seconds=$queue.ready_time_seconds;
        active_jobs=$queue.active_jobs;queued_jobs=$queue.queued_jobs;failure_domain=$queue.failure_domain
    }
}

$report=[ordered]@{
    schema=1
    product='YOMI'
    report_type='incident-replay'
    generated_utc=[DateTime]::UtcNow.ToString('o')
    contracts=$contractSnapshot
    event_count=$events.Count
    journal_count=$journal.Count
    runtime_count=$runtimeGroups.Count
    event_types=$eventTypeCounts
    current=[ordered]@{
        runtime=$runtime
        lease=$lease
        watchdog=$watchdog
        queue_summary=$queueSummary
    }
    findings=$findings.ToArray()
    timeline=@($events|Select-Object -Last 250)
    mutation_ledger=[ordered]@{
        chained_records=$ledgerRows.Count
        legacy_records=$legacyLedgerRows.Count
        head=$ledgerHead
        verified_tail=$(if($ledgerRows.Count -gt 0){$ledgerRows[-1]}else{$null})
    }
    mutation_tail=@($journal|Select-Object -Last 100)
}

$parent=Split-Path $OutputPath -Parent
if($parent){New-Item -ItemType Directory -Path $parent -Force|Out-Null}
[IO.File]::WriteAllText($OutputPath,($report|ConvertTo-Json -Depth 12),(New-Object System.Text.UTF8Encoding($false)))

Write-Host '===== YOMI INCIDENT REPLAY =====' -ForegroundColor Cyan
Write-Host "Events: $($events.Count)  Mutations: $($journal.Count)  Findings: $($findings.Count)"
foreach($f in $findings){Write-Host ("{0,-5} {1}: {2}" -f $f.severity,$f.code,$f.message)}
Write-Host "Report: $OutputPath"
if($Copy){try{Set-Clipboard $OutputPath}catch{}}
if(@($findings|Where-Object{$_.severity -eq 'ERROR'}).Count -gt 0){exit 2}
exit 0
