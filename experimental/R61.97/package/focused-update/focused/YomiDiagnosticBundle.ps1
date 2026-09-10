$ErrorActionPreference = "SilentlyContinue"

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$desktop = [Environment]::GetFolderPath("Desktop")
$out = Join-Path $desktop ("YOMI-Diagnostics-" + $stamp)
$zip = $out + ".zip"

New-Item -ItemType Directory -Force -Path $out | Out-Null

function Safe-Name([string]$s) {
    return ($s -replace '[:\\\/\*\?"<>\|]+','_')
}

function Copy-FilePreserve {
    param(
        [string]$FilePath,
        [string]$Root,
        [string]$DestRoot
    )
    try {
        $f = Get-Item -LiteralPath $FilePath -Force
        if (-not $f -or $f.PSIsContainer) { return }
        $rel = $f.FullName.Substring($Root.Length).TrimStart('\')
        $dest = Join-Path $DestRoot $rel
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
        Copy-Item -LiteralPath $f.FullName -Destination $dest -Force
    } catch {}
}

$summary = @(
    "YOMI Diagnostic Bundle v9 / R61.94",
    "Collected: $(Get-Date -Format o)",
    "Computer: $env:COMPUTERNAME",
    "User: $env:USERNAME",
    "Windows: $([Environment]::OSVersion.VersionString)",
    "PowerShell: $($PSVersionTable.PSVersion)",
    "Collector revision: R61.94",
    ""
)
$summary | Out-File (Join-Path $out "00-summary.txt") -Encoding utf8

# Broad discovery roots. We intentionally do NOT assume one canonical YOMI layout.
$roots = @()
$candidates = @(
    (Join-Path $env:LOCALAPPDATA "YOMI"),
    (Join-Path $env:APPDATA "YOMI"),
    (Join-Path $env:PROGRAMDATA "YOMI"),
    (Join-Path $env:ProgramFiles "YOMI"),
    (Join-Path ${env:ProgramFiles(x86)} "YOMI")
)
foreach ($r in $candidates) {
    if ($r -and (Test-Path $r)) { $roots += (Get-Item -LiteralPath $r).FullName }
}

# Include only recent YOMI installer/runtime temp roots. Old setup archaeology is already
# represented by persistent LocalAppData logs and should not make every bundle enormous.
if (Test-Path $env:TEMP) {
    Get-ChildItem -LiteralPath $env:TEMP -Directory -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^YOMI' -and $_.Name -notmatch '^YOMI-Diagnostics-' -and $_.LastWriteTime -gt (Get-Date).AddDays(-7) } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 16 |
        ForEach-Object { $roots += $_.FullName }
}

$roots = $roots | Sort-Object -Unique

# Record discovered roots.
"DISCOVERED YOMI ROOTS" | Out-File (Join-Path $out "01-discovered-roots.txt") -Encoding utf8
$roots | Out-File (Join-Path $out "01-discovered-roots.txt") -Append -Encoding utf8

# Diagnostic-ish extensions / names to collect broadly.
$diagExtensions = @(
    ".log",".txt",".json",".jsonl",".xml",".yaml",".yml",".ini",".cfg",".conf",
    ".csv",".tsv",".md",".ps1",".lua",".xaml",".cs"
)
$diagNameRegex = '(log|status|state|current|engine|supervisor|server|config|setting|diagnostic|health|lease|ipc|obs|mpv|ffmpeg|yt-dlp|update|install|bootstrap|patch|manifest|telemetry|trace|error|crash|queue|runtime)'

# Paths that are potentially enormous. We still inventory them separately.
$heavyPathRegex = '\\(cache|artwork-cache|video-cache|audio-cache|downloads?|installer-cache|backup|backups|recovery|runtime\\ffmpeg|runtime\\mpv|runtime\\yt-dlp)\\'

$collected = New-Object System.Collections.Generic.List[string]

foreach ($root in $roots) {
    $rootTag = Safe-Name $root
    $destRoot = Join-Path $out ("YOMI-ROOT_" + $rootTag)
    New-Item -ItemType Directory -Force -Path $destRoot | Out-Null

    # Full inventory for this root, even for excluded/heavy files.
    $inventoryPath = Join-Path $destRoot "_INVENTORY.txt"
    "ROOT: $root" | Out-File $inventoryPath -Encoding utf8

    Get-ChildItem -LiteralPath $root -Force -Recurse -ErrorAction SilentlyContinue |
        Select-Object FullName,Length,LastWriteTime,Attributes |
        Sort-Object FullName |
        Format-Table -AutoSize | Out-String -Width 4096 |
        Out-File $inventoryPath -Append -Encoding utf8

    # Collect broadly, but cap each file at 25 MB and avoid obvious media/binary payloads.
    Get-ChildItem -LiteralPath $root -File -Force -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            $f = $_
            $ext = $f.Extension.ToLowerInvariant()
            $isHeavyPath = $f.FullName -match $heavyPathRegex
            $interestingName = $f.Name -match $diagNameRegex
            $interestingExt = $diagExtensions -contains $ext

            # In cache-ish trees, only take tiny textual state/sidecar/manifest files.
            $allowFromHeavy = $isHeavyPath -and $interestingExt -and $f.Length -lt 2MB -and $interestingName

            if (
                $f.Length -lt 25MB -and
                (($interestingExt -or $interestingName) -and ((-not $isHeavyPath) -or $allowFromHeavy))
            ) {
                Copy-FilePreserve -FilePath $f.FullName -Root $root -DestRoot $destRoot
                $collected.Add($f.FullName) | Out-Null
            }
        }
}

