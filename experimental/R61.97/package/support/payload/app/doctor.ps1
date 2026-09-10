$ErrorActionPreference='SilentlyContinue'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$out=New-Object System.Collections.Generic.List[string]
$fail=0;$warn=0
function Check([string]$Level,[string]$Name,[string]$Detail=''){
    $script:fail += $(if($Level -eq 'FAIL'){1}else{0})
    $script:warn += $(if($Level -eq 'WARN'){1}else{0})
    $line=("{0,-4}  {1}" -f $Level,$Name)
    if($Detail){$line+='  -  '+$Detail}
    $out.Add($line)
}
function Hash-Text([string]$Text){
    $sha=[System.Security.Cryptography.SHA256]::Create()
    try{$bytes=[System.Text.Encoding]::UTF8.GetBytes($Text);return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
}
function Aegis-Hash32([string]$Text,[uint64]$Seed){
    [uint64]$h=$Seed
    foreach($b in [System.Text.Encoding]::UTF8.GetBytes([string]$Text)){$h=(($h*65599)+[uint64]$b+17)%4294967296}
    return ('{0:x8}' -f [uint32]$h)
}
function Aegis-IdentityHash([string]$Text){return (Aegis-Hash32 $Text 2166136261)+(Aegis-Hash32 $Text 2246822519)}
function Aegis-JournalPayload($Entry){
    return (@(
        [string]$Entry.schema,[string]$Entry.session_id,[string]$Entry.runtime_id,[string]$Entry.sequence,
        [string]$Entry.revision,[string]$Entry.work_generation,[string]$Entry.action,[string]$Entry.current_occurrence,
        [string]$Entry.active_count,[string]$Entry.registry_count,[string]$Entry.order_hash,[string]$Entry.previous_hash,[string]$Entry.unix
    ) -join [char]31)
}

$out.Add("===== YOMI $(Get-YomiVersionText) DOCTOR =====")
$out.Add((Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))
$out.Add('')

$config=$null
try{$config=Get-Content (Join-Path $DataRoot 'config.json') -Raw|ConvertFrom-Json;Check PASS 'Config parses'}catch{Check FAIL 'Config parses' $_.Exception.Message}
$contracts=$null
try{$contracts=Get-YomiContractSnapshot;Check PASS 'Contract registry' ($contracts|ConvertTo-Json -Compress)}catch{Check FAIL 'Contract registry' $_.Exception.Message}
if($config -and $contracts){
    $schema=if($null -ne $config.PSObject.Properties['config_schema_version']){[int]$config.config_schema_version}else{1}
    if($schema -le [int]$contracts.config_schema){Check PASS 'Config schema contract' "$schema <= $($contracts.config_schema)"}else{Check FAIL 'Config schema contract' "$schema > $($contracts.config_schema)"}
}
$previousConfig=Join-Path $DataRoot 'config.previous.json'
if(Test-Path $previousConfig){
    try{$null=Get-Content $previousConfig -Raw|ConvertFrom-Json;Check PASS 'Previous-good config parses'}catch{Check WARN 'Previous-good config parses' $_.Exception.Message}
}else{Check WARN 'Previous-good config' 'Not created yet; it appears after a successful replacement of an existing valid config.'}
$configRecovery=Join-Path $DataRoot 'state\config-recovery.txt'
if(Test-Path $configRecovery){Check WARN 'Config recovery notice' ((Get-Content $configRecovery -Raw).Trim())}
$oracleResetPending=Join-Path $DataRoot 'state\reset-oracle-learning.pending'
if(Test-Path $oracleResetPending){Check WARN 'Oracle Learning reset' 'Pending next YOMI startup'}
elseif(Test-Path (Join-Path $PSScriptRoot 'reset-oracle-learning.ps1')){Check PASS 'Oracle Learning reset' 'tool available'}

if(Test-Path $routeIntelligencePath){
    try{
        $routeMemory=Get-Content $routeIntelligencePath -Raw|ConvertFrom-Json
        $entryCount=@($routeMemory.entries.PSObject.Properties).Count
        if($contracts -and [int]$routeMemory.schema -gt [int]$contracts.route_intelligence_schema){Check FAIL 'Persistent route intelligence' "schema $($routeMemory.schema) newer than supported $($contracts.route_intelligence_schema)"}
        else{Check PASS 'Persistent route intelligence' "schema $($routeMemory.schema), $entryCount record(s), $([Math]::Round((Get-Item $routeIntelligencePath).Length/1KB,1)) KB"}
    }catch{Check WARN 'Persistent route intelligence' 'File is unreadable; it is cache intelligence and may be safely cleared.'}
}
if(Test-Path $configIncompatiblePath){Check FAIL 'Config compatibility notice' ((Get-Content $configIncompatiblePath -Raw).Trim())}
$playlist=Join-Path $DataRoot 'playlist.txt'
$sessionPath=Join-Path $DataRoot 'state\session.json'
$queuePath=Join-Path $DataRoot 'state\queue-runtime.json'
$runtimePath=Join-Path $DataRoot 'state\runtime-instance.json'
$leasePath=Join-Path $DataRoot 'state\runtime-lease.json'
$watchdogPath=Join-Path $DataRoot 'state\watchdog-status.json'
$preflightPath=Join-Path $DataRoot 'state\preflight.json'
$orderPreviousPath=Join-Path $DataRoot 'state\session-order.previous.json'
$updateTransactionPath=Join-Path $DataRoot 'state\update-transaction.json'
$installStatusPath=Join-Path $DataRoot 'install-status.txt'
$configIncompatiblePath=Join-Path $DataRoot 'state\config-incompatible.txt'
$routeIntelligencePath=Join-Path $DataRoot 'cache\capabilities\video-route-memory.json'

$urls=@()
if(Test-Path $playlist){$urls=@(Get-Content $playlist|Where-Object{$_ -match '^https?://'})}
if($urls.Count -gt 0){Check PASS 'Playlist exists' "$($urls.Count) occurrences"}else{Check FAIL 'Playlist exists' 'No active playlist URLs'}

$session=$null
try{$session=Get-Content $sessionPath -Raw|ConvertFrom-Json;Check PASS 'Session parses' ([string]$session.session_id)}catch{Check WARN 'Session parses' 'Legacy/no session snapshot'}
if($session -and $contracts){
    if([int]$session.schema -le [int]$contracts.session_schema){Check PASS 'Session schema contract' ([string]$session.schema)}else{Check FAIL 'Session schema contract' "$($session.schema) > $($contracts.session_schema)"}
    if($null -ne $session.PSObject.Properties['cache_identity_schema'] -and [int]$session.cache_identity_schema -gt [int]$contracts.cache_identity_schema){Check FAIL 'Cache identity schema contract' "$($session.cache_identity_schema) > $($contracts.cache_identity_schema)"}else{Check PASS 'Cache identity schema contract'}
}
if($session){
    if([int]$session.count -eq $urls.Count){Check PASS 'Session count matches playlist'}else{Check FAIL 'Session count matches playlist' "session=$($session.count) playlist=$($urls.Count)"}
    $occ=@($session.occurrences)
    $keys=@($occ|ForEach-Object{[string]$_.source_key}|Where-Object{$_})
    $unique=@($keys|Sort-Object -Unique)
    $reuse=[Math]::Max(0,$occ.Count-$unique.Count)
    if([int]$session.schema -ge 2 -and $keys.Count -eq $occ.Count){
        Check PASS 'Media object identity' "$($unique.Count) unique source(s), $reuse duplicate occurrence(s) eligible for shared bytes"
    }else{
        Check WARN 'Media object identity' 'Legacy session; Shuffle once to populate SHA-256 source keys.'
    }
    $order=($urls -join "`n")
    $hash=Hash-Text $order
    if($hash -eq [string]$session.order_sha256){Check PASS 'Session order hash matches'}else{Check FAIL 'Session order hash matches' "expected $($session.order_sha256), found $hash"}
}

$runtime=$null;$queue=$null
try{$runtime=Get-Content $runtimePath -Raw|ConvertFrom-Json;Check PASS 'Runtime identity record' ([string]$runtime.runtime_id)}catch{Check WARN 'Runtime identity record' 'No active runtime record'}
if($runtime -and $contracts){
    if([int]$runtime.schema -le [int]$contracts.runtime_instance_schema){Check PASS 'Runtime-instance schema contract' ([string]$runtime.schema)}else{Check FAIL 'Runtime-instance schema contract' "$($runtime.schema) > $($contracts.runtime_instance_schema)"}
}
try{$queue=Get-Content $queuePath -Raw|ConvertFrom-Json;Check PASS 'Queue runtime parses' "schema $($queue.schema), revision $($queue.revision)"}catch{Check WARN 'Queue runtime parses' 'Engine stopped or no queue state yet'}
if($queue -and $contracts){
    if([int]$queue.schema -le [int]$contracts.queue_runtime_schema){Check PASS 'Queue runtime schema contract' ([string]$queue.schema)}else{Check FAIL 'Queue runtime schema contract' "$($queue.schema) > $($contracts.queue_runtime_schema)"}
    if($null -ne $queue.PSObject.Properties['protocol'] -and [int]$queue.protocol -ne [int]$contracts.browser_protocol){Check WARN 'Queue/browser protocol projection' "$($queue.protocol) != $($contracts.browser_protocol)"}else{Check PASS 'Queue/browser protocol projection'}
}
$lease=$null
try{$lease=Get-Content $leasePath -Raw|ConvertFrom-Json}catch{}
if($lease){
    if($contracts -and [int]$lease.schema -gt [int]$contracts.runtime_lease_schema){Check FAIL 'Runtime-lease schema contract' "$($lease.schema) > $($contracts.runtime_lease_schema)"}else{Check PASS 'Runtime-lease schema contract' ([string]$lease.schema)}
    $age=[Math]::Max(0,[DateTimeOffset]::UtcNow.ToUnixTimeSeconds()-[int64]$lease.unix)
    if($age -le 15){Check PASS 'Runtime lease freshness' "$age sec"}else{Check WARN 'Runtime lease freshness' "$age sec"}
    if($runtime -and [string]$lease.runtime_id -ne [string]$runtime.runtime_id){Check FAIL 'Runtime lease epoch' "lease=$($lease.runtime_id) runtime=$($runtime.runtime_id)"}else{Check PASS 'Runtime lease epoch'}
    if($session -and [string]$lease.session_id -ne [string]$session.session_id){Check FAIL 'Runtime lease session' "lease=$($lease.session_id) session=$($session.session_id)"}else{Check PASS 'Runtime lease session'}
}elseif($runtime){Check WARN 'Runtime lease' 'Runtime record exists but no engine lease is present'}

$watchdog=$null
try{$watchdog=Get-Content $watchdogPath -Raw|ConvertFrom-Json}catch{}
$preflight=$null
try{$preflight=Get-Content $preflightPath -Raw|ConvertFrom-Json}catch{}
if($preflight){
    if($contracts -and [int]$preflight.schema -gt [int]$contracts.preflight_schema){Check FAIL 'Preflight schema contract' "$($preflight.schema) > $($contracts.preflight_schema)"}else{Check PASS 'Preflight schema contract' ([string]$preflight.schema)}
    if([string]$preflight.verdict -eq 'GO'){Check PASS 'Last startup preflight' 'GO'}
    elseif([string]$preflight.verdict -eq 'WARN'){Check WARN 'Last startup preflight' "$($preflight.warnings) warning(s)"}
    else{Check FAIL 'Last startup preflight' "$($preflight.failures) blocking failure(s)"}
}

if($watchdog){
    if($contracts -and [int]$watchdog.schema -gt [int]$contracts.watchdog_status_schema){Check FAIL 'Watchdog schema contract' "$($watchdog.schema) > $($contracts.watchdog_status_schema)"}else{Check PASS 'Watchdog schema contract' ([string]$watchdog.schema)}
    if([string]$watchdog.state -eq 'healthy'){Check PASS 'Supervisor watchdog' "server restarts=$($watchdog.server_restarts)"}else{Check WARN 'Supervisor watchdog' "$($watchdog.state): $($watchdog.message)"}
    if($runtime -and [string]$watchdog.runtime_id -ne [string]$runtime.runtime_id){Check FAIL 'Watchdog runtime epoch' "watchdog=$($watchdog.runtime_id) runtime=$($runtime.runtime_id)"}else{Check PASS 'Watchdog runtime epoch'}
}elseif($runtime){Check WARN 'Supervisor watchdog' 'No watchdog projection yet'}

if($runtime -and $queue){
    if([string]$runtime.runtime_id -eq [string]$queue.runtime_id){Check PASS 'Runtime epoch matches queue'}else{Check FAIL 'Runtime epoch matches queue' "runtime=$($runtime.runtime_id) queue=$($queue.runtime_id)"}
    if([int]$queue.work_generation -ge 1){Check PASS 'Work generation' ([string]$queue.work_generation)}else{Check WARN 'Work generation' 'missing/invalid'}
}
if($runtime){
    if([bool]$runtime.safe_mode){Check WARN 'Safe Mode active' "runtime $($runtime.runtime_id)"}else{Check PASS 'Safe Mode inactive'}
    if([int]$runtime.rapid_failures -gt 0){Check WARN 'Rapid-start failure history' ([string]$runtime.rapid_failures)}
}
if($session -and $queue){
    if([string]$session.session_id -eq [string]$queue.session_id){Check PASS 'Session identity matches queue'}else{Check FAIL 'Session identity matches queue'}
}

$orderPath=Join-Path $DataRoot 'state\session-order.json'
if(Test-Path $orderPath){
    try{
        $orderState=Get-Content $orderPath -Raw|ConvertFrom-Json
        $orderValues=@($orderState.order|ForEach-Object{[int]$_})
        $unique=@($orderValues|Sort-Object -Unique)
        $inserted=@($orderState.inserted)
        $registryCount=$urls.Count+$inserted.Count
        $insertedValid=$true;$expected=$urls.Count+1
        foreach($entry in @($inserted|Sort-Object {[int]$_.occurrence_id})){
            if([int]$entry.occurrence_id -ne $expected -or [string]$entry.url -notmatch '^https?://' -or -not [string]$entry.source_key){$insertedValid=$false;break};$expected++
        }
        $range=if($orderValues.Count -gt 0){$orderValues|Measure-Object -Minimum -Maximum}else{$null}
        $orderValid=($session -and [string]$orderState.session_id -eq [string]$session.session_id -and $insertedValid -and $orderValues.Count -ge 1 -and $orderValues.Count -le $registryCount -and $unique.Count -eq $orderValues.Count -and $range.Minimum -ge 1 -and $range.Maximum -le $registryCount)
        if($orderValid){Check PASS 'Mutable occurrence order' "schema $($orderState.schema), revision $($orderState.revision), active $($orderValues.Count), registry $registryCount, inserted $($inserted.Count), undo $(@($orderState.undo).Count), redo $(@($orderState.redo).Count)"}
        else{Check FAIL 'Mutable occurrence order' 'Invalid active subset, generated registry or session identity'}
        if($contracts -and [int]$orderState.schema -gt [int]$contracts.occurrence_order_schema){Check FAIL 'Occurrence-order schema contract' "$($orderState.schema) > $($contracts.occurrence_order_schema)"}else{Check PASS 'Occurrence-order schema contract'}

        if(Test-Path $orderPreviousPath){
            try{
                $previousOrder=Get-Content $orderPreviousPath -Raw|ConvertFrom-Json
                if($session -and [string]$previousOrder.session_id -eq [string]$session.session_id){Check PASS 'Previous-good occurrence order' "revision $($previousOrder.revision)"}else{Check WARN 'Previous-good occurrence order' 'Different session identity'}
            }catch{Check WARN 'Previous-good occurrence order' 'Unreadable'}
        }

        $journal=Join-Path $DataRoot 'state\session-journal.jsonl'
        if(Test-Path $journal){
            try{$last=Get-Content $journal -Tail 1|ConvertFrom-Json;if([int]$last.revision -eq [int]$orderState.revision){Check PASS 'Mutation journal revision' ([string]$last.revision)}else{Check WARN 'Mutation journal revision' "snapshot=$($orderState.revision) journal=$($last.revision)"}}catch{Check WARN 'Mutation journal' 'Last record unreadable'}
        }else{Check WARN 'Mutation journal' 'No successful order mutation journal record yet'}
    }catch{Check FAIL 'Mutable occurrence order' $_.Exception.Message}
}else{Check PASS 'Mutable occurrence order' 'Original shuffle order active; no mutation sidecar yet'}

$resume=1;$resumePath=Join-Path $DataRoot 'state\resume-track.txt'
if(Test-Path $resumePath){[void][int]::TryParse((Get-Content $resumePath -Raw).Trim(),[ref]$resume)}
if($urls.Count -gt 0 -and $resume -ge 1 -and $resume -le $urls.Count){Check PASS 'Resume position valid' "$resume/$($urls.Count)"}else{Check FAIL 'Resume position valid' "$resume"}

foreach($pair in @(@('engine','engine.pid'),@('server','server.pid'),@('supervisor','supervisor.pid'))){
    $role=[string]$pair[0]
    $p=Join-Path $DataRoot ('state\'+$pair[1]);$n=0
    if(Test-Path $p){[void][int]::TryParse((Get-Content $p -Raw).Trim(),[ref]$n)}
    if($n -le 0){continue}
    $proc=Get-Process -Id $n -ErrorAction SilentlyContinue
    if(-not $proc){Check WARN ($role+' process provenance') "stale PID $n";continue}

    if($runtime -and [int]$runtime.schema -ge 4 -and $runtime.processes -and $runtime.processes.$role){
        $identity=$runtime.processes.$role
        $issues=New-Object System.Collections.Generic.List[string]
        if([int]$identity.pid -ne $n){$issues.Add("recorded PID $($identity.pid) != pid-file $n")}
        $actualPath='';try{$actualPath=[IO.Path]::GetFullPath([string]$proc.MainModule.FileName)}catch{}
        $expectedPath='';try{$expectedPath=[IO.Path]::GetFullPath([string]$identity.executable_path)}catch{}
        if(-not $actualPath -or -not $expectedPath -or -not [string]::Equals($actualPath,$expectedPath,[StringComparison]::OrdinalIgnoreCase)){$issues.Add('executable path mismatch')}
        try{
            $actualTicks=[int64]$proc.StartTime.ToUniversalTime().Ticks
            if([Math]::Abs($actualTicks-[int64]$identity.start_ticks_utc) -gt [TimeSpan]::TicksPerSecond){$issues.Add('process start identity mismatch')}
        }catch{$issues.Add('process start identity unreadable')}
        if($actualPath -and [string]$identity.executable_sha256){
            try{if((Get-FileHash $actualPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne ([string]$identity.executable_sha256).ToLowerInvariant()){$issues.Add('executable SHA-256 mismatch')}}catch{$issues.Add('executable SHA-256 unreadable')}
        }
        if($issues.Count -eq 0){Check PASS ($role+' process provenance') "PID $n exact path/start/hash verified"}
        else{Check FAIL ($role+' process provenance') ($issues -join '; ')}
    }else{
        Check WARN ($role+' process provenance') "PID $n is alive but runtime record predates Aegis schema 4"
    }
}

if($config -and [string]$config.app_mode -eq 'Streamer / OBS'){
    $port=[int]$config.server_port
    try{
        $health=Invoke-RestMethod -Uri ("http://127.0.0.1:$port/v1/health") -TimeoutSec 2
        if([int]$health.protocol -eq 1){Check PASS 'HTTP protocol health' "v1 runtime=$($health.runtime_id) clients=$($health.clients) clock=$($health.clock_connected) lease=$($health.engine_lease_age_ms)ms"}
        else{Check WARN 'HTTP protocol health' 'Unexpected protocol version'}
        if($runtime -and [string]$health.runtime_id -ne [string]$runtime.runtime_id){Check FAIL 'HTTP runtime identity' "http=$($health.runtime_id) runtime=$($runtime.runtime_id)"}else{Check PASS 'HTTP runtime identity'}
        try{
            $ready=Invoke-WebRequest -UseBasicParsing -Uri ("http://127.0.0.1:$port/v1/ready") -TimeoutSec 2
            if($ready.StatusCode -eq 200){Check PASS 'Engine readiness endpoint'}
        }catch{
            if($runtime){Check WARN 'Engine readiness endpoint' 'Server alive but data plane not ready'}else{Check PASS 'Engine readiness endpoint' 'YOMI is intentionally stopped'}
        }
        try{
            $metrics=Invoke-RestMethod -UseBasicParsing -Uri ("http://127.0.0.1:$port/v1/metrics") -TimeoutSec 2
            if([int]$metrics.schema -eq 1){Check PASS 'Aggregate metrics endpoint' "queue=$([bool]$metrics.queue) watchdog=$([bool]$metrics.watchdog)"}else{Check WARN 'Aggregate metrics endpoint' "unexpected schema $($metrics.schema)"}
        }catch{Check WARN 'Aggregate metrics endpoint' 'Not reachable'}
    }catch{Check WARN 'HTTP protocol health' 'Not reachable'}
    try{
        $clients=Invoke-RestMethod -Uri ("http://127.0.0.1:$port/clients") -TimeoutSec 2
        $ids=@($clients.clients|ForEach-Object{[string]$_.id})
        if($ids.Count -gt 0){Check PASS 'Active OBS/YOMI browser clients' ($ids -join ', ')}else{Check WARN 'Active OBS/YOMI browser clients' 'No /state client heartbeat in the last 30 seconds'}
        $obviousVideo=@($ids|Where-Object{$_ -match 'video'})
        if([bool]$config.video_enabled -and $ids -contains 'classic-overlay'){$obviousVideo+=@('classic-overlay')}
        $obviousVideo=@($obviousVideo|Select-Object -Unique)
        if($obviousVideo.Count -gt 1){Check WARN 'Multiple obvious Browser Source video decoders' ($obviousVideo -join ', ')}
    }catch{}
}

$objectRoot=Join-Path $DataRoot 'cache\objects'
$objectFiles=@(Get-ChildItem $objectRoot -Recurse -File -ErrorAction SilentlyContinue)
$objectBytes=($objectFiles|Measure-Object Length -Sum).Sum;if($null -eq $objectBytes){$objectBytes=0}
Check PASS 'Content-addressed object store' "$($objectFiles.Count) object file(s), $([Math]::Round([double]$objectBytes/1MB,1)) MB"

$partials=@(Get-ChildItem (Join-Path $DataRoot 'cache') -Recurse -File -ErrorAction SilentlyContinue|Where-Object{$_.Name -like '*.downloading*'})
if($partials.Count -eq 0){Check PASS 'No partial cache artifacts'}else{Check WARN 'Partial cache artifacts' "$($partials.Count) file(s)"}

try{
    $drive=[System.IO.DriveInfo]::new([System.IO.Path]::GetPathRoot($DataRoot))
    $free=[Math]::Round($drive.AvailableFreeSpace/1GB,1)
    if($free -ge 5){Check PASS 'Free disk space' "$free GB"}elseif($free -ge 1){Check WARN 'Free disk space' "$free GB"}else{Check FAIL 'Free disk space' "$free GB"}
}catch{}

if($queue){
    Check PASS 'Buffer health' ("$($queue.buffer_health), $($queue.ready_ahead)/$($queue.target_ahead), ready time $([Math]::Round([double]$queue.ready_time_seconds)) sec")
    if([bool]$queue.audio_circuit_open){Check WARN 'Audio infrastructure circuit' 'OPEN - cached continuity rescue / hold-and-retry policy active'}else{Check PASS 'Audio infrastructure circuit' 'closed'}
    if($queue.scheduler){Check PASS 'Scheduler policy' "$($queue.scheduler.strategy) | $($queue.scheduler.transition_policy) | SLA $($queue.scheduler.presentation_sla_seconds)s | $($queue.scheduler.source_demand_policy)"}
    if($queue.oracle){
        $oracleText="verdict=$($queue.oracle.verdict) min=$($queue.oracle.confidence_min)% avg=$($queue.oracle.confidence_average)% attention=$($queue.oracle.urgent_or_at_risk)"
        if([string]$queue.oracle.verdict -eq 'ATTENTION'){Check WARN 'Oracle forecast' $oracleText}else{Check PASS 'Oracle forecast' $oracleText}
    }
    $domain=[string]$queue.failure_domain
    if($domain -eq 'OK'){Check PASS 'Failure domain' 'OK'}elseif($domain){Check WARN 'Failure domain' $domain}
    if($queue.disk){
        $free=[double]$queue.disk.free_mb;$pressure=[string]$queue.disk.pressure
        if($pressure -in @('CRITICAL','LOW')){Check WARN 'Runtime disk pressure policy' "$pressure - $([Math]::Round($free)) MB free"}else{Check PASS 'Runtime disk pressure policy' "$pressure - $([Math]::Round($free)) MB free"}
    }
    if($queue.freeze -and [bool]$queue.freeze.active){Check PASS 'Broadcast Freeze' "$($queue.freeze.count) occurrence(s) protected"}else{Check PASS 'Broadcast Freeze' 'inactive'}
    Check PASS 'Video capability memory' "$([int]$queue.compatibility_memory_entries) persistent source identity record(s)"
    if($queue.route_intelligence){
        if($contracts -and [int]$queue.route_intelligence.schema -gt [int]$contracts.route_intelligence_schema){Check FAIL 'Route intelligence schema contract' "$($queue.route_intelligence.schema) > $($contracts.route_intelligence_schema)"}else{Check PASS 'Route intelligence schema contract' "schema $($queue.route_intelligence.schema), TTL $($queue.route_intelligence.bad_route_ttl_days)d / $($queue.route_intelligence.entry_ttl_days)d"}
    }
    if($queue.resource){
        if($contracts -and [int]$queue.resource.schema -gt [int]$contracts.resource_governor_schema){Check FAIL 'Resource governor schema contract' "$($queue.resource.schema) > $($contracts.resource_governor_schema)"}
        else{Check PASS 'Resource governor' "$($queue.resource.governor_mode) / $($queue.resource.governor_reason), workers $($queue.resource.effective_workers)/$($queue.resource.configured_workers)"}
    }
    if($queue.performance_memory){
        if($contracts -and [int]$queue.performance_memory.schema -gt [int]$contracts.performance_memory_schema){Check FAIL 'Performance memory schema contract' "$($queue.performance_memory.schema) > $($contracts.performance_memory_schema)"}
        else{Check PASS 'Machine calibration memory' "profiles=$($queue.performance_memory.profiles) audio=$([Math]::Round([double]$queue.performance_memory.audio_prior_seconds,2))s video=$([Math]::Round([double]$queue.performance_memory.video_prior_seconds,2))s"}
    }
    if($queue.rehearsal -and [bool]$queue.rehearsal.active){
        $r=$queue.rehearsal
        $detail="sync $($r.sync_ready)/$($r.target), presentation $($r.presentation_complete)/$($r.target)"
        $detail+="; verdict=$([string]$r.verdict)"
        if([string]$r.verdict -eq 'GO'){Check PASS 'Rehearsal window' $detail}
        elseif([string]$r.verdict -eq 'DEGRADED'){Check WARN 'Rehearsal window' $detail}
        else{Check WARN 'Rehearsal window' $detail}
    }
    $next=@($queue.items|Where-Object{[int]$_.relative -eq 1})|Select-Object -First 1
    if($next){
        $detail=("risk={0}, eta={1:n0}s, estimate={2:n1}s, slack={3:n1}s" -f $next.risk,[double]$next.eta_seconds,[double]$next.estimated_prepare_seconds,[double]$next.slack_seconds)
        if([string]$next.risk -in @('URGENT','AT_RISK')){Check WARN 'Next transition prediction' $detail}else{Check PASS 'Next transition prediction' $detail}
    }
}

if(Test-Path $updateTransactionPath){
    try{
        $tx=Get-Content $updateTransactionPath -Raw|ConvertFrom-Json
        if($contracts -and [int]$tx.schema -gt [int]$contracts.update_transaction_schema){Check FAIL 'Update-transaction schema contract' "$($tx.schema) > $($contracts.update_transaction_schema)"}else{Check PASS 'Update-transaction schema contract' ([string]$tx.schema)}
        if([string]$tx.phase -eq 'failed'){Check WARN 'Last update transaction' ([string]$tx.message)}else{Check PASS 'Last update transaction' ([string]$tx.phase)}
    }catch{Check WARN 'Last update transaction' 'Unreadable transaction record'}
}
if(Test-Path $installStatusPath){
    $installStatus=(Get-Content $installStatusPath -Raw).Trim()
    if($installStatus -match '^FAIL'){Check WARN 'Last install status' $installStatus}else{Check PASS 'Last install status' $installStatus}
}
if(Test-Path (Join-Path $PSScriptRoot 'support-bundle.ps1')){Check PASS 'Support bundle tool available'}else{Check WARN 'Support bundle tool available' 'missing'}
if(Test-Path (Join-Path $PSScriptRoot 'recovery-point.ps1')){
    $recoveryCount=@(Get-ChildItem (Join-Path $DataRoot 'recovery-points') -Filter 'YOMI-recovery-*.zip' -File -ErrorAction SilentlyContinue).Count
    Check PASS 'Recovery Point tool available' "$recoveryCount local recovery point(s)"
}else{Check WARN 'Recovery Point tool available' 'missing'}
if(Test-Path (Join-Path $PSScriptRoot 'incident-replay.ps1')){Check PASS 'Incident Replay tool available'}else{Check WARN 'Incident Replay tool available' 'missing'}
if(Test-Path (Join-Path $PSScriptRoot 'operations-center.ps1')){Check PASS 'Operations Center' 'local Aegis console available'}else{Check WARN 'Operations Center' 'tool missing'}
if(Test-Path (Join-Path $PSScriptRoot 'aegis-repair.ps1')){
    $repairState=Join-Path $DataRoot 'state\aegis-repair.json'
    if(Test-Path $repairState){
        try{
            $rr=Get-Content $repairState -Raw|ConvertFrom-Json
            if($contracts -and [int]$rr.schema -gt [int]$contracts.aegis_repair_schema){Check FAIL 'Aegis Safe Repair' "report schema $($rr.schema) > $($contracts.aegis_repair_schema)"}
            elseif([string]$rr.status -eq 'ATTENTION'){Check WARN 'Aegis Safe Repair' "latest=ATTENTION, actions=$(@($rr.actions).Count)"}
            else{Check PASS 'Aegis Safe Repair' "latest=$($rr.status), actions=$(@($rr.actions).Count)"}
        }catch{Check WARN 'Aegis Safe Repair' 'latest report unreadable'}
    }else{Check PASS 'Aegis Safe Repair' 'tool available; no repair report yet'}
}else{Check WARN 'Aegis Safe Repair' 'tool missing'}
if(Test-Path (Join-Path $PSScriptRoot 'oracle-lab.ps1')){
    $oracleReports=@(Get-ChildItem (Join-Path $DataRoot 'oracle-lab') -Filter 'oracle-*.json' -File -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending)
    if($oracleReports.Count -gt 0){
        try{$lastOracle=Get-Content $oracleReports[0].FullName -Raw|ConvertFrom-Json;if([string]$lastOracle.status -eq 'PASS'){Check PASS 'Oracle Lab' "$(@($lastOracle.scenarios).Count) scenario(s), deterministic=$($lastOracle.deterministic)"}else{Check WARN 'Oracle Lab' 'Latest simulation report contains a failed invariant'}}catch{Check WARN 'Oracle Lab' 'Latest report unreadable'}
    }else{Check PASS 'Oracle Lab' 'tool available; no local run yet'}
}else{Check WARN 'Oracle Lab' 'tool missing'}
if(Test-Path (Join-Path $PSScriptRoot 'config-history.ps1')){
    $configSnapshots=@(Get-ChildItem (Join-Path $DataRoot 'config-history') -Filter 'config-*.json' -File -ErrorAction SilentlyContinue).Count
    if($configSnapshots -le 8){Check PASS 'Config Time Machine' "$configSnapshots/8 snapshots"}else{Check WARN 'Config Time Machine' "$configSnapshots snapshots exceeds retention target 8"}
}else{Check WARN 'Config Time Machine' 'tool missing'}

$journalPath=Join-Path $DataRoot 'state\session-journal.jsonl'
$journalHeadPath=Join-Path $DataRoot 'state\session-journal-head.json'
if(Test-Path $journalPath){
    $records=@()
    foreach($line in @(Get-Content $journalPath -ErrorAction SilentlyContinue)){
        if([string]::IsNullOrWhiteSpace($line)){continue}
        try{$records+=($line|ConvertFrom-Json)}catch{Check FAIL 'Mutation ledger JSON' 'Unreadable journal record'}
    }
    $chain=@($records|Where-Object{[int]$_.schema -eq 2})
    $chainOk=$true;$previous=$null
    foreach($entry in $chain){
        $computed=Aegis-IdentityHash (Aegis-JournalPayload $entry)
        if($computed -ne [string]$entry.entry_hash){$chainOk=$false;break}
        if($previous -and ([int64]$entry.sequence -ne ([int64]$previous.sequence+1) -or [string]$entry.previous_hash -ne [string]$previous.entry_hash)){$chainOk=$false;break}
        $previous=$entry
    }
    if($chain.Count -gt 0){
        if($chainOk){Check PASS 'Mutation ledger chain' "$($chain.Count) retained chained record(s), tail seq $($chain[-1].sequence)"}
        else{Check FAIL 'Mutation ledger chain' 'Hash/sequence/previous-hash invariant failed'}
    }elseif($records.Count -gt 0){Check WARN 'Mutation ledger chain' "$($records.Count) legacy unchained record(s)"}

    if((Test-Path $journalHeadPath) -and $chain.Count -gt 0){
        try{
            $head=Get-Content $journalHeadPath -Raw|ConvertFrom-Json
            $tail=$chain[-1]
            if([int64]$head.sequence -gt [int64]$tail.sequence){Check FAIL 'Mutation ledger head' "head $($head.sequence) is ahead of tail $($tail.sequence)"}
            elseif([int64]$head.sequence -lt [int64]$tail.sequence){Check WARN 'Mutation ledger head' "head $($head.sequence) lags tail $($tail.sequence); crash-lag compatible"}
            elseif([string]$head.entry_hash -ne [string]$tail.entry_hash){Check FAIL 'Mutation ledger head' 'head/tail hash mismatch'}
            else{Check PASS 'Mutation ledger head' "seq $($head.sequence) matches durable tail"}

            if($orderState -and [string]$orderState.session_id -eq [string]$tail.session_id){
                $liveOrderHash=Aegis-IdentityHash ((@($orderState.order|ForEach-Object{[string][int]$_}) -join ','))
                if([int64]$orderState.revision -ne [int64]$tail.revision){Check FAIL 'Mutation ledger vs order' "revision order=$($orderState.revision) ledger=$($tail.revision)"}
                elseif($liveOrderHash -ne [string]$tail.order_hash){Check FAIL 'Mutation ledger vs order' "order hash $liveOrderHash != ledger $($tail.order_hash)"}
                else{Check PASS 'Mutation ledger vs order' "revision $($tail.revision) / order hash agree"}
            }
        }catch{Check WARN 'Mutation ledger head' 'unreadable'}
    }
}

$out.Add('')
$out.Add('===== FLIGHT RECORDER - LAST 20 EVENTS =====')
$events=Join-Path $DataRoot 'state\events.jsonl'
if(Test-Path $events){Get-Content $events -Tail 20|ForEach-Object{$out.Add($_)}}else{$out.Add('(no events yet)')}

$out.Add('')
$out.Add("SUMMARY: FAIL=$fail  WARN=$warn")
$text=$out -join "`r`n"
if($env:YOMI_DOCTOR_NO_CLIPBOARD -ne '1'){try{Set-Clipboard -Value $text}catch{}}
$text
Write-Host ''
Write-Host 'Doctor report copied to clipboard.' -ForegroundColor Green