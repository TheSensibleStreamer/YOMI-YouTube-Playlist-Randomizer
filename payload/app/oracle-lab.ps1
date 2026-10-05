param(
    [ValidateSet('All','HealthyMixed','ShortClips','LongPerformances','SlowPresentation','NetworkOutage','DuplicateCapability','DiskPressure','GenerationJump')]
    [string]$Scenario='All',
    [int]$Seed=42012,
    [switch]$CI,
    [switch]$Quiet,
    [string]$OutputPath=''
)

$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

function Track {
    param(
        [string]$Source,[double]$Duration,[double]$Audio,[double]$Art,[double]$Video,[double]$Viz,
        [bool]$VideoUnavailable=$false,[bool]$CachedAudio=$false
    )
    [PSCustomObject][ordered]@{
        source=$Source;duration=$Duration;audio=$Audio;art=$Art;video=$Video;viz=$Viz;
        video_unavailable=$VideoUnavailable;cached_audio=$CachedAudio
    }
}
function Scenario-Definition([string]$Name){
    switch($Name){
        'HealthyMixed' {
            return [ordered]@{
                name=$Name;workers=2;prefetch=6;buffer_target=900;transition='Synchronized presentation';sla=3;
                tracks=@(
                    (Track A 240 4 1 7 4),(Track B 180 5 1 6 4),(Track C 420 5 2 8 5),
                    (Track D 210 4 1 7 4),(Track E 360 5 2 9 5),(Track F 195 4 1 6 4)
                )
            }
        }
        'ShortClips' {
            $tracks=@()
            for($i=1;$i -le 30;$i++){$tracks+=Track ("S$i") (18+($i%5)*4) 4 1 6 3}
            return [ordered]@{name=$Name;workers=1;prefetch=8;buffer_target=240;transition='Continuity first';sla=1;tracks=$tracks}
        }
        'LongPerformances' {
            $tracks=@()
            for($i=1;$i -le 12;$i++){$tracks+=Track ("L$i") (720+($i%3)*180) 5 2 8 5}
            return [ordered]@{name=$Name;workers=4;prefetch=20;buffer_target=900;transition='Synchronized presentation';sla=3;tracks=$tracks}
        }
        'SlowPresentation' {
            return [ordered]@{
                name=$Name;workers=2;prefetch=4;buffer_target=600;transition='Synchronized presentation';sla=3;
                tracks=@(
                    (Track P1 5 1 1 1 1),(Track P2 60 1 1 20 14),(Track P3 60 1 1 18 12),(Track P4 60 1 1 1 1)
                )
            }
        }
        'NetworkOutage' {
            return [ordered]@{
                name=$Name;workers=2;prefetch=6;buffer_target=480;transition='Continuity first';sla=1;
                outages=@(@{start=45.0;end=125.0});
                tracks=@(
                    (Track N1 55 3 1 5 3 $false $true),
                    (Track N2 55 3 1 5 3 $false $true),
                    (Track N3 55 5 1 6 3),
                    (Track N4 55 5 1 6 3),
                    (Track N5 55 5 1 6 3)
                )
            }
        }
        'DuplicateCapability' {
            return [ordered]@{
                name=$Name;workers=1;prefetch=2;buffer_target=180;transition='Synchronized presentation';sla=3;
                tracks=@(
                    (Track START 80 2 1 3 2),
                    (Track DUP 80 2 1 3 2 $true),
                    (Track X 80 2 1 3 2),
                    (Track Y 80 2 1 3 2),
                    (Track DUP 80 2 1 3 2 $true),
                    (Track Z 80 2 1 3 2)
                )
            }
        }
        'DiskPressure' {
            return [ordered]@{
                name=$Name;workers=2;prefetch=5;buffer_target=400;transition='Continuity first';sla=1;suppress_optional=$true;
                tracks=@(
                    (Track D1 70 3 1 6 4),(Track D2 70 3 1 6 4),(Track D3 70 3 1 6 4),
                    (Track D4 70 3 1 6 4),(Track D5 70 3 1 6 4)
                )
            }
        }
        'GenerationJump' {
            return [ordered]@{
                name=$Name;workers=3;prefetch=8;buffer_target=600;transition='Synchronized presentation';sla=3;
                jump_after_transition=1;jump_to=7;
                tracks=@(
                    (Track J1 20 3 1 20 12),(Track J2 20 3 1 20 12),(Track J3 20 3 1 20 12),
                    (Track J4 20 3 1 20 12),(Track J5 20 3 1 20 12),(Track J6 20 3 1 20 12),
                    (Track J7 20 3 1 20 12),(Track J8 20 3 1 20 12),(Track J9 20 3 1 20 12)
                )
            }
        }
        default {throw "Unknown Oracle Lab scenario: $Name"}
    }
}
function Add-OutageDelay([double]$Start,[double]$Duration,$Outages){
    $finish=$Start+$Duration
    foreach($o in @($Outages)){
        $os=[double]$o.start;$oe=[double]$o.end
        if($finish -le $os -or $Start -ge $oe){continue}
        if($Start -lt $os){$finish += [Math]::Max(0,$oe-$os)}
        else{$finish += [Math]::Max(0,$oe-$Start)}
    }
    return $finish
}
function Invoke-OracleScenario($Definition){
    $tracks=@($Definition.tracks)
    $workers=[Math]::Max(1,[int]$Definition.workers)
    $prefetch=[Math]::Max(1,[int]$Definition.prefetch)
    $bufferTarget=[Math]::Max(1,[double]$Definition.buffer_target)
    $transition=[string]$Definition.transition
    $sla=[Math]::Max(1,[double]$Definition.sla)
    $outages=@($Definition.outages)
    $suppressOptional=[bool]$Definition.suppress_optional

    $state=[ordered]@{
        time=0.0;generation=1;current=0;transition_count=0;gap_total=0.0;gap_max=0.0;
        presentation_wait_max=0.0;presentation_yields=0;max_workers=0;stale_completions=0;
        jobs_started=0;jobs_completed=0;optional_jobs=0;audio_jobs=0;format_fail_attempts=0;
        capability_skips=0;max_horizon=0
    }
    $ready=@{}
    $queued=New-Object System.Collections.ArrayList
    $active=New-Object System.Collections.ArrayList
    $capabilityBad=@{}
    $scheduledGeneration=@{}

    function Ready-Key([int]$Index,[string]$Kind){return "$Index|$Kind"}
    function Timing-Jitter([int]$Index,[string]$Kind){
        $text="$Seed|$($Definition.name)|$Index|$Kind"
        [int64]$h=17
        foreach($ch in $text.ToCharArray()){$h=(($h*131)+[int]$ch)%10007}
        return 0.90+(($h%2001)/10000.0)
    }
    function Is-Ready([int]$Index,[string]$Kind){return $ready.ContainsKey((Ready-Key $Index $Kind))}
    function Presentation-Ready([int]$Index){
        if($suppressOptional){return $true}
        return (Is-Ready $Index 'art') -and (Is-Ready $Index 'video') -and (Is-Ready $Index 'viz')
    }
    function Job-Duration([int]$Index,[string]$Kind){
        $t=$tracks[$Index]
        $j=Timing-Jitter $Index $Kind
        switch($Kind){
            'audio' {if([bool]$t.cached_audio){return 0.0};return ([double]$t.audio)*$j}
            'art' {return ([double]$t.art)*$j}
            'viz' {return ([double]$t.viz)*$j}
            'video' {
                if([bool]$t.video_unavailable){
                    if($capabilityBad.ContainsKey([string]$t.source)){
                        $state.capability_skips++
                        return ([double]$t.video)*$j
                    }
                    return 4.0+(([double]$t.video)*$j)
                }
                return ([double]$t.video)*$j
            }
        }
        return 0.0
    }
    function Queue-Track([int]$Index,[int]$Priority,[int]$Generation){
        if($Index -lt 0 -or $Index -ge $tracks.Count){return}
        if($scheduledGeneration.ContainsKey($Index) -and [int]$scheduledGeneration[$Index] -eq $Generation){return}
        $scheduledGeneration[$Index]=$Generation
        $kinds=@('audio')
        if(-not $suppressOptional){$kinds+=@('art','video','viz')}
        foreach($kind in $kinds){
            $key=Ready-Key $Index $kind
            if($ready.ContainsKey($key)){continue}
            $p=$Priority
            if($kind -eq 'audio'){$p-=20}else{$p+=5}
            [void]$queued.Add([PSCustomObject]@{
                index=$Index;kind=$kind;priority=$p;generation=$Generation;serial=$state.jobs_started+$queued.Count+1
            })
        }
    }
    function Schedule-Window([int]$Base){
        $covered=[double]$tracks[$Base].duration
        $count=0
        for($i=$Base+1;$i -lt $tracks.Count -and $count -lt $prefetch;$i++){
            $count++
            Queue-Track $i (10*$count) $state.generation
            $covered += [double]$tracks[$i].duration
            if($count -ge 2 -and $covered -ge $bufferTarget){break}
        }
        $state.max_horizon=[Math]::Max($state.max_horizon,$count)
    }
    function Fill-Workers {
        while($active.Count -lt $workers -and $queued.Count -gt 0){
            $next=@($queued|Sort-Object priority,serial|Select-Object -First 1)[0]
            [void]$queued.Remove($next)
            $duration=Job-Duration $next.index $next.kind
            $network=($next.kind -in @('audio','art','video'))
            $finish=if($network){Add-OutageDelay $state.time $duration $outages}else{$state.time+$duration}
            $job=[PSCustomObject]@{
                index=$next.index;kind=$next.kind;priority=$next.priority;generation=$next.generation;
                start=$state.time;finish=$finish
            }
            [void]$active.Add($job)
            $state.jobs_started++
            if($next.kind -eq 'audio'){$state.audio_jobs++}else{$state.optional_jobs++}
            $state.max_workers=[Math]::Max($state.max_workers,$active.Count)
        }
    }
    function Complete-Due {
        $done=@($active|Where-Object{[double]$_.finish -le $state.time+0.000001})
        foreach($job in $done){
            [void]$active.Remove($job)
            $state.jobs_completed++
            if([int]$job.generation -ne [int]$state.generation){
                $state.stale_completions++
                continue
            }
            $t=$tracks[[int]$job.index]
            if($job.kind -eq 'video' -and [bool]$t.video_unavailable -and -not $capabilityBad.ContainsKey([string]$t.source)){
                $capabilityBad[[string]$t.source]=$true
                $state.format_fail_attempts++
            }
            $ready[(Ready-Key ([int]$job.index) ([string]$job.kind))]=$state.time
        }
    }
    function Next-Completion {
        if($active.Count -eq 0){return [double]::PositiveInfinity}
        return [double](@($active|Sort-Object finish|Select-Object -First 1)[0].finish)
    }
    function Run-Until([double]$Target,[scriptblock]$StopWhen=$null){
        while($state.time -lt $Target-0.000001){
            Fill-Workers
            if($StopWhen -and (& $StopWhen)){break}
            $next=Next-Completion
            if([double]::IsPositiveInfinity($next)){$state.time=$Target;break}
            $state.time=[Math]::Min($Target,$next)
            Complete-Due
            if($StopWhen -and (& $StopWhen)){break}
        }
    }

    # Current track is already playing. Schedule its future neighborhood.
    $state.current=0
    $ready[(Ready-Key 0 'audio')]=0.0
    $ready[(Ready-Key 0 'art')]=0.0;$ready[(Ready-Key 0 'video')]=0.0;$ready[(Ready-Key 0 'viz')]=0.0
    Schedule-Window 0
    $playEnd=[double]$tracks[0].duration

    while($state.current -lt $tracks.Count-1){
        Run-Until $playEnd
        $target=$state.current+1

        if($null -ne $Definition.jump_after_transition -and
            $state.transition_count -eq [int]$Definition.jump_after_transition){
            $target=[Math]::Min($tracks.Count-1,[int]$Definition.jump_to-1)
            $state.generation++
            $queued.Clear()
            $scheduledGeneration=@{}
            Queue-Track $target 0 $state.generation
            Schedule-Window $target
        }

        # Audio continuity wait.
        if(-not(Is-Ready $target 'audio')){
            $gapStart=$state.time
            while(-not(Is-Ready $target 'audio')){
                Fill-Workers
                $next=Next-Completion
                if([double]::IsPositiveInfinity($next)){throw "Oracle simulator deadlocked waiting for audio track $target."}
                $state.time=$next
                Complete-Due
            }
            $gap=$state.time-$gapStart
            $state.gap_total+=$gap
            $state.gap_max=[Math]::Max($state.gap_max,$gap)
        }

        # Optional presentation may add at most the SLA in synchronized mode.
        if($transition -eq 'Synchronized presentation' -and -not(Presentation-Ready $target)){
            $waitStart=$state.time
            $deadline=$waitStart+$sla
            while($state.time -lt $deadline-0.000001 -and -not(Presentation-Ready $target)){
                Fill-Workers
                $next=Next-Completion
                if([double]::IsPositiveInfinity($next)){$state.time=$deadline;break}
                $state.time=[Math]::Min($deadline,$next)
                Complete-Due
            }
            $wait=$state.time-$waitStart
            $state.presentation_wait_max=[Math]::Max($state.presentation_wait_max,$wait)
            if(-not(Presentation-Ready $target)){$state.presentation_yields++}
        }

        $state.current=$target
        $state.transition_count++
        $playEnd=$state.time+[double]$tracks[$target].duration
        Schedule-Window $target
    }

    # Drain enough work to make capability-memory accounting deterministic.
    $drainGuard=0
    while(($active.Count -gt 0 -or $queued.Count -gt 0) -and $drainGuard -lt 10000){
        $drainGuard++
        Fill-Workers
        $next=Next-Completion
        if([double]::IsPositiveInfinity($next)){break}
        $state.time=$next
        Complete-Due
    }

    $violations=New-Object System.Collections.Generic.List[string]
    if($state.max_workers -gt $workers){$violations.Add("worker ceiling exceeded: $($state.max_workers) > $workers")}
    if($state.max_horizon -gt $prefetch){$violations.Add("prefetch horizon exceeded: $($state.max_horizon) > $prefetch")}
    if($transition -eq 'Synchronized presentation' -and $state.presentation_wait_max -gt $sla+0.001){
        $violations.Add("presentation SLA exceeded: $($state.presentation_wait_max) > $sla")
    }
    if($transition -eq 'Continuity first' -and $state.presentation_wait_max -gt 0.001){
        $violations.Add('Continuity First waited for optional presentation.')
    }
    if($suppressOptional -and $state.optional_jobs -ne 0){$violations.Add("disk/resource suppression still launched $($state.optional_jobs) optional jobs")}
    if($Definition.name -eq 'DuplicateCapability'){
        if($state.format_fail_attempts -ne 1){$violations.Add("duplicate capability scenario expected one format discovery, got $($state.format_fail_attempts)")}
        if($state.capability_skips -lt 1){$violations.Add('duplicate capability memory never skipped the proven-bad route')}
    }
    if($Definition.name -eq 'SlowPresentation'){
        if($state.presentation_yields -lt 1){$violations.Add('slow presentation scenario never exercised the SLA yield path')}
        if($state.presentation_wait_max -lt [Math]::Max(0.5,$sla-0.1)){$violations.Add('slow presentation scenario did not approach the configured SLA boundary')}
    }
    if($Definition.name -eq 'LongPerformances' -and $state.max_horizon -gt 2){$violations.Add("adaptive time target over-prefetched long performances: horizon $($state.max_horizon)")}
    if($Definition.name -eq 'DiskPressure' -and $state.optional_jobs -ne 0){$violations.Add('disk-pressure scenario launched optional presentation work')}
    if($Definition.name -eq 'GenerationJump' -and $state.stale_completions -lt 1){$violations.Add('jump scenario produced no stale completion to prove generation isolation')}
    if($state.jobs_started -lt $state.jobs_completed){$violations.Add('completed job count exceeds started job count')}

    return [PSCustomObject][ordered]@{
        schema=1;scenario=$Definition.name;seed=$Seed;workers=$workers;prefetch=$prefetch;
        transition_policy=$transition;presentation_sla_seconds=$sla;
        transitions=$state.transition_count;simulated_seconds=[Math]::Round($state.time,3);
        audio_gap_total_seconds=[Math]::Round($state.gap_total,3);
        audio_gap_max_seconds=[Math]::Round($state.gap_max,3);
        presentation_wait_max_seconds=[Math]::Round($state.presentation_wait_max,3);
        presentation_sla_yields=$state.presentation_yields;
        jobs_started=$state.jobs_started;jobs_completed=$state.jobs_completed;
        audio_jobs=$state.audio_jobs;optional_jobs=$state.optional_jobs;
        max_workers=$state.max_workers;max_prefetch_horizon=$state.max_horizon;
        stale_job_completions=$state.stale_completions;
        format_discoveries=$state.format_fail_attempts;capability_skips=$state.capability_skips;
        status=$(if($violations.Count -eq 0){'PASS'}else{'FAIL'});
        violations=$violations.ToArray()
    }
}

