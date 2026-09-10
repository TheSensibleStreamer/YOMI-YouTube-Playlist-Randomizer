$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$stateRoot=Join-Path $DataRoot 'state'
$sessionPath=Join-Path $stateRoot 'session.json'
$orderPath=Join-Path $stateRoot 'session-order.json'
$queuePath=Join-Path $stateRoot 'queue-runtime.json'
$currentPath=Join-Path $stateRoot 'current.json'
$enginePidPath=Join-Path $stateRoot 'engine.pid'
$objectRoot=Join-Path $DataRoot 'cache\objects'
$statusRoot=Join-Path $DataRoot 'cache\status'
$pipeName='yomi-v4'

$script:sessionStamp=0L
$script:orderStamp=0L
$script:queueStamp=0L
$script:baseMeta=@{}
$script:insertedMeta=@{}
$script:activeOrder=@()
$script:allRows=@()
$script:viewRows=@()
$script:queueByOccurrence=@{}
$script:queueSummary=$null
$script:currentOccurrence=0
$script:statusCache=@{}

function Live-Engine {
    if(-not(Test-Path $enginePidPath)){return $false}
    $n=0
    try{[void][int]::TryParse((Get-Content $enginePidPath -Raw).Trim(),[ref]$n)}catch{return $false}
    return ($n -gt 0 -and $null -ne (Get-Process -Id $n -ErrorAction SilentlyContinue))
}
function Send-Mpv([object[]]$command){
    $pipe=$null;$writer=$null
    try{
        $pipe=New-Object IO.Pipes.NamedPipeClientStream('.',$pipeName,[IO.Pipes.PipeDirection]::Out)
        $pipe.Connect(180)
        $writer=New-Object IO.StreamWriter($pipe,(New-Object System.Text.UTF8Encoding($false)))
        $writer.AutoFlush=$true
        $writer.WriteLine((@{command=$command}|ConvertTo-Json -Compress))
        return $true
    }catch{return $false}
    finally{if($writer){$writer.Dispose()};if($pipe){$pipe.Dispose()}}
}
function Stamp([string]$path){
    if(-not(Test-Path $path)){return 0L}
    try{return (Get-Item $path).LastWriteTimeUtc.Ticks}catch{return 0L}
}
function Duration([double]$seconds){
    if($seconds -le 0){return ''}
    $t=[TimeSpan]::FromSeconds($seconds)
    if($t.TotalHours -ge 1){return '{0}:{1:00}:{2:00}' -f [int]$t.TotalHours,$t.Minutes,$t.Seconds}
    return '{0}:{1:00}' -f [int]$t.TotalMinutes,$t.Seconds
}
function Metadata([int]$occ){
    $key=[string]$occ
    if($script:insertedMeta.ContainsKey($key)){return $script:insertedMeta[$key]}
    if($script:baseMeta.ContainsKey($key)){return $script:baseMeta[$key]}
    return [PSCustomObject]@{occurrence_id=$occ;title="Occurrence $occ";channel='';youtube_id='';source_key='';duration=0;kind='UNKNOWN'}
}
function Load-Session([switch]$Force){
    $sessionStamp=Stamp $sessionPath
    $orderStamp=Stamp $orderPath
    if(-not $Force -and $sessionStamp -eq $script:sessionStamp -and $orderStamp -eq $script:orderStamp){return $false}
    $script:sessionStamp=$sessionStamp;$script:orderStamp=$orderStamp
    $script:baseMeta=@{};$script:insertedMeta=@{};$script:activeOrder=@();$script:statusCache=@{}

    $session=$null
    if(Test-Path $sessionPath){try{$session=Get-Content $sessionPath -Raw|ConvertFrom-Json}catch{}}
    if($session){
        foreach($entry in @($session.occurrences)){
            $occ=[int]$entry.position
            if($occ -lt 1){continue}
            $script:baseMeta[[string]$occ]=[PSCustomObject]@{
                occurrence_id=$occ
                title=$(if($entry.title){[string]$entry.title}else{"Occurrence $occ"})
                channel=[string]$entry.channel
                youtube_id=[string]$entry.id
                source_key=[string]$entry.source_key
                duration=[double]$entry.duration
                kind='BASE'
            }
        }
    }

    $order=$null
    if(Test-Path $orderPath){try{$order=Get-Content $orderPath -Raw|ConvertFrom-Json}catch{}}
    if($order){
        foreach($entry in @($order.inserted)){
            $occ=[int]$entry.occurrence_id
            if($occ -lt 1){continue}
            $script:insertedMeta[[string]$occ]=[PSCustomObject]@{
                occurrence_id=$occ
                title=$(if($entry.title){[string]$entry.title}else{"Replay $occ"})
                channel=[string]$entry.channel
                youtube_id=[string]$entry.youtube_id
                source_key=[string]$entry.source_key
                duration=[double]$entry.duration
                kind='REPLAY'
            }
        }
        $script:activeOrder=@($order.order|ForEach-Object{[int]$_})
    }else{
        $count=$script:baseMeta.Count
        if($count -gt 0){$script:activeOrder=@(1..$count)}
    }

    $rows=New-Object System.Collections.Generic.List[System.Object]
    for($slot=0;$slot -lt $script:activeOrder.Count;$slot++){
        $occ=[int]$script:activeOrder[$slot]
        $meta=Metadata $occ
        $rows.Add([PSCustomObject]@{slot=$slot+1;occurrence=$occ;meta=$meta})
    }
    $script:allRows=$rows.ToArray()
    Apply-Filter
    return $true
}
function Load-Queue([switch]$Force){
    $stamp=Stamp $queuePath
    if(-not $Force -and $stamp -eq $script:queueStamp){return $false}
    $script:queueStamp=$stamp;$script:queueByOccurrence=@{};$script:queueSummary=$null;$script:statusCache=@{}
    if(Test-Path $queuePath){
        try{
            $q=Get-Content $queuePath -Raw|ConvertFrom-Json
            $script:queueSummary=$q
            $script:currentOccurrence=[int]$q.current_index
            foreach($item in @($q.items)){$script:queueByOccurrence[[string][int]$item.index]=$item}
        }catch{}
    }
    if($script:currentOccurrence -lt 1 -and (Test-Path $currentPath)){
        try{$script:currentOccurrence=[int](Get-Content $currentPath -Raw|ConvertFrom-Json).index}catch{}
    }
    return $true
}
function Passive-State([int]$occ){
    $key=[string]$occ
    if($script:queueByOccurrence.ContainsKey($key)){
        $item=$script:queueByOccurrence[$key]
        if([bool]$item.presentation_complete){return 'READY'}
        if([bool]$item.sync_ready){return 'READY*'}
        if([bool]$item.transition_ready){return 'AUDIO READY'}
        if($item.phase){return [string]$item.phase}
        return 'BUILDING'
    }
    if($script:statusCache.ContainsKey($key)){return $script:statusCache[$key]}

    $meta=Metadata $occ
    $store=$script:queueSummary.object_store
    $state='UNKNOWN'
    if($store -and [string]$meta.source_key){
        $audio=Join-Path $objectRoot ("audio\$($meta.source_key)-$([string]$store.audio_signature).audio")
        $info=Join-Path $objectRoot ("meta\$($meta.source_key)-$([string]$store.meta_signature).info.json")
        $gain=Join-Path $objectRoot ("gain\$($meta.source_key)-$([string]$store.gain_signature).gain")
        if((Test-Path $audio)-and(Test-Path $info)-and(Test-Path $gain)){$state='AUDIO CACHED'}else{$state='UNPREPARED'}
    }elseif(Test-Path (Join-Path $statusRoot "track-$occ.audio.permanent")){$state='FAILED'}
    $script:statusCache[$key]=$state
    return $state
}
function Apply-Filter {
    if($null -eq $grid){return}
    $query=$search.Text.Trim().ToLowerInvariant()
    $kind=[string]$kindFilter.SelectedItem
    $filtered=New-Object System.Collections.Generic.List[System.Object]
    foreach($row in $script:allRows){
        $m=$row.meta
        if($kind -eq 'BASE ONLY' -and $m.kind -ne 'BASE'){continue}
        if($kind -eq 'REPLAYS ONLY' -and $m.kind -ne 'REPLAY'){continue}
        if($query){
            $hay=(([string]$m.title)+' '+([string]$m.channel)+' '+([string]$m.youtube_id)+' '+([string]$row.occurrence)).ToLowerInvariant()
            if(-not $hay.Contains($query)){continue}
        }
        $filtered.Add($row)
    }
    $script:viewRows=$filtered.ToArray()
    $grid.RowCount=$script:viewRows.Count
    $grid.Invalidate()
    $statusLabel.Text="VIEW $($script:viewRows.Count) / ACTIVE $($script:allRows.Count)  |  CURRENT OCC $($script:currentOccurrence)  |  "+$(if(Live-Engine){'LIVE'}else{'OFFLINE BROWSE'})
}
function Selected-Occurrence {
    if($grid.SelectedRows.Count -lt 1){return 0}
    $r=$grid.SelectedRows[0].Index
    if($r -lt 0 -or $r -ge $script:viewRows.Count){return 0}
    return [int]$script:viewRows[$r].occurrence
}
function Selected-Slot {
    if($grid.SelectedRows.Count -lt 1){return 0}
    $r=$grid.SelectedRows[0].Index
    if($r -lt 0 -or $r -ge $script:viewRows.Count){return 0}
    return [int]$script:viewRows[$r].slot
}
function Require-Live([string]$action){
    if(Live-Engine){return $true}
    [Windows.Forms.MessageBox]::Show("$action requires YOMI playback to be running.",'YOMI Session Explorer')|Out-Null
    return $false
}
function Action-PlayNow {
    if(-not(Require-Live 'Play Now')){return};$occ=Selected-Occurrence;if($occ -gt 0){[void](Send-Mpv @('script-message','yomi-jump',[string]$occ))}
}
function Action-Prepare {
    if(-not(Require-Live 'Prepare')){return};$occ=Selected-Occurrence;if($occ -gt 0){[void](Send-Mpv @('script-message','yomi-prepare',[string]$occ))}
}
function Action-PlayNext {
    if(-not(Require-Live 'Play Next')){return};$occ=Selected-Occurrence;if($occ -gt 0){[void](Send-Mpv @('script-message','yomi-order-play-next',[string]$occ))}
}
function Action-Replay {
    if(-not(Require-Live 'Replay Next')){return};$occ=Selected-Occurrence;if($occ -gt 0){[void](Send-Mpv @('script-message','yomi-order-replay-next',[string]$occ))}
}
function Action-Remove {
    if(-not(Require-Live 'Remove')){return};$occ=Selected-Occurrence
    if($occ -lt 1 -or $occ -eq $script:currentOccurrence){return}
    $m=Metadata $occ
    $answer=[Windows.Forms.MessageBox]::Show(
        "Remove this occurrence from the active session?`r`n`r`nOCC $occ`r`n$($m.title)`r`n`r`nMedia objects are not deleted and Undo Order can restore the active-order mutation while it remains in the current edit scope.",
        'YOMI Session Explorer',
        [Windows.Forms.MessageBoxButtons]::YesNo,
        [Windows.Forms.MessageBoxIcon]::Question
    )
    if($answer -eq [Windows.Forms.DialogResult]::Yes){[void](Send-Mpv @('script-message','yomi-order-remove',[string]$occ))}
}
function Return-ToCurrent {
    if($script:currentOccurrence -lt 1){return}
    for($n=0;$n -lt $script:viewRows.Count;$n++){
        if([int]$script:viewRows[$n].occurrence -eq $script:currentOccurrence){
            $grid.ClearSelection()
            $grid.Rows[$n].Selected=$true
            if($n -ge 0){$grid.FirstDisplayedScrollingRowIndex=[Math]::Max(0,$n-3)}
            break
        }
    }
}

