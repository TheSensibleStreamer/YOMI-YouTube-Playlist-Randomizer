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
    if($null -eq $Value){return '\"\"'}
    return '\"' + (($Value -replace '\"','\\\"')) + '\"'
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
                if($candidateTitle -match '^Track\s+\d+
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
    $state[$key]=[ordered]@{success=$false;title='';channel='';url='';reasons=@();routes=@()}
}

function Invoke-ProbeRoute([object[]]$ProbeRows,[string]$Route){
    if($ProbeRows.Count -eq 0){return}
    $batch=Join-Path $env:TEMP ("yomi-unavailable-audit-"+[Guid]::NewGuid().ToString('N')+".txt")
    try{
        [IO.File]::WriteAllLines($batch,@($ProbeRows|ForEach-Object{$_.url}),(New-Object Text.UTF8Encoding($false)))
        $args=@('--batch-file',$batch,'--ignore-errors','--dump-json','--skip-download','--no-playlist','--quiet','--no-warnings','--socket-timeout','10','--retries','0','--fragment-retries','0','--extractor-retries','0','--cache-dir',$cacheDir)
        if(Test-Path -LiteralPath $deno -PathType Leaf){$args+=@('--js-runtimes',('deno:'+$deno))}
        if($Route -ne 'default'){$args+=@('--extractor-args',('youtube:player_client='+$Route))}
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
        $stdout=$p.StandardOutput.ReadToEnd()
        $stderr=$p.StandardError.ReadToEnd()
        $p.WaitForExit()

        foreach($line in @($stdout -split "\r?\n")){
            if([string]::IsNullOrWhiteSpace($line)){continue}
            try{
                $j=$line|ConvertFrom-Json
                $id=Text-Of $j.id
                $url=Text-Of $j.webpage_url
                $key=''
                if($id -and $state.ContainsKey($id)){$key=$id}
                else{
                    $found=$ProbeRows|Where-Object{$_.url -eq $url}|Select-Object -First 1
                    if($found){$key=if($found.youtube_id){$found.youtube_id}else{$found.url}}
                }
                if($key -and $state.ContainsKey($key)){
                    $s=$state[$key];$s.success=$true;$s.routes+=($Route+':OK')
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
    }finally{Remove-Item -LiteralPath $batch -Force -ErrorAction SilentlyContinue}
}

$routes=@('default','web_embedded,default','android,default')
foreach($route in $routes){
    $pending=@()
    foreach($key in $unique.Keys){if(-not $state[$key].success){$pending+=$unique[$key]}}
    if($pending.Count -eq 0){break}
    Invoke-ProbeRoute -ProbeRows $pending -Route $route
}

$results=@()
foreach($row in $rows){
    $key=if($row.youtube_id){$row.youtube_id}else{$row.url}
    $s=$state[$key]
    $title=if($row.title){$row.title}else{$s.title}
    $channel=if($row.channel){$row.channel}else{$s.channel}
    $canonical=@($s.reasons|ForEach-Object{Canonical-Reason $_}|Where-Object{$_}|Select-Object -Unique)
    $status=if($s.success){'AVAILABLE'}elseif($canonical.Count -gt 0){'UNAVAILABLE'}else{'INCONCLUSIVE'}
    $reason=if($status -eq 'UNAVAILABLE'){$canonical -join '; '}elseif($status -eq 'INCONCLUSIVE'){($s.reasons -join ' | ')}else{'Available'}
    $results += [pscustomobject]@{
        occurrence=$row.occurrence
        status=$status
        title=$title
        channel=$channel
        youtube_id=$row.youtube_id
        reason=$reason
        url=$row.url
    }
}

$results|Sort-Object occurrence|Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding UTF8
$unavailable=@($results|Where-Object{$_.status -eq 'UNAVAILABLE'}|Sort-Object occurrence)
$inconclusive=@($results|Where-Object{$_.status -eq 'INCONCLUSIVE'}|Sort-Object occurrence)
$available=@($results|Where-Object{$_.status -eq 'AVAILABLE'})

$label=if($FilterLabel){"Filter: $FilterLabel"}else{'Filter: all tracks'}
$out=New-Object Collections.Generic.List[string]
$out.Add('YOMI UNAVAILABLE TRACK AUDIT')
$out.Add($label)
$out.Add('Scanned: '+$results.Count+' occurrences / '+$unique.Count+' unique sources')
$out.Add('Unavailable: '+$unavailable.Count+' | Available: '+$available.Count+' | Inconclusive: '+$inconclusive.Count)
$out.Add('CSV: '+$csvPath)
$out.Add('')
$out.Add('UNAVAILABLE')
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
){$candidateTitle=''}
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
    $state[$key]=[ordered]@{success=$false;title='';channel='';url='';reasons=@();routes=@()}
}

function Invoke-ProbeRoute([object[]]$ProbeRows,[string]$Route){
    if($ProbeRows.Count -eq 0){return}
    $batch=Join-Path $env:TEMP ("yomi-unavailable-audit-"+[Guid]::NewGuid().ToString('N')+".txt")
    try{
        [IO.File]::WriteAllLines($batch,@($ProbeRows|ForEach-Object{$_.url}),(New-Object Text.UTF8Encoding($false)))
        $args=@('--batch-file',$batch,'--ignore-errors','--dump-json','--skip-download','--no-playlist','--quiet','--no-warnings','--socket-timeout','10','--retries','0','--fragment-retries','0','--extractor-retries','0','--cache-dir',$cacheDir)
        if(Test-Path -LiteralPath $deno -PathType Leaf){$args+=@('--js-runtimes',('deno:'+$deno))}
        if($Route -ne 'default'){$args+=@('--extractor-args',('youtube:player_client='+$Route))}
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
        $stdout=$p.StandardOutput.ReadToEnd()
        $stderr=$p.StandardError.ReadToEnd()
        $p.WaitForExit()

        foreach($line in @($stdout -split "\r?\n")){
            if([string]::IsNullOrWhiteSpace($line)){continue}
            try{
                $j=$line|ConvertFrom-Json
                $id=Text-Of $j.id
                $url=Text-Of $j.webpage_url
                $key=''
                if($id -and $state.ContainsKey($id)){$key=$id}
                else{
                    $found=$ProbeRows|Where-Object{$_.url -eq $url}|Select-Object -First 1
                    if($found){$key=if($found.youtube_id){$found.youtube_id}else{$found.url}}
                }
                if($key -and $state.ContainsKey($key)){
                    $s=$state[$key];$s.success=$true;$s.routes+=($Route+':OK')
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
    }finally{Remove-Item -LiteralPath $batch -Force -ErrorAction SilentlyContinue}
}

$routes=@('default','web_embedded,default','android,default')
foreach($route in $routes){
    $pending=@()
    foreach($key in $unique.Keys){if(-not $state[$key].success){$pending+=$unique[$key]}}
    if($pending.Count -eq 0){break}
    Invoke-ProbeRoute -ProbeRows $pending -Route $route
}

$results=@()
foreach($row in $rows){
    $key=if($row.youtube_id){$row.youtube_id}else{$row.url}
    $s=$state[$key]
    $title=if($row.title){$row.title}else{$s.title}
    $channel=if($row.channel){$row.channel}else{$s.channel}
    $canonical=@($s.reasons|ForEach-Object{Canonical-Reason $_}|Where-Object{$_}|Select-Object -Unique)
    $status=if($s.success){'AVAILABLE'}elseif($canonical.Count -gt 0){'UNAVAILABLE'}else{'INCONCLUSIVE'}
    $reason=if($status -eq 'UNAVAILABLE'){$canonical -join '; '}elseif($status -eq 'INCONCLUSIVE'){($s.reasons -join ' | ')}else{'Available'}
    $results += [pscustomobject]@{
        occurrence=$row.occurrence
        status=$status
        title=$title
        channel=$channel
        youtube_id=$row.youtube_id
        reason=$reason
        url=$row.url
    }
}

$results|Sort-Object occurrence|Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding UTF8
$unavailable=@($results|Where-Object{$_.status -eq 'UNAVAILABLE'}|Sort-Object occurrence)
$inconclusive=@($results|Where-Object{$_.status -eq 'INCONCLUSIVE'}|Sort-Object occurrence)
$available=@($results|Where-Object{$_.status -eq 'AVAILABLE'})

$label=if($FilterLabel){"Filter: $FilterLabel"}else{'Filter: all tracks'}
$out=New-Object Collections.Generic.List[string]
$out.Add('YOMI UNAVAILABLE TRACK AUDIT')
$out.Add($label)
$out.Add('Scanned: '+$results.Count+' occurrences / '+$unique.Count+' unique sources')
$out.Add('Unavailable: '+$unavailable.Count+' | Available: '+$available.Count+' | Inconclusive: '+$inconclusive.Count)
$out.Add('CSV: '+$csvPath)
$out.Add('')
$out.Add('UNAVAILABLE')
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