# Search outside YOMI roots for recent installer/bootstrap/update logs.
# R61.83 in-app collector prunes prior YOMI-Diagnostics-* bundles/folders so a bundle never recursively eats older bundles.
$recentDest = Join-Path $out "Recent-External-YOMI-Logs"
New-Item -ItemType Directory -Force -Path $recentDest | Out-Null

function Get-PrunedRecentFiles {
    param([string]$Root,[int]$MaxDepth=4)
    if (-not $Root -or -not (Test-Path $Root)) { return }
    $queue = New-Object System.Collections.Queue
    $queue.Enqueue(@($Root,0))
    while ($queue.Count -gt 0) {
        $item = $queue.Dequeue(); $dir = [string]$item[0]; $depth = [int]$item[1]
        try {
            foreach ($file in @(Get-ChildItem -LiteralPath $dir -File -Force -ErrorAction SilentlyContinue)) { $file }
            if ($depth -ge $MaxDepth) { continue }
            foreach ($child in @(Get-ChildItem -LiteralPath $dir -Directory -Force -ErrorAction SilentlyContinue)) {
                if ($child.Name -match '^(?i)YOMI-Diagnostics-' -or $child.Name -match '^(?i)diag[_-]' -or $child.Name -eq '$RECYCLE.BIN') { continue }
                $queue.Enqueue(@($child.FullName,$depth+1))
            }
        } catch {}
    }
}

$externalRoots = @(
    $desktop,
    (Join-Path $env:USERPROFILE "Downloads")
) | Where-Object { $_ -and (Test-Path $_) }

foreach ($root in $externalRoots) {
    Get-PrunedRecentFiles -Root $root -MaxDepth 4 |
        Where-Object {
            $_.LastWriteTime -gt (Get-Date).AddDays(-7) -and
            $_.Length -lt 25MB -and
            $_.Name -notmatch '^(?i)YOMI-Diagnostics-.*\.zip$' -and
            (
                $_.Name -match '(?i)yomi.*\.(log|txt|json)$' -or
                $_.Name -match '(?i)(bootstrap|quick-patch|install|installer|update).*\.log$'
            )
        } |
        ForEach-Object {
            $safe = Safe-Name $_.FullName
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $recentDest $safe) -Force
            $collected.Add($_.FullName) | Out-Null
        }
}

# Current process snapshot.
Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -match '(?i)^(YOMI|mpv|ffmpeg|ffprobe|yt-dlp|deno|powershell|pwsh).*' -or
        $_.CommandLine -match '(?i)YOMI|8876'
    } |
    Select-Object ProcessId,ParentProcessId,Name,ExecutablePath,CreationDate,CommandLine |
    Format-List | Out-String -Width 4096 |
    Out-File (Join-Path $out "02-processes.txt") -Encoding utf8

