param(
    [ValidateSet('Analyze','RepairSafe')]
    [string]$Mode='Analyze',
    [switch]$Force
)

$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$stateRoot=Join-Path $DataRoot 'state'
$actions=New-Object System.Collections.Generic.List[System.Object]
$issues=New-Object System.Collections.Generic.List[System.Object]

function Note([string]$Kind,[string]$Code,[string]$Message,$Evidence=$null){
    $issues.Add([PSCustomObject][ordered]@{kind=$Kind;code=$Code;message=$Message;evidence=$Evidence})
}
function Action([string]$Code,[string]$Message){$actions.Add([PSCustomObject][ordered]@{code=$Code;message=$Message})}
function Read-Json([string]$Path){if(Test-Path $Path){try{return Get-Content $Path -Raw|ConvertFrom-Json}catch{}};return $null}
function Hash32([string]$Text,[uint64]$Seed){
    [uint64]$h=$Seed
    foreach($b in [System.Text.Encoding]::UTF8.GetBytes([string]$Text)){$h=(($h*65599)+[uint64]$b+17)%4294967296}
    return ('{0:x8}' -f [uint32]$h)
}
function IdentityHash([string]$Text){return (Hash32 $Text 2166136261)+(Hash32 $Text 2246822519)}
function JournalPayload($Entry){
    return (@(
        [string]$Entry.schema,[string]$Entry.session_id,[string]$Entry.runtime_id,[string]$Entry.sequence,
        [string]$Entry.revision,[string]$Entry.work_generation,[string]$Entry.action,[string]$Entry.current_occurrence,
        [string]$Entry.active_count,[string]$Entry.registry_count,[string]$Entry.order_hash,[string]$Entry.previous_hash,[string]$Entry.unix
    ) -join [char]31)
}
function Write-AtomicJson([string]$Path,$Object){
    $json=$Object|ConvertTo-Json -Depth 10 -Compress
    $tmp=$Path+'.repair.'+$PID+'.'+[Guid]::NewGuid().ToString('N')
    $parent=Split-Path $Path -Parent
    New-Item -ItemType Directory -Path $parent -Force|Out-Null
    [IO.File]::WriteAllText($tmp,$json,(New-Object System.Text.UTF8Encoding($false)))
    if(Test-Path $Path){
        $backup=$Path+'.repair-backup'
        try{[IO.File]::Replace($tmp,$Path,$backup,$true);Remove-Item $backup -Force -ErrorAction SilentlyContinue}
        catch{Remove-Item $tmp -Force -ErrorAction SilentlyContinue;throw}
    }else{[IO.File]::Move($tmp,$Path)}
}
function Live-PidFile([string]$Name){
    $path=Join-Path $stateRoot $Name
    if(-not(Test-Path $path)){return $false}
    $n=0
    try{[void][int]::TryParse((Get-Content $path -Raw).Trim(),[ref]$n)}catch{}
    return ($n -gt 0 -and $null -ne (Get-Process -Id $n -ErrorAction SilentlyContinue))
}
function Runtime-Live {
    return (Live-PidFile 'engine.pid') -or (Live-PidFile 'supervisor.pid')
}
function Validate-Ledger {
    $journalPath=Join-Path $stateRoot 'session-journal.jsonl'
    $rows=@()
    if(Test-Path $journalPath){
        foreach($line in @(Get-Content $journalPath -ErrorAction SilentlyContinue)){
            if([string]::IsNullOrWhiteSpace($line)){continue}
            try{$obj=$line|ConvertFrom-Json}catch{return [PSCustomObject]@{valid=$false;reason='journal JSON parse failure';rows=@()}}
            if([int]$obj.schema -eq 2){$rows+=$obj}
        }
    }
    if($rows.Count -eq 0){return [PSCustomObject]@{valid=$true;reason='no chained records';rows=@();tail=$null}}
    $previous=$null
    foreach($entry in $rows){
        if((IdentityHash (JournalPayload $entry)) -ne [string]$entry.entry_hash){return [PSCustomObject]@{valid=$false;reason="hash mismatch at sequence $($entry.sequence)";rows=$rows;tail=$rows[-1]}}
        if($previous){
            if([int64]$entry.sequence -ne ([int64]$previous.sequence+1)){return [PSCustomObject]@{valid=$false;reason="sequence gap at $($entry.sequence)";rows=$rows;tail=$rows[-1]}}
            if([string]$entry.previous_hash -ne [string]$previous.entry_hash){return [PSCustomObject]@{valid=$false;reason="previous-hash break at $($entry.sequence)";rows=$rows;tail=$rows[-1]}}
        }
        $previous=$entry
    }
    return [PSCustomObject]@{valid=$true;reason='chain valid';rows=$rows;tail=$rows[-1]}
}

$live=Runtime-Live
if($live){Note 'INFO' 'RUNTIME_LIVE' 'YOMI runtime is live; Aegis will analyze only.'}


if($Mode -eq 'RepairSafe' -and -not $Force){
    $answer=[System.Windows.Forms.MessageBox]::Show(
        "Run Aegis Safe Repair?`r`n`r`nOnly stale transient state and a provably lagging mutation-ledger head are eligible. Authoritative config/session/order and media objects are never rewritten by this repair mode.",
        'YOMI Aegis Safe Repair',
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Question
    )
    if($answer -ne [System.Windows.Forms.DialogResult]::Yes){Write-Host 'Aegis Safe Repair cancelled.';exit 0}
}

