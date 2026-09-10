param(
    [switch]$CopyReport,
    [switch]$OpenReport
)

$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

function Read-JsonSafe([string]$Path) {
    if(-not (Test-Path -LiteralPath $Path)){return $null}
    try { return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json) } catch { return $null }
}
function Read-JsonLines([string]$Path,[int]$Tail=400) {
    if(-not (Test-Path -LiteralPath $Path)){return @()}
    $rows=@()
    foreach($line in @(Get-Content -LiteralPath $Path -Tail $Tail -Encoding UTF8 -ErrorAction SilentlyContinue)){
        if([string]::IsNullOrWhiteSpace($line)){continue}
        try{$rows+=($line|ConvertFrom-Json)}catch{}
    }
    return @($rows)
}
function Pct([double]$v){return [Math]::Round($v,2)}
function Ms([double]$seconds){if($seconds -lt 0){return $null};return [int][Math]::Round($seconds*1000)}

$queue=Read-JsonSafe (Join-Path $DataRoot 'state\queue-runtime.json')
$history=Read-JsonSafe (Join-Path $DataRoot 'state\slo-history.json')
$events=Read-JsonLines (Join-Path $DataRoot 'state\events.jsonl') 1000
$config=Read-JsonSafe (Join-Path $DataRoot 'config.json')

$currentSlo=$null
$currentPerf=$null
$currentCache=$null
$currentFault=$null
if($queue){
    $currentSlo=$queue.slo
    $currentPerf=$queue.performance
    $currentCache=$queue.coherence
    $currentFault=$queue.faults
}

$recentSloEvents=@($events|Where-Object{
    [string]$_.type -like 'SLO_*'
}|Select-Object -Last 80)

$sessions=@()
if($history -and $history.sessions){$sessions=@($history.sessions)}
$eligible=@($sessions|Where-Object{$_.eligible -eq $true})
$context=''
if($currentSlo){$context=[string]$currentSlo.context}
$matching=@($eligible|Where-Object{[string]$_.context -eq $context})

$report=[ordered]@{
    schema=1
    product='YOMI'
    report_type='service-level-regression-sentinel'
    generated_utc=[DateTime]::UtcNow.ToString('o')
    interpretation='SLO regression evidence uses metrics YOMI actually observes. Historical baselines contain only mature healthy sessions; breached or regressed sessions cannot normalize themselves into the baseline.'
    policy=$(if($config){[string]$config.slo_policy}else{'unknown'})
    current=$currentSlo
    performance=$currentPerf
    cache_coherence=$currentCache
    fault_containment=$currentFault
    baseline=[ordered]@{
        context=$context
        total_history_sessions=$sessions.Count
        eligible_sessions=$eligible.Count
        matching_eligible_sessions=$matching.Count
        matching_tail=@($matching|Select-Object -Last 12)
    }
    recent_slo_events=$recentSloEvents
}

$reports=Join-Path $DataRoot 'reports'
New-Item -ItemType Directory -Path $reports -Force|Out-Null
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$path=Join-Path $reports ("YOMI-slo-regression-$stamp.json")
[IO.File]::WriteAllText($path,($report|ConvertTo-Json -Depth 14),(New-Object Text.UTF8Encoding($false)))

Write-Host '===== YOMI SERVICE LEVEL / REGRESSION SENTINEL ====='
if($currentSlo){
    Write-Host ("State: {0}  Policy: {1}  Reason: {2}" -f $currentSlo.state,$currentSlo.policy,$currentSlo.reason)
    Write-Host ("Baseline: {0} matching healthy session(s)  Regressions: {1}  Breaches: {2}" -f $currentSlo.baseline_sessions,$currentSlo.regressions,$currentSlo.breaches)
    if($null -ne $currentSlo.handoff_p95_seconds){Write-Host ("Handoff p95: {0} ms" -f (Ms ([double]$currentSlo.handoff_p95_seconds)))}
    if($null -ne $currentSlo.handoff_baseline_p95_seconds -and [double]$currentSlo.handoff_baseline_p95_seconds -ge 0){Write-Host ("Handoff healthy baseline p95: {0} ms" -f (Ms ([double]$currentSlo.handoff_baseline_p95_seconds)))}
    if($null -ne $currentSlo.audio_p95_seconds){Write-Host ("Audio prep p95: {0:N2} s" -f [double]$currentSlo.audio_p95_seconds)}
    if($null -ne $currentSlo.audio_baseline_p95_seconds -and [double]$currentSlo.audio_baseline_p95_seconds -ge 0){Write-Host ("Audio healthy baseline p95: {0:N2} s" -f [double]$currentSlo.audio_baseline_p95_seconds)}
    Write-Host ("Anomaly rate: {0:N2}%  Samples: {1}" -f [double]$currentSlo.anomaly_rate_percent,[int]$currentSlo.samples)
}else{
    Write-Host 'No queue SLO projection is currently available. Start YOMI and gather timing samples.'
}
Write-Host "Report: $path"
if($CopyReport){try{Set-Clipboard $path}catch{}}
if($OpenReport){try{Start-Process notepad.exe ('"'+$path+'"')}catch{}}