$form=New-Object Windows.Forms.Form
$form.Text='YOMI Session Explorer'
$form.StartPosition='CenterScreen'
$form.Size=New-Object Drawing.Size(1280,780)
$form.MinimumSize=New-Object Drawing.Size(980,620)
$form.Font=New-Object Drawing.Font('Segoe UI',10)

$title=New-Object Windows.Forms.Label;$title.Text='SESSION EXPLORER';$title.Font=New-Object Drawing.Font('Segoe UI Semibold',16);$title.Location=New-Object Drawing.Point(15,12);$title.Size=New-Object Drawing.Size(250,32);$form.Controls.Add($title)
$statusLabel=New-Object Windows.Forms.Label;$statusLabel.Location=New-Object Drawing.Point(270,17);$statusLabel.Size=New-Object Drawing.Size(970,25);$statusLabel.TextAlign='MiddleRight';$statusLabel.ForeColor=[Drawing.Color]::DimGray;$form.Controls.Add($statusLabel)

$search=New-Object Windows.Forms.TextBox;$search.Location=New-Object Drawing.Point(15,52);$search.Size=New-Object Drawing.Size(500,28);$form.Controls.Add($search)
$kindFilter=New-Object Windows.Forms.ComboBox;$kindFilter.DropDownStyle='DropDownList';$kindFilter.Items.AddRange(@('ALL ACTIVE','BASE ONLY','REPLAYS ONLY'));$kindFilter.SelectedIndex=0;$kindFilter.Location=New-Object Drawing.Point(525,51);$kindFilter.Size=New-Object Drawing.Size(160,28);$form.Controls.Add($kindFilter)
$follow=New-Object Windows.Forms.CheckBox;$follow.Text='Follow current';$follow.Checked=$true;$follow.Location=New-Object Drawing.Point(700,53);$follow.Size=New-Object Drawing.Size(130,26);$form.Controls.Add($follow)

