param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[A-Za-z0-9_-]{11}$')]
    [string]$VideoId
)
# A bounded, anonymous YouTube Music HTML check. Only emits one safe 11-character ID.
# Never reads cookies, browser profiles, user tokens, or playlist files.
$ErrorActionPreference = 'Stop'
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $headers = @{
        'User-Agent' = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36'
        'Accept-Language' = 'en-US,en;q=0.9'
    }
    $uri = 'https://music.youtube.com/watch?v=' + $VideoId
    $response = Invoke-WebRequest -Uri $uri -Headers $headers -UseBasicParsing -TimeoutSec 15 -MaximumRedirection 5
    if ([int]$response.StatusCode -ne 200) { exit 2 }
    $html = [string]$response.Content
    # The URL can remain unchanged while INITIAL_ENDPOINT supplies a different,
    # playable music video ID. Limit parsing to this particular endpoint object.
    $anchor = $html.IndexOf('INITIAL_ENDPOINT', [StringComparison]::OrdinalIgnoreCase)
    if ($anchor -lt 0) { exit 2 }
    $scope = $html.Substring($anchor, [Math]::Min(10000, $html.Length - $anchor))
    $scope = $scope.Replace('\u0022', '"').Replace('\"', '"').Replace('&quot;', '"')
    $matches = [regex]::Matches($scope, '(?s)"watchEndpoint"\s*:\s*\{.{0,1200}?"videoId"\s*:\s*"([A-Za-z0-9_-]{11})"')
    $ids = New-Object System.Collections.Generic.List[string]
    foreach ($match in $matches) {
        $id = $match.Groups[1].Value
        if (-not $ids.Contains($id)) { [void]$ids.Add($id) }
        if ($ids.Count -gt 1) { exit 2 }  # Ambiguous; never guess.
    }
    if ($ids.Count -eq 1 -and $ids[0] -ne $VideoId) {
        Write-Output ('MUSIC_ENDPOINT_ID=' + $ids[0])
        exit 0
    }
    exit 2
} catch {
    # Intentionally avoid logging raw HTML, request URLs, or browser identifiers.
    exit 3
}