# Runtime truth + configured OBS port ownership snapshot.
$localYomi = Join-Path $env:LOCALAPPDATA "YOMI"
$stateRoot = Join-Path $localYomi "state"
$truthDir = Join-Path $out "Runtime-Truth"
New-Item -ItemType Directory -Force -Path $truthDir | Out-Null
foreach ($name in @(
    "runtime-instance.json","runtime-lease.json","engine-status.json","current.json",
    "watchdog-status.json","presentation-status.json","obs-repair-request.json","obs-repair-result.json",
    "preflight.json","controller-ui.json","active-slot.json","session.json","supervisor-status.txt","supervisor.pid","server.pid","engine.pid","controller.pid"
)) {
    $p = Join-Path $stateRoot $name
    if (Test-Path $p -PathType Leaf) { Copy-Item -LiteralPath $p -Destination (Join-Path $truthDir $name) -Force }
}
$configPath = Join-Path $localYomi "config.json"
if (Test-Path $configPath -PathType Leaf) { Copy-Item -LiteralPath $configPath -Destination (Join-Path $truthDir "config.json") -Force }
$focusedBuild = Join-Path $env:ProgramFiles "YOMI\\app\\FOCUSED-BUILD.txt"
if (Test-Path $focusedBuild -PathType Leaf) { Copy-Item -LiteralPath $focusedBuild -Destination (Join-Path $truthDir "FOCUSED-BUILD.txt") -Force }

$port = 8876
try {
    if (Test-Path $configPath) {
        $cfg = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $candidatePort = 0
        if ([int]::TryParse([string]$cfg.server_port,[ref]$candidatePort) -and $candidatePort -gt 0 -and $candidatePort -le 65535) { $port = $candidatePort }
    }
} catch {}

$net = Join-Path $out ("03-network-port-" + $port + ".txt")
"Configured OBS port: $port" | Out-File $net -Encoding utf8
"`r`n===== Get-NetTCPConnection =====" | Out-File $net -Append -Encoding utf8
$conns = @(Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue)
$conns | Format-List * | Out-String -Width 4096 | Out-File $net -Append -Encoding utf8
"`r`n===== netstat =====" | Out-File $net -Append -Encoding utf8
(netstat -ano | Select-String (":" + $port)) | Out-File $net -Append -Encoding utf8

