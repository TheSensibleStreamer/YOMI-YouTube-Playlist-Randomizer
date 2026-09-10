param([switch]$Quiet,[switch]$SafeMode,[string]$InstallRootOverride='')

$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$checks=New-Object System.Collections.Generic.List[System.Object]
function Check([string]$Severity,[string]$Code,[string]$Message,$Evidence=$null){
    $checks.Add([PSCustomObject][ordered]@{severity=$Severity;code=$Code;message=$Message;evidence=$Evidence})
}
function Read-Json([string]$Path){return Get-Content $Path -Raw|ConvertFrom-Json}

$config=$null;$contracts=$null
try{$contracts=Get-YomiContractSnapshot;Check 'PASS' 'CONTRACTS' 'Schema contract registry loaded.'}
catch{Check 'FAIL' 'CONTRACTS' $_.Exception.Message}

try{
    $config=Read-Json (Join-Path $DataRoot 'config.json')
    $schema=if($null -ne $config.PSObject.Properties['config_schema_version']){[int]$config.config_schema_version}else{1}
    if($contracts -and $schema -gt [int]$contracts.config_schema){Check 'FAIL' 'CONFIG_SCHEMA' "Config schema $schema is newer than supported $($contracts.config_schema)."}
    else{Check 'PASS' 'CONFIG' "Config schema $schema parses."}
}catch{Check 'FAIL' 'CONFIG' $_.Exception.Message}

$installRoot=if([string]::IsNullOrWhiteSpace($InstallRootOverride)){$script:InstallRoot}else{$InstallRootOverride}
foreach($required in @(
    @('MPV','runtime\mpv\mpv.exe'),
    @('YT_DLP','runtime\yt-dlp\yt-dlp.exe')
)){
    $full=Join-Path $installRoot $required[1]
    if(Test-Path $full){Check 'PASS' $required[0] "$($required[1]) present."}
    else{Check 'FAIL' $required[0] "$($required[1]) missing."}
}

$playlistPath=Join-Path $DataRoot 'playlist.txt'
if(Test-Path $playlistPath){
    $count=@(Get-Content $playlistPath -ErrorAction SilentlyContinue|Where-Object{$_ -match '^https?://'}).Count
    if($count -gt 0){Check 'PASS' 'PLAYLIST' "$count active playlist occurrence(s)."}else{Check 'FAIL' 'PLAYLIST' 'playlist.txt contains no URLs.'}
}else{
    if($config -and -not [string]::IsNullOrWhiteSpace([string]$config.playlist)){Check 'WARN' 'PLAYLIST' 'playlist.txt missing; supervisor may create it by Shuffle.'}
    else{Check 'FAIL' 'PLAYLIST' 'No active playlist and no configured playlist source.'}
}

$sessionPath=Join-Path $DataRoot 'state\session.json'
$orderPath=Join-Path $DataRoot 'state\session-order.json'
$resumePath=Join-Path $DataRoot 'state\resume-track.txt'
$session=$null;$order=$null
if(Test-Path $sessionPath){
    try{
        $session=Read-Json $sessionPath
        if($contracts -and [int]$session.schema -gt [int]$contracts.session_schema){Check 'FAIL' 'SESSION_SCHEMA' "$($session.schema) > $($contracts.session_schema)"}
        else{Check 'PASS' 'SESSION' "session_id=$($session.session_id)"}
    }catch{Check 'FAIL' 'SESSION' $_.Exception.Message}
}else{Check 'WARN' 'SESSION' 'No session snapshot; legacy/first-run path.'}

