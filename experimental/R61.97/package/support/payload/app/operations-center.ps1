$ErrorActionPreference='SilentlyContinue'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$created=$false
$mutex=New-Object System.Threading.Mutex($true,'Local\YOMI_OPERATIONS_CENTER',[ref]$created)
if(-not $created){try{$mutex.Dispose()}catch{};exit 0}

$stateRoot=Join-Path $DataRoot 'state'
$script:lastSnapshot=$null
$script:lastRows=@()
$script:selectedKey=''

function Read-JsonSafe([string]$Path){
    if(-not(Test-Path $Path)){return $null}
    try{return Get-Content $Path -Raw|ConvertFrom-Json}catch{return $null}
}
function Read-TextSafe([string]$Path){
    if(-not(Test-Path $Path)){return ''}
    try{return (Get-Content $Path -Raw).Trim()}catch{return ''}
}
function Live-Process([int]$ProcessId){
    if($ProcessId -le 0){return $null}
    try{return Get-Process -Id $ProcessId -ErrorAction Stop}catch{return $null}
}
function Format-Age([double]$Seconds){
    if($Seconds -lt 0){return '--'}
    if($Seconds -lt 60){return ('{0:N0}s' -f $Seconds)}
    if($Seconds -lt 3600){return ('{0:N1}m' -f ($Seconds/60))}
    return ('{0:N1}h' -f ($Seconds/3600))
}
function Tool([string]$Script,[string]$Arguments=''){
    $path=Join-Path $PSScriptRoot $Script
    if(-not(Test-Path $path)){
        [Windows.Forms.MessageBox]::Show("Tool is not installed:`r`n$path",'YOMI Operations Center')|Out-Null
        return
    }
    $arg="-NoProfile -ExecutionPolicy Bypass -File `"$path`""
    if($Arguments){$arg+=' '+$Arguments}
    Start-Process powershell.exe -ArgumentList $arg
}
function Status-Row([string]$Key,[string]$Area,[string]$State,[string]$Summary,$Detail){
    [PSCustomObject][ordered]@{Key=$Key;Area=$Area;State=$State;Summary=$Summary;Detail=$Detail}
}
function Snapshot {
    $runtime=Read-JsonSafe (Join-Path $stateRoot 'runtime-instance.json')
    $lease=Read-JsonSafe (Join-Path $stateRoot 'runtime-lease.json')
    $watchdog=Read-JsonSafe (Join-Path $stateRoot 'watchdog-status.json')
    $queue=Read-JsonSafe (Join-Path $stateRoot 'queue-runtime.json')
    $preflight=Read-JsonSafe (Join-Path $stateRoot 'preflight.json')
    $update=Read-JsonSafe (Join-Path $stateRoot 'update-transaction.json')
    $head=Read-JsonSafe (Join-Path $stateRoot 'session-journal-head.json')
    $current=Read-JsonSafe (Join-Path $stateRoot 'current.json')
    $browser=Read-JsonSafe (Join-Path $stateRoot 'browser-clients.json')
    $config=Read-JsonSafe (Join-Path $DataRoot 'config.json')
    $route=Read-JsonSafe (Join-Path $DataRoot 'cache\capabilities\video-route-memory.json')
    $perf=Read-JsonSafe (Join-Path $DataRoot 'cache\capabilities\performance-memory.json')

    $leaseAge=-1
    $leasePath=Join-Path $stateRoot 'runtime-lease.json'
    if(Test-Path $leasePath){try{$leaseAge=[Math]::Max(0,([DateTime]::UtcNow-(Get-Item $leasePath).LastWriteTimeUtc).TotalSeconds)}catch{}}

    $driveFree=-1
    try{$drive=New-Object IO.DriveInfo([IO.Path]::GetPathRoot($DataRoot));$driveFree=$drive.AvailableFreeSpace/1GB}catch{}

    $recoveryCount=@(Get-ChildItem (Join-Path $DataRoot 'recovery-points') -Filter 'YOMI-recovery-*.zip' -File -ErrorAction SilentlyContinue).Count
    $historyCount=@(Get-ChildItem (Join-Path $DataRoot 'config-history') -Filter 'config-*.json' -File -ErrorAction SilentlyContinue).Count
    $oracleReports=@(Get-ChildItem (Join-Path $DataRoot 'oracle-lab') -Filter 'oracle-*.json' -File -ErrorAction SilentlyContinue|Sort-Object LastWriteTimeUtc -Descending)
    $oracleLab=if($oracleReports.Count -gt 0){Read-JsonSafe $oracleReports[0].FullName}else{$null}
    $configNotice=Read-TextSafe (Join-Path $stateRoot 'config-incompatible.txt')

    [PSCustomObject][ordered]@{
        generated_utc=[DateTime]::UtcNow.ToString('o')
        runtime=$runtime;lease=$lease;lease_age=$leaseAge;watchdog=$watchdog;queue=$queue
        preflight=$preflight;update=$update;ledger_head=$head;current=$current;browser=$browser
        config=$config;route_intelligence=$route;performance_memory=$perf;oracle_lab=$oracleLab
        recovery_points=$recoveryCount;config_history=$historyCount;free_gb=$driveFree
        config_incompatible=$configNotice
    }
}
function Rows($s){
    $rows=New-Object System.Collections.Generic.List[System.Object]

    $runtimeState=if($s.runtime){[string]$s.runtime.state}else{'STOPPED'}
    $runtimeLevel=if($s.runtime -and $runtimeState -eq 'running'){'OK'}elseif($s.runtime){'WARN'}else{'IDLE'}
    $runtimeSummary=if($s.runtime){"$runtimeState  runtime $($s.runtime.runtime_id)"}else{'No active runtime'}
    $rows.Add((Status-Row 'runtime' 'Runtime' $runtimeLevel $runtimeSummary $s.runtime))

    if($s.runtime -and $s.runtime.processes){
        foreach($role in @('supervisor','engine','server')){
            $id=$s.runtime.processes.$role
            if($null -eq $id){continue}
            $proc=Live-Process ([int]$id.pid)
            $level=if($proc){'OK'}else{'WARN'}
            $summary=if($proc){"PID $($id.pid) verified record present"}else{"PID $($id.pid) not alive"}
            $rows.Add((Status-Row ("process-"+$role) ("Process / "+$role) $level $summary $id))
        }
    }

    if($s.lease){
        $level=if($s.lease_age -le 15){'OK'}elseif($s.lease_age -le 30){'WARN'}else{'FAIL'}
        $rows.Add((Status-Row 'lease' 'Engine lease' $level ("age "+(Format-Age $s.lease_age)+"  occurrence "+$s.lease.occurrence) $s.lease))
    }else{$rows.Add((Status-Row 'lease' 'Engine lease' 'IDLE' 'No active engine lease' $null))}

    if($s.watchdog){
        $level=if([string]$s.watchdog.state -eq 'healthy'){'OK'}else{'WARN'}
        $rows.Add((Status-Row 'watchdog' 'Watchdog' $level ("$($s.watchdog.state)  restarts $($s.watchdog.server_restarts)") $s.watchdog))
    }

    if($s.queue){
        $buffer=[string]$s.queue.buffer_health
        $level=if($buffer -eq 'HEALTHY'){'OK'}elseif($buffer -eq 'THIN'){'WARN'}else{'FAIL'}
        $rows.Add((Status-Row 'buffer' 'Playback buffer' $level ("$buffer  ready $($s.queue.ready_ahead)/$($s.queue.target_ahead)  $([Math]::Round([double]$s.queue.ready_time_seconds))s covered") $s.queue))

        if($s.queue.oracle){
            $verdict=[string]$s.queue.oracle.verdict
            $level=if($verdict -eq 'NOMINAL'){'OK'}elseif($verdict -eq 'WATCH'){'WARN'}else{'FAIL'}
            $rows.Add((Status-Row 'oracle' 'Oracle forecast' $level ("$verdict  min $($s.queue.oracle.confidence_min)%  avg $($s.queue.oracle.confidence_average)%") $s.queue.oracle))
        }
        if($s.queue.service_level){
            $svc=[string]$s.queue.service_level.state
            $level=if($svc -eq 'HEALTHY'){'OK'}elseif($svc -eq 'AT_RISK'){'FAIL'}else{'WARN'}
            $rows.Add((Status-Row 'sla' 'Service level' $level ("$svc  presentation SLA $($s.queue.service_level.presentation_sla_seconds)s") $s.queue.service_level))
        }
        if($s.queue.resource){
            $mode=[string]$s.queue.resource.governor_mode
            $level=if($mode -eq 'NORMAL'){'OK'}elseif($mode -eq 'CONSERVE'){'WARN'}elseif($mode -eq 'BROWNOUT'){'WARN'}else{'FAIL'}
            $rows.Add((Status-Row 'resource' 'Resource governor' $level ("$mode  $($s.queue.resource.governor_reason)  workers $($s.queue.resource.effective_workers)/$($s.queue.resource.configured_workers)") $s.queue.resource))
        }
    }

    if($s.preflight){
        $v=[string]$s.preflight.verdict
        $level=if($v -eq 'GO'){'OK'}elseif($v -eq 'WARN'){'WARN'}else{'FAIL'}
        $rows.Add((Status-Row 'preflight' 'Startup preflight' $level ("$v  fail $($s.preflight.failures) warn $($s.preflight.warnings)") $s.preflight))
    }

    if($s.ledger_head){
        $rows.Add((Status-Row 'ledger' 'Mutation ledger' 'OK' ("seq $($s.ledger_head.sequence)  revision $($s.ledger_head.revision)  "+$s.ledger_head.entry_hash) $s.ledger_head))
    }else{$rows.Add((Status-Row 'ledger' 'Mutation ledger' 'IDLE' 'No chained mutation head yet' $null))}

    if($s.update){
        $phase=[string]$s.update.phase
        $level=if($phase -eq 'failed'){'FAIL'}elseif($phase -eq 'completed'){'OK'}else{'WARN'}
        $rows.Add((Status-Row 'update' 'Update transaction' $level $phase $s.update))
    }

    $rows.Add((Status-Row 'recovery' 'Recovery Points' 'OK' "$($s.recovery_points) local checkpoint(s)" @{count=$s.recovery_points}))
    $rows.Add((Status-Row 'history' 'Config Time Machine' 'OK' "$($s.config_history)/8 snapshots" @{count=$s.config_history}))

    if($s.route_intelligence){
        $count=@($s.route_intelligence.entries.PSObject.Properties).Count
        $rows.Add((Status-Row 'route' 'Route intelligence' 'OK' "$count learned source record(s)" @{schema=$s.route_intelligence.schema;entries=$count}))
    }
    if($s.performance_memory){
        $count=@($s.performance_memory.entries.PSObject.Properties).Count
        $rows.Add((Status-Row 'perf' 'Machine calibration' 'OK' "$count timing profile(s)" @{schema=$s.performance_memory.schema;profiles=$count}))
    }
    if($s.oracle_lab){
        $level=if([string]$s.oracle_lab.status -eq 'PASS'){'OK'}else{'FAIL'}
        $rows.Add((Status-Row 'oracle-lab' 'Oracle Lab' $level ("$($s.oracle_lab.status)  deterministic=$($s.oracle_lab.deterministic)") $s.oracle_lab))
    }

    if($s.browser){
        $count=@($s.browser.clients).Count
        $rows.Add((Status-Row 'browser' 'Browser Sources' $(if($count -gt 0){'OK'}else{'IDLE'}) "$count active heartbeat(s)" $s.browser))
    }
    if($s.free_gb -ge 0){
        $level=if($s.free_gb -ge 4){'OK'}elseif($s.free_gb -ge 1){'WARN'}else{'FAIL'}
        $rows.Add((Status-Row 'disk' 'Data drive' $level ("{0:N1} GB free" -f $s.free_gb) @{free_gb=$s.free_gb}))
    }
    if($s.config_incompatible){
        $rows.Add((Status-Row 'config-schema' 'Config compatibility' 'FAIL' 'Newer configuration schema detected' @{notice=$s.config_incompatible}))
    }
    return $rows.ToArray()
}
function Refresh-Operations {
    $selected=$script:selectedKey
    $script:lastSnapshot=Snapshot
    $script:lastRows=Rows $script:lastSnapshot
    $grid.Rows.Clear()
    $fail=0;$warn=0
    foreach($r in $script:lastRows){
        $idx=$grid.Rows.Add($r.Area,$r.State,$r.Summary)
        $grid.Rows[$idx].Tag=$r.Key
        if($r.State -eq 'FAIL'){$fail++}
        elseif($r.State -eq 'WARN'){$warn++}
        if($r.Key -eq $selected){$grid.Rows[$idx].Selected=$true}
    }
    $overall=if($fail -gt 0){'ATTENTION'}elseif($warn -gt 0){'WATCH'}elseif($script:lastSnapshot.runtime){'HEALTHY'}else{'READY / STOPPED'}
    $headline.Text="AEGIS  $overall"
    $summary.Text="FAIL $fail   WARN $warn   |   "+(Get-Date).ToString('HH:mm:ss')+"   |   "+$DataRoot
    if($grid.SelectedRows.Count -eq 0 -and $grid.Rows.Count -gt 0){$grid.Rows[0].Selected=$true}
    Show-Detail
}
function Show-Detail {
    if($grid.SelectedRows.Count -lt 1){$detail.Text='';return}
    $key=[string]$grid.SelectedRows[0].Tag
    $script:selectedKey=$key
    $row=@($script:lastRows|Where-Object{$_.Key -eq $key}|Select-Object -First 1)
    if($row.Count -lt 1){$detail.Text='';return}
    try{$detail.Text=($row[0].Detail|ConvertTo-Json -Depth 10)}catch{$detail.Text=[string]$row[0].Detail}
}
function Copy-Snapshot {
    $payload=[ordered]@{
        schema=1;product='YOMI';report_type='operations-center-snapshot'
        generated_utc=[DateTime]::UtcNow.ToString('o')
        rows=@($script:lastRows|Select-Object Area,State,Summary)
        snapshot=$script:lastSnapshot
    }
    try{Set-Clipboard ($payload|ConvertTo-Json -Depth 12)}catch{}
    $summary.Text='Operations snapshot copied to clipboard.'
}

$form=New-Object Windows.Forms.Form
$form.Text='YOMI Operations Center'
$form.StartPosition='CenterScreen'
$form.Size=New-Object Drawing.Size(1220,790)
$form.MinimumSize=New-Object Drawing.Size(980,650)
$form.Font=New-Object Drawing.Font('Segoe UI',10)

$headline=New-Object Windows.Forms.Label
$headline.Text='AEGIS'
$headline.Font=New-Object Drawing.Font('Segoe UI Semibold',18)
$headline.Location=New-Object Drawing.Point(16,12)
$headline.Size=New-Object Drawing.Size(400,38)
$form.Controls.Add($headline)

$summary=New-Object Windows.Forms.Label
$summary.Location=New-Object Drawing.Point(420,18)
$summary.Size=New-Object Drawing.Size(760,26)
$summary.TextAlign='MiddleRight'
$form.Controls.Add($summary)

$grid=New-Object Windows.Forms.DataGridView
$grid.Location=New-Object Drawing.Point(16,58)
$grid.Size=New-Object Drawing.Size(700,565)
$grid.Anchor='Top,Bottom,Left'
$grid.ReadOnly=$true;$grid.AllowUserToAddRows=$false;$grid.AllowUserToDeleteRows=$false
$grid.AllowUserToResizeRows=$false;$grid.MultiSelect=$false;$grid.SelectionMode='FullRowSelect'
$grid.RowHeadersVisible=$false;$grid.AutoSizeRowsMode='None';$grid.RowTemplate.Height=26
[void]$grid.Columns.Add('Area','AREA');$grid.Columns['Area'].Width=185
[void]$grid.Columns.Add('State','STATE');$grid.Columns['State'].Width=90
[void]$grid.Columns.Add('Summary','SUMMARY');$grid.Columns['Summary'].AutoSizeMode='Fill'
$form.Controls.Add($grid)

$detail=New-Object Windows.Forms.RichTextBox
$detail.Location=New-Object Drawing.Point(730,58)
$detail.Size=New-Object Drawing.Size(458,565)
$detail.Anchor='Top,Bottom,Left,Right'
$detail.ReadOnly=$true;$detail.WordWrap=$false
$detail.Font=New-Object Drawing.Font('Consolas',9)
$form.Controls.Add($detail)

$grid.Add_SelectionChanged({Show-Detail})

$buttons=@(
    @('REFRESH',{Refresh-Operations}),
    @('COPY SNAPSHOT',{Copy-Snapshot}),
    @('DOCTOR',{Tool 'doctor.ps1'}),
    @('SESSION EXPLORER',{Tool 'session-explorer.ps1'}),
    @('CREATE RECOVERY',{Tool 'recovery-point.ps1' '-Mode Create'}),
    @('RESTORE RECOVERY',{Tool 'recovery-point.ps1' '-Mode Restore'}),
    @('INCIDENT REPLAY',{Tool 'incident-replay.ps1' '-Copy'}),
    @('ORACLE LAB',{Tool 'oracle-lab.ps1' '-Scenario All'}),
    @('SUPPORT BUNDLE',{Tool 'support-bundle.ps1'}),
    @('CONFIG TIME MACHINE',{Tool 'config-history.ps1' '-Mode List'}),
    @('RESET ORACLE',{Tool 'reset-oracle-learning.ps1'}),
    @('SAFE REPAIR',{Tool 'aegis-repair.ps1' '-Mode RepairSafe'}),
    @('DATA FOLDER',{Start-Process explorer.exe $DataRoot})
)
$x=16;$y=640
foreach($spec in $buttons){
    $b=New-Object Windows.Forms.Button
    $b.Text=$spec[0];$b.Size=New-Object Drawing.Size(142,34)
    if($x+142 -gt 1190){$x=16;$y+=42}
    $b.Location=New-Object Drawing.Point($x,$y)
    $handler=$spec[1]
    $b.Add_Click($handler)
    $form.Controls.Add($b)
    $x+=150
}

$timer=New-Object Windows.Forms.Timer
$timer.Interval=2000
$timer.Add_Tick({Refresh-Operations})
$form.Add_Shown({Refresh-Operations;$timer.Start()})
try{[void]$form.ShowDialog()}finally{
    $timer.Stop();$timer.Dispose()
    try{$mutex.ReleaseMutex()}catch{}
    try{$mutex.Dispose()}catch{}
    $form.Dispose()
}
