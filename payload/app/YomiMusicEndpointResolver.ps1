param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[A-Za-z0-9_-]{11}$')]
    [string]$VideoId,
    [string]$Title = '',
    [string]$Channel = '',
    [int]$Duration = 0
)
# Anonymous resolution: original YouTube Music endpoint, then conservative official-song search.
# No browser cookies, playlist writes, tokens, or credentials. Candidate must download before caching.
$ErrorActionPreference = 'Stop'

function Simple-Text([string]$Raw) {
    if(-not $Raw){return ''}
    $s = $Raw.ToLowerInvariant() -replace '[\u2010-\u2015]','-' -replace '&','and'
    $s = $s -replace '(?i)\s*\((official\s+(music\s+)?(audio|video)|lyrics?|visualizer|topic)\)\s*',' '
    $s = $s -replace '(?i)\s*\[(official\s+(music\s+)?(audio|video)|lyrics?|visualizer|topic)\]\s*',' '
    return (($s -replace '[^a-z0-9 ]',' ' -replace '\s+',' ').Trim())
}
function Fail-Reason([string]$Kind) {
    Write-Output ('MUSIC_ENDPOINT_REASON=' + (($Kind -replace '[^a-zA-Z0-9_-]','').Substring(0,[Math]::Min(64,$Kind.Length))))
}
function Try-Primary {
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $headers = @{
            'User-Agent' = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36'
            'Accept-Language' = 'en-US,en;q=0.9'
        }
        $response = Invoke-WebRequest -Uri ('https://music.youtube.com/watch?v=' + $VideoId) -Headers $headers -UseBasicParsing -TimeoutSec 7 -MaximumRedirection 5
        if ([int]$response.StatusCode -ne 200) { Fail-Reason 'music-http';return $null }
        $html = [string]$response.Content
        $anchor = $html.IndexOf('INITIAL_ENDPOINT', [StringComparison]::OrdinalIgnoreCase)
        if ($anchor -lt 0) { Fail-Reason 'no-initial-endpoint';return $null }
        $scope = $html.Substring($anchor, [Math]::Min(10000, $html.Length - $anchor))
        $scope = $scope.Replace('\u0022', '"').Replace('\"', '"').Replace('&quot;', '"')
        $hits = [regex]::Matches($scope, '(?s)"watchEndpoint"\s*:\s*\{.{0,1200}?"videoId"\s*:\s*"([A-Za-z0-9_-]{11})"')
        $ids = @($hits | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
        if($ids.Count -eq 1 -and $ids[0] -ne $VideoId){return $ids[0]}
        Fail-Reason 'primary-unresolved'
    } catch {
        $type=$_.Exception.GetType().Name
        Fail-Reason ('primary-' + $type)
    }
    return $null
}
function Try-Search {
    $titleText = Simple-Text $Title
    if(-not $titleText -or $titleText -match '^track\s*\d+$'){Fail-Reason 'no-track-metadata';return $null}
    $artist = (($Channel -replace '(?i)\s*-\s*Topic\s*$','').Trim())
    $artistText = Simple-Text $artist
    if(-not $artistText){Fail-Reason 'no-channel-metadata';return $null}

    $yt = Join-Path $PSScriptRoot '..\runtime\yt-dlp\yt-dlp.exe'
    if(-not (Test-Path -LiteralPath $yt -PathType Leaf)){Fail-Reason 'yt-dlp-missing';return $null}
    $query='ytsearch8:' + $artist + ' ' + $Title
    try {
        # JSON metadata only, never download media until Lua's extractor validates the candidate.
        $jsonText= & $yt '--ignore-config' '--dump-single-json' '--flat-playlist' '--skip-download' '--no-warnings' '--socket-timeout' '5' '--retries' '0' '--extractor-retries' '0' $query 2>$null | Out-String
        if($LASTEXITCODE -ne 0 -or -not $jsonText){Fail-Reason 'ytsearch-unavailable';return $null}
        $results=ConvertFrom-Json -InputObject $jsonText
        $matches=New-Object System.Collections.Generic.List[Object]
        foreach($entry in @($results.entries)){
            $id=[string]$entry.id
            if($id -notmatch '^[A-Za-z0-9_-]{11}$' -or $id -eq $VideoId){continue}
            $foundTitle=Simple-Text ([string]$entry.title)
            if($foundTitle.StartsWith($artistText+' ')){$foundTitle=$foundTitle.Substring($artistText.Length+1).Trim()}
            if($foundTitle -ne $titleText){continue}
            $source=Simple-Text ([string]$entry.channel+' '+[string]$entry.uploader)
            if(-not $source.Contains($artistText)){continue}
            $secs=[int]$entry.duration
            if($Duration -gt 30 -and $secs -gt 0 -and [Math]::Abs($secs-$Duration) -gt [Math]::Max(20,[Math]::Round($Duration*0.12))){continue}
            $rank=if($source.Contains($artistText+' topic')){3}else{2}
            if($Duration -gt 30 -and $secs -gt 0){$rank += 1}
            [void]$matches.Add([pscustomobject]@{id=$id;rank=$rank})
        }
        if($matches.Count -eq 0){Fail-Reason 'search-no-verified-match';return $null}
        $ordered=@($matches | Sort-Object -Property @{Expression='rank';Descending=$true})
        if($ordered.Count -gt 1 -and $ordered[0].rank -eq $ordered[1].rank){Fail-Reason 'search-ambiguous';return $null}
        return [string]$ordered[0].id
    } catch {
        Fail-Reason ('search-' + $_.Exception.GetType().Name)
        return $null
    }
}
$candidate=Try-Primary
if($candidate -match '^[A-Za-z0-9_-]{11}$' -and $candidate -ne $VideoId){
    Write-Output ('MUSIC_ENDPOINT_ID=' + $candidate)
    exit 0
}
$candidate=Try-Search
if($candidate -match '^[A-Za-z0-9_-]{11}$' -and $candidate -ne $VideoId){
    Write-Output ('MUSIC_ENDPOINT_ID=' + $candidate)
    exit 0
}
exit 2
