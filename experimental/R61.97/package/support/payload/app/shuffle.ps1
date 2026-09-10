param([switch]$Interactive)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
$config = Get-YomiConfig

$statusFile = Join-Path $DataRoot 'state\shuffle-status.txt'
$final = Join-Path $DataRoot 'playlist.txt'
$sessionFile = Join-Path $DataRoot 'state\session.json'

function Set-ShuffleStatus([string]$Text) {
    try { Write-YomiUtf8NoBom -Path $statusFile -Text $Text } catch {}
    Write-Host $Text
}

function Finish-Shuffle([string]$Message,[bool]$Ok=$true) {
    Set-ShuffleStatus $Message
    if ($Ok) { Write-Host $Message -ForegroundColor Green } else { Write-Host $Message -ForegroundColor Red }
    if ($Interactive) { Write-Host ''; Read-Host 'Press Enter to close' | Out-Null }
}

function Resolve-EntryUrl($Entry) {
    $u = [string]$Entry.webpage_url
    if (-not [string]::IsNullOrWhiteSpace($u) -and $u -match '^https?://') { return $u }
    $candidate = [string]$Entry.url
    if (-not [string]::IsNullOrWhiteSpace($candidate) -and $candidate -match '^https?://') { return $candidate }
    $id = [string]$Entry.id
    if (-not [string]::IsNullOrWhiteSpace($id)) { return 'https://www.youtube.com/watch?v=' + $id }
    return ''
}