$listener = @($conns | Where-Object { $_.State -eq 'Listen' } | Select-Object -First 1)
$ownerPid = 0
if ($listener.Count -gt 0) { $ownerPid = [int]$listener[0].OwningProcess }
$recordedServerPid = 0
try { if (Test-Path (Join-Path $stateRoot 'server.pid')) { [void][int]::TryParse(([string](Get-Content (Join-Path $stateRoot 'server.pid') -Raw)).Trim(),[ref]$recordedServerPid) } } catch {}
$runtimeId = ''; $runtimeSupervisorPid = 0; $runtimeServerPid = 0
try {
    $ri = Get-Content (Join-Path $stateRoot 'runtime-instance.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $runtimeId = [string]$ri.runtime_id
    [void][int]::TryParse([string]$ri.supervisor_pid,[ref]$runtimeSupervisorPid)
    [void][int]::TryParse([string]$ri.server_pid,[ref]$runtimeServerPid)
} catch {}

$owner = $null; $parent = $null
if ($ownerPid -gt 0) {
    $owner = Get-CimInstance Win32_Process -Filter ("ProcessId=" + $ownerPid) -ErrorAction SilentlyContinue
    if ($owner -and [int]$owner.ParentProcessId -gt 0) { $parent = Get-CimInstance Win32_Process -Filter ("ProcessId=" + [int]$owner.ParentProcessId) -ErrorAction SilentlyContinue }
}
$ownership = Join-Path $out "03b-obs-port-ownership.txt"
@(
    "configured_port=$port",
    "listener_pid=$ownerPid",
    "recorded_server_pid=$recordedServerPid",
    "runtime_server_pid=$runtimeServerPid",
    "runtime_supervisor_pid=$runtimeSupervisorPid",
    "runtime_id=$runtimeId",
    "pid_match_server_pid=$($ownerPid -gt 0 -and $ownerPid -eq $recordedServerPid)",
    "pid_match_runtime_server=$($ownerPid -gt 0 -and $ownerPid -eq $runtimeServerPid)",
    "",
    "LISTENER PROCESS",
    ($owner | Select-Object ProcessId,ParentProcessId,Name,ExecutablePath,CreationDate,CommandLine | Format-List | Out-String -Width 4096),
    "PARENT PROCESS",
    ($parent | Select-Object ProcessId,ParentProcessId,Name,ExecutablePath,CreationDate,CommandLine | Format-List | Out-String -Width 4096)
) | Out-File -LiteralPath $ownership -Encoding utf8

$baseUrl = "http://127.0.0.1:$port"
$probeSpecs = @(
    @{ Name = "root";     Url = ($baseUrl + "/") },
    @{ Name = "health";   Url = ($baseUrl + "/health") },
    @{ Name = "v1-health";Url = ($baseUrl + "/v1/health") },
    @{ Name = "snapshot"; Url = ($baseUrl + "/snapshot?client=diagnostic-bundle") },
    @{ Name = "config";   Url = ($baseUrl + "/config") },
    @{ Name = "obs";      Url = ($baseUrl + "/v1/obs") },
    @{ Name = "clients";  Url = ($baseUrl + "/clients") },
    @{ Name = "metrics";  Url = ($baseUrl + "/v1/metrics") }
)
$probeDir = Join-Path $out "OBS-HTTP-Probes"
New-Item -ItemType Directory -Force -Path $probeDir | Out-Null
foreach ($p in $probeSpecs) {
    try {
        $r = Invoke-WebRequest -UseBasicParsing -Uri $p.Url -TimeoutSec 3
        "$($p.Url) -> HTTP $($r.StatusCode), bytes=$($r.RawContentLength)" | Out-File $net -Append -Encoding utf8
        @("URL: $($p.Url)", "HTTP: $($r.StatusCode)", "", [string]$r.Content) | Out-File -LiteralPath (Join-Path $probeDir ($p.Name + ".txt")) -Encoding utf8
    } catch {
        "$($p.Url) -> FAIL: $($_.Exception.Message)" | Out-File $net -Append -Encoding utf8
        @("URL: $($p.Url)", "FAIL: $($_.Exception.Message)") | Out-File -LiteralPath (Join-Path $probeDir ($p.Name + "-FAIL.txt")) -Encoding utf8
    }
}


# R61.82 presentation/media reality: inspect the actual bytes current.json points at, not only state projections.
$mediaReality = Join-Path $out "06-presentation-media-reality.txt"
@(
    "YOMI PRESENTATION / MEDIA REALITY",
    "Collected: $(Get-Date -Format o)",
    "Configured OBS port: $port",
    ""
) | Out-File -LiteralPath $mediaReality -Encoding utf8

$presentationObj = $null
$currentObj = $null
$activeSlotObj = $null
try {
    $presentationPath = Join-Path $stateRoot 'presentation-status.json'
    if (Test-Path $presentationPath) { $presentationObj = Get-Content -LiteralPath $presentationPath -Raw -Encoding UTF8 | ConvertFrom-Json }
} catch {}
try {
    $currentPath = Join-Path $stateRoot 'current.json'
    if (Test-Path $currentPath) { $currentObj = Get-Content -LiteralPath $currentPath -Raw -Encoding UTF8 | ConvertFrom-Json }
} catch {}
try {
    $activeSlotPath = Join-Path $stateRoot 'active-slot.json'
    if (Test-Path $activeSlotPath) { $activeSlotObj = Get-Content -LiteralPath $activeSlotPath -Raw -Encoding UTF8 | ConvertFrom-Json }
} catch {}

"===== PRESENTATION STATUS (PARSED) =====" | Out-File $mediaReality -Append -Encoding utf8
if ($presentationObj) {
    $presentationObj | ConvertTo-Json -Depth 16 | Out-File $mediaReality -Append -Encoding utf8
} else { "(missing/unreadable)" | Out-File $mediaReality -Append -Encoding utf8 }
"`r`n===== CURRENT STATE (PARSED) =====" | Out-File $mediaReality -Append -Encoding utf8
if ($currentObj) { $currentObj | ConvertTo-Json -Depth 12 | Out-File $mediaReality -Append -Encoding utf8 } else { "(missing/unreadable)" | Out-File $mediaReality -Append -Encoding utf8 }
"`r`n===== ACTIVE SLOT (STARTUP INTENT) =====" | Out-File $mediaReality -Append -Encoding utf8
if ($activeSlotObj) { $activeSlotObj | ConvertTo-Json -Depth 12 | Out-File $mediaReality -Append -Encoding utf8 } else { "(missing/unreadable)" | Out-File $mediaReality -Append -Encoding utf8 }

$ffprobe = $null
$ffprobeCandidates = @(
    (Join-Path $env:ProgramFiles 'YOMI\runtime\ffmpeg\ffprobe.exe'),
    (Join-Path $env:ProgramFiles 'YOMI\runtime\ffprobe.exe'),
    (Join-Path $env:ProgramFiles 'YOMI\app\ffprobe.exe')
)
foreach ($candidate in $ffprobeCandidates) { if ($candidate -and (Test-Path $candidate -PathType Leaf)) { $ffprobe = $candidate; break } }
if (-not $ffprobe) {
    try { $ffprobe = (Get-ChildItem -LiteralPath (Join-Path $env:ProgramFiles 'YOMI') -Filter ffprobe.exe -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1).FullName } catch {}
}
"`r`nffprobe=$ffprobe" | Out-File $mediaReality -Append -Encoding utf8

function Write-CurrentMediaReality {
    param([string]$Label,[string]$MediaPath)
    "`r`n===== $Label =====" | Out-File $mediaReality -Append -Encoding utf8
    "path=$MediaPath" | Out-File $mediaReality -Append -Encoding utf8
    if ([string]::IsNullOrWhiteSpace($MediaPath) -or -not (Test-Path -LiteralPath $MediaPath -PathType Leaf)) {
        "exists=false" | Out-File $mediaReality -Append -Encoding utf8
        return
    }
    try {
        $f = Get-Item -LiteralPath $MediaPath -Force
        "exists=true" | Out-File $mediaReality -Append -Encoding utf8
        "bytes=$($f.Length)" | Out-File $mediaReality -Append -Encoding utf8
        "last_write_utc=$($f.LastWriteTimeUtc.ToString('o'))" | Out-File $mediaReality -Append -Encoding utf8
        "sha256=$((Get-FileHash -LiteralPath $MediaPath -Algorithm SHA256).Hash.ToLowerInvariant())" | Out-File $mediaReality -Append -Encoding utf8
        $ext = $f.Extension.ToLowerInvariant()
        if ($ext -match '^\.(png|jpg|jpeg|bmp|gif)$') {
            try {
                Add-Type -AssemblyName System.Drawing -ErrorAction SilentlyContinue
                $img = [System.Drawing.Image]::FromFile($MediaPath)
                "image_width=$($img.Width)" | Out-File $mediaReality -Append -Encoding utf8
                "image_height=$($img.Height)" | Out-File $mediaReality -Append -Encoding utf8
                "image_pixel_format=$($img.PixelFormat)" | Out-File $mediaReality -Append -Encoding utf8
                $img.Dispose()
            } catch { "image_probe_error=$($_.Exception.Message)" | Out-File $mediaReality -Append -Encoding utf8 }
        }
        if ($ffprobe -and $ext -match '^\.(mp4|mkv|webm|mov|avi)$') {
            try {
                "--- ffprobe ---" | Out-File $mediaReality -Append -Encoding utf8
                (& $ffprobe -v error -select_streams v:0 -show_entries stream=codec_name,width,height,pix_fmt,r_frame_rate,avg_frame_rate -show_entries format=duration -of json $MediaPath 2>&1 | Out-String -Width 4096) | Out-File $mediaReality -Append -Encoding utf8
            } catch { "ffprobe_error=$($_.Exception.Message)" | Out-File $mediaReality -Append -Encoding utf8 }
        }
    } catch { "probe_error=$($_.Exception.Message)" | Out-File $mediaReality -Append -Encoding utf8 }
}

if ($currentObj) {
    Write-CurrentMediaReality -Label 'ARTWORK BYTES' -MediaPath ([string]$currentObj.artwork)
    Write-CurrentMediaReality -Label 'VIDEO BYTES' -MediaPath ([string]$currentObj.video)
    Write-CurrentMediaReality -Label 'VISUALIZER BYTES' -MediaPath ([string]$currentObj.visualizer)
}


# R61.83 performance-budget summary: make dispatcher pressure readable without digging through JSON.
$budgetReport = Join-Path $out "08-performance-budget.txt"
"YOMI UI PERFORMANCE BUDGET" | Out-File $budgetReport -Encoding utf8
if ($presentationObj -and $presentationObj.performance_budget) {
    $presentationObj.performance_budget | ConvertTo-Json -Depth 8 | Out-File $budgetReport -Append -Encoding utf8
} else {
    "(performance_budget missing from presentation-status.json)" | Out-File $budgetReport -Append -Encoding utf8
}
$perfLog = Join-Path $env:LOCALAPPDATA 'YOMI\logs\controller-performance.log'
if (Test-Path $perfLog -PathType Leaf) {
    "`r`n===== RECENT CONTROLLER PERFORMANCE =====" | Out-File $budgetReport -Append -Encoding utf8
    Get-Content -LiteralPath $perfLog -Tail 240 -ErrorAction SilentlyContinue | Out-File $budgetReport -Append -Encoding utf8
}

# Installed focused runtime hashes prove whether the target actually promoted the intended build.
$runtimeHashes = Join-Path $out "07-installed-runtime-hashes.txt"
"INSTALLED YOMI RUNTIME HASHES" | Out-File $runtimeHashes -Encoding utf8
$appRoot = Join-Path $env:ProgramFiles 'YOMI\app'
foreach ($name in @('FOCUSED-BUILD.txt','YomiControllerWpf.exe','YomiControllerWpf.cs','YomiControllerWpf.xaml','YomiDesign.xaml','music.lua','server.ps1','supervisor.ps1','contracts.ps1','YomiUpdateHost.ps1','YomiDiagnosticBundle.ps1')) {
    $p = Join-Path $appRoot $name
    if (Test-Path $p -PathType Leaf) {
        try {
            $f = Get-Item -LiteralPath $p
            $h = (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()
            "$name`t$($f.Length)`t$($f.LastWriteTimeUtc.ToString('o'))`t$h" | Out-File $runtimeHashes -Append -Encoding utf8
            if ($name -eq 'FOCUSED-BUILD.txt') { (Get-Content -LiteralPath $p -Raw) | Out-File $runtimeHashes -Append -Encoding utf8 }
        } catch {}
    } else { "$name`tMISSING" | Out-File $runtimeHashes -Append -Encoding utf8 }
}

# Recent relevant Application log events.
try {
    $since = (Get-Date).AddHours(-12)
    Get-WinEvent -FilterHashtable @{LogName="Application"; StartTime=$since; Level=1,2,3} -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Message -match '(?i)YOMI|mpv|ffmpeg|yt-dlp|deno' -or
            $_.ProviderName -match 'Application Error|Windows Error Reporting'
        } |
        Select-Object TimeCreated,Id,LevelDisplayName,ProviderName,Message |
        Format-List | Out-String -Width 4096 |
        Out-File (Join-Path $out "04-windows-events.txt") -Encoding utf8
} catch {}

# Summary of what was copied.
"`r`nCOLLECTED FILES: $($collected.Count)" |
    Out-File (Join-Path $out "00-summary.txt") -Append -Encoding utf8
$collected | Sort-Object -Unique |
    Out-File (Join-Path $out "05-collected-file-list.txt") -Encoding utf8

# Robust ZIP creation.
$zipError = $null
if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }

