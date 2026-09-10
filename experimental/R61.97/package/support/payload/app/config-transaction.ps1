param(
    [switch]$ShowState,
    [switch]$DiscardPending,
    [switch]$RollbackLast,
    [switch]$ApplyPendingAtStartup,
    [switch]$CopyReport,
    [switch]$OpenReport
)

$ErrorActionPreference='Stop'
if(-not (Get-Command Initialize-YomiData -ErrorAction SilentlyContinue)){
    . (Join-Path $PSScriptRoot 'common.ps1')
}
Initialize-YomiData

function ConvertTo-YomiConfigJson($Value){
    return ($Value | ConvertTo-Json -Depth 20 -Compress)
}
function Get-YomiSha256Text([string]$Text){
    $sha=[Security.Cryptography.SHA256]::Create()
    try{
        $bytes=[Text.Encoding]::UTF8.GetBytes($Text)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()
    }finally{$sha.Dispose()}
}
function Get-YomiFileSha256([string]$Path){
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){return 'missing'}
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Write-YomiAtomicJson([string]$Path,$Value){
    $dir=Split-Path $Path -Parent
    if($dir){New-Item -ItemType Directory -Path $dir -Force|Out-Null}
    $json=$Value|ConvertTo-Json -Depth 24
    $tmp=$Path+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
    $backup=$Path+'.replace-backup'
    Write-YomiUtf8NoBom -Path $tmp -Text $json
    try{
        if(Test-Path -LiteralPath $Path){
            Remove-Item -LiteralPath $backup -Force -ErrorAction SilentlyContinue
            [IO.File]::Replace($tmp,$Path,$backup,$true)
            Remove-Item -LiteralPath $backup -Force -ErrorAction SilentlyContinue
        }else{[IO.File]::Move($tmp,$Path)}
    }finally{Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue}
}
function Read-YomiJsonSafe([string]$Path){
    if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){return $null}
    try{return (Get-Content -LiteralPath $Path -Raw -Encoding UTF8|ConvertFrom-Json)}catch{return $null}
}
function Test-YomiTransactionRuntimeActive{
    $state=Join-Path $DataRoot 'state'
    foreach($name in @('engine.pid','supervisor.pid')){
        $p=Join-Path $state $name
        if(-not(Test-Path -LiteralPath $p)){continue}
        $n=0
        try{[void][int]::TryParse((Get-Content -LiteralPath $p -Raw).Trim(),[ref]$n)}catch{}
        if($n -gt 0 -and $n -ne $PID -and (Get-Process -Id $n -ErrorAction SilentlyContinue)){return $true}
    }
    return $false
}
function Get-YomiComparableValue($Value){
    if($null -eq $Value){return '<null>'}
    return (ConvertTo-YomiConfigJson $Value)
}
function Get-YomiConfigClass([string]$Name){
    $n=$Name.ToLowerInvariant()
    if($n -in @('version','general_preset','overlay_preset','style_preset','visualizer_preset','performance_preset','director_preset','sources_preset','outputs_preset','composition_preset','media_size_preset','config_change_policy')){return 'METADATA_HOT'}
    if($n -in @('overlay_width','overlay_height','overlay_fps')){return 'DERIVED_HOT'}
    if($n -eq 'playlist'){return 'PLAYLIST_RESTART'}
    if($n -in @('media_width','media_height','smart_artwork_crop')){return 'ARTWORK_REBUILD_RESTART'}
    if($n -in @('overlay_video_quality','video_preference','video_fps')){return 'VIDEO_REBUILD_RESTART'}
    if($n -in @('audio_quality','audio_preference','loudness_normalization')){return 'AUDIO_REBUILD_RESTART'}
    if($n -in @('visualizer_internal_width','visualizer_internal_height','visualizer_activity','visualizer_frequency_scale','visualizer_fps','visualizer_high_frequency_lift_db')){return 'VISUALIZER_REBUILD_RESTART'}
    if($n -in @('browser_fps_mode','canvas_width','canvas_height','canvas_preset')){return 'OBS_RECONFIG'}
    if($n -in @('video_zoom','overlay_text_gap_px','overlay_safe_margin_px','overlay_auto_fit_text','overlay_min_text_size')){return 'BROWSER_HOT'}
    if($n -in @('title_enabled','channel_enabled')){return 'BROWSER_HOT'}
    if($n -like 'text_*' -or $n -like 'composition_*' -or $n -like 'media_border_*' -or $n -eq 'media_corner_style' -or $n -eq 'title_channel_spacing' -or $n -eq 'corner'){return 'BROWSER_HOT'}
    if($n -in @('visualizer_opacity','visualizer_length_multiplier','visualizer_color_mode','visualizer_solid_color','visualizer_gradient_preset','visualizer_gradient_orientation','visualizer_direction','visualizer_layer','visualizer_shape','visualizer_vertical_anchor','visualizer_bar_spacing','visualizer_peak_glow','visualizer_high_frequency_trim','visualizer_adaptive_fill')){return 'BROWSER_HOT'}
    if($n -in @('director_theme','director_timeline','director_motion','director_palette','director_visualizer_shape','stats_detail')){return 'BROWSER_HOT'}
    if($n -in @('featured_comment_enabled','comment_max_chars','comment_filter_mode')){return 'COMMENTS_REBUILD_RESTART'}
    if($n -in @('app_mode','player_video_quality','artwork_enabled','video_enabled','visualizer_enabled','director_mode','director_outputs','director_fixed_sources','telemetry_enabled','telemetry_probe_enabled','history_enabled','history_max_entries','server_port','cache_workers','prefetch_ahead','video_prefetch_ahead','video_cache_limit_mb','cache_priority','scheduler_strategy','transition_policy','presentation_sla_seconds','predictive_control_mode','media_admission_policy','media_fault_containment','cache_coherence_policy','performance_attribution_mode','slo_policy','slo_handoff_p95_ms','slo_regression_percent','slo_anomaly_budget_percent','source_demand_policy','buffer_target_minutes')){return 'ENGINE_RESTART'}
    return 'ENGINE_RESTART'
}
function Get-YomiCacheFootprint([string[]]$Scopes){
    $rows=@();$seen=@{}
    foreach($scope in @($Scopes)){
        if([string]::IsNullOrWhiteSpace($scope) -or $seen.ContainsKey($scope)){continue};$seen[$scope]=$true
        $dirs=@()
        switch($scope){
            'audio'{$dirs=@('audio','meta','gain')}
            'artwork'{$dirs=@('artwork')}
            'video'{$dirs=@('video')}
            'visualizer'{$dirs=@('visualizer')}
            'comments'{$dirs=@('comments')}
        }
        $count=0L;$bytes=0L
        foreach($d in $dirs){
            $path=Join-Path $DataRoot ('cache\'+$d)
            foreach($f in @(Get-ChildItem -LiteralPath $path -File -Force -ErrorAction SilentlyContinue)){$count++;$bytes+=[long]$f.Length}
        }
        $rows+=[pscustomobject]@{scope=$scope;objects=$count;bytes=$bytes}
    }
    return @($rows)
}
function Get-YomiConfigChangePlan($Old,$New){
    $names=@(@($Old.PSObject.Properties|ForEach-Object{[string]$_.Name})+@($New.PSObject.Properties|ForEach-Object{[string]$_.Name})|Sort-Object -Unique)
    $changes=@();$scopes=@()
    $requiresRestart=$false;$hotOnly=$true;$obsReconfig=$false;$playlistChanged=$false;$rank=0
    foreach($name in @($names|Sort-Object)){
        $op=$Old.PSObject.Properties[$name];$np=$New.PSObject.Properties[$name]
        $ov=if($op){Get-YomiComparableValue $op.Value}else{'<missing>'}
        $nv=if($np){Get-YomiComparableValue $np.Value}else{'<missing>'}
        if($ov -eq $nv){continue}
        $class=Get-YomiConfigClass $name
        $localRank=0
        switch($class){
            'BROWSER_HOT'{$localRank=0}
            'METADATA_HOT'{$localRank=0}
            'DERIVED_HOT'{$localRank=0}
            'OBS_RECONFIG'{$localRank=1;$obsReconfig=$true}
            'ARTWORK_REBUILD_RESTART'{$localRank=3;$requiresRestart=$true;$hotOnly=$false;$scopes+='artwork'}
            'VIDEO_REBUILD_RESTART'{$localRank=3;$requiresRestart=$true;$hotOnly=$false;$scopes+='video'}
            'AUDIO_REBUILD_RESTART'{$localRank=4;$requiresRestart=$true;$hotOnly=$false;$scopes+='audio';$scopes+='visualizer'}
            'VISUALIZER_REBUILD_RESTART'{$localRank=3;$requiresRestart=$true;$hotOnly=$false;$scopes+='visualizer'}
            'COMMENTS_REBUILD_RESTART'{$localRank=2;$requiresRestart=$true;$hotOnly=$false;$scopes+='comments'}
            'PLAYLIST_RESTART'{$localRank=4;$requiresRestart=$true;$hotOnly=$false;$playlistChanged=$true;foreach($s in @('audio','artwork','video','visualizer','comments')){$scopes+=$s}}
            default{$localRank=2;$requiresRestart=$true;$hotOnly=$false}
        }
        if($localRank -gt $rank){$rank=$localRank}
        $changes+=[pscustomobject]@{field=$name;class=$class;old=$ov;new=$nv}
    }
    $scopeList=@($scopes|Sort-Object -Unique)
    $footprint=Get-YomiCacheFootprint $scopeList
    $objects=0L;$bytes=0L;foreach($r in $footprint){$objects+=[long]$r.objects;$bytes+=[long]$r.bytes}
    $activation=if($changes.Count -eq 0){'NOOP'}elseif($requiresRestart){'CONTROLLED_RESTART'}elseif($obsReconfig){'HOT_PLUS_OBS_RECONFIG'}else{'HOT'}
    $risk=switch($rank){0{'LOW'}1{'LOW'}2{'MEDIUM'}3{'MEDIUM'}default{'HIGH'}}
    [pscustomobject]@{
        schema=1;created_utc=[DateTime]::UtcNow.ToString('o');changed_count=$changes.Count;changes=@($changes)
        requires_restart=$requiresRestart;hot_only=$hotOnly;obs_reconfig=$obsReconfig;playlist_changed=$playlistChanged
        cache_scopes=$scopeList;cache_footprint=@($footprint);affected_cache_objects=$objects;affected_cache_bytes=$bytes
        activation=$activation;risk=$risk;impact_rank=$rank
    }
}
function Add-YomiTransactionJournal($Record){
    $path=Join-Path $DataRoot 'state\config-transactions.json'
    $journal=Read-YomiJsonSafe $path
    if($null -eq $journal -or [int]$journal.schema -ne 1){$journal=[pscustomobject]@{schema=1;transactions=@()}}
    $list=@($journal.transactions)+@($Record)
    if($list.Count -gt 12){$list=@($list|Select-Object -Last 12)}
    $journal=[pscustomobject]@{schema=1;transactions=$list}
    Write-YomiAtomicJson $path $journal
}
function Set-YomiResetMarkers($Plan){
    $state=Join-Path $DataRoot 'state';New-Item -ItemType Directory -Path $state -Force|Out-Null
    foreach($scope in @($Plan.cache_scopes)){
        switch([string]$scope){
            'audio'{Set-Content (Join-Path $state 'audio-reset.pending') 'config-transaction' -Encoding ASCII}
            'artwork'{Set-Content (Join-Path $state 'artwork-reset.pending') 'config-transaction' -Encoding ASCII}
            'video'{Set-Content (Join-Path $state 'video-reset.pending') 'config-transaction' -Encoding ASCII}
            'visualizer'{Set-Content (Join-Path $state 'visualizer-reset.pending') 'config-transaction' -Encoding ASCII}
            'comments'{Get-ChildItem (Join-Path $DataRoot 'cache\comments') -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue;Get-ChildItem (Join-Path $DataRoot 'cache\status') -Filter 'track-*.comment.*' -Force -ErrorAction SilentlyContinue|Remove-Item -Force -ErrorAction SilentlyContinue}
        }
    }
    if([bool]$Plan.playlist_changed){Remove-Item (Join-Path $DataRoot 'playlist.txt') -Force -ErrorAction SilentlyContinue;Set-Content (Join-Path $state 'resume-track.txt') '1' -Encoding ASCII}
}
function Invoke-YomiConfigCommit($Old,$New,$Plan,[string]$Id,[string]$Status='COMMITTED'){
    $configPath=Join-Path $DataRoot 'config.json'
    try{
        Write-YomiAtomicJson $configPath $New
        $verify=Read-YomiJsonSafe $configPath
        if($null -eq $verify){throw 'post-commit config parse failed'}
        if((Get-YomiSha256Text (ConvertTo-YomiConfigJson $verify)) -ne (Get-YomiSha256Text (ConvertTo-YomiConfigJson $New))){throw 'post-commit semantic hash mismatch'}
        Set-YomiResetMarkers $Plan
    }catch{
        try{Write-YomiAtomicJson $configPath $Old}catch{}
        $record=[pscustomobject]@{id=$Id;utc=[DateTime]::UtcNow.ToString('o');status='ROLLED_BACK';error=$_.Exception.Message;plan=$Plan;old_config=$Old;new_config=$New}
        try{Add-YomiTransactionJournal $record}catch{}
        throw
    }
    $record=[pscustomobject]@{id=$Id;utc=[DateTime]::UtcNow.ToString('o');status=$Status;plan=$Plan;old_config=$Old;new_config=$New}
    $journalWarning=''
    try{Add-YomiTransactionJournal $record}catch{
        # The committed config has already passed semantic validation and its
        # reset markers are safe. Journal availability must not roll back a
        # valid config into a state where reset markers describe the successor.
        $journalWarning=$_.Exception.Message
        try{Write-YomiAtomicJson (Join-Path $DataRoot 'state\config-transaction-journal-warning.json') ([pscustomobject]@{schema=1;utc=[DateTime]::UtcNow.ToString('o');id=$Id;status=$Status;error=$journalWarning})}catch{}
    }
    if($journalWarning -ne ''){$record|Add-Member -NotePropertyName journal_warning -NotePropertyValue $journalWarning}
    return $record
}
function Invoke-YomiConfigTransaction($Old,$New,[bool]$RuntimeRunning,[string]$Policy='Safe staging'){
    $plan=Get-YomiConfigChangePlan $Old $New
    $id=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
    if([int]$plan.changed_count -eq 0){return [pscustomobject]@{id=$id;action='NOOP';plan=$plan;restart_requested=$false}}
    $stage=$false
    if($RuntimeRunning){
        if($Policy -eq 'Stage every change while running'){$stage=$true}
        elseif($Policy -eq 'Safe staging' -and [bool]$plan.requires_restart){$stage=$true}
    }
    if($stage){
        $pendingPath=Join-Path $DataRoot 'state\config-pending.json'
        $currentHash=Get-YomiFileSha256 (Join-Path $DataRoot 'config.json')
        $existing=Read-YomiJsonSafe $pendingPath
        $pending=[pscustomobject]@{schema=1;id=$id;created_utc=[DateTime]::UtcNow.ToString('o');policy=$Policy;base_file_sha256=$currentHash;plan=$plan;new_config=$New;supersedes=if($existing){[string]$existing.id}else{''}}
        Write-YomiAtomicJson $pendingPath $pending
        # A new successor transaction supersedes any conflict that belonged to
        # the older pending generation. Its CAS base is the current committed file.
        Remove-Item -LiteralPath (Join-Path $DataRoot 'state\config-pending-conflict.json') -Force -ErrorAction SilentlyContinue
        Add-YomiTransactionJournal ([pscustomobject]@{id=$id;utc=[DateTime]::UtcNow.ToString('o');status='STAGED';plan=$plan;old_config=$Old;new_config=$New})
        return [pscustomobject]@{id=$id;action='STAGED';plan=$plan;restart_requested=$false}
    }
    $pendingPath=Join-Path $DataRoot 'state\config-pending.json'
    $superseded=Read-YomiJsonSafe $pendingPath
    if($superseded){
        Remove-Item -LiteralPath $pendingPath -Force -ErrorAction SilentlyContinue
        Add-YomiTransactionJournal ([pscustomobject]@{id=[string]$superseded.id;utc=[DateTime]::UtcNow.ToString('o');status='SUPERSEDED_BY_COMMIT';plan=$superseded.plan})
    }
    Remove-Item -LiteralPath (Join-Path $DataRoot 'state\config-pending-conflict.json') -Force -ErrorAction SilentlyContinue
    $record=Invoke-YomiConfigCommit $Old $New $plan $id
    $restart=($RuntimeRunning -and [bool]$plan.requires_restart -and $Policy -eq 'Restart immediately')
    return [pscustomobject]@{id=$id;action='COMMITTED';plan=$plan;record=$record;restart_requested=$restart}
}
function Apply-YomiPendingConfigAtStartup{
    $pendingPath=Join-Path $DataRoot 'state\config-pending.json'
    $pending=Read-YomiJsonSafe $pendingPath
    if($null -eq $pending){return [pscustomobject]@{action='NONE'}}
    if(Test-YomiTransactionRuntimeActive){return [pscustomobject]@{action='DEFERRED_ACTIVE_RUNTIME';id=[string]$pending.id}}
    $configPath=Join-Path $DataRoot 'config.json'
    $actualHash=Get-YomiFileSha256 $configPath
    if($actualHash -ne [string]$pending.base_file_sha256){
        $conflict=Join-Path $DataRoot 'state\config-pending-conflict.json'
        Write-YomiAtomicJson $conflict ([pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');id=[string]$pending.id;expected=[string]$pending.base_file_sha256;actual=$actualHash;pending=$pending})
        return [pscustomobject]@{action='CONFLICT';id=[string]$pending.id;expected=[string]$pending.base_file_sha256;actual=$actualHash}
    }
    $old=Get-YomiConfig
    $record=Invoke-YomiConfigCommit $old $pending.new_config $pending.plan ([string]$pending.id) 'ACTIVATED_STARTUP'
    Remove-Item -LiteralPath $pendingPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $DataRoot 'state\config-pending-conflict.json') -Force -ErrorAction SilentlyContinue
    $activation=Join-Path $DataRoot 'state\config-activation.json'
    Write-YomiAtomicJson $activation ([pscustomobject]@{schema=1;id=[string]$pending.id;activated_utc=[DateTime]::UtcNow.ToString('o');status='ACTIVATED_STARTUP';plan=$pending.plan})
    return [pscustomobject]@{action='ACTIVATED';id=[string]$pending.id;record=$record}
}
function Get-YomiConfigTransactionState{
    $pending=Read-YomiJsonSafe (Join-Path $DataRoot 'state\config-pending.json')
    $conflict=Read-YomiJsonSafe (Join-Path $DataRoot 'state\config-pending-conflict.json')
    $journal=Read-YomiJsonSafe (Join-Path $DataRoot 'state\config-transactions.json')
    $last=$null
    if($journal -and @($journal.transactions).Count -gt 0){$last=@($journal.transactions)|Select-Object -Last 1}
    return [pscustomobject]@{schema=1;pending=$pending;conflict=$conflict;last=$last;runtime_active=(Test-YomiTransactionRuntimeActive)}
}
function Remove-YomiPendingConfig{
    $pendingPath=Join-Path $DataRoot 'state\config-pending.json'
    $p=Read-YomiJsonSafe $pendingPath
    Remove-Item -LiteralPath $pendingPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $DataRoot 'state\config-pending-conflict.json') -Force -ErrorAction SilentlyContinue
    if($p){Add-YomiTransactionJournal ([pscustomobject]@{id=[string]$p.id;utc=[DateTime]::UtcNow.ToString('o');status='DISCARDED';plan=$p.plan})}
    return [pscustomobject]@{action='DISCARDED';id=if($p){[string]$p.id}else{''}}
}
function Invoke-YomiRollbackLast{
    $journal=Read-YomiJsonSafe (Join-Path $DataRoot 'state\config-transactions.json')
    $candidate=$null
    $rows=@($journal.transactions)
    for($ri=$rows.Count-1;$ri -ge 0;$ri--){
        $row=$rows[$ri]
        if([string]$row.status -in @('COMMITTED','ACTIVATED_STARTUP') -and $row.old_config){$candidate=$row;break}
    }
    if($null -eq $candidate){return [pscustomobject]@{action='NONE';reason='no committed transaction with rollback snapshot'}}
    $current=Get-YomiConfig;$target=$candidate.old_config
    $policy='Safe staging';$runtime=Test-YomiTransactionRuntimeActive
    $result=Invoke-YomiConfigTransaction $current $target $runtime $policy
    return [pscustomobject]@{action='ROLLBACK_PLANNED';source_id=[string]$candidate.id;result=$result}
}
function Write-YomiTransactionReport($State){
    $reports=Join-Path $DataRoot 'reports';New-Item -ItemType Directory -Path $reports -Force|Out-Null
    $path=Join-Path $reports ('YOMI-config-transaction-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json')
    Write-YomiAtomicJson $path ([pscustomobject]@{schema=1;product='YOMI';report_type='configuration-change-control';generated_utc=[DateTime]::UtcNow.ToString('o');state=$State})
    return $path
}

# When dot-sourced, export functions only.
if($MyInvocation.InvocationName -ne '.'){
    $result=$null
    if($ApplyPendingAtStartup){$result=Apply-YomiPendingConfigAtStartup}
    elseif($DiscardPending){$result=Remove-YomiPendingConfig}
    elseif($RollbackLast){$result=Invoke-YomiRollbackLast}
    else{$result=Get-YomiConfigTransactionState}
    $path=Write-YomiTransactionReport $result
    Write-Host '===== YOMI CONFIGURATION CHANGE CONTROL ====='
    Write-Host ($result|ConvertTo-Json -Depth 10)
    Write-Host "Report: $path"
    if($CopyReport){try{Set-Clipboard $path}catch{}}
    if($OpenReport){try{Start-Process notepad.exe ('"'+$path+'"')}catch{}}
}
