param(
    [switch]$CopyReport,
    [switch]$OpenReport,
    [int]$Top = 20
)

$ErrorActionPreference='Stop'
Set-StrictMode -Version 2.0
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$eventsPath=Join-Path $DataRoot 'state\events.jsonl'
$reports=Join-Path $DataRoot 'reports'
New-Item -ItemType Directory -Path $reports -Force|Out-Null
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$jsonPath=Join-Path $reports ("YOMI-performance-attribution-$stamp.json")
$textPath=Join-Path $reports ("YOMI-performance-attribution-$stamp.txt")

function Percentile([object[]]$Values,[double]$P){
    $v=@($Values|Where-Object{$_ -ne $null}|ForEach-Object{[double]$_}|Sort-Object)
    if($v.Count -eq 0){return $null}
    $idx=[Math]::Max(0,[Math]::Min($v.Count-1,[int][Math]::Ceiling($P*$v.Count)-1))
    return [double]$v[$idx]
}
function Parse-Detail([string]$Text){
    $o=[ordered]@{}
    foreach($name in @('queue_ms','service_ms','total_ms','start_ms','end_ms','anomaly')){
        $m=[regex]::Match([string]$Text,('(?:^|\s)'+[regex]::Escape($name)+'=([0-9]+)'))
        if($m.Success){$o[$name]=[int64]$m.Groups[1].Value}
    }
    $m=[regex]::Match([string]$Text,'(?:^|\s)outcome=([^\s]+)')
    if($m.Success){$o['outcome']=$m.Groups[1].Value}
    return [pscustomobject]$o
}

$events=@()
if(Test-Path -LiteralPath $eventsPath){
    foreach($line in Get-Content -LiteralPath $eventsPath -Encoding UTF8){
        if([string]::IsNullOrWhiteSpace($line)){continue}
        try{$events+=($line|ConvertFrom-Json)}catch{}
    }
}

$session=''
for($i=$events.Count-1;$i -ge 0;$i--){
    $sid=[string]$events[$i].session_id
    if(-not [string]::IsNullOrWhiteSpace($sid)){$session=$sid;break}
}
if(-not [string]::IsNullOrWhiteSpace($session)){
    $events=@($events|Where-Object{[string]$_.session_id -eq $session})
}
$perf=@($events|Where-Object{[string]$_.type -eq 'PERF_STAGE_SAMPLE'})
$rows=@()
foreach($e in $perf){
    $d=Parse-Detail ([string]$e.detail)
    $rows += [pscustomobject]@{
        seq=[int64]$e.seq
        unix=[int64]$e.unix
        session_id=[string]$e.session_id
        track=[int]$e.index
        stage=[string]$e.job
        queue_ms=$(if($d.PSObject.Properties['queue_ms']){[int64]$d.queue_ms}else{0})
        service_ms=$(if($d.PSObject.Properties['service_ms']){[int64]$d.service_ms}else{0})
        total_ms=$(if($d.PSObject.Properties['total_ms']){[int64]$d.total_ms}else{0})
        start_ms=$(if($d.PSObject.Properties['start_ms']){[int64]$d.start_ms}else{0})
        end_ms=$(if($d.PSObject.Properties['end_ms']){[int64]$d.end_ms}else{0})
        anomaly=$(if($d.PSObject.Properties['anomaly']){[int]$d.anomaly -ne 0}else{$false})
        outcome=$(if($d.PSObject.Properties['outcome']){[string]$d.outcome}else{'UNKNOWN'})
    }
}

$stageSummaries=@()
foreach($g in @($rows|Group-Object stage|Sort-Object Name)){
    $r=@($g.Group)
    $stageSummaries += [pscustomobject]@{
        stage=[string]$g.Name
        samples=$r.Count
        p50_total_ms=[int64](Percentile @($r.total_ms) .50)
        p95_total_ms=[int64](Percentile @($r.total_ms) .95)
        max_total_ms=[int64](($r|Measure-Object total_ms -Maximum).Maximum)
        p95_queue_ms=[int64](Percentile @($r.queue_ms) .95)
        p95_service_ms=[int64](Percentile @($r.service_ms) .95)
        anomalies=@($r|Where-Object{$_.anomaly}).Count
    }
}

$critical=$null
if($stageSummaries.Count -gt 0){$critical=$stageSummaries|Sort-Object p95_total_ms -Descending|Select-Object -First 1}