try {
    $pidFile = Join-Path $DataRoot 'state\engine.pid'
    $enginePid = 0
    if (Test-Path $pidFile) {
        try { [void][int]::TryParse((Get-Content $pidFile -Raw).Trim(),[ref]$enginePid) } catch {}
    }
    if ($enginePid -gt 0 -and (Get-Process -Id $enginePid -ErrorAction SilentlyContinue)) {
        throw 'YOMI is currently playing. Use Shuffle Playlist in the Controller so it can stop and restart safely.'
    }

    $input = ([string]$config.playlist).Trim()
    if ([string]::IsNullOrWhiteSpace($input)) { throw 'No YouTube playlist is saved. Open YOMI Settings first.' }
    if ($input -match '^[A-Za-z0-9_-]{10,}$' -and $input -notmatch '^https?://') {
        $url = 'https://www.youtube.com/playlist?list=' + $input
    } else { $url = $input }

    $yt = Join-Path $InstallRoot 'runtime\yt-dlp\yt-dlp.exe'
    $runner = Join-Path $InstallRoot 'app\PriorityRun.exe'
    $deno = Join-Path $InstallRoot 'runtime\deno\deno.exe'

    Set-ShuffleStatus 'Reading playlist metadata from YouTube...'
    $args = @('below',$yt,'--flat-playlist','--ignore-errors','--quiet','--no-warnings','--dump-single-json')
    if (Test-Path $deno) { $args += @('--js-runtimes',("deno:" + $deno)) }
    $args += $url
    $rawLines = & $runner @args 2>&1
    $entries = @()

    try {
        $jsonLine = @($rawLines | ForEach-Object { [string]$_ } | Where-Object { $_.TrimStart().StartsWith('{') }) | Select-Object -Last 1
        if ($jsonLine) {
            $playlistObject = $jsonLine | ConvertFrom-Json
            $sourceIndex = 0
            foreach ($entry in @($playlistObject.entries)) {
                if ($null -eq $entry) { continue }
                $entryUrl = Resolve-EntryUrl $entry
                if ($entryUrl -notmatch '^https?://') { continue }
                $sourceIndex++
                $entries += [PSCustomObject]@{
                    source_index = $sourceIndex
                    id = [string]$entry.id
                    url = $entryUrl
                    title = [string]$entry.title
                    channel = $(if($entry.channel){[string]$entry.channel}elseif($entry.uploader){[string]$entry.uploader}else{''})
                    duration = [double]$(if($null -ne $entry.duration){$entry.duration}else{0})
                }
            }
        }
    } catch { $entries = @() }

    if ($entries.Count -lt 1) {
        Set-ShuffleStatus 'Metadata snapshot unavailable; falling back to URL-only playlist read...'
        $fallbackArgs = @('below',$yt,'--flat-playlist','--ignore-errors','--quiet','--no-warnings','--print','%(webpage_url)s')
        if (Test-Path $deno) { $fallbackArgs += @('--js-runtimes',("deno:" + $deno)) }
        $fallbackArgs += $url
        $lines = & $runner @fallbackArgs 2>&1
        $sourceIndex = 0
        foreach($line in @($lines)) {
            $u = ([string]$line).Trim()
            if($u -notmatch '^https?://') { continue }
            $sourceIndex++
            $entries += [PSCustomObject]@{source_index=$sourceIndex;id='';url=$u;title='';channel='';duration=0}
        }
    }

    if ($entries.Count -lt 1) { throw 'No playlist videos were returned by YouTube.' }

    $seedBytes = New-Object byte[] 4
    $crypto = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $crypto.GetBytes($seedBytes) } finally { $crypto.Dispose() }
    $seed = [BitConverter]::ToInt32($seedBytes,0) -band 0x7fffffff
    $rng = New-Object System.Random($seed)

    $items = @($entries)
    Set-ShuffleStatus "Shuffling $($items.Count) playlist occurrences..."
    for ($i = $items.Count - 1; $i -gt 0; $i--) {
        $j = $rng.Next($i + 1)
        $tmp = $items[$i]; $items[$i] = $items[$j]; $items[$j] = $tmp
    }

    $duplicateCounts = [System.Collections.Generic.Dictionary[string,int]]::new([System.StringComparer]::Ordinal)
    foreach($entry in $items) {
        $key = if(-not [string]::IsNullOrWhiteSpace([string]$entry.id)){ 'id:' + [string]$entry.id }else{ 'url:' + [string]$entry.url }
        if($duplicateCounts.ContainsKey($key)){$duplicateCounts[$key]++}else{$duplicateCounts[$key]=1}
    }
    $duplicateSeen = [System.Collections.Generic.Dictionary[string,int]]::new([System.StringComparer]::Ordinal)
    $sourceKeyCache = [System.Collections.Generic.Dictionary[string,string]]::new([System.StringComparer]::Ordinal)
    function Get-YomiSourceKey([string]$Identity) {
        if($sourceKeyCache.ContainsKey($Identity)){return $sourceKeyCache[$Identity]}
        $shaSource=[System.Security.Cryptography.SHA256]::Create()
        try{
            $bytes=[System.Text.Encoding]::UTF8.GetBytes($Identity)
            $value=([BitConverter]::ToString($shaSource.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()
            $sourceKeyCache[$Identity]=$value
            return $value
        }finally{$shaSource.Dispose()}
    }
    $sessionId = [Guid]::NewGuid().ToString('N')
    $occurrences = @()

    for($i=0;$i -lt $items.Count;$i++) {
        $entry = $items[$i]
        $key = if(-not [string]::IsNullOrWhiteSpace([string]$entry.id)){ 'id:' + [string]$entry.id }else{ 'url:' + [string]$entry.url }
        if($duplicateSeen.ContainsKey($key)){$duplicateSeen[$key]++}else{$duplicateSeen[$key]=1}
        $occurrences += [PSCustomObject]@{
            position = $i + 1
            occurrence_id = ($sessionId.Substring(0,8) + '-' + ([int]$entry.source_index).ToString('D5'))
            source_index = [int]$entry.source_index
            source_key = Get-YomiSourceKey $key
            id = [string]$entry.id
            url = [string]$entry.url
            title = [string]$entry.title
            channel = [string]$entry.channel
            duration = [double]$entry.duration
            duplicate_ordinal = [int]$duplicateSeen[$key]
            duplicate_count = [int]$duplicateCounts[$key]
        }
    }

    $orderText = ($occurrences | ForEach-Object { [string]$_.url }) -join "`n"
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $orderBytes = [System.Text.Encoding]::UTF8.GetBytes($orderText)
        $orderHash = ([BitConverter]::ToString($sha.ComputeHash($orderBytes))).Replace('-','').ToLowerInvariant()
    } finally { $sha.Dispose() }

    $session = [ordered]@{
        schema = 2
        cache_identity_schema = 1
        session_id = $sessionId
        created_utc = [DateTime]::UtcNow.ToString('o')
        source_playlist = $url
        shuffle_algorithm = 'fisher-yates'
        shuffle_seed = $seed
        count = $occurrences.Count
        order_sha256 = $orderHash
        metadata_snapshot = [bool](@($occurrences | Where-Object { $_.title }).Count -gt 0)
        occurrences = $occurrences
    }

    $playlistTemp = Join-Path $DataRoot ('playlist.shuffle.' + $sessionId + '.tmp')
    $sessionTemp = Join-Path $DataRoot ('session.shuffle.' + $sessionId + '.tmp')
    Write-YomiUtf8NoBom -Path $playlistTemp -Text (($occurrences | ForEach-Object {$_.url}) -join "`r`n")
    Write-YomiUtf8NoBom -Path $sessionTemp -Text ($session | ConvertTo-Json -Depth 8)

    $playlistCheck = @(Get-Content $playlistTemp | Where-Object { $_ -match '^https?://' })
    $sessionCheck = Get-Content $sessionTemp -Raw | ConvertFrom-Json
    if($playlistCheck.Count -ne $occurrences.Count -or [int]$sessionCheck.count -ne $occurrences.Count) {
        throw 'Shuffle transaction validation failed before commit.'
    }

    $cacheRoot = Join-Path $DataRoot 'cache'
    $cacheBackup = Join-Path $DataRoot ('cache.previous.' + $sessionId)
    $playlistBackup = Join-Path $DataRoot ('playlist.previous.' + $sessionId)
    $sessionBackup = Join-Path $DataRoot ('session.previous.' + $sessionId)
    $history = Join-Path $DataRoot 'state\history.jsonl'
    $historyBackup = Join-Path $DataRoot ('history.previous.' + $sessionId)
    $resume = Join-Path $DataRoot 'state\resume-track.txt'
    $resumeBackup = Join-Path $DataRoot ('resume.previous.' + $sessionId)
    $committed = $false

    try {
        Set-ShuffleStatus 'Committing new shuffled session...'
        if(Test-Path $cacheRoot){Move-Item $cacheRoot $cacheBackup}
        New-Item -ItemType Directory -Path $cacheRoot -Force | Out-Null
        foreach($name in @('audio','artwork','video','visualizer','meta','gain','status','comments','telemetry')) {
            New-Item -ItemType Directory -Path (Join-Path $cacheRoot $name) -Force | Out-Null
        }

        if(Test-Path $final){Move-Item $final $playlistBackup}
        if(Test-Path $sessionFile){Move-Item $sessionFile $sessionBackup}
        if(Test-Path $history){Move-Item $history $historyBackup}
        if(Test-Path $resume){Move-Item $resume $resumeBackup}

        Move-Item $playlistTemp $final
        Move-Item $sessionTemp $sessionFile
        Set-Content $resume '1' -Encoding ASCII
        Remove-Item (Join-Path $DataRoot 'state\playlist-dirty.pending') -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $DataRoot 'state\freeze-window.json') -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $DataRoot 'state\video-capability.json') -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $DataRoot 'state\rehearsal-report.json') -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $DataRoot 'state\session-order.json') -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $DataRoot 'state\session-order.previous.json') -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $DataRoot 'state\session-journal.jsonl') -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $DataRoot 'state\session-journal-head.json') -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $DataRoot 'state\browser-clients.json') -Force -ErrorAction SilentlyContinue
        $committed = $true
    }
    catch {
        Remove-Item $final,$sessionFile,$resume -Force -ErrorAction SilentlyContinue
        Remove-Item $cacheRoot -Recurse -Force -ErrorAction SilentlyContinue
        if(Test-Path $playlistBackup){Move-Item $playlistBackup $final -Force}
        if(Test-Path $sessionBackup){Move-Item $sessionBackup $sessionFile -Force}
        if(Test-Path $historyBackup){Move-Item $historyBackup $history -Force}
        if(Test-Path $resumeBackup){Move-Item $resumeBackup $resume -Force}
        if(Test-Path $cacheBackup){Move-Item $cacheBackup $cacheRoot -Force}
        throw
    }
    finally {
        if($committed) {
            Remove-Item $cacheBackup -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item $playlistBackup,$sessionBackup,$historyBackup,$resumeBackup -Force -ErrorAction SilentlyContinue
        }
        Remove-Item $playlistTemp,$sessionTemp -Force -ErrorAction SilentlyContinue
    }

    Finish-Shuffle "Shuffle Playlist complete: $($occurrences.Count) occurrences. Session $($sessionId.Substring(0,8)) committed." $true
    exit 0
}
catch {
    Finish-Shuffle ("Shuffle Playlist failed: " + $_.Exception.Message) $false
    exit 1
}