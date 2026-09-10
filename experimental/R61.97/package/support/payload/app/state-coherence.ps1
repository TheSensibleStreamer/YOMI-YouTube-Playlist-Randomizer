param([switch]$CopyReport,[switch]$OpenReport)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
function Read-J([string]$p){if(-not(Test-Path -LiteralPath $p)){return $null};try{return Get-Content -LiteralPath $p -Raw -Encoding UTF8|ConvertFrom-Json}catch{return $null}}
function G($o,[string]$n,$d=$null){if($null -eq $o){return $d};$p=$o.PSObject.Properties[$n];if($null -eq $p){return $d};return $p.Value}
$state=Join-Path $DataRoot 'state'
$current=Read-J (Join-Path $state 'current.json');$queue=Read-J (Join-Path $state 'queue-runtime.json');$head=Read-J (Join-Path $state 'state-coherence.json');$runtime=Read-J (Join-Path $state 'runtime-instance.json');$session=Read-J (Join-Path $state 'session.json')
$cp=G $current 'state_protocol';$qp=G $queue 'state_protocol'
$cr=[string](G $cp 'runtime_id' '');$qr=[string](G $qp 'runtime_id' '');$hr=[string](G $head 'runtime_id' '');$rr=[string](G $runtime 'runtime_id' '')
$cs=[string](G $cp 'session_id' '');$qs=[string](G $qp 'session_id' '');$ss=[string](G $session 'session_id' '')
$cseq=[int64](G $cp 'sequence' -1);$qseq=[int64](G $qp 'sequence' -1);$hseq=[int64](G $head 'sequence' -1)
$ce=[int](G $cp 'presentation_epoch' -1);$qe=[int](G $qp 'presentation_epoch' -1)
$findings=New-Object Collections.Generic.List[object]
function F([string]$severity,[string]$code,[string]$detail){$findings.Add([pscustomobject]@{severity=$severity;code=$code;detail=$detail})}
if($cseq -lt 0 -or $qseq -lt 0){F 'INFO' 'PROTOCOL_WARMING' 'Current/queue protocol stamps are not both available yet.'}
if($cr -and $qr -and $cr -ne $qr){F 'ERROR' 'RUNTIME_IDENTITY_CONFLICT' "current=$cr queue=$qr"}
if($rr -and $cr -and $rr -ne $cr){F 'ERROR' 'CURRENT_RUNTIME_STALE' "runtime=$rr current=$cr"}
if($hr -and $cr -and $hr -ne $cr){F 'ERROR' 'HEAD_RUNTIME_STALE' "head=$hr current=$cr"}
if($cs -and $qs -and $cs -ne $qs){F 'ERROR' 'SESSION_IDENTITY_CONFLICT' "current=$cs queue=$qs"}
if($ss -and $cs -and $ss -ne $cs){F 'ERROR' 'CURRENT_SESSION_STALE' "session=$ss current=$cs"}
if($cseq -ge 0 -and $qseq -ge 0){if($qseq -gt $cseq -and $qe -ge 0 -and $ce -ge 0 -and $qe -lt $ce){F 'ERROR' 'QUEUE_CAUSAL_REGRESSION' "newer queue S$qseq carries epoch E$qe behind current E$ce"};if($cseq -gt $qseq -and $ce -ge 0 -and $qe -ge 0 -and $ce -lt $qe){F 'ERROR' 'CURRENT_CAUSAL_REGRESSION' "newer current S$cseq carries epoch E$ce behind queue E$qe"}}
$max=[Math]::Max($cseq,$qseq);$min=[Math]::Min($cseq,$qseq);$lag=if($min -ge 0){$max-$min}else{-1}
if($hseq -ge 0 -and $max -ge 0 -and $hseq -ne $max){F 'WARN' 'HEAD_SYNCING' "head S$hseq projected max S$max"}
$errors=@($findings|Where-Object severity -eq 'ERROR').Count;$warns=@($findings|Where-Object severity -eq 'WARN').Count
$status=if($errors){'CONFLICT'}elseif($cseq -lt 0 -or $qseq -lt 0){'WARMING'}elseif($warns){'SYNCING'}else{'COHERENT'}
$report=[ordered]@{schema=1;report_type='yomi-runtime-state-coherence';generated_utc=[DateTime]::UtcNow.ToString('o');status=$status;runtime_id=$rr;session_id=$ss;current=[ordered]@{sequence=$cseq;epoch=$ce;runtime_id=$cr;session_id=$cs};queue=[ordered]@{sequence=$qseq;epoch=$qe;runtime_id=$qr;session_id=$qs};head=[ordered]@{sequence=$hseq;runtime_id=$hr;surface=[string](G $head 'surface' '')};lag=$lag;findings=$findings.ToArray()}
$reports=Join-Path $DataRoot 'reports';New-Item -ItemType Directory -Path $reports -Force|Out-Null;$path=Join-Path $reports ('YOMI-state-coherence-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json');Write-YomiUtf8NoBom -Path $path -Text ($report|ConvertTo-Json -Depth 10)
Write-Host '===== YOMI RUNTIME STATE COHERENCE =====';Write-Host "STATUS=$status";Write-Host "RUNTIME=$rr SESSION=$ss";Write-Host "CURRENT=S$cseq/E$ce QUEUE=S$qseq/E$qe HEAD=S$hseq LAG=$lag";foreach($f in $findings){Write-Host ("{0} {1}: {2}" -f $f.severity,$f.code,$f.detail)};Write-Host "REPORT=$path"
if($CopyReport){try{Set-Clipboard $path}catch{}};if($OpenReport){try{Start-Process notepad.exe ('"'+$path+'"')}catch{}}