function Button([string]$text,[int]$x,[int]$width){
    $b=New-Object Windows.Forms.Button;$b.Text=$text;$b.Location=New-Object Drawing.Point($x,91);$b.Size=New-Object Drawing.Size($width,34);$form.Controls.Add($b);return $b
}
$play=Button 'PLAY NOW' 15 110
$prepare=Button 'PREPARE' 133 105
$playNext=Button 'PLAY NEXT' 246 110
$replay=Button 'REPLAY NEXT' 364 120
$remove=Button 'REMOVE' 492 100
$return=Button 'RETURN TO CURRENT' 600 155
$refresh=Button 'REFRESH' 763 95

$hint=New-Object Windows.Forms.Label
$hint.Text='Virtualized full-session view. Observation is passive: search/scroll never downloads, prepares, or decodes media. Actions are explicit.'
$hint.Location=New-Object Drawing.Point(875,94);$hint.Size=New-Object Drawing.Size(370,40);$hint.ForeColor=[Drawing.Color]::DimGray;$form.Controls.Add($hint)

$grid=New-Object Windows.Forms.DataGridView
$grid.Location=New-Object Drawing.Point(15,138)
$grid.Size=New-Object Drawing.Size(1230,575)
$grid.Anchor='Top,Bottom,Left,Right'
$grid.ReadOnly=$true;$grid.AllowUserToAddRows=$false;$grid.AllowUserToDeleteRows=$false
$grid.AllowUserToResizeRows=$false;$grid.MultiSelect=$false;$grid.SelectionMode='FullRowSelect'
$grid.RowHeadersVisible=$false;$grid.VirtualMode=$true;$grid.AutoSizeRowsMode='None';$grid.RowTemplate.Height=25
$grid.BackgroundColor=[Drawing.Color]::White;$grid.BorderStyle='FixedSingle'
$form.Controls.Add($grid)
$boldGridFont=New-Object Drawing.Font($grid.Font,[Drawing.FontStyle]::Bold)

