param(
    [string]$CandidatePath='',
    [string]$FilterLabel='',
    [switch]$All,
    [switch]$NoOpen
)

$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData

$reports=Join-Path $script:DataRoot 'reports'
New-Item -ItemType Directory -Force -Path $reports | Out-Null
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$csvPath=Join-Path $reports ("unavailable-track-audit-"+$stamp+".csv")
$txtPath=Join-Path $reports ("unavailable-track-audit-"+$stamp+".txt")
$yt=Join-Path $script:InstallRoot 'runtime\yt-dlp\yt-dlp.exe'
$deno=Join-Path $script:InstallRoot 'runtime\deno\deno.exe'
$cacheDir=Join-Path $script:DataRoot 'cache\yt-dlp'
if(-not(Test-Path -LiteralPath $yt -PathType Leaf)){throw 'yt-dlp is missing from the YOMI runtime.'}

function Get-YoutubeId([string]$Url){
    if([string]::IsNullOrWhiteSpace($Url)){return ''}
    $m=[regex]::Match($Url,'(?:[?&]v=|youtu\.be/|youtube\.com/shorts/)([A-Za-z0-9_-]{6,})',[Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if($m.Success){return $m.Groups[1].Value}
    return ''
}
function Read-Json([string]$Path){
    try{if(Test-Path -LiteralPath $Path -PathType Leaf){return Get-Content -LiteralPath $Path -Raw -Encoding UTF8|ConvertFrom-Json}}catch{}
    return $null
}
function Text-Of($v){if($null -eq $v){return ''};return ([string]$v).Trim()}
function Quote-Arg([string]$Value){
    if($null -eq $Value){return '""'}
    # ProcessStartInfo.Arguments needs real quote delimiters, not backslash-quote.
    return '"' + $Value.Replace('"','\"') + '"'
}
function Canonical-Reason([string]$Reason){
    $s=(Text-Of $Reason).ToLowerInvariant()
    if($s -match 'private video'){return 'Private video'}
    if($s -match 'video unavailable'){return 'Video unavailable'}
    if($s -match 'has been removed'){return 'Removed'}
    if($s -match 'account associated with this video has been terminated'){return 'Uploader account terminated'}
    if($s -match 'no longer available due to a copyright|copyright claim'){return 'Copyright unavailable'}
    if($s -match 'blocked in your country|not available in your country'){return 'Region blocked'}
    return ''
}

$rows=@()
if($CandidatePath -and (Test-Path -LiteralPath $CandidatePath -PathType Leaf)){
    $snap=Read-Json $CandidatePath
    foreach($raw in @($snap.rows)){
        $url=Text-Of $raw.url
        if(-not $url){continue}
        $id=Text-Of $raw.youtube_id
        if(-not $id){$id=Get-YoutubeId $url}
        $rows += [pscustomobject]@{
            occurrence=[int]$raw.occurrence
            title=Text-Of $raw.title
            channel=Text-Of $raw.channel
            youtube_id=$id
            url=$url
        }
    }
    if(-not $FilterLabel){$FilterLabel=Text-Of $snap.filter}
}else{
    $playlist=Join-Path $script:DataRoot 'playlist.txt'
    if(-not(Test-Path -LiteralPath $playlist -PathType Leaf)){throw 'playlist.txt was not found.'}
    $library=Read-Json (Join-Path $script:DataRoot 'state\controller-library.json')
    $session=Read-Json (Join-Path $script:DataRoot 'state\session.json')
    $pool=Read-Json (Join-Path $script:DataRoot 'pool-current.json')
    $byId=@{};$byUrl=@{};$byOccurrence=@{}
    foreach($t in @($library.tracks)){
        $id=Text-Of $t.youtube_id;$url=Text-Of $t.url
        if(-not $id){$id=Get-YoutubeId $url}
        if($id -and -not $byId.ContainsKey($id)){$byId[$id]=$t}
        if($url -and -not $byUrl.ContainsKey($url)){$byUrl[$url]=$t}
        $last=0;[void][int]::TryParse((Text-Of $t.last_occurrence_id),[ref]$last)
        if($last -gt 0 -and -not $byOccurrence.ContainsKey($last)){$byOccurrence[$last]=$t}
    }
    foreach($t in @($pool.tracks)){
        $url=Text-Of $t.url;$id=Text-Of $t.youtube_id
        if(-not $id){$id=Text-Of $t.id};if(-not $id){$id=Get-YoutubeId $url}
        if($id -and -not $byId.ContainsKey($id)){$byId[$id]=$t}
        if($url -and -not $byUrl.ContainsKey($url)){$byUrl[$url]=$t}
    }
    foreach($t in @($session.occurrences)){
        $url=Text-Of $t.url;$id=Get-YoutubeId $url
        $position=0;[void][int]::TryParse((Text-Of $t.position),[ref]$position)
        if($position -le 0){[void][int]::TryParse((Text-Of $t.source_index),[ref]$position)}
        if($id -and -not $byId.ContainsKey($id)){$byId[$id]=$t}
        if($url -and -not $byUrl.ContainsKey($url)){$byUrl[$url]=$t}
        if($position -gt 0 -and -not $byOccurrence.ContainsKey($position)){$byOccurrence[$position]=$t}
    }
    $occ=0
    foreach($line in @(Get-Content -LiteralPath $playlist -Encoding UTF8)){
        $url=(Text-Of $line)
        if(-not $url){continue}
        $occ++
        $id=Get-YoutubeId $url
        $title='';$channel=''
        $meta=Read-Json (Join-Path $script:DataRoot ("cache\meta\track-"+$occ+".info.json"))
        if($meta){
            $title=Text-Of $meta.title
            if($meta.channel){$channel=Text-Of $meta.channel}
            elseif($meta.uploader){$channel=Text-Of $meta.uploader}
            elseif($meta.channel_name){$channel=Text-Of $meta.channel_name}
            if(-not $id){$id=Text-Of $meta.id}
        }
        if(-not $title -or -not $channel){
            $lib=$null
            if($id -and $byId.ContainsKey($id)){$lib=$byId[$id]}
            elseif($byUrl.ContainsKey($url)){$lib=$byUrl[$url]}
            elseif($byOccurrence.ContainsKey($occ)){$lib=$byOccurrence[$occ]}
            if($lib){
                $candidateTitle=Text-Of $lib.title
                if($candidateTitle -match '^Track\s+\d+$'){$candidateTitle=''}
                if(-not $title){$title=$candidateTitle}
                if(-not $channel){
                    $channel=Text-Of $lib.channel
                    if(-not $channel){$channel=Text-Of $lib.uploader}
                    if(-not $channel){$channel=Text-Of $lib.channel_name}
                }
                if(-not $id){$id=Text-Of $lib.youtube_id}
                if(-not $id){$id=Text-Of $lib.id}
                if(-not $id){$id=Get-YoutubeId $url}
            }
        }
        if(-not $All -and $FilterLabel){
            $hay=($title+' '+$channel+' '+$id+' '+$url)
            if($hay.IndexOf($FilterLabel,[StringComparison]::OrdinalIgnoreCase) -lt 0){continue}
        }
        $rows += [pscustomobject]@{occurrence=$occ;title=$title;channel=$channel;youtube_id=$id;url=$url}
    }
}

if($rows.Count -eq 0){
    $nl=[Environment]::NewLine
    $suffix=if($FilterLabel){" '$FilterLabel'"}else{''}
    $msg='Unavailable Track Audit'+$nl+$nl+'No tracks matched'+$suffix+'.'
    Set-Clipboard -Value $msg
    Write-Output $msg
    exit 0
}

$unique=@{}
foreach($row in $rows){
    $key=if($row.youtube_id){$row.youtube_id}else{$row.url}
    if(-not $unique.ContainsKey($key)){$unique[$key]=$row}
}
$state=@{}
foreach($key in $unique.Keys){
    $state[$key]=[ordered]@{success=$false;method='';resolved_id='';title='';channel='';url='';reasons=@();routes=@()}
}

function Start-ProbeRoute([object[]]$ProbeRows,[string]$Route){
    if($ProbeRows.Count -eq 0){return $null}
    $batch=Join-Path $env:TEMP ("yomi-unavailable-audit-"+[Guid]::NewGuid().ToString('N')+".txt")
    try{
        [IO.File]::WriteAllLines($batch,@($ProbeRows|ForEach-Object{
            if($Route -eq 'music'){
                $id=Text-Of $_.youtube_id
                if(-not $id){$id=Get-YoutubeId $_.url}
                if($id){'https://music.youtube.com/watch?v='+$id}else{$_.url}
            }else{$_.url}
        }),(New-Object Text.UTF8Encoding($false)))
        $args=@('--batch-file',$batch,'--ignore-errors','--dump-json','--skip-download','--no-playlist','--quiet','--no-warnings','--socket-timeout','10','--retries','0','--fragment-retries','0','--extractor-retries','0','--cache-dir',$cacheDir)
        if(Test-Path -LiteralPath $deno -PathType Leaf){$args+=@('--js-runtimes',('deno:'+$deno))}
        if($Route -ne 'default' -and $Route -ne 'music'){$args+=@('--extractor-args',('youtube:player_client='+$Route))}
        $psi=New-Object Diagnostics.ProcessStartInfo
        $psi.FileName=$yt
        $psi.WorkingDirectory=$script:DataRoot
        $psi.UseShellExecute=$false
        $psi.CreateNoWindow=$true
        $psi.RedirectStandardOutput=$true
        $psi.RedirectStandardError=$true
        $psi.Arguments=(@($args|ForEach-Object{Quote-Arg ([string]$_)}) -join ' ')
        $p=New-Object Diagnostics.Process
        $p.StartInfo=$psi
        [void]$p.Start()
        # Collect both streams asynchronously so parallel workers cannot deadlock
        # when yt-dlp emits several megabytes of metadata or diagnostics.
        $outTask=$p.StandardOutput.ReadToEndAsync()
        $errTask=$p.StandardError.ReadToEndAsync()
        $limitMs=[Math]::Min(90000,[Math]::Max(20000,$ProbeRows.Count*6000))
        return [pscustomobject]@{
            Process=$p; StdoutTask=$outTask; StderrTask=$errTask;
            Started=[DateTime]::UtcNow; LimitMs=$limitMs; Batch=$batch;
            Rows=@($ProbeRows); Route=$Route
        }
    }catch{
        Remove-Item -LiteralPath $batch -Force -ErrorAction SilentlyContinue
        throw
    }
}

function Finish-ProbeRoute($Handle){
    $ProbeRows=@($Handle.Rows)
    $Route=[string]$Handle.Route
    $p=$Handle.Process
    $batch=[string]$Handle.Batch
    try{
        $elapsed=([DateTime]::UtcNow-$Handle.Started).TotalMilliseconds
        $timedOut=($elapsed -ge $Handle.LimitMs -and -not $p.HasExited)
        if($timedOut){try{$p.Kill()}catch{}}
        try{$p.WaitForExit(5000)|Out-Null}catch{}
        $stdout='';$stderr=''
        try{if($Handle.StdoutTask.Wait(5000)){$stdout=$Handle.StdoutTask.Result}}catch{}
        try{if($Handle.StderrTask.Wait(5000)){$stderr=$Handle.StderrTask.Result}}catch{}
        if($timedOut){$stderr+=[Environment]::NewLine+'YOMI audit deadline exceeded for batch'}
        foreach($line in @($stdout -split "\r?\n")){
            if([string]::IsNullOrWhiteSpace($line)){continue}
            try{
                $j=$line|ConvertFrom-Json
                $id=Text-Of $j.id
                $url=Text-Of $j.webpage_url
                $sourceUrl=Text-Of $j.original_url
                $key=''
                $ids=@($id,(Get-YoutubeId $sourceUrl),(Get-YoutubeId $url))|Where-Object{$_}
                foreach($candidate in $ids){
                    if($state.ContainsKey($candidate)){$key=$candidate;break}
                }
                if(-not $key){
                    $found=$ProbeRows|Where-Object{
                        $_.url -eq $url -or $_.url -eq $sourceUrl -or
                        ($_.youtube_id -and ($sourceUrl -like ('*v='+$_.youtube_id+'*')))
                    }|Select-Object -First 1
                    if($found){$key=if($found.youtube_id){$found.youtube_id}else{$found.url}}
                }
                if($key -and $state.ContainsKey($key)){
                    $s=$state[$key];$s.success=$true;$s.routes+=($Route+':OK')
                    $s.method=if($Route -eq 'music'){'YOUTUBE_MUSIC'}else{'YOUTUBE'}
                    $s.resolved_id=$id
                    $s.title=Text-Of $j.title
                    if($j.channel){$s.channel=Text-Of $j.channel}
                    elseif($j.uploader){$s.channel=Text-Of $j.uploader}
                    elseif($j.channel_name){$s.channel=Text-Of $j.channel_name}
                    $s.url=if($url){$url}else{Text-Of $j.original_url}
                }
            }catch{}
        }

        foreach($line in @($stderr -split "\r?\n")){
            if([string]::IsNullOrWhiteSpace($line)){continue}
            $m=[regex]::Match($line,'\[youtube\]\s+([A-Za-z0-9_-]{6,})\s*:\s*(.+)$')
            if(-not $m.Success){continue}
            $id=$m.Groups[1].Value;$reason=$m.Groups[2].Value.Trim()
            if($state.ContainsKey($id)){
                $state[$id].reasons+=($Route+': '+$reason)
                $state[$id].routes+=($Route+':FAIL')
            }
        }
        # Never silently call a batch 'inconclusive': preserve useful extractor
        # diagnostics even when yt-dlp did not emit a per-video error or JSON.
        $general=@($stderr -split "\r?\n"|Where-Object{$_ -match 'ERROR:|WARNING:|timeout|failed'}|Select-Object -Last 2) -join ' | '
        if(-not $general){$general='No matched JSON output (extractor or network failure)'}
        if($general.Length -gt 500){$general=$general.Substring(0,500)+'...'}
        foreach($row in $ProbeRows){
            $key=if($row.youtube_id){$row.youtube_id}else{$row.url}
            if($state.ContainsKey($key) -and -not $state[$key].success){
                $state[$key].reasons+=($Route+': '+$general)
                $state[$key].routes+=($Route+':NO_METADATA')
            }
        }
    }finally{
        Remove-Item -LiteralPath $batch -Force -ErrorAction SilentlyContinue
        try{$p.Dispose()}catch{}
    }
}

# The controller consumes these lightweight progress markers as the audit proceeds.
# Metadata checks never play the tracks or download their audio.
function Write-AuditProgress([int]$Completed,[int]$Total,[string]$Stage){
    $safeStage=($Stage -replace '[\r\n|]',' ').Trim()
    Write-Output ('YOMI_AUDIT_PROGRESS|{0}|{1}|{2}' -f [Math]::Max(0,$Completed),[Math]::Max(1,$Total),$safeStage)
}
$routes=@('default','web_embedded,default','android,default','music')
$progressTotal=[Math]::Max(1,$unique.Count*$routes.Count)
Write-AuditProgress 0 $progressTotal ('Beginning audit of '+$unique.Count+' unique tracks')
for($routeIndex=0;$routeIndex -lt $routes.Count;$routeIndex++){
    $route=$routes[$routeIndex]
    $pending=@()
    foreach($key in $unique.Keys){if(-not $state[$key].success){$pending+=$unique[$key]}}
    if($pending.Count -eq 0){
        Write-AuditProgress $progressTotal $progressTotal 'All tracks resolved; creating report'
        break
    }
    # Run four independent, bounded yt-dlp batches together rather than waiting
    # for a single slow batch before starting the next.
    $chunkSize=12
    $maxConcurrent=4
    $nextOffset=0
    $completedRows=0
    $active=@()
    Write-AuditProgress ($routeIndex*$unique.Count) $progressTotal ('Checking '+$route+' ('+$pending.Count+' tracks remaining)')
    while($nextOffset -lt $pending.Count -or $active.Count -gt 0){
        while($active.Count -lt $maxConcurrent -and $nextOffset -lt $pending.Count){
            $last=[Math]::Min($pending.Count-1,$nextOffset+$chunkSize-1)
            $slice=@($pending[$nextOffset..$last])
            $handle=Start-ProbeRoute -ProbeRows $slice -Route $route
            if($handle){$active+=,$handle}
            $nextOffset=$last+1
        }
        $remaining=@()
        $progressed=$false
        foreach($handle in $active){
            $elapsed=([DateTime]::UtcNow-$handle.Started).TotalMilliseconds
            if($handle.Process.HasExited -or $elapsed -ge $handle.LimitMs){
                Finish-ProbeRoute $handle
                $completedRows+=$handle.Rows.Count
                $progressed=$true
                Write-AuditProgress ($routeIndex*$unique.Count+$completedRows) $progressTotal ('Checking '+$route+': '+$completedRows+' of '+$pending.Count+' attempted')
            }else{
                $remaining+=,$handle
            }
        }
        $active=@($remaining)
        if(-not $progressed){Start-Sleep -Milliseconds 150}
    }
    Write-AuditProgress (($routeIndex+1)*$unique.Count) $progressTotal ('Completed '+$route+'; '+($routeIndex+1)+' of '+$routes.Count+' routes')
}
Write-AuditProgress $progressTotal $progressTotal 'Writing CSV and text reports'

$results=@()
foreach($row in $rows){
    $key=if($row.youtube_id){$row.youtube_id}else{$row.url}
    $s=$state[$key]
    $title=if($row.title){$row.title}else{$s.title}
    $channel=if($row.channel){$row.channel}else{$s.channel}
    $canonical=@($s.reasons|ForEach-Object{Canonical-Reason $_}|Where-Object{$_}|Select-Object -Unique)
    # Extraction failures do not establish browser unavailability (e.g. Maize).
    $status=if($s.success){if($s.method -eq 'YOUTUBE_MUSIC'){'AVAILABLE_MUSIC'}else{'AVAILABLE_YOUTUBE'}}
        elseif($canonical.Count -gt 0){'EXTRACTOR_UNAVAILABLE'}else{'INCONCLUSIVE'}
    $reason=if($s.success){$s.method}
        elseif($canonical.Count -gt 0){($canonical -join '; ')+' | '+($s.reasons -join ' | ')}
        else{($s.reasons -join ' | ')}
    $results += [pscustomobject]@{
        occurrence=$row.occurrence
        status=$status
        title=$title
        channel=$channel
        youtube_id=$row.youtube_id
        resolved_id=$s.resolved_id
        playback_method=$s.method
        reason=$reason
        url=$row.url
    }
}

$results|Sort-Object occurrence|Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding UTF8
$unavailable=@($results|Where-Object{$_.status -eq 'EXTRACTOR_UNAVAILABLE'}|Sort-Object occurrence)
$inconclusive=@($results|Where-Object{$_.status -eq 'INCONCLUSIVE'}|Sort-Object occurrence)
$youtube=@($results|Where-Object{$_.status -eq 'AVAILABLE_YOUTUBE'})
$music=@($results|Where-Object{$_.status -eq 'AVAILABLE_MUSIC'})

$label=if($FilterLabel){"Filter: $FilterLabel"}else{'Filter: all tracks'}
$out=New-Object Collections.Generic.List[string]
$out.Add('YOMI UNAVAILABLE TRACK AUDIT')
$out.Add($label)
$out.Add('Scanned: '+$results.Count+' occurrences / '+$unique.Count+' unique sources')
$out.Add('YouTube available: '+$youtube.Count+' | YouTube Music available: '+$music.Count+' | Extractor unavailable: '+$unavailable.Count+' | Inconclusive: '+$inconclusive.Count)
$out.Add('CSV: '+$csvPath)
$out.Add('')
$out.Add('EXTRACTOR UNAVAILABLE (browser playback not ruled out)')
if($unavailable.Count -eq 0){$out.Add('(none)')}
foreach($r in $unavailable){
    $name=if($r.title){$r.title}else{'(title unavailable)'}
    $chan=if($r.channel){$r.channel}else{'(channel unavailable)'}
    $out.Add(('{0} | {1} | {2} | {3} | {4}' -f $r.occurrence,$name,$chan,$r.youtube_id,$r.reason))
    $out.Add('  '+$r.url)
}
if($inconclusive.Count -gt 0){
    $out.Add('')
    $out.Add('INCONCLUSIVE / NETWORK OR EXTRACTOR ERROR')
    foreach($r in $inconclusive){
        $name=if($r.title){$r.title}else{'(title unavailable)'}
        $chan=if($r.channel){$r.channel}else{'(channel unavailable)'}
        $out.Add(('{0} | {1} | {2} | {3} | {4}' -f $r.occurrence,$name,$chan,$r.youtube_id,$r.reason))
    }
}
$text=$out -join [Environment]::NewLine
[IO.File]::WriteAllText($txtPath,$text,(New-Object Text.UTF8Encoding($false)))
try{Set-Clipboard -Value $text}catch{}
Write-Output $text
if(-not $NoOpen){try{Start-Process explorer.exe -ArgumentList @('/select,',('"'+$csvPath+'"'))}catch{}}