try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $out,
        $zip,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false
    )
} catch {
    $zipError = $_ | Out-String
}

if (-not (Test-Path $zip)) {
    try {
        Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip -CompressionLevel Optimal -Force -ErrorAction Stop
        $zipError = $null
    } catch {
        $zipError = (($zipError + "`r`n--- FALLBACK ---`r`n" + ($_ | Out-String))).Trim()
    }
}

$zipValid = $false
$entryCount = 0
if (Test-Path $zip) {
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
        $z = [System.IO.Compression.ZipFile]::OpenRead($zip)
        $entryCount = $z.Entries.Count
        $z.Dispose()
        if ($entryCount -gt 0 -and (Get-Item -LiteralPath $zip).Length -gt 100) {
            $zipValid = $true
        }
    } catch {
        $zipError = (($zipError + "`r`n--- VALIDATION ---`r`n" + ($_ | Out-String))).Trim()
    }
}

if ($zipValid) {
    Remove-Item -LiteralPath $out -Recurse -Force
    try { Set-Clipboard -Value $zip } catch {}
    Write-Host ""
    Write-Host "YOMI DIAGNOSTICS COMPLETE" -ForegroundColor Green
    Write-Host "Collected files: $($collected.Count)"
    Write-Host "ZIP entries: $entryCount"
    Write-Host $zip -ForegroundColor Cyan
    Write-Host ""
    Start-Process explorer.exe "/select,`"$zip`""
    exit 0
} else {
    $errFile = Join-Path $out "ZIP-FAILURE.txt"
    @(
        "YOMI diagnostic ZIP creation failed.",
        "Time: $(Get-Date -Format o)",
        "Working folder: $out",
        "Target ZIP: $zip",
        "",
        "Compression diagnostics:",
        $zipError
    ) | Out-File -LiteralPath $errFile -Encoding utf8

    Write-Host ""
    Write-Host "FAILED TO CREATE DIAGNOSTIC ZIP" -ForegroundColor Red
    Write-Host "Nothing was deleted." -ForegroundColor Yellow
    Write-Host $out -ForegroundColor Cyan
    Start-Process explorer.exe "`"$out`""
    exit 5
}