foreach($c in @(
    @('Slot','SLOT',70),@('Occ','OCC',75),@('Title','TITLE',400),@('Channel','CHANNEL',210),
    @('Duration','TIME',70),@('Kind','TYPE',85),@('Oracle','ORACLE',70),@('State','STATE',125),@('Reason','WHY',145)
)){
    $i=$grid.Columns.Add($c[0],$c[1]);$grid.Columns[$i].Width=$c[2]
}
$grid.Columns['Title'].AutoSizeMode='Fill'

$grid.Add_CellValueNeeded({
    param($sender,$e)
    if($e.RowIndex -lt 0 -or $e.RowIndex -ge $script:viewRows.Count){return}
    $row=$script:viewRows[$e.RowIndex];$m=$row.meta
    switch($grid.Columns[$e.ColumnIndex].Name){
        'Slot'{$e.Value=$row.slot}
        'Occ'{$e.Value=$row.occurrence}
        'Title'{$e.Value=$m.title}
        'Channel'{$e.Value=$m.channel}
        'Duration'{$e.Value=Duration ([double]$m.duration)}
        'Kind'{$e.Value=$m.kind}
        'Oracle'{
            $q=$script:queueByOccurrence[[string][int]$row.occurrence]
            $e.Value=if($q -and $null -ne $q.oracle_confidence){([string][int]$q.oracle_confidence)+'%'}else{'--'}
        }
        'State'{$e.Value=Passive-State ([int]$row.occurrence)}
        'Reason'{
            $q=$script:queueByOccurrence[[string][int]$row.occurrence]
            $e.Value=if($q){[string]$q.decision_reason}else{''}
        }
    }
})
$grid.Add_CellFormatting({
    param($sender,$e)
    if($e.RowIndex -lt 0 -or $e.RowIndex -ge $script:viewRows.Count){return}
    $row=$script:viewRows[$e.RowIndex]
    if([int]$row.occurrence -eq $script:currentOccurrence){
        $e.CellStyle.Font=$boldGridFont
    }
})
$grid.Add_CellDoubleClick({param($sender,$e)if($e.RowIndex -ge 0){Action-PlayNow}})
$search.Add_TextChanged({Apply-Filter})
$kindFilter.Add_SelectedIndexChanged({Apply-Filter})
$play.Add_Click({Action-PlayNow});$prepare.Add_Click({Action-Prepare});$playNext.Add_Click({Action-PlayNext})
$replay.Add_Click({Action-Replay});$remove.Add_Click({Action-Remove});$return.Add_Click({Return-ToCurrent})
$refresh.Add_Click({[void](Load-Session -Force);[void](Load-Queue -Force);Apply-Filter;if($follow.Checked){Return-ToCurrent}})

$timer=New-Object Windows.Forms.Timer;$timer.Interval=1000
$timer.Add_Tick({
    $sessionChanged=Load-Session
    $queueChanged=Load-Queue
    if($sessionChanged){
        Apply-Filter
        if($follow.Checked){Return-ToCurrent}
    }elseif($queueChanged){
        $grid.Invalidate()
        $statusLabel.Text="VIEW $($script:viewRows.Count) / ACTIVE $($script:allRows.Count)  |  CURRENT OCC $($script:currentOccurrence)  |  "+$(if(Live-Engine){'LIVE'}else{'OFFLINE BROWSE'})
        if($follow.Checked){Return-ToCurrent}
    }
    $live=Live-Engine
    $play.Enabled=$live;$prepare.Enabled=$live;$playNext.Enabled=$live;$replay.Enabled=$live;$remove.Enabled=$live
})
[void](Load-Session -Force);[void](Load-Queue -Force);Apply-Filter
$timer.Start()
try{[void]$form.ShowDialog()}finally{$timer.Stop();$timer.Dispose();$boldGridFont.Dispose();$form.Dispose()}