foreach($name in @('engine.pid','server.pid','supervisor.pid')){
    $path=Join-Path $stateRoot $name
    if((Test-Path $path) -and -not (Live-PidFile $name)){
        Note 'WARN' 'STALE_PID_FILE' "$name does not identify a live process."
        if($Mode -eq 'RepairSafe' -and -not $live){
            Remove-Item $path -Force -ErrorAction SilentlyContinue
            Action 'REMOVE_STALE_PID' "Removed $name."
        }
    }
}

$ledger=Validate-Ledger
if(-not $ledger.valid){Note 'ERROR' 'MUTATION_LEDGER_CORRUPT' $ledger.reason}
elseif($ledger.tail){
    $headPath=Join-Path $stateRoot 'session-journal-head.json'
    $head=Read-Json $headPath
    $order=Read-Json (Join-Path $stateRoot 'session-order.json')
    $tail=$ledger.tail
    $orderMatches=$false
    if($order -and [string]$order.session_id -eq [string]$tail.session_id){
        $orderHash=IdentityHash ((@($order.order|ForEach-Object{[string][int]$_}) -join ','))
        $orderMatches=([int64]$order.revision -eq [int64]$tail.revision -and $orderHash -eq [string]$tail.order_hash)
        if(-not $orderMatches){Note 'ERROR' 'MUTATION_LEDGER_ORDER_DIVERGENCE' 'Ledger tail does not match persisted session-order.json.'}
    }
    if(-not $head){
        Note 'WARN' 'MUTATION_LEDGER_HEAD_MISSING' 'Validated ledger has no persisted head.'
    }elseif([int64]$head.sequence -lt [int64]$tail.sequence){
        Note 'WARN' 'MUTATION_LEDGER_HEAD_LAG' "Head $($head.sequence) lags validated tail $($tail.sequence)."
    }elseif([int64]$head.sequence -gt [int64]$tail.sequence -or ([int64]$head.sequence -eq [int64]$tail.sequence -and [string]$head.entry_hash -ne [string]$tail.entry_hash)){
        Note 'ERROR' 'MUTATION_LEDGER_HEAD_INVALID' 'Ledger head cannot be reconciled safely with the validated tail.'
    }

    $repairableHead=$orderMatches -and (-not $head -or [int64]$head.sequence -lt [int64]$tail.sequence)
    if($Mode -eq 'RepairSafe' -and -not $live -and $repairableHead){
        $newHead=[ordered]@{
            schema=1;session_id=[string]$tail.session_id;runtime_id=[string]$tail.runtime_id;
            sequence=[int64]$tail.sequence;entry_hash=[string]$tail.entry_hash;revision=[int64]$tail.revision;
            work_generation=[int64]$tail.work_generation;order_hash=[string]$tail.order_hash;updated_unix=[DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        }
        Write-AtomicJson $headPath $newHead
        Action 'REPAIR_LEDGER_HEAD' "Advanced ledger head to validated sequence $($tail.sequence)."
    }
}

if(-not $live){
    foreach($name in @('runtime-lease.json','watchdog-status.json','browser-clients.json','current.json','queue-runtime.json','engine-status.json')){
        $path=Join-Path $stateRoot $name
        if(Test-Path $path){
            $age=([DateTime]::UtcNow-(Get-Item $path).LastWriteTimeUtc).TotalMinutes
            if($age -gt 2){
                Note 'INFO' 'STALE_TRANSIENT_STATE' "$name is $([Math]::Round($age,1)) minutes old with no live runtime."
                if($Mode -eq 'RepairSafe'){
                    Remove-Item $path -Force -ErrorAction SilentlyContinue
                    Action 'REMOVE_TRANSIENT' "Removed stale $name."
                }
            }
        }
    }
    foreach($file in @(Get-ChildItem $stateRoot -File -Filter '*.tmp.*' -ErrorAction SilentlyContinue)){
        if(([DateTime]::UtcNow-$file.LastWriteTimeUtc).TotalHours -gt 1){
            Note 'INFO' 'ORPHAN_TEMP' $file.Name
            if($Mode -eq 'RepairSafe'){Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue;Action 'REMOVE_ORPHAN_TEMP' "Removed $($file.Name)."}
        }
    }
}

if($Mode -eq 'RepairSafe' -and $live){
    Note 'WARN' 'REPAIR_DEFERRED' 'Safe repair refused to mutate state while YOMI is live.'
}

$report=[ordered]@{
    schema=1;product='YOMI';report_type='aegis-safe-repair';mode=$Mode;
    generated_utc=[DateTime]::UtcNow.ToString('o');runtime_live=$live;
    issues=$issues.ToArray();actions=$actions.ToArray();
    status=$(if(@($issues|Where-Object{$_.kind -eq 'ERROR'}).Count -gt 0){'ATTENTION'}elseif($actions.Count -gt 0){'REPAIRED'}else{'CLEAN'})
}
$reportPath=Join-Path $stateRoot 'aegis-repair.json'
Write-AtomicJson $reportPath $report

Write-Host '===== YOMI AEGIS SAFE REPAIR =====' -ForegroundColor Cyan
Write-Host "Mode: $Mode  Status: $($report.status)  Live: $live"
foreach($i in $issues){Write-Host ("{0,-5} {1}: {2}" -f $i.kind,$i.code,$i.message)}
foreach($a in $actions){Write-Host ("FIX   {0}: {1}" -f $a.code,$a.message)}
Write-Host "Report: $reportPath"

if($Mode -eq 'Analyze'){
    if(@($issues|Where-Object{$_.kind -eq 'ERROR'}).Count -gt 0){exit 2}
    exit 0
}
if(@($issues|Where-Object{$_.kind -eq 'ERROR'}).Count -gt 0){exit 2}
exit 0
