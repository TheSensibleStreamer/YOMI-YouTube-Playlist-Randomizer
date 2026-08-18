param([switch]$Interactive)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
Initialize-YomiData
$config = Get-YomiConfig

$statusFile = Join-Path $DataRoot 'state\shuffle-status.txt'
$final = Join-Path $DataRoot 'playlist.txt'

function Set-ShuffleStatus([string]$Text) {
    try { Write-YomiUtf8NoBom -Path $statusFile -Text $Text } catch {}
    Write-Host $Text
}

function Finish-Shuffle([string]$Message,[bool]$Ok=$true) {
    Set-ShuffleStatus $Message
    if ($Ok) { Write-Host $Message -ForegroundColor Green } else { Write-Host $Message -ForegroundColor Red }
    if ($Interactive) { Write-Host ''; Read-Host 'Press Enter to close' | Out-Null }
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
    $args = @(
        'below',$yt,
        '--flat-playlist','--ignore-errors','--quiet','--no-warnings',
        '--print','%(webpage_url)s'
    )
    $deno = Join-Path $InstallRoot 'runtime\deno\deno.exe'
    if (Test-Path $deno) { $args += @('--js-runtimes',("deno:" + $deno)) }
    $args += $url

    Set-ShuffleStatus 'Reading the latest playlist from YouTube...'
    $lines = & $runner @args 2>&1
    if ($LASTEXITCODE -ne 0 -and -not $lines) { throw 'yt-dlp could not read the YouTube playlist.' }

    # Deliberately NO dedupe. Every playlist occurrence is its own shuffle entry.
    $items = @($lines | Where-Object { $_ -match '^https?://' })
    if ($items.Count -lt 1) { throw 'No playlist videos were returned by YouTube.' }

    Set-ShuffleStatus "Shuffling $($items.Count) playlist entries..."
    $rng = New-Object System.Random
    for ($i = $items.Count - 1; $i -gt 0; $i--) {
        $j = $rng.Next($i + 1)
        $tmp = $items[$i]; $items[$i] = $items[$j]; $items[$j] = $tmp
    }

    $temp = Join-Path $DataRoot 'playlist.shuffle.tmp'
    $items | Set-Content $temp -Encoding ASCII
    Clear-YomiCache
    # Navigator history stores exact shuffled occurrence indices. A new shuffle
    # creates a new index map, so retaining old rows would make jumps point at
    # unrelated tracks.
    Remove-Item (Join-Path $DataRoot 'state\history.jsonl') -Force -ErrorAction SilentlyContinue
    Move-Item $temp $final -Force
    Set-Content (Join-Path $DataRoot 'state\resume-track.txt') '1' -Encoding ASCII

    Finish-Shuffle "Shuffle Playlist complete: $($items.Count) entries. Latest YouTube contents included." $true
    exit 0
}
catch {
    Finish-Shuffle ("Shuffle Playlist failed: " + $_.Exception.Message) $false
    exit 1
}