$trackSummaries=@()
foreach($g in @($rows|Where-Object{$_.track -gt 0 -and $_.stage -ne 'handoff'}|Group-Object track)){
    $r=@($g.Group)
    $starts=@($r|Where-Object{$_.start_ms -gt 0}|ForEach-Object{[int64]$_.start_ms-[int64]$_.queue_ms})
    $ends=@($r|Where-Object{$_.end_ms -gt 0}|ForEach-Object{[int64]$_.end_ms})
    $earliest=$(if($starts.Count -gt 0){($starts|Measure-Object -Minimum).Minimum}else{0})
    $latest=$(if($ends.Count -gt 0){($ends|Measure-Object -Maximum).Maximum}else{0})
    $terminal=$r|Sort-Object end_ms -Descending|Select-Object -First 1
    $dominant=$r|Sort-Object total_ms -Descending|Select-Object -First 1
    $trackSummaries += [pscustomobject]@{
        track=[int]$g.Name
        observed_span_ms=$(if($earliest -gt 0 -and $latest -ge $earliest){[int64]($latest-$earliest)}else{0})
        terminal_stage=$(if($terminal){[string]$terminal.stage}else{''})
        dominant_stage=$(if($dominant){[string]$dominant.stage}else{''})
        dominant_total_ms=$(if($dominant){[int64]$dominant.total_ms}else{0})
        anomaly_count=@($r|Where-Object{$_.anomaly}).Count
    }
}

$slow=@($rows|Sort-Object total_ms -Descending|Select-Object -First ([Math]::Max(1,[Math]::Min(100,$Top))))
$queueTotal=[int64](($rows|Measure-Object queue_ms -Sum).Sum)
$serviceTotal=[int64](($rows|Measure-Object service_ms -Sum).Sum)

$report=[ordered]@{
    schema=1
    product='YOMI'
    report_type='performance-attribution'
    generated_utc=[DateTime]::UtcNow.ToString('o')
    session_id=$session
    retained_event_count=$events.Count
    performance_sample_count=$rows.Count
    anomaly_count=@($rows|Where-Object{$_.anomaly}).Count
    critical_stage=$(if($critical){[string]$critical.stage}else{''})
    critical_p95_ms=$(if($critical){[int64]$critical.p95_total_ms}else{0})
    scheduler_queue_ms_total=$queueTotal
    service_ms_total=$serviceTotal
    stages=$stageSummaries
    tracks=@($trackSummaries|Sort-Object observed_span_ms -Descending|Select-Object -First 100)
    slowest_samples=$slow
}
[IO.File]::WriteAllText($jsonPath,($report|ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding($false)))

$lines=New-Object Collections.Generic.List[string]
$lines.Add('YOMI PERFORMANCE ATTRIBUTION')
$lines.Add('============================')
$lines.Add("Session: $(if($session){$session}else{'unknown'})")
$lines.Add("Samples: $($rows.Count)   Anomalies: $($report.anomaly_count)")
if($critical){$lines.Add("Critical session stage by p95: $($critical.stage.ToUpperInvariant())  $($critical.p95_total_ms) ms")}
$lines.Add("Scheduler queue time observed: $queueTotal ms")
$lines.Add("Worker/service time observed:   $serviceTotal ms")
$lines.Add('')
$lines.Add('STAGE DISTRIBUTIONS')
foreach($s in $stageSummaries){
    $lines.Add(('{0,-12} N={1,-4} P50={2,7}ms P95={3,7}ms MAX={4,7}ms QUEUE95={5,7}ms SERVICE95={6,7}ms A={7}' -f
        $s.stage.ToUpperInvariant(),$s.samples,$s.p50_total_ms,$s.p95_total_ms,$s.max_total_ms,$s.p95_queue_ms,$s.p95_service_ms,$s.anomalies))
}
$lines.Add('')
$lines.Add('SLOWEST OBSERVATIONS')
foreach($s in $slow){
    $lines.Add(('Track {0,-5} {1,-10} total={2,7}ms queue={3,7}ms service={4,7}ms {5}' -f
        $s.track,$s.stage.ToUpperInvariant(),$s.total_ms,$s.queue_ms,$s.service_ms,$(if($s.anomaly){'ANOMALY'}else{''})))
}
$lines.Add('')
$lines.Add('Interpretation:')
$lines.Add('- queue_ms is time waiting for an existing authorized worker slot.')
$lines.Add('- service_ms is elapsed execution time once the job started.')
$lines.Add('- stage labels identify YOMI work classes; this report does not pretend wall time is CPU utilization.')
$lines.Add('- terminal/dominant stage reconstruction uses observed job intervals, not guessed hardware causality.')
$lines.Add('')
$lines.Add("JSON: $jsonPath")
[IO.File]::WriteAllLines($textPath,$lines,(New-Object Text.UTF8Encoding($false)))

Write-Host '===== YOMI PERFORMANCE ATTRIBUTION =====' -ForegroundColor Cyan
$lines|ForEach-Object{Write-Host $_}
if($CopyReport){try{Set-Clipboard ($lines -join "`r`n")}catch{}}
if($OpenReport){try{Start-Process notepad.exe ('"'+$textPath+'"')}catch{}}
exit 0
