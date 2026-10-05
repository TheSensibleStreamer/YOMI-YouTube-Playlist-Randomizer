$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms;Add-Type -AssemblyName System.Drawing
. (Join-Path $PSScriptRoot 'common.ps1');Initialize-YomiData
$yomiVersion=Get-YomiVersionText
$created=$false;$mutex=New-Object System.Threading.Mutex($true,'Local\YOMI_CONTROLLER_V4',[ref]$created);if(-not $created){exit 0}
$pipeName='yomi-v4';$supervisor=Join-Path $PSScriptRoot 'supervisor.ps1';$shuffleScript=Join-Path $PSScriptRoot 'shuffle.ps1';$launcher=Join-Path $PSScriptRoot 'YomiLauncher.exe';$iconPath=Join-Path $InstallRoot 'assets\yomi-v408.ico'
$stateRoot=Join-Path $DataRoot 'state';$enginePidFile=Join-Path $stateRoot 'engine.pid';$serverPidFile=Join-Path $stateRoot 'server.pid';$supervisorPidFile=Join-Path $stateRoot 'supervisor.pid';$engineStatusFile=Join-Path $stateRoot 'engine-status.json';$supervisorStatusFile=Join-Path $stateRoot 'supervisor-status.txt';$currentFile=Join-Path $stateRoot 'current.json';$queueRuntimeFile=Join-Path $stateRoot 'queue-runtime.json';$orderFile=Join-Path $stateRoot 'session-order.json';$shuffleStatusFile=Join-Path $stateRoot 'shuffle-status.txt';$shuffleRequestFile=Join-Path $stateRoot 'shuffle-request.txt';$restartRequestFile=Join-Path $stateRoot 'restart-request.txt';$playlistFile=Join-Path $DataRoot 'playlist.txt';$sessionFile=Join-Path $stateRoot 'session.json';$historyFile=Join-Path $stateRoot 'history.jsonl';$metaDir=Join-Path $DataRoot 'cache\meta';$audioDir=Join-Path $DataRoot 'cache\audio';$artDir=Join-Path $DataRoot 'cache\artwork';$videoDir=Join-Path $DataRoot 'cache\video';$vizDir=Join-Path $DataRoot 'cache\visualizer';$gainDir=Join-Path $DataRoot 'cache\gain';$statusDir=Join-Path $DataRoot 'cache\status';$objectRoot=Join-Path $DataRoot 'cache\objects'
function LivePid($p){if(-not(Test-Path $p)){return 0};$n=0;try{[void][int]::TryParse((Get-Content $p -Raw).Trim(),[ref]$n)}catch{return 0};if($n -gt 0 -and (Get-Process -Id $n -ErrorAction SilentlyContinue)){return $n};return 0}
function Running{return (LivePid $enginePidFile)-gt 0};function Starting{return (LivePid $supervisorPidFile)-gt 0}
function Send-Mpv([object[]]$cmd){$pipe=$null;$writer=$null;try{$pipe=New-Object System.IO.Pipes.NamedPipeClientStream('.',$pipeName,[System.IO.Pipes.PipeDirection]::Out);$pipe.Connect(120);$writer=New-Object System.IO.StreamWriter($pipe,(New-Object System.Text.UTF8Encoding($false)));$writer.AutoFlush=$true;$writer.WriteLine((@{command=$cmd}|ConvertTo-Json -Compress));return $true}catch{return $false}finally{if($writer){$writer.Dispose()};if($pipe){$pipe.Dispose()}}}
function StartEngine{if((Running) -or (Starting)){return};Remove-Item $engineStatusFile -Force -ErrorAction SilentlyContinue;Set-Content $supervisorStatusFile 'Launching YOMI...' -Encoding UTF8;$e=$supervisor.Replace("'","''");Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-Command',("& '"+$e+"'")) -WindowStyle Hidden|Out-Null}
function Stop-VerifiedYomiProcess([string]$role,[string]$pidFile){
    $pid=LivePid $pidFile;if($pid -le 0 -or $pid -eq $PID){return}
    $runtimePath=Join-Path $stateRoot 'runtime-instance.json'
    $runtime=$null;try{$runtime=Get-Content $runtimePath -Raw|ConvertFrom-Json}catch{}
    if(-not $runtime -or [int]$runtime.schema -lt 4 -or $null -eq $runtime.processes){return}

    $identity=$runtime.processes.$role
    if($null -eq $identity -or [int]$identity.pid -ne $pid){return}

    $process=Get-Process -Id $pid -ErrorAction SilentlyContinue
    if(-not $process){return}

    $actualPath=''
    try{$actualPath=[IO.Path]::GetFullPath([string]$process.MainModule.FileName)}catch{return}
    $expectedPath=''
    try{$expectedPath=[IO.Path]::GetFullPath([string]$identity.executable_path)}catch{return}
    if(-not [string]::Equals($actualPath,$expectedPath,[StringComparison]::OrdinalIgnoreCase)){return}

    try{
        $actualTicks=[int64]$process.StartTime.ToUniversalTime().Ticks
        $expectedTicks=[int64]$identity.start_ticks_utc
        if($expectedTicks -le 0 -or [Math]::Abs($actualTicks-$expectedTicks) -gt [TimeSpan]::TicksPerSecond){return}
    }catch{return}

    $expectedHash=[string]$identity.executable_sha256
    if(-not [string]::IsNullOrWhiteSpace($expectedHash)){
        try{
            $actualHash=(Get-FileHash $actualPath -Algorithm SHA256).Hash.ToLowerInvariant()
            if($actualHash -ne $expectedHash.ToLowerInvariant()){return}
        }catch{return}
    }

    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
}
function ForceStop{
    Stop-VerifiedYomiProcess 'engine' $enginePidFile
    Stop-VerifiedYomiProcess 'server' $serverPidFile
    Stop-VerifiedYomiProcess 'supervisor' $supervisorPidFile
    Remove-Item $enginePidFile,$serverPidFile,$supervisorPidFile -Force -ErrorAction SilentlyContinue
}
$script:stopping=$false;$script:deadline=[DateTime]::MinValue;$script:shufflePending=$false;$script:restartPending=$false;$script:shuffleProcess=$null
function BeginStop{Set-Content (Join-Path $stateRoot 'planned-stop.pending') '1' -Encoding ASCII -ErrorAction SilentlyContinue;if(-not(Running)-and -not(Starting)){$script:stopping=$false;return};$script:stopping=$true;$script:deadline=[DateTime]::UtcNow.AddSeconds(2);[void](Send-Mpv @('quit'))}
function StopForExit{
    Set-Content (Join-Path $stateRoot 'planned-stop.pending') '1' -Encoding ASCII -ErrorAction SilentlyContinue
    if(-not(Running)-and -not(Starting)){return}
    [void](Send-Mpv @('quit'))
    $d=[DateTime]::UtcNow.AddSeconds(2)
    while([DateTime]::UtcNow -lt $d){
        if(-not(Running)-and -not(Starting)){return}
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 60
    }
    ForceStop
}
function StartShuffle{
    Remove-Item $shuffleStatusFile -Force -ErrorAction SilentlyContinue
    $escaped=$shuffleScript.Replace("'","''")
    $script:shuffleProcess=Start-Process powershell.exe -ArgumentList @(
        '-NoProfile',
        '-ExecutionPolicy','Bypass',
        '-Command',
        ("& '"+$escaped+"'")
    ) -WindowStyle Hidden -PassThru
}
function RequestShuffle{$a=[System.Windows.Forms.MessageBox]::Show("Shuffle Playlist now?`r`n`r`nThis reloads the latest YouTube playlist, preserves duplicate occurrences, creates a new random order, resets cache mapping and restarts from track 1.",'Shuffle Playlist',[System.Windows.Forms.MessageBoxButtons]::YesNo,[System.Windows.Forms.MessageBoxIcon]::Question);if($a -ne [System.Windows.Forms.DialogResult]::Yes){return};if((Running) -or (Starting)){$script:shufflePending=$true;BeginStop}else{StartShuffle}}
function BeginApprovedShuffle{
    if($script:shuffleProcess -or $script:shufflePending){return}
    if((Running) -or (Starting)){
        $script:shufflePending=$true
        BeginStop
    }else{
        StartShuffle
    }
}
function BeginApprovedRestart{
    if($script:shuffleProcess -or $script:shufflePending){return}
    if((Running) -or (Starting)){$script:restartPending=$true;BeginStop}else{StartEngine}
}
function ContinueAfterStop{
    if($script:shufflePending){$script:shufflePending=$false;StartShuffle;return}
    if($script:restartPending){$script:restartPending=$false;StartEngine}
}
$compactSize=New-Object System.Drawing.Size(535,375);$expandedSize=New-Object System.Drawing.Size(1460,890)
$form=New-Object System.Windows.Forms.Form;$form.Text="YOMI $yomiVersion - YouTube OBS Music Interface";$form.StartPosition='CenterScreen';$form.Size=$compactSize;$form.MinimumSize=$compactSize;$form.MaximumSize=$expandedSize;$form.MaximizeBox=$false;$form.Font=New-Object System.Drawing.Font('Segoe UI',10);if(Test-Path $iconPath){try{$form.Icon=New-Object System.Drawing.Icon($iconPath)}catch{}}
$brand=New-Object System.Windows.Forms.Label;$brand.Text='YOMI';$brand.Font=New-Object System.Drawing.Font('Segoe UI Semibold',20);$brand.Location=New-Object System.Drawing.Point(18,10);$brand.Size=New-Object System.Drawing.Size(90,38);$form.Controls.Add($brand)
$modeLabel=New-Object System.Windows.Forms.Label;$modeLabel.Location=New-Object System.Drawing.Point(115,18);$modeLabel.Size=New-Object System.Drawing.Size(390,24);$modeLabel.ForeColor=[System.Drawing.Color]::DimGray;$form.Controls.Add($modeLabel)
$status=New-Object System.Windows.Forms.Label;$status.Location=New-Object System.Drawing.Point(20,52);$status.Size=New-Object System.Drawing.Size(485,23);$status.Font=New-Object System.Drawing.Font('Segoe UI Semibold',10);$form.Controls.Add($status)
$now=New-Object System.Windows.Forms.Label;$now.Location=New-Object System.Drawing.Point(20,78);$now.Size=New-Object System.Drawing.Size(485,45);$now.AutoEllipsis=$true;$form.Controls.Add($now)
function Btn($text,$x,$y,$w){$b=New-Object System.Windows.Forms.Button;$b.Text=$text;$b.Location=New-Object System.Drawing.Point($x,$y);$b.Size=New-Object System.Drawing.Size($w,34);$form.Controls.Add($b);return $b}
$prev=Btn 'Previous' 20 130 100;$pause=Btn 'Pause' 130 130 90;$next=Btn 'Next' 230 130 90;$startStop=Btn 'Stop YOMI' 330 130 175
$shuffle=Btn 'Shuffle Playlist' 20 174 145;$settingsBtn=Btn 'Settings' 175 174 95;$obsBtn=Btn 'OBS Setup' 280 174 105;$dataBtn=Btn 'Data Folder' 395 174 110
$hideBtn=Btn 'Hide to tray' 20 218 120;$exitBtn=Btn 'Exit controller' 150 218 125;$hideComment=Btn 'Hide comment' 285 218 120;$queueBtn=Btn 'Show queue' 415 218 90
$hint=New-Object System.Windows.Forms.Label;$hint.Text='X stops YOMI and exits. Use Hide to tray if you want playback to continue. Hide comment suppresses the current track''s Featured Comment.';$hint.Location=New-Object System.Drawing.Point(20,265);$hint.Size=New-Object System.Drawing.Size(485,48);$hint.ForeColor=[System.Drawing.Color]::DimGray;$form.Controls.Add($hint)

$queuePanel=New-Object System.Windows.Forms.Panel;$queuePanel.Location=New-Object System.Drawing.Point(530,10);$queuePanel.Size=New-Object System.Drawing.Size(900,825);$queuePanel.Visible=$false;$form.Controls.Add($queuePanel)
$queueTitle=New-Object System.Windows.Forms.Label;$queueTitle.Text='PLAYLIST NAVIGATOR';$queueTitle.Font=New-Object System.Drawing.Font('Segoe UI Semibold',14);$queueTitle.Location=New-Object System.Drawing.Point(8,8);$queueTitle.Size=New-Object System.Drawing.Size(230,32);$queuePanel.Controls.Add($queueTitle)
$queueStatus=New-Object System.Windows.Forms.Label;$queueStatus.Text='Passive session view';$queueStatus.Location=New-Object System.Drawing.Point(240,14);$queueStatus.Size=New-Object System.Drawing.Size(650,24);$queueStatus.TextAlign='MiddleRight';$queueStatus.ForeColor=[System.Drawing.Color]::DimGray;$queuePanel.Controls.Add($queueStatus)
$queueSearchLabel=New-Object System.Windows.Forms.Label;$queueSearchLabel.Text='Search session:';$queueSearchLabel.Location=New-Object System.Drawing.Point(8,46);$queueSearchLabel.Size=New-Object System.Drawing.Size(110,24);$queuePanel.Controls.Add($queueSearchLabel)
$queueSearch=New-Object System.Windows.Forms.TextBox;$queueSearch.Location=New-Object System.Drawing.Point(120,43);$queueSearch.Size=New-Object System.Drawing.Size(300,27);$queuePanel.Controls.Add($queueSearch)
$queueSearchHint=New-Object System.Windows.Forms.Label;$queueSearchHint.Text='title / channel / video ID - no network';$queueSearchHint.Location=New-Object System.Drawing.Point(430,46);$queueSearchHint.Size=New-Object System.Drawing.Size(455,24);$queueSearchHint.ForeColor=[System.Drawing.Color]::DimGray;$queuePanel.Controls.Add($queueSearchHint)
$sessionExplorerBtn=New-Object System.Windows.Forms.Button;$sessionExplorerBtn.Text='EXPLORE ALL';$sessionExplorerBtn.Location=New-Object System.Drawing.Point(735,39);$sessionExplorerBtn.Size=New-Object System.Drawing.Size(150,34);$queuePanel.Controls.Add($sessionExplorerBtn);$sessionExplorerBtn.Add_Click({$x=Join-Path $PSScriptRoot 'session-explorer.ps1';Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"' + $x + '"'))})
$queueGrid=New-Object System.Windows.Forms.DataGridView;$queueGrid.Location=New-Object System.Drawing.Point(8,78);$queueGrid.Size=New-Object System.Drawing.Size(884,480);$queueGrid.ReadOnly=$true;$queueGrid.AllowUserToAddRows=$false;$queueGrid.AllowUserToDeleteRows=$false;$queueGrid.AllowUserToResizeRows=$false;$queueGrid.MultiSelect=$false;$queueGrid.SelectionMode='FullRowSelect';$queueGrid.RowHeadersVisible=$false;$queueGrid.AutoSizeRowsMode='None';$queueGrid.RowTemplate.Height=24;$queueGrid.BackgroundColor=[System.Drawing.Color]::White;$queueGrid.BorderStyle='FixedSingle';$queuePanel.Controls.Add($queueGrid)
[void]$queueGrid.Columns.Add('Position','POSITION');[void]$queueGrid.Columns.Add('Number','OCC');[void]$queueGrid.Columns.Add('Title','TITLE');[void]$queueGrid.Columns.Add('Channel','CHANNEL');[void]$queueGrid.Columns.Add('Eta','ETA');[void]$queueGrid.Columns.Add('Risk','RISK');[void]$queueGrid.Columns.Add('Cache','STATE');[void]$queueGrid.Columns.Add('Detail','PIPELINE')
$queueGrid.Columns['Position'].Width=72;$queueGrid.Columns['Number'].Width=42;$queueGrid.Columns['Title'].Width=205;$queueGrid.Columns['Channel'].Width=115;$queueGrid.Columns['Eta'].Width=62;$queueGrid.Columns['Risk'].Width=74;$queueGrid.Columns['Cache'].Width=90;$queueGrid.Columns['Detail'].Width=205
$playSelected=New-Object System.Windows.Forms.Button;$playSelected.Text='PLAY SELECTED';$playSelected.Location=New-Object System.Drawing.Point(8,570);$playSelected.Size=New-Object System.Drawing.Size(145,38);$queuePanel.Controls.Add($playSelected)
$prepareSelected=New-Object System.Windows.Forms.Button;$prepareSelected.Text='PREPARE SELECTED';$prepareSelected.Location=New-Object System.Drawing.Point(163,570);$prepareSelected.Size=New-Object System.Drawing.Size(160,38);$queuePanel.Controls.Add($prepareSelected)
$returnNow=New-Object System.Windows.Forms.Button;$returnNow.Text='RETURN TO NOW';$returnNow.Location=New-Object System.Drawing.Point(333,570);$returnNow.Size=New-Object System.Drawing.Size(140,38);$queuePanel.Controls.Add($returnNow)
$doctorBtn=New-Object System.Windows.Forms.Button;$doctorBtn.Text='DOCTOR';$doctorBtn.Location=New-Object System.Drawing.Point(483,570);$doctorBtn.Size=New-Object System.Drawing.Size(100,38);$queuePanel.Controls.Add($doctorBtn)
$rehearseBtn=New-Object System.Windows.Forms.Button;$rehearseBtn.Text='REHEARSE NEXT 10';$rehearseBtn.Location=New-Object System.Drawing.Point(593,570);$rehearseBtn.Size=New-Object System.Drawing.Size(150,38);$queuePanel.Controls.Add($rehearseBtn)
$safeModeBtn=New-Object System.Windows.Forms.Button;$safeModeBtn.Text='SAFE MODE';$safeModeBtn.Location=New-Object System.Drawing.Point(753,570);$safeModeBtn.Size=New-Object System.Drawing.Size(137,38);$queuePanel.Controls.Add($safeModeBtn)
$freezeBtn=New-Object System.Windows.Forms.Button;$freezeBtn.Text='FREEZE WINDOW';$freezeBtn.Location=New-Object System.Drawing.Point(8,615);$freezeBtn.Size=New-Object System.Drawing.Size(145,38);$queuePanel.Controls.Add($freezeBtn)
$queueHint=New-Object System.Windows.Forms.Label;$queueHint.Text='Freeze protects exact order. Unfreeze before reordering. OCC is the stable occurrence ID; order can move without changing media identity.';$queueHint.Location=New-Object System.Drawing.Point(165,615);$queueHint.Size=New-Object System.Drawing.Size(725,38);$queueHint.ForeColor=[System.Drawing.Color]::DimGray;$queuePanel.Controls.Add($queueHint)
$playNextOrderBtn=New-Object System.Windows.Forms.Button;$playNextOrderBtn.Text='PLAY NEXT';$playNextOrderBtn.Location=New-Object System.Drawing.Point(8,660);$playNextOrderBtn.Size=New-Object System.Drawing.Size(125,38);$queuePanel.Controls.Add($playNextOrderBtn)
$moveLaterOrderBtn=New-Object System.Windows.Forms.Button;$moveLaterOrderBtn.Text='MOVE +5';$moveLaterOrderBtn.Location=New-Object System.Drawing.Point(143,660);$moveLaterOrderBtn.Size=New-Object System.Drawing.Size(115,38);$queuePanel.Controls.Add($moveLaterOrderBtn)
$reshuffleOrderBtn=New-Object System.Windows.Forms.Button;$reshuffleOrderBtn.Text='RESHUFFLE TAIL';$reshuffleOrderBtn.Location=New-Object System.Drawing.Point(268,660);$reshuffleOrderBtn.Size=New-Object System.Drawing.Size(145,38);$queuePanel.Controls.Add($reshuffleOrderBtn)
$undoOrderBtn=New-Object System.Windows.Forms.Button;$undoOrderBtn.Text='UNDO ORDER';$undoOrderBtn.Location=New-Object System.Drawing.Point(423,660);$undoOrderBtn.Size=New-Object System.Drawing.Size(125,38);$queuePanel.Controls.Add($undoOrderBtn)
$redoOrderBtn=New-Object System.Windows.Forms.Button;$redoOrderBtn.Text='REDO ORDER';$redoOrderBtn.Location=New-Object System.Drawing.Point(558,660);$redoOrderBtn.Size=New-Object System.Drawing.Size(125,38);$queuePanel.Controls.Add($redoOrderBtn)
$restoreOrderBtn=New-Object System.Windows.Forms.Button;$restoreOrderBtn.Text='RESTORE ORDER';$restoreOrderBtn.Location=New-Object System.Drawing.Point(693,660);$restoreOrderBtn.Size=New-Object System.Drawing.Size(145,38);$queuePanel.Controls.Add($restoreOrderBtn)
$replayOrderBtn=New-Object System.Windows.Forms.Button;$replayOrderBtn.Text='REPLAY NEXT';$replayOrderBtn.Location=New-Object System.Drawing.Point(8,710);$replayOrderBtn.Size=New-Object System.Drawing.Size(130,38);$queuePanel.Controls.Add($replayOrderBtn)
$removeOrderBtn=New-Object System.Windows.Forms.Button;$removeOrderBtn.Text='REMOVE';$removeOrderBtn.Location=New-Object System.Drawing.Point(148,710);$removeOrderBtn.Size=New-Object System.Drawing.Size(105,38);$queuePanel.Controls.Add($removeOrderBtn)
$shuffleUnreadyBtn=New-Object System.Windows.Forms.Button;$shuffleUnreadyBtn.Text='SHUFFLE UNREADY';$shuffleUnreadyBtn.Location=New-Object System.Drawing.Point(263,710);$shuffleUnreadyBtn.Size=New-Object System.Drawing.Size(155,38);$queuePanel.Controls.Add($shuffleUnreadyBtn)
$selfTestBtn=New-Object System.Windows.Forms.Button;$selfTestBtn.Text='SELF TEST';$selfTestBtn.Location=New-Object System.Drawing.Point(428,710);$selfTestBtn.Size=New-Object System.Drawing.Size(120,38);$queuePanel.Controls.Add($selfTestBtn)
$supportBundleBtn=New-Object System.Windows.Forms.Button;$supportBundleBtn.Text='SUPPORT BUNDLE';$supportBundleBtn.Location=New-Object System.Drawing.Point(558,710);$supportBundleBtn.Size=New-Object System.Drawing.Size(160,38);$queuePanel.Controls.Add($supportBundleBtn)
$opsHint=New-Object System.Windows.Forms.Label;$opsHint.Text='Replay creates a new occurrence that shares the same media object. Remove/Replay/Shuffle Unready are fully Undo/Redo journaled.';$opsHint.Location=New-Object System.Drawing.Point(8,762);$opsHint.Size=New-Object System.Drawing.Size(875,44);$opsHint.ForeColor=[System.Drawing.Color]::DimGray;$queuePanel.Controls.Add($opsHint)

$script:queueVisible=$false;$script:playlistStamp=0L;$script:sessionStamp=0L;$script:queueRuntimeStamp=0L;$script:orderStamp=0L;$script:sessionId='';$script:baseOccurrenceCount=0;$script:orderLoadedSessionId='';$script:sessionOrder=@();$script:orderPosition=@{};$script:orderRevision=0;$script:playlistLines=@();$script:sessionMeta=@{};$script:metaCache=@{};$script:queueRuntimeByIndex=@{};$script:queueRuntimeSummary=$null;$script:nextQueueRefresh=[DateTime]::MinValue
$script:controllerConfig=$null;$script:controllerConfigStamp=0L;$script:controllerConfigPath=Join-Path $DataRoot 'config.json'
function Get-ControllerConfigCached {
    $stamp=0L
    if(Test-Path $script:controllerConfigPath){$stamp=(Get-Item $script:controllerConfigPath -ErrorAction SilentlyContinue).LastWriteTimeUtc.Ticks}
    if($null -eq $script:controllerConfig -or $stamp -ne $script:controllerConfigStamp){$script:controllerConfig=Get-YomiConfig;$script:controllerConfigStamp=$stamp}
    return $script:controllerConfig
}
function Refresh-PlaylistIndex {
    if(-not(Test-Path $playlistFile)){$script:playlistLines=@();$script:sessionMeta=@{};return}
    $stamp=(Get-Item $playlistFile -ErrorAction SilentlyContinue).LastWriteTimeUtc.Ticks
    if($stamp -ne $script:playlistStamp){$script:playlistStamp=$stamp;$script:playlistLines=@(Get-Content $playlistFile | Where-Object {$_ -match '^https?://' });$script:metaCache=@{}}

    $sessionStamp=0L
    if(Test-Path $sessionFile){$sessionStamp=(Get-Item $sessionFile -ErrorAction SilentlyContinue).LastWriteTimeUtc.Ticks}
    if($sessionStamp -ne $script:sessionStamp){
        $script:sessionStamp=$sessionStamp
        $script:sessionId=''
        $script:sessionMeta=@{}
        if($sessionStamp -gt 0){
            try{
                $session=Get-Content $sessionFile -Raw|ConvertFrom-Json
                $script:sessionId=[string]$session.session_id
                $script:baseOccurrenceCount=[int]$session.count
                foreach($entry in @($session.occurrences)){
                    $index=[int]$entry.position
                    if($index -lt 1){continue}
                    $title=if($entry.title){[string]$entry.title}else{"Track $index (metadata pending)"}
                    $script:sessionMeta[[string]$index]=[PSCustomObject]@{
                        title=$title
                        channel=[string]$entry.channel
                        id=[string]$entry.id
                        source_key=[string]$entry.source_key
                        duration=[double]$entry.duration
                        duplicate_ordinal=[int]$entry.duplicate_ordinal
                        duplicate_count=[int]$entry.duplicate_count
                    }
                }
            }catch{$script:sessionMeta=@{}}
        }
    }
}
function Refresh-SessionOrder {
    $base=[int]$script:baseOccurrenceCount
    if($base -lt 1){$base=$script:playlistLines.Count}
    if($base -lt 1){$script:sessionOrder=@();$script:orderPosition=@{};return}

    # Strip previously materialized generated records before rebuilding from the
    # authoritative order sidecar.
    if($script:playlistLines.Count -gt $base){$script:playlistLines=@($script:playlistLines[0..($base-1)])}
    foreach($key in @($script:sessionMeta.Keys)){if([int]$key -gt $base){$script:sessionMeta.Remove($key)}}

    $stamp=0L;if(Test-Path $orderFile){$stamp=(Get-Item $orderFile -ErrorAction SilentlyContinue).LastWriteTimeUtc.Ticks}
    if($stamp -eq $script:orderStamp -and $script:orderLoadedSessionId -eq $script:sessionId -and $script:sessionOrder.Count -gt 0){return}
    $script:orderStamp=$stamp
    $order=@();$revision=0;$registryCount=$base

    if($stamp -gt 0){
        try{
            $saved=Get-Content $orderFile -Raw|ConvertFrom-Json
            if([string]$saved.session_id -eq $script:sessionId){
                $expected=$base+1
                foreach($entry in @($saved.inserted|Sort-Object {[int]$_.occurrence_id})){
                    $id=[int]$entry.occurrence_id
                    if($id -ne $expected -or [string]$entry.url -notmatch '^https?://'){throw "Invalid generated occurrence registry"}
                    $script:playlistLines += [string]$entry.url
                    $script:sessionMeta[[string]$id]=[PSCustomObject]@{
                        title=$(if($entry.title){[string]$entry.title}else{"Replay occurrence $id"})
                        channel=[string]$entry.channel
                        id=[string]$entry.youtube_id
                        source_key=[string]$entry.source_key
                        duration=[double]$entry.duration
                        duplicate_ordinal=0
                        duplicate_count=0
                        generated=$true
                        source_occurrence=[int]$entry.source_occurrence
                    }
                    $registryCount=$id;$expected++
                }

                $candidate=@($saved.order|ForEach-Object{[int]$_})
                $unique=@($candidate|Sort-Object -Unique)
                $bad=@($candidate|Where-Object{$_ -lt 1 -or $_ -gt $registryCount})
                if($candidate.Count -ge 1 -and $candidate.Count -le $registryCount -and $unique.Count -eq $candidate.Count -and $bad.Count -eq 0){
                    $order=$candidate;$revision=[int]$saved.revision
                }
            }
        }catch{$order=@()}
    }

    if($order.Count -lt 1){$order=@(1..$base);$revision=0}
    $script:sessionOrder=$order;$script:orderRevision=$revision;$script:orderLoadedSessionId=$script:sessionId;$script:orderPosition=@{}
    for($slot=0;$slot -lt $order.Count;$slot++){$script:orderPosition[[string][int]$order[$slot]]=$slot}
}
function Get-TrackMeta([int]$index) {
    $path=Join-Path $metaDir ("track-$index.info.json")
    if(-not(Test-Path $path)){
        $fallback=$script:sessionMeta[[string]$index]
        if($fallback){return $fallback}
        return [PSCustomObject]@{title="Track $index (metadata pending)";channel=''}
    }
    $stamp=(Get-Item $path -ErrorAction SilentlyContinue).LastWriteTimeUtc.Ticks;$key=[string]$index;$cached=$script:metaCache[$key]
    if($cached -and $cached.Stamp -eq $stamp){return $cached.Value}
    try{$raw=Get-Content $path -Raw|ConvertFrom-Json;$value=[PSCustomObject]@{title=if($raw.title){[string]$raw.title}else{"Track $index"};channel=if($raw.channel){[string]$raw.channel}elseif($raw.uploader){[string]$raw.uploader}else{''}}}
    catch{$value=[PSCustomObject]@{title="Track $index (metadata pending)";channel=''}}
    $script:metaCache[$key]=[PSCustomObject]@{Stamp=$stamp;Value=$value};return $value
}
function Config-WantsModule($config,[string]$module) {
    if([string]$config.app_mode -ne 'Streamer / OBS'){return $false}
    if($module -eq 'visualizer' -and -not(Test-YomiComponent 'ffmpeg')){return $false}
    switch($module){'artwork'{if([bool]$config.artwork_enabled){return $true}}'video'{if([bool]$config.video_enabled){return $true}}'visualizer'{if([bool]$config.visualizer_enabled){return $true}}}
    if([bool]$config.director_mode){
        foreach($source in @($config.director_fixed_sources)){if([bool]$source.enabled -and [string]$source.module -eq $module){return $true}}
        foreach($output in @($config.director_outputs)){if([bool]$output.enabled -and @(([string]$output.modules).ToLowerInvariant().Split(',')|ForEach-Object{$_.Trim()}) -contains $module){return $true}}
    }
    return $false
}
function Get-CacheLabel([int]$index,$config) {
    $meta=$script:sessionMeta[[string]$index]
    $store=$script:queueRuntimeSummary.object_store
    if($meta -and $store -and [string]$meta.source_key){
        $key=[string]$meta.source_key
        $audio=Join-Path $objectRoot ("audio\$key-$([string]$store.audio_signature).audio")
        $info=Join-Path $objectRoot ("meta\$key-$([string]$store.audio_signature).info.json")
        $gain=Join-Path $objectRoot ("gain\$key-$([string]$store.gain_signature).gain")
        if(-not((Test-Path $audio)-and(Test-Path $info)-and(Test-Path $gain))){return 'WAITING'}

        if(Config-WantsModule $config 'artwork'){
            $ok=$false;foreach($ext in @('jpg','jpeg','png','webp')){if(Test-Path (Join-Path $objectRoot ("artwork\$key-$([string]$store.artwork_signature).$ext"))){$ok=$true;break}}
            if(-not $ok){$ok=Test-Path (Join-Path $statusDir "track-$index.artwork.failed")};if(-not $ok){return 'BUILDING'}
        }
        if(Config-WantsModule $config 'video'){
            $ok=(Test-Path (Join-Path $objectRoot ("video\$key-$([string]$store.video_signature).mp4"))) -or (Test-Path (Join-Path $statusDir "track-$index.video.failed"));if(-not $ok){return 'BUILDING'}
        }
        if(Config-WantsModule $config 'visualizer'){
            $ok=(Test-Path (Join-Path $objectRoot ("visualizer\$key-$([string]$store.visualizer_signature).mp4"))) -or (Test-Path (Join-Path $statusDir "track-$index.visualizer.failed"));if(-not $ok){return 'BUILDING'}
        }
        return 'READY'
    }

    $audioReady=(Test-Path (Join-Path $audioDir "track-$index.audio")) -and (Test-Path (Join-Path $metaDir "track-$index.info.json")) -and (Test-Path (Join-Path $gainDir "track-$index.gain"))
    if(-not $audioReady){return 'WAITING'}
    return 'LEGACY READY'
}
function Refresh-QueueRuntime {
    $stamp=0L
    if(Test-Path $queueRuntimeFile){$stamp=(Get-Item $queueRuntimeFile -ErrorAction SilentlyContinue).LastWriteTimeUtc.Ticks}
    if($stamp -eq $script:queueRuntimeStamp){return}
    $script:queueRuntimeStamp=$stamp;$script:queueRuntimeByIndex=@{};$script:queueRuntimeSummary=$null
    if($stamp -lt 1){return}
    try{
        $runtime=Get-Content $queueRuntimeFile -Raw|ConvertFrom-Json
        $script:queueRuntimeSummary=$runtime
        foreach($item in @($runtime.items)){if([int]$item.index -gt 0){$script:queueRuntimeByIndex[[string][int]$item.index]=$item}}
    }catch{$script:queueRuntimeByIndex=@{};$script:queueRuntimeSummary=$null}
}
function Format-QueueTime([double]$seconds) {
    if($seconds -lt 0){return '--'}
    $s=[Math]::Max(0,[int][Math]::Round($seconds));$m=[int][Math]::Floor($s/60);$s=$s%60
    if($m -ge 60){$h=[int][Math]::Floor($m/60);$m=$m%60;return ("{0}:{1:00}" -f $h,$m)}
    return ("{0}:{1:00}" -f $m,$s)
}
function Format-Stage([string]$state) {
    switch($state){'READY'{'OK'}'ACTIVE'{'RUN'}'QUEUED'{'Q'}'NOT_REQUIRED'{'--'}'FAILED_OPTIONAL'{'FAIL'}'FAILED_PERMANENT'{'DEAD'}'DEFERRED_NETWORK'{'NET'}'DEFERRED_RESOURCE'{'RES'}default{'WAIT'}}
}
function Get-QueueDisplay([int]$index,$config) {
    $item=$script:queueRuntimeByIndex[[string]$index]
    if($item){
        $state=if([bool]$item.presentation_complete){'READY'}elseif([bool]$item.sync_ready){'READY*'}elseif([bool]$item.transition_ready){'AUDIO READY'}elseif($item.phase){([string]$item.phase).ToUpperInvariant()}else{'WAITING'}
        $a=Format-Stage ([string]$item.audio);$art=Format-Stage ([string]$item.artwork);$v=Format-Stage ([string]$item.video);$vz=Format-Stage ([string]$item.visualizer)
        if([int]$item.video_actual_height -gt 0){$v=([string][int]$item.video_actual_height)+'p';if([bool]$item.video_fallback){$v+='^'}}
        $detail="A $a | ART $art | V $v | VIZ $vz"
        if([bool]$item.pinned){$detail+=' | PIN'}
        if([bool]$item.rehearsal){$detail+=' | REH'}
        if([bool]$item.frozen){$detail+=' | LOCK'}
        if([string]$item.decision_reason){$detail+=' | WHY '+([string]$item.decision_reason)}
        if($null -ne $item.oracle_confidence){$detail+=' | ORACLE '+([int]$item.oracle_confidence)+'%'}
        return [PSCustomObject]@{State=$state;Detail=$detail;Eta=(Format-QueueTime ([double]$item.eta_seconds));Risk=[string]$item.risk}
    }
    return [PSCustomObject]@{State=(Get-CacheLabel $index $config);Detail='legacy cache view';Eta='--';Risk='--'}
}
function Add-QueueRow([string]$position,[int]$index,[string]$title,[string]$channel,[string]$eta,[string]$risk,[string]$cache,[string]$detail,[System.Drawing.Color]$color) {
    $rowIndex=$queueGrid.Rows.Add($position,$index,$title,$channel,$eta,$risk,$cache,$detail);$row=$queueGrid.Rows[$rowIndex];$row.Tag=$index;$row.DefaultCellStyle.BackColor=$color
    if($risk -eq 'URGENT'){$row.DefaultCellStyle.ForeColor=[System.Drawing.Color]::DarkRed}
    elseif($risk -eq 'AT_RISK'){$row.DefaultCellStyle.ForeColor=[System.Drawing.Color]::DarkOrange}
}
function Next-QueueCandidate([int]$from,[int]$ignoredCount) {
    Refresh-SessionOrder
    $count=$script:sessionOrder.Count
    if($count -lt 1){return 0}
    $slot=$script:orderPosition[[string]$from]
    if($null -eq $slot){$slot=-1}
    for($tries=0;$tries -lt $count;$tries++){
        $slot=([int]$slot+1)%$count
        $index=[int]$script:sessionOrder[$slot]
        if(-not(Test-Path (Join-Path $statusDir "track-$index.audio.permanent"))){return $index}
    }
    return 0
}
function Refresh-QueueView([int]$currentIndex,$config) {
    if(-not $script:queueVisible){return};Refresh-PlaylistIndex;Refresh-SessionOrder;Refresh-QueueRuntime;$count=$script:sessionOrder.Count;if($count -lt 1){$queueGrid.Rows.Clear();$queueStatus.Text='Playlist is not ready';return}
    if($currentIndex -lt 1 -or $currentIndex -gt $count){$currentIndex=1}
    $selectedIndex=0;if($queueGrid.SelectedRows.Count -gt 0){$selectedIndex=[int]$queueGrid.SelectedRows[0].Tag};$firstVisible=-1;try{$firstVisible=$queueGrid.FirstDisplayedScrollingRowIndex}catch{}
    $queueGrid.SuspendLayout();try{
        $queueGrid.Rows.Clear()
        $query=$queueSearch.Text.Trim().ToLowerInvariant()
        if($query.Length -ge 2 -and $script:sessionMeta.Count -gt 0){
            $matches=@()
            foreach($key in $script:sessionMeta.Keys){
                $idx=[int]$key
                if(-not $script:orderPosition.ContainsKey([string]$idx)){continue}
                $m=$script:sessionMeta[$key];$hay=(([string]$m.title)+' '+([string]$m.channel)+' '+([string]$m.id)).ToLowerInvariant()
                if($hay.Contains($query)){$matches+=[PSCustomObject]@{Index=$idx;Meta=$m}}
            }
            $matches=@($matches|Sort-Object Index|Select-Object -First 100)
            foreach($match in $matches){$d=Get-QueueDisplay ([int]$match.Index) $config;Add-QueueRow 'SEARCH' ([int]$match.Index) ([string]$match.Meta.title) ([string]$match.Meta.channel) ([string]$d.Eta) ([string]$d.Risk) ([string]$d.State) ([string]$d.Detail) ([System.Drawing.Color]::White)}
            $queueStatus.Text="$($matches.Count) match(es) shown  |  passive search"
        }else{
            $history=@()
            if(Test-Path $historyFile){foreach($line in @(Get-Content $historyFile -Tail 30 -ErrorAction SilentlyContinue)){try{$entry=$line|ConvertFrom-Json;if([int]$entry.index -gt 0){$history+=$entry}}catch{}}}
            if($history.Count -gt 0 -and [int]$history[-1].index -eq $currentIndex){$history=@($history|Select-Object -First ([Math]::Max(0,$history.Count-1)))}
            $history=@($history|Select-Object -Last 12)
            foreach($entry in $history){$d=Get-QueueDisplay ([int]$entry.index) $config;Add-QueueRow 'PREVIOUS' ([int]$entry.index) ([string]$entry.title) ([string]$entry.channel) ([string]$d.Eta) ([string]$d.Risk) ([string]$d.State) ([string]$d.Detail) ([System.Drawing.Color]::FromArgb(242,242,242))}
            $meta=Get-TrackMeta $currentIndex;$d=Get-QueueDisplay $currentIndex $config;$currentLabel=if(Running){'PLAYING'}else{'RESUME'};Add-QueueRow $currentLabel $currentIndex ([string]$meta.title) ([string]$meta.channel) 'NOW' 'NOW' $currentLabel ([string]$d.Detail) ([System.Drawing.Color]::FromArgb(214,245,220))
            $cursor=$currentIndex;for($offset=1;$offset -le [Math]::Min(20,$count-1);$offset++){$index=Next-QueueCandidate $cursor $count;if($index -lt 1 -or $index -eq $currentIndex){break};$cursor=$index;$meta=Get-TrackMeta $index;$position=if($offset -eq 1){'UP NEXT'}else{"+$offset"};$d=Get-QueueDisplay $index $config;Add-QueueRow $position $index ([string]$meta.title) ([string]$meta.channel) ([string]$d.Eta) ([string]$d.Risk) ([string]$d.State) ([string]$d.Detail) ([System.Drawing.Color]::White)}
            if($script:queueRuntimeSummary){$rehearsalText='';if($script:queueRuntimeSummary.rehearsal -and [bool]$script:queueRuntimeSummary.rehearsal.active){$rehearsalText="  |  REHEARSE $([int]$script:queueRuntimeSummary.rehearsal.sync_ready)/$([int]$script:queueRuntimeSummary.rehearsal.target) $([string]$script:queueRuntimeSummary.rehearsal.verdict)"}
$safeText=$(if([bool]$script:queueRuntimeSummary.safe_mode){'  |  SAFE MODE'}else{''})
$freezeText='';if($script:queueRuntimeSummary.freeze -and [bool]$script:queueRuntimeSummary.freeze.active){$freezeText="  |  FROZEN $([int]$script:queueRuntimeSummary.freeze.count)";$freezeBtn.Text='UNFREEZE WINDOW'}else{$freezeBtn.Text='FREEZE WINDOW'}
$domainText='';if($script:queueRuntimeSummary.failure_domain -and [string]$script:queueRuntimeSummary.failure_domain -ne 'OK'){$domainText="  |  $([string]$script:queueRuntimeSummary.failure_domain)"}
$queueStatus.Text="BUFFER $([string]$script:queueRuntimeSummary.buffer_health)  |  ready $([int]$script:queueRuntimeSummary.ready_ahead)/$([int]$script:queueRuntimeSummary.target_ahead)  |  $(Format-QueueTime ([double]$script:queueRuntimeSummary.ready_time_seconds)) covered  |  ORDER r$([int]$script:queueRuntimeSummary.order_revision) slot $([int]$script:queueRuntimeSummary.current_order_slot)/$count$rehearsalText$freezeText$safeText$domainText"}else{$queueStatus.Text="Track $currentIndex of $count  |  legacy queue state"}
        }
        $queueGrid.ClearSelection();$target=if($selectedIndex -gt 0){$selectedIndex}else{$currentIndex};foreach($row in $queueGrid.Rows){if([int]$row.Tag -eq $target){$row.Selected=$true;break}}
        if($firstVisible -ge 0 -and $firstVisible -lt $queueGrid.Rows.Count){$queueGrid.FirstDisplayedScrollingRowIndex=$firstVisible}elseif($query.Length -lt 2){foreach($row in $queueGrid.Rows){if([int]$row.Tag -eq $currentIndex){$queueGrid.FirstDisplayedScrollingRowIndex=[Math]::Max(0,$row.Index-3);break}}}
    }finally{$queueGrid.ResumeLayout()}
}
function Current-QueueIndex {
    if(Test-Path $currentFile){try{$s=Get-Content $currentFile -Raw|ConvertFrom-Json;if([int]$s.index -gt 0){return [int]$s.index}}catch{}}
    $resume=Join-Path $stateRoot 'resume-track.txt';if(Test-Path $resume){$n=0;if([int]::TryParse((Get-Content $resume -Raw).Trim(),[ref]$n)){return $n}}
    return 1
}
function Jump-ToQueueSelection {
    if($queueGrid.SelectedRows.Count -lt 1){return};$index=[int]$queueGrid.SelectedRows[0].Tag;if($index -lt 1){return}
    Refresh-SessionOrder
    if(-not $script:orderPosition.ContainsKey([string]$index)){return}
    if(Running){[void](Send-Mpv @('script-message','yomi-jump',[string]$index))}else{Set-Content (Join-Path $stateRoot 'resume-track.txt') ([string]$index) -Encoding ASCII;StartEngine}
}
function Prepare-QueueSelection {
    if(-not(Running) -or $queueGrid.SelectedRows.Count -lt 1){return};$index=[int]$queueGrid.SelectedRows[0].Tag;if($index -lt 1){return}
    [void](Send-Mpv @('script-message','yomi-prepare',[string]$index));$script:nextQueueRefresh=[DateTime]::MinValue
}
function Return-QueueToNow {
    $queueSearch.Clear();$script:nextQueueRefresh=[DateTime]::MinValue;Refresh-QueueView (Current-QueueIndex) (Get-ControllerConfigCached)
}
function Start-QueueRehearsal {
    if(-not(Running)){return}
    [void](Send-Mpv @('script-message','yomi-rehearse','10'))
    $script:nextQueueRefresh=[DateTime]::MinValue
}
function Restart-SafeMode {
    Set-Content (Join-Path $stateRoot 'safe-mode.once') '1' -Encoding ASCII
    BeginApprovedRestart
}
function Toggle-BroadcastFreeze {
    if(-not(Running)){return}
    [void](Send-Mpv @('script-message','yomi-freeze-toggle'))
    $script:nextQueueRefresh=[DateTime]::MinValue
}
function Selected-Occurrence {
    if($queueGrid.SelectedRows.Count -lt 1){return 0}
    return [int]$queueGrid.SelectedRows[0].Tag
}
function Order-PlayNext {$n=Selected-Occurrence;if($n -gt 0){[void](Send-Mpv @('script-message','yomi-order-play-next',[string]$n));$script:nextQueueRefresh=[DateTime]::MinValue}}
function Order-MoveLater {$n=Selected-Occurrence;if($n -gt 0){[void](Send-Mpv @('script-message','yomi-order-move-later',[string]$n,'5'));$script:nextQueueRefresh=[DateTime]::MinValue}}
function Order-ReshuffleTail {[void](Send-Mpv @('script-message','yomi-order-reshuffle-tail'));$script:nextQueueRefresh=[DateTime]::MinValue}
function Order-Undo {[void](Send-Mpv @('script-message','yomi-order-undo'));$script:nextQueueRefresh=[DateTime]::MinValue}
function Order-Redo {[void](Send-Mpv @('script-message','yomi-order-redo'));$script:nextQueueRefresh=[DateTime]::MinValue}
function Order-Restore {[void](Send-Mpv @('script-message','yomi-order-restore'));$script:nextQueueRefresh=[DateTime]::MinValue}
function Order-ReplayNext {$n=Selected-Occurrence;if($n -gt 0){[void](Send-Mpv @('script-message','yomi-order-replay-next',[string]$n));$script:nextQueueRefresh=[DateTime]::MinValue}}
function Order-Remove {$n=Selected-Occurrence;if($n -gt 0){[void](Send-Mpv @('script-message','yomi-order-remove',[string]$n));$script:nextQueueRefresh=[DateTime]::MinValue}}
function Order-ShuffleUnready {[void](Send-Mpv @('script-message','yomi-order-reshuffle-unprepared'));$script:nextQueueRefresh=[DateTime]::MinValue}
function Run-YomiSelfTest {$p=Join-Path $PSScriptRoot 'self-test.ps1';Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$p+'"'))}
function Build-YomiSupportBundle {$p=Join-Path $PSScriptRoot 'support-bundle.ps1';Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$p+'"'))}
function Set-QueueVisible([bool]$visible) {
    $script:queueVisible=$visible;$queuePanel.Visible=$visible
    if($visible){$queueBtn.Text='Hide queue';$form.Size=$expandedSize;$script:nextQueueRefresh=[DateTime]::MinValue;Refresh-QueueView (Current-QueueIndex) (Get-ControllerConfigCached)}
    else{$queueBtn.Text='Show queue';$form.Size=$compactSize}
}
$tray=New-Object System.Windows.Forms.NotifyIcon;$tray.Text='YOMI';$tray.Visible=$true;if(Test-Path $iconPath){try{$tray.Icon=New-Object System.Drawing.Icon($iconPath)}catch{$tray.Icon=[System.Drawing.SystemIcons]::Application}}else{$tray.Icon=[System.Drawing.SystemIcons]::Application}
$menu=New-Object System.Windows.Forms.ContextMenuStrip;$miShow=$menu.Items.Add('Show controller');$miPause=$menu.Items.Add('Play / Pause');$miPrev=$menu.Items.Add('Previous');$miNext=$menu.Items.Add('Next');$miHideComment=$menu.Items.Add('Hide current Featured Comment');$miShuffle=$menu.Items.Add('Shuffle Playlist...');[void]$menu.Items.Add('-');$miSettings=$menu.Items.Add('Settings');$miStop=$menu.Items.Add('Stop YOMI');$miExit=$menu.Items.Add('Exit controller');$tray.ContextMenuStrip=$menu
$prev.Add_Click({[void](Send-Mpv @('script-message','yomi-prev'))});$next.Add_Click({[void](Send-Mpv @('script-message','yomi-next'))});$pause.Add_Click({[void](Send-Mpv @('cycle','pause'))});$hideComment.Add_Click({[void](Send-Mpv @('script-message','yomi-hide-comment'))});$shuffle.Add_Click({RequestShuffle});$settingsBtn.Add_Click({Start-Process $launcher -ArgumentList 'settings'});$obsBtn.Add_Click({$c=Get-YomiConfig;if([string]$c.app_mode -ne 'Streamer / OBS'){return};$p=Write-ObsInstructions $c;Start-Process notepad.exe ('"'+$p+'"')});$dataBtn.Add_Click({Start-Process explorer.exe $DataRoot});$hideBtn.Add_Click({$form.Hide()});$queueBtn.Add_Click({Set-QueueVisible (-not $script:queueVisible)});$playSelected.Add_Click({Jump-ToQueueSelection});$prepareSelected.Add_Click({Prepare-QueueSelection});$returnNow.Add_Click({Return-QueueToNow});$doctorBtn.Add_Click({$d=Join-Path $PSScriptRoot 'doctor.ps1';Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$d+'"'))});$rehearseBtn.Add_Click({Start-QueueRehearsal});$safeModeBtn.Add_Click({Restart-SafeMode});$freezeBtn.Add_Click({Toggle-BroadcastFreeze});$playNextOrderBtn.Add_Click({Order-PlayNext});$moveLaterOrderBtn.Add_Click({Order-MoveLater});$reshuffleOrderBtn.Add_Click({Order-ReshuffleTail});$undoOrderBtn.Add_Click({Order-Undo});$redoOrderBtn.Add_Click({Order-Redo});$restoreOrderBtn.Add_Click({Order-Restore});$replayOrderBtn.Add_Click({Order-ReplayNext});$removeOrderBtn.Add_Click({Order-Remove});$shuffleUnreadyBtn.Add_Click({Order-ShuffleUnready});$selfTestBtn.Add_Click({Run-YomiSelfTest});$supportBundleBtn.Add_Click({Build-YomiSupportBundle});$queueSearch.Add_TextChanged({$script:nextQueueRefresh=[DateTime]::MinValue});$queueGrid.Add_CellDoubleClick({param($sender,$e)if($e.RowIndex -ge 0){$queueGrid.Rows[$e.RowIndex].Selected=$true;Jump-ToQueueSelection}});$startStop.Add_Click({if((Running) -or (Starting)){BeginStop}else{StartEngine}});$exitBtn.Add_Click({$script:allowClose=$true;StopForExit;$tray.Visible=$false;$form.Close()})
$miShow.Add_Click({$form.Show();$form.WindowState='Normal';$form.Activate()});$miPause.Add_Click({[void](Send-Mpv @('cycle','pause'))});$miPrev.Add_Click({[void](Send-Mpv @('script-message','yomi-prev'))});$miNext.Add_Click({[void](Send-Mpv @('script-message','yomi-next'))});$miHideComment.Add_Click({[void](Send-Mpv @('script-message','yomi-hide-comment'))});$miShuffle.Add_Click({RequestShuffle});$miSettings.Add_Click({Start-Process $launcher -ArgumentList 'settings'});$miStop.Add_Click({BeginStop});$miExit.Add_Click({$script:allowClose=$true;StopForExit;$tray.Visible=$false;$form.Close()});$tray.Add_DoubleClick({$form.Show();$form.WindowState='Normal';$form.Activate()})
$script:allowClose=$false
$form.Add_FormClosing({
    param($sender,$e)
    if($e.CloseReason -eq [System.Windows.Forms.CloseReason]::UserClosing -and -not $script:allowClose){
        $script:allowClose=$true
        StopForExit
        $tray.Visible=$false
    }
})
$timer=New-Object System.Windows.Forms.Timer;$timer.Interval=350;$timer.Add_Tick({
 if(Test-Path $restartRequestFile){try{Remove-Item $restartRequestFile -Force -ErrorAction SilentlyContinue;BeginApprovedRestart}catch{}}
 if(Test-Path $shuffleRequestFile){
    try{
        Remove-Item $shuffleRequestFile -Force -ErrorAction SilentlyContinue
        BeginApprovedShuffle
    }catch{}
 }
 if($script:stopping){if(-not(Running)-and -not(Starting)){$script:stopping=$false;ContinueAfterStop}elseif([DateTime]::UtcNow -ge $script:deadline){ForceStop;$script:stopping=$false;ContinueAfterStop}}
 if($script:shuffleProcess){if($script:shuffleProcess.HasExited){$code=$script:shuffleProcess.ExitCode;$script:shuffleProcess.Dispose();$script:shuffleProcess=$null;if($code -eq 0){StartEngine}else{$status.Text='Shuffle failed';if(Test-Path $shuffleStatusFile){try{$now.Text=(Get-Content $shuffleStatusFile -Raw).Trim()}catch{$now.Text='Shuffle Playlist failed.'}}else{$now.Text='Shuffle Playlist failed.'}}}else{$status.Text='Shuffling playlist...';if(Test-Path $shuffleStatusFile){try{$now.Text=(Get-Content $shuffleStatusFile -Raw).Trim()}catch{}};return}}
 $c=Get-ControllerConfigCached
 if($script:queueVisible -and [DateTime]::UtcNow -ge $script:nextQueueRefresh){$script:nextQueueRefresh=[DateTime]::UtcNow.AddMilliseconds(750);Refresh-QueueView (Current-QueueIndex) $c}
 $modeDetail='One-source OBS overlay'
 if([bool]$c.director_mode){$modeDetail='OBS + Director Mode'}
 if([string]$c.app_mode -eq 'Player'){$modeDetail=[string]$c.player_video_quality}
 $modeLabel.Text=([string]$c.app_mode)+'  |  '+$modeDetail
 $obsBtn.Enabled=([string]$c.app_mode -eq 'Streamer / OBS')
 $running=Running;$starting=Starting;$prepareSelected.Enabled=$running;$rehearseBtn.Enabled=$running;$freezeBtn.Enabled=$running;$playNextOrderBtn.Enabled=$running;$moveLaterOrderBtn.Enabled=$running;$reshuffleOrderBtn.Enabled=$running;$undoOrderBtn.Enabled=$running;$redoOrderBtn.Enabled=$running;$restoreOrderBtn.Enabled=$running;$replayOrderBtn.Enabled=$running;$removeOrderBtn.Enabled=$running;$shuffleUnreadyBtn.Enabled=$running;if(-not $running -and -not $starting){$status.Text='Stopped';$now.Text='YOMI is not playing.';$pause.Enabled=$false;$prev.Enabled=$false;$next.Enabled=$false;$startStop.Text='Start YOMI';$tray.Text='YOMI - Stopped';return};$startStop.Text='Stop YOMI'
 if(-not $running){$pause.Enabled=$false;$prev.Enabled=$false;$next.Enabled=$false;$status.Text='Starting...';if(Test-Path $supervisorStatusFile){try{$now.Text=(Get-Content $supervisorStatusFile -Raw).Trim()}catch{}};$tray.Text='YOMI - Starting';return}
 $pause.Enabled=$true;$prev.Enabled=$true;$next.Enabled=$true;$engine=$null;$track=$null;if(Test-Path $engineStatusFile){try{$engine=Get-Content $engineStatusFile -Raw|ConvertFrom-Json}catch{}};if(Test-Path $currentFile){try{$track=Get-Content $currentFile -Raw|ConvertFrom-Json}catch{}}
 if($engine){switch([string]$engine.phase){'paused'{$status.Text='Paused';$pause.Text='Play';$tray.Text='YOMI - Paused'}'playing'{$status.Text='Playing';$pause.Text='Pause';$tray.Text='YOMI - Playing'}'error'{$status.Text='Playback error';$tray.Text='YOMI - Error'}default{$status.Text='Preparing...';$pause.Text='Pause';$tray.Text='YOMI - Preparing'}};if($engine.message){$now.Text=[string]$engine.message}}else{$status.Text='Preparing...';$now.Text='Preparing the first playable track...'}
 if($track -and $engine -and ([string]$engine.phase -in @('playing','paused'))){$name=[string]$track.title;if($track.channel){$name+='  -  '+[string]$track.channel};$now.Text=$name}
});$timer.Start();StartEngine;$form.Add_Shown({$form.Activate();Start-Process (Join-Path $PSScriptRoot 'YomiLauncher.exe') -ArgumentList 'update-auto'});try{[void]$form.ShowDialog()}finally{$timer.Stop();$timer.Dispose();$tray.Visible=$false;$tray.Dispose();try{$mutex.ReleaseMutex()}catch{};$mutex.Dispose()}