$names=if($Scenario -eq 'All'){
    @('HealthyMixed','ShortClips','LongPerformances','SlowPresentation','NetworkOutage','DuplicateCapability','DiskPressure','GenerationJump')
}else{@($Scenario)}

$results=@()
foreach($name in $names){$results+=Invoke-OracleScenario (Scenario-Definition $name)}

# Determinism contract: re-run the same definitions and compare canonical JSON.
$repeat=@()
foreach($name in $names){$repeat+=Invoke-OracleScenario (Scenario-Definition $name)}
$canonical1=$results|ConvertTo-Json -Depth 8 -Compress
$canonical2=$repeat|ConvertTo-Json -Depth 8 -Compress
$deterministic=($canonical1 -ceq $canonical2)

$failed=@($results|Where-Object{$_.status -ne 'PASS'})
$report=[ordered]@{
    schema=1;product='YOMI';lab='Oracle deterministic scheduler/fault laboratory';
    generated_utc=[DateTime]::UtcNow.ToString('o');seed=$Seed;deterministic=$deterministic;
    scenarios=@($results);status=if($failed.Count -eq 0 -and $deterministic){'PASS'}else{'FAIL'}
}

if([string]::IsNullOrWhiteSpace($OutputPath)){
    $root=Join-Path $DataRoot 'oracle-lab'
    New-Item -ItemType Directory -Path $root -Force|Out-Null
    $OutputPath=Join-Path $root ('oracle-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json')
}
$parent=Split-Path $OutputPath -Parent
if($parent){New-Item -ItemType Directory -Path $parent -Force|Out-Null}
[IO.File]::WriteAllText($OutputPath,($report|ConvertTo-Json -Depth 10),(New-Object System.Text.UTF8Encoding($false)))

if(-not $Quiet){
    Write-Host '===== YOMI ORACLE LAB =====' -ForegroundColor Cyan
    foreach($r in $results){
        $color=if($r.status -eq 'PASS'){'Green'}else{'Red'}
        Write-Host ("{0,-20} {1,-4} gap max {2,6:N2}s  SLA wait {3,5:N2}s  workers {4}/{5}  stale {6}  capability skips {7}" -f
            $r.scenario,$r.status,$r.audio_gap_max_seconds,$r.presentation_wait_max_seconds,$r.max_workers,$r.workers,$r.stale_job_completions,$r.capability_skips) -ForegroundColor $color
        foreach($v in @($r.violations)){Write-Host ("    "+$v) -ForegroundColor Red}
    }
    Write-Host "Deterministic replay: $deterministic"
    Write-Host "Report: $OutputPath"
}

if(-not $deterministic){Write-Error 'Oracle Lab produced different results for the same deterministic scenario definitions.'}
if($failed.Count -gt 0 -or -not $deterministic){exit 1}
exit 0