if(Test-Path $orderPath){
    try{
        $order=Read-Json $orderPath
        if($contracts -and [int]$order.schema -gt [int]$contracts.occurrence_order_schema){Check 'FAIL' 'ORDER_SCHEMA' "$($order.schema) > $($contracts.occurrence_order_schema)"}
        elseif($session -and [string]$order.session_id -ne [string]$session.session_id){Check 'FAIL' 'ORDER_SESSION' 'Mutable order belongs to a different session.'}
        else{
            $values=@($order.order|ForEach-Object{[int]$_})
            $unique=@($values|Sort-Object -Unique)
            $baseCount=if($null -ne $order.PSObject.Properties['base_count']){[int]$order.base_count}elseif($session){[int]$session.count}else{0}
            $inserted=@($order.inserted)
            $registry=New-Object 'System.Collections.Generic.HashSet[System.Int32]'
            for($n=1;$n -le $baseCount;$n++){[void]$registry.Add($n)}
            $insertedValid=$true
            foreach($entry in $inserted){
                $id=[int]$entry.occurrence_id
                if($id -le $baseCount -or -not $registry.Add($id) -or [string]$entry.url -notmatch '^https?://' -or [string]::IsNullOrWhiteSpace([string]$entry.source_key)){$insertedValid=$false;break}
            }
            $activeValid=$values.Count -ge 1 -and $unique.Count -eq $values.Count
            foreach($id in $values){if(-not $registry.Contains([int]$id)){$activeValid=$false;break}}
            if(-not $insertedValid){Check 'FAIL' 'ORDER_REGISTRY' 'Generated occurrence registry is invalid or incomplete.'}
            elseif(-not $activeValid){Check 'FAIL' 'ORDER_SET' 'Active order is empty, duplicated, or references an occurrence outside the registry.'}
            elseif($session -and $baseCount -ne [int]$session.count){Check 'FAIL' 'ORDER_BASE' "Order base_count $baseCount differs from session count $($session.count)."}
            else{Check 'PASS' 'ORDER' "revision=$($order.revision) active=$($values.Count) registry=$($registry.Count)"}

            if(Test-Path $resumePath){
                $resume=0
                [void][int]::TryParse((Get-Content $resumePath -Raw).Trim(),[ref]$resume)
                if($resume -gt 0 -and $values -notcontains $resume){Check 'WARN' 'RESUME' "Resume occurrence $resume is no longer active; engine will resolve current order."}
                else{Check 'PASS' 'RESUME' "occurrence=$resume"}
            }
        }
    }catch{Check 'FAIL' 'ORDER' $_.Exception.Message}
}

try{
    $probe=Join-Path $DataRoot ('state\.preflight-write-'+[Guid]::NewGuid().ToString('N'))
    [IO.File]::WriteAllText($probe,'ok',(New-Object System.Text.UTF8Encoding($false)))
    Remove-Item $probe -Force
    Check 'PASS' 'DATA_WRITE' 'YOMI data/state directory is writable.'
}catch{Check 'FAIL' 'DATA_WRITE' $_.Exception.Message}

try{
    $root=[IO.Path]::GetPathRoot($DataRoot)
    $drive=New-Object IO.DriveInfo($root)
    $free=[int64]$drive.AvailableFreeSpace
    if($free -lt 64MB){Check 'FAIL' 'DISK' ("Only {0:N0} MB free." -f ($free/1MB))}
    elseif($free -lt 512MB){Check 'WARN' 'DISK' ("Low free space: {0:N0} MB." -f ($free/1MB))}
    else{Check 'PASS' 'DISK' ("{0:N1} GB free." -f ($free/1GB))}
}catch{Check 'WARN' 'DISK' 'Could not query free space.'}

if($config -and [string]$config.app_mode -eq 'Streamer / OBS' -and -not $SafeMode){
    $port=[int]$config.server_port
    if($port -lt 1024 -or $port -gt 65535){Check 'FAIL' 'PORT' "Invalid server port $port."}
    else{
        $occupied=$false
        $client=New-Object Net.Sockets.TcpClient
        try{
            $async=$client.BeginConnect('127.0.0.1',$port,$null,$null)
            $occupied=$async.AsyncWaitHandle.WaitOne(150)
            if($occupied){try{$client.EndConnect($async)}catch{}}
        }catch{}finally{$client.Dispose()}
        if($occupied){Check 'FAIL' 'PORT' "127.0.0.1:$port is already accepting connections."}
        else{Check 'PASS' 'PORT' "127.0.0.1:$port available."}
    }
}

$failCount=@($checks|Where-Object{$_.severity -eq 'FAIL'}).Count
$warnCount=@($checks|Where-Object{$_.severity -eq 'WARN'}).Count
$report=[ordered]@{
    schema=1
    product='YOMI'
    report_type='startup-preflight'
    generated_utc=[DateTime]::UtcNow.ToString('o')
    verdict=$(if($failCount -gt 0){'BLOCK'}elseif($warnCount -gt 0){'WARN'}else{'GO'})
    failures=$failCount
    warnings=$warnCount
    checks=$checks.ToArray()
}
$reportPath=Join-Path $DataRoot 'state\preflight.json'
[IO.File]::WriteAllText($reportPath,($report|ConvertTo-Json -Depth 8),(New-Object System.Text.UTF8Encoding($false)))

if(-not $Quiet){
    Write-Host '===== YOMI STARTUP PREFLIGHT =====' -ForegroundColor Cyan
    foreach($c in $checks){Write-Host ("{0,-4} {1,-16} {2}" -f $c.severity,$c.code,$c.message)}
    Write-Host "Verdict: $($report.verdict)"
}
if($failCount -gt 0){exit 1}
exit 0
