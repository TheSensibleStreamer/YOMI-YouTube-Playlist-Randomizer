$ErrorActionPreference = 'Stop'

$packageRoot = Split-Path $PSScriptRoot -Parent
$payload = Join-Path $packageRoot 'payload'

if (-not (Test-Path (Join-Path $payload 'app\supervisor.ps1'))) {
    Write-Host ''
    Write-Host 'ERROR: Installer files are missing.' -ForegroundColor Red
    Write-Host 'Fully extract the ZIP before running INSTALL YOMI.cmd.' -ForegroundColor Yellow
    exit 2
}

if (-not (Test-Path (Join-Path $payload 'Uninstall YOMI.cmd'))) {
    Write-Host ''
    Write-Host 'ERROR: Uninstall YOMI.cmd is missing from the installer payload.' -ForegroundColor Red
    Write-Host 'Fully extract the ZIP before running INSTALL YOMI.cmd.' -ForegroundColor Yellow
    exit 2
}

if (-not (Test-Path (Join-Path $payload 'web\director.html'))) {
    Write-Host ''
    Write-Host 'ERROR: Director Mode web engine is missing from the installer payload.' -ForegroundColor Red
    Write-Host 'Fully extract the ZIP before running INSTALL YOMI.cmd.' -ForegroundColor Yellow
    exit 2
}

# Relaunch elevated because Program Files is protected.
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    $args = @(
        '-NoProfile'
        '-ExecutionPolicy','Bypass'
        '-File',('"' + $PSCommandPath + '"')
    )

    try {
        $elevated = Start-Process powershell.exe `
            -Verb RunAs `
            -ArgumentList $args `
            -Wait `
            -PassThru

        exit $elevated.ExitCode
    }
    catch {
        Write-Host 'Administrator permission was not granted.' -ForegroundColor Red
        exit 5
    }
}

$installRoot = Join-Path $env:ProgramFiles 'YOMI'
$dataRoot = Join-Path $env:LOCALAPPDATA 'YOMI'
$defenderMarker = Join-Path $dataRoot 'defender-yt-dlp-process-exclusion.txt'
$tempRoot = Join-Path $env:TEMP ('YOMI-Install-' + [Guid]::NewGuid().ToString('N'))

# Permanent installer log so a fast-closing admin window can never hide
# the actual failure again.
New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null
$installLog = Join-Path $dataRoot 'install.log'

try {
    Start-Transcript -Path $installLog -Append -Force | Out-Null
}
catch {}

Write-Host '===== YOMI 4.2.0.4 - YOUTUBE OBS MUSIC INTERFACE =====' -ForegroundColor Cyan
Write-Host ''
Write-Host 'This installs a SEPARATE copy.' -ForegroundColor Green
Write-Host 'It does not modify unrelated mpv installations.' -ForegroundColor Green
Write-Host ''
Write-Host "Program:  $installRoot"
Write-Host "Settings: $dataRoot"
Write-Host "Install log: $installLog"
Write-Host ''

New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

$stageFile = Join-Path $dataRoot 'install-stage.txt'
$downloadCache = Join-Path $dataRoot 'installer-cache'
New-Item -ItemType Directory -Path $downloadCache -Force | Out-Null

function Set-InstallStage {
    param(
        [int]$Number,
        [int]$Total,
        [string]$Text
    )

    $line = "[$Number/$Total] $Text"
    Write-Host ''
    Write-Host $line -ForegroundColor Cyan

    try {
        Set-Content $stageFile `
            ((Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' ' + $line) `
            -Encoding ASCII
    }
    catch {}
}

function Download-FileWithProgress {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Uri,

        [Parameter(Mandatory=$true)]
        [string]$OutFile,

        [Parameter(Mandatory=$true)]
        [string]$Label,

        [hashtable]$Headers = @{}
    )

    $request = [System.Net.HttpWebRequest]::Create($Uri)
    $request.Method = 'GET'
    $request.AllowAutoRedirect = $true
    $request.MaximumAutomaticRedirections = 10
    $request.UserAgent = 'YOMI-4.2.0.4-Installer'
    $request.Timeout = 30000
    $request.ReadWriteTimeout = 30000
    $request.KeepAlive = $true

    foreach ($key in $Headers.Keys) {
        if ($key -ieq 'User-Agent') {
            $request.UserAgent = [string]$Headers[$key]
        }
        else {
            $request.Headers[$key] = [string]$Headers[$key]
        }
    }

    $response = $null
    $input = $null
    $output = $null

    try {
        $response = $request.GetResponse()
        $total = [int64]$response.ContentLength
        $input = $response.GetResponseStream()

        $output = New-Object System.IO.FileStream(
            $OutFile,
            [System.IO.FileMode]::Create,
            [System.IO.FileAccess]::Write,
            [System.IO.FileShare]::None,
            1048576,
            [System.IO.FileOptions]::SequentialScan
        )

        $buffer = New-Object byte[] 1048576
        [int64]$downloaded = 0
        $lastUi = [DateTime]::MinValue

        while (($read = $input.Read($buffer,0,$buffer.Length)) -gt 0) {
            $output.Write($buffer,0,$read)
            $downloaded += $read

            $now = Get-Date
            if (($now - $lastUi).TotalMilliseconds -ge 150) {
                $mb = $downloaded / 1MB

                if ($total -gt 0) {
                    $pct = [Math]::Min(
                        100,
                        [Math]::Floor(($downloaded * 100.0) / $total)
                    )

                    $totalMb = $total / 1MB

                    Write-Progress `
                        -Activity $Label `
                        -Status ("{0:N1} MB / {1:N1} MB   {2}%" -f $mb,$totalMb,$pct) `
                        -PercentComplete $pct
                }
                else {
                    Write-Progress `
                        -Activity $Label `
                        -Status ("{0:N1} MB downloaded" -f $mb) `
                        -PercentComplete 0
                }

                $lastUi = $now
            }
        }

        $output.Flush()

        if ($downloaded -le 0) {
            throw "$Label downloaded zero bytes."
        }

        Write-Progress -Activity $Label -Completed

        if ($total -gt 0 -and $downloaded -ne $total) {
            throw "$Label download was incomplete: expected $total bytes, received $downloaded."
        }

        Write-Host (
            "      Download complete: {0:N1} MB" -f ($downloaded / 1MB)
        ) -ForegroundColor Green
    }
    finally {
        if ($output) { $output.Dispose() }
        if ($input) { $input.Dispose() }
        if ($response) { $response.Dispose() }
    }
}


function Get-CachedDownload {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Uri,

        [Parameter(Mandatory=$true)]
        [string]$CacheFile,

        [Parameter(Mandatory=$true)]
        [string]$OutFile,

        [Parameter(Mandatory=$true)]
        [string]$Label,

        [hashtable]$Headers = @{}
    )

    if (Test-Path $CacheFile) {
        $item = Get-Item $CacheFile -ErrorAction SilentlyContinue

        if ($item -and $item.Length -gt 1048576) {
            Write-Host (
                "      Using cached download: {0:N1} MB" -f ($item.Length / 1MB)
            ) -ForegroundColor Green

            Copy-Item $CacheFile $OutFile -Force
            return
        }

        Remove-Item $CacheFile -Force -ErrorAction SilentlyContinue
    }

    $tempCache = $CacheFile + '.downloading'
    Remove-Item $tempCache -Force -ErrorAction SilentlyContinue

    Download-FileWithProgress `
        -Uri $Uri `
        -OutFile $tempCache `
        -Label $Label `
        -Headers $Headers

    Move-Item $tempCache $CacheFile -Force
    Copy-Item $CacheFile $OutFile -Force
}

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    [Net.ServicePointManager]::DefaultConnectionLimit = 8

    # ------------------------------------------------------------
    # Preflight + clean portable runtime dependencies.
    # ------------------------------------------------------------

    Set-InstallStage 1 8 'Preflight checks...'

    if (-not [Environment]::Is64BitOperatingSystem) {
        throw 'YOMI currently requires 64-bit Windows.'
    }

    if (-not (Test-Path $payload)) {
        throw "Payload folder is missing: $payload"
    }

    # Verify Program Files is writable under the elevated installer.
    $programFilesProbe = Join-Path $env:ProgramFiles (
        '.YOMI-write-test-' + [Guid]::NewGuid().ToString('N')
    )
    try {
        New-Item -ItemType Directory -Path $programFilesProbe -Force | Out-Null
    }
    finally {
        Remove-Item $programFilesProbe -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host '      Administrator access: OK' -ForegroundColor Green
    Write-Host '      64-bit Windows: OK' -ForegroundColor Green
    Write-Host '      Installer payload: OK' -ForegroundColor Green

    $headers = @{ 'User-Agent' = 'YOMI-4.2.0.4-Installer' }

    # Ask what the user wants BEFORE optional prerequisite downloads.
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    $pf = New-Object System.Windows.Forms.Form
    $pf.Text = 'YOMI 4.2.0.4 - YouTube OBS Music Interface'
    $pf.StartPosition = 'CenterScreen'
    $pf.Size = New-Object System.Drawing.Size(640,500)
    $pf.MinimumSize = $pf.Size
    $pf.MaximumSize = $pf.Size
    $pf.MaximizeBox = $false
    $pf.Font = New-Object System.Drawing.Font('Segoe UI',10)
    $headline = New-Object System.Windows.Forms.Label
    $headline.Text = 'YOMI - YouTube OBS Music Interface'
    $headline.Font = New-Object System.Drawing.Font('Segoe UI Semibold',13)
    $headline.Location = New-Object System.Drawing.Point(20,18)
    $headline.Size = New-Object System.Drawing.Size(560,30)
    $pf.Controls.Add($headline)
    $full = New-Object System.Windows.Forms.RadioButton
    $full.Text = 'Full YOMI (recommended) - Player + Streamer/OBS + Director Mode + Deno + FFmpeg'
    $profilePrompt = New-Object System.Windows.Forms.Label
    $profilePrompt.Text = 'Choose what you want YOMI to install:'
    $profilePrompt.Location = New-Object System.Drawing.Point(25,52); $profilePrompt.Size = New-Object System.Drawing.Size(570,24); $pf.Controls.Add($profilePrompt)
    $full.Location = New-Object System.Drawing.Point(25,82); $full.Size = New-Object System.Drawing.Size(570,28); $full.Checked = $true; $pf.Controls.Add($full)
    $player = New-Object System.Windows.Forms.RadioButton
    $player.Text = 'Player - mpv + yt-dlp + Deno; add streamer media tools later if wanted'
    $player.Location = New-Object System.Drawing.Point(25,122); $player.Size = New-Object System.Drawing.Size(570,28); $pf.Controls.Add($player)
    $minimal = New-Object System.Windows.Forms.RadioButton
    $minimal.Text = 'Minimal Player - mpv + yt-dlp only (YouTube format support may be limited)'
    $minimal.Location = New-Object System.Drawing.Point(25,162); $minimal.Size = New-Object System.Drawing.Size(570,28); $pf.Controls.Add($minimal)
    $explain = New-Object System.Windows.Forms.Label
    $explain.Text = 'Deno is the recommended JavaScript runtime for modern YouTube extraction. FFmpeg Media Tools power loudness leveling, smart artwork crop and the retro visualizer. Optional components can be installed or removed later from YOMI Settings.'
    $explain.Location = New-Object System.Drawing.Point(25,207); $explain.Size = New-Object System.Drawing.Size(570,64); $explain.ForeColor = [System.Drawing.Color]::DimGray; $pf.Controls.Add($explain)
    $defender = New-Object System.Windows.Forms.CheckBox
    $defender.Text = 'OPT IN: Reduce Windows Defender CPU spikes during track changes'
    $defender.Location = New-Object System.Drawing.Point(25,282); $defender.Size = New-Object System.Drawing.Size(570,28)
    $defender.Checked = Test-Path $defenderMarker
    $pf.Controls.Add($defender)
    $defenderExplain = New-Object System.Windows.Forms.Label
    $defenderExplain.Text = 'Adds only YOMI''s bundled yt-dlp.exe as a process exclusion. It never excludes PowerShell, TEMP, mpv, Deno, your profile, or broad YOMI folders. Fresh installs leave this unchecked; an existing YOMI-managed choice is preserved.'
    $defenderExplain.Location = New-Object System.Drawing.Point(45,312); $defenderExplain.Size = New-Object System.Drawing.Size(545,62); $defenderExplain.ForeColor = [System.Drawing.Color]::DimGray; $pf.Controls.Add($defenderExplain)
    $ok = New-Object System.Windows.Forms.Button; $ok.Text='INSTALL'; $ok.Location=New-Object System.Drawing.Point(355,402); $ok.Size=New-Object System.Drawing.Size(110,36); $ok.DialogResult=[System.Windows.Forms.DialogResult]::OK; $pf.Controls.Add($ok)
    $cancel = New-Object System.Windows.Forms.Button; $cancel.Text='CANCEL'; $cancel.Location=New-Object System.Drawing.Point(480,402); $cancel.Size=New-Object System.Drawing.Size(110,36); $cancel.DialogResult=[System.Windows.Forms.DialogResult]::Cancel; $pf.Controls.Add($cancel)
    $pf.AcceptButton=$ok; $pf.CancelButton=$cancel
    if ($pf.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { throw 'Installation cancelled.' }
    $installFfmpeg = $false; $installDeno = $false; $initialMode = 'Player'; $profileName = 'Minimal Player'
    if ($full.Checked) { $installFfmpeg=$true; $installDeno=$true; $initialMode='Streamer / OBS'; $profileName='Full YOMI' }
    elseif ($player.Checked) { $installDeno=$true; $initialMode='Player'; $profileName='Player' }
    $enableDefenderExclusion = [bool]$defender.Checked
    Write-Host "      Profile: $profileName" -ForegroundColor Green
    Write-Host ("      Defender performance opt-in: " + $(if($enableDefenderExclusion){'selected'}else{'not selected'})) -ForegroundColor Green

    Set-InstallStage 2 8 'Finding current mpv Windows release...'


    $mpvAssetUrl = $null

    foreach ($mpvApi in @(
        'https://api.github.com/repos/mpv-player/mpv/releases/latest',
        'https://api.github.com/repos/mpv-player/mpv/releases/tags/git-release'
    )) {
        if ($mpvAssetUrl) { break }

        try {
            $release = Invoke-RestMethod `
                -Uri $mpvApi `
                -Headers $headers `
                -UseBasicParsing `
                -TimeoutSec 30

            $asset = $release.assets |
                Where-Object {
                    $_.name -match '^mpv-v.*-x86_64-pc-windows-msvc\.zip$'
                } |
                Select-Object -First 1

            if ($asset) {
                $mpvAssetUrl = $asset.browser_download_url
            }
        }
        catch {
            Write-Host "      Release lookup failed on one endpoint; trying fallback..." -ForegroundColor DarkYellow
        }
    }

    if (-not $mpvAssetUrl) {
        throw 'Could not locate a current official x86_64 Windows mpv build.'
    }

    Write-Host '      mpv release located.' -ForegroundColor Green

    Set-InstallStage 3 8 'Downloading mpv...'
    $mpvZip = Join-Path $tempRoot 'mpv.zip'
    Get-CachedDownload `
        -Uri $mpvAssetUrl `
        -CacheFile (Join-Path $downloadCache 'mpv-current.zip') `
        -OutFile $mpvZip `
        -Label 'Downloading mpv' `
        -Headers $headers

    Set-InstallStage 4 8 'Downloading yt-dlp...'
    $ytdlpExe = Join-Path $tempRoot 'yt-dlp.exe'
    Get-CachedDownload `
        -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' `
        -CacheFile (Join-Path $downloadCache 'yt-dlp-current.exe') `
        -OutFile $ytdlpExe `
        -Label 'Downloading yt-dlp' `
        -Headers $headers

    Set-InstallStage 5 8 'Downloading selected optional components...'
    $ffmpegZip = Join-Path $tempRoot 'ffmpeg.zip'
    $denoZip = Join-Path $tempRoot 'deno.zip'
    if ($installFfmpeg) {
        Get-CachedDownload -Uri 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip' -CacheFile (Join-Path $downloadCache 'ffmpeg-release-essentials.zip') -OutFile $ffmpegZip -Label 'Downloading FFmpeg Media Tools'
    } else { Write-Host '      FFmpeg Media Tools: skipped by profile' -ForegroundColor DarkGray }
    if ($installDeno) {
        Get-CachedDownload -Uri 'https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip' -CacheFile (Join-Path $downloadCache 'deno-current.zip') -OutFile $denoZip -Label 'Downloading Deno' -Headers $headers
    } else { Write-Host '      Deno: skipped by profile' -ForegroundColor DarkGray }

    # ------------------------------------------------------------
    # Build clean Program Files tree in a staging directory first.
    # ------------------------------------------------------------

    Set-InstallStage 6 8 'Extracting and building the program...'

    $stage = Join-Path $tempRoot 'install-stage'
    $appStage = Join-Path $stage 'app'
    $webStage = Join-Path $stage 'web'
    $runtimeStage = Join-Path $stage 'runtime'
    $assetsStage = Join-Path $stage 'assets'
    $mpvStage = Join-Path $runtimeStage 'mpv'
    $ffmpegStage = Join-Path $runtimeStage 'ffmpeg'
    $ytdlpStage = Join-Path $runtimeStage 'yt-dlp'
    $denoStage = Join-Path $runtimeStage 'deno'

    foreach ($dir in @($appStage,$webStage,$assetsStage,$mpvStage,$ffmpegStage,$ytdlpStage,$denoStage)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    Copy-Item (Join-Path $payload 'app\*') $appStage -Recurse -Force
    Copy-Item (Join-Path $payload 'web\*') $webStage -Recurse -Force
    Copy-Item (Join-Path $payload 'assets\*') $assetsStage -Recurse -Force
    Copy-Item (Join-Path $payload 'README-EASY.txt') (Join-Path $stage 'README-EASY.txt') -Force
    Copy-Item (Join-Path $payload 'README-TECHNICAL.txt') (Join-Path $stage 'README-TECHNICAL.txt') -Force
    Copy-Item (Join-Path $payload 'THIRD-PARTY.txt') (Join-Path $stage 'THIRD-PARTY.txt') -Force
    Copy-Item (Join-Path $payload 'VERSION.txt') (Join-Path $stage 'VERSION.txt') -Force
    Copy-Item (Join-Path $payload 'Uninstall YOMI.cmd') (Join-Path $stage 'Uninstall YOMI.cmd') -Force

    Write-Host '      Extracting mpv...' -ForegroundColor DarkCyan
    $mpvExtract = Join-Path $tempRoot 'mpv-extract'
    Expand-Archive -Path $mpvZip -DestinationPath $mpvExtract -Force
    $mpvFound = Get-ChildItem $mpvExtract -Filter 'mpv.exe' -File -Recurse | Select-Object -First 1
    if (-not $mpvFound) { throw 'mpv.exe was not found in the downloaded mpv package.' }

    # YOMI only needs the actual player. Do NOT install mpv.pdb
    # (debug symbols), registration helpers, or other development baggage.
    Copy-Item $mpvFound.FullName (Join-Path $mpvStage 'mpv.exe') -Force

    Write-Host (
        "      mpv runtime installed: {0:N1} MB" -f (
            (Get-Item (Join-Path $mpvStage 'mpv.exe')).Length / 1MB
        )
    ) -ForegroundColor DarkGray

    if ($installFfmpeg) {
        Write-Host '      Extracting FFmpeg Media Tools...' -ForegroundColor DarkCyan
        $ffmpegExtract = Join-Path $tempRoot 'ffmpeg-extract'
        Expand-Archive -Path $ffmpegZip -DestinationPath $ffmpegExtract -Force
        $ffmpegFound = Get-ChildItem $ffmpegExtract -Filter 'ffmpeg.exe' -File -Recurse | Select-Object -First 1
        $ffprobeFound = Get-ChildItem $ffmpegExtract -Filter 'ffprobe.exe' -File -Recurse | Select-Object -First 1
        if (-not $ffmpegFound -or -not $ffprobeFound) { throw 'FFmpeg/ffprobe were not found in the downloaded package.' }
        Copy-Item $ffmpegFound.FullName (Join-Path $ffmpegStage 'ffmpeg.exe') -Force
        Copy-Item $ffprobeFound.FullName (Join-Path $ffmpegStage 'ffprobe.exe') -Force
    }
    if ($installDeno) {
        Write-Host '      Extracting Deno...' -ForegroundColor DarkCyan
        $denoExtract = Join-Path $tempRoot 'deno-extract'
        Expand-Archive -Path $denoZip -DestinationPath $denoExtract -Force
        $denoFound = Get-ChildItem $denoExtract -Filter 'deno.exe' -File -Recurse | Select-Object -First 1
        if (-not $denoFound) { throw 'deno.exe was not found in the downloaded package.' }
        Copy-Item $denoFound.FullName (Join-Path $denoStage 'deno.exe') -Force
    }

    Copy-Item $ytdlpExe (Join-Path $ytdlpStage 'yt-dlp.exe') -Force

    # ------------------------------------------------------------
    # Compile the tiny process-priority runner.
    # It lets the cache workers start at very low priority without
    # spawning a PowerShell process for every FFmpeg / yt-dlp job.
    # ------------------------------------------------------------

    Write-Host '      Compiling low-priority process runner...' -ForegroundColor DarkCyan
    $prioritySource = Get-Content (Join-Path $appStage 'PriorityRun.cs') -Raw
    $priorityExe = Join-Path $appStage 'PriorityRun.exe'
    if (Test-Path $priorityExe) { Remove-Item $priorityExe -Force }

    Add-Type `
        -TypeDefinition $prioritySource `
        -Language CSharp `
        -OutputAssembly $priorityExe `
        -OutputType ConsoleApplication

    if (-not (Test-Path $priorityExe)) { throw 'PriorityRun.exe failed to compile.' }

    Write-Host '      Compiling smart artwork edge detector...' -ForegroundColor DarkCyan
    $detectorSource = Get-Content (Join-Path $appStage 'ArtworkEdgeDetector.cs') -Raw
    $detectorExe = Join-Path $appStage 'ArtworkEdgeDetector.exe'
    if (Test-Path $detectorExe) { Remove-Item $detectorExe -Force }

    Add-Type `
        -TypeDefinition $detectorSource `
        -Language CSharp `
        -ReferencedAssemblies 'System.Drawing.dll' `
        -OutputAssembly $detectorExe `
        -OutputType ConsoleApplication

    if (-not (Test-Path $detectorExe)) { throw 'ArtworkEdgeDetector.exe failed to compile.' }


    Write-Host '      Compiling console-free YOMI GUI launcher...' -ForegroundColor DarkCyan
    $launcherSource = Get-Content (Join-Path $appStage 'YomiLauncher.cs') -Raw
    $launcherExe = Join-Path $appStage 'YomiLauncher.exe'
    if (Test-Path $launcherExe) { Remove-Item $launcherExe -Force }

    Add-Type `
        -TypeDefinition $launcherSource `
        -Language CSharp `
        -OutputAssembly $launcherExe `
        -OutputType WindowsApplication

    if (-not (Test-Path $launcherExe)) { throw 'YomiLauncher.exe failed to compile.' }

    # Source is useful for transparency but not needed at runtime.
    # Keep it in the installation so advanced users can inspect it.

    Write-Host '      Checking runtime scripts...' -ForegroundColor DarkCyan

    # Parse every PowerShell runtime file before anything is installed.
    foreach ($psFile in (Get-ChildItem $appStage -Filter '*.ps1' -File)) {
        $tokens = $null
        $parseErrors = $null
        [void][System.Management.Automation.Language.Parser]::ParseFile(
            $psFile.FullName,
            [ref]$tokens,
            [ref]$parseErrors
        )
        if ($parseErrors.Count -gt 0) {
            throw "PowerShell syntax check failed: $($psFile.Name): $($parseErrors[0].Message)"
        }
    }

    # ------------------------------------------------------------
    # Install application atomically-ish after dependencies passed.
    # ------------------------------------------------------------

    Set-InstallStage 7 8 'Installing program and shortcuts...'

    # Safe upgrade: stop only the EXISTING YOMI runtime.
    #
    # RC3 accidentally matched and killed its own elevated installer here
    # because the installer command line itself mentioned C:\Program Files\YOMI.
    # Protect both the elevated installer and the non-elevated launcher parent.
    $installerPid = $PID
    $installerParentPid = 0

    try {
        $selfInfo = Get-CimInstance Win32_Process `
            -Filter "ProcessId=$installerPid" `
            -ErrorAction Stop
        $installerParentPid = [int]$selfInfo.ParentProcessId
    }
    catch {}

    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object {
            $_.ProcessId -ne $installerPid -and
            $_.ProcessId -ne $installerParentPid -and
            (
                ($_.ExecutablePath -and $_.ExecutablePath -like "$installRoot*") -or
                (
                    $_.CommandLine -and
                    (
                        $_.CommandLine -like "*C:\Program Files\YOMI\app\supervisor.ps1*" -or
                        $_.CommandLine -like "*C:\Program Files\YOMI\app\controller.ps1*" -or
                        $_.CommandLine -like "*C:\Program Files\YOMI\app\server.ps1*" -or
                        $_.CommandLine -like "*C:\Program Files\YOMI\app\settings.ps1*" -or
                        $_.CommandLine -like "*C:\Program Files\YOMI\app\playlist-refresh.ps1*" -or
                        $_.CommandLine -like "*yomi-rc2*" -or
                        $_.CommandLine -like "*yomi-rc3*" -or
                        $_.CommandLine -like "*yomi-v4*" -or
                        $_.CommandLine -like "*YOMI_V4*"
                    )
                )
            )
        } |
        ForEach-Object {
            Write-Host ("      Stopping old YOMI process PID " + $_.ProcessId + " " + $_.Name) -ForegroundColor DarkGray
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }

    Start-Sleep -Milliseconds 500

    Write-Host ("      Installer process protected: PID " + $installerPid) -ForegroundColor Green

    try {
        Set-Content $stageFile `
            ((Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' [7/8] Old YOMI stopped; installer survived cleanup') `
            -Encoding ASCII
    }
    catch {}

    if (Test-Path $installRoot) {
        $old = "$installRoot.old"
        Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
        Move-Item $installRoot $old -Force
        try {
            Move-Item $stage $installRoot -Force
            Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
        }
        catch {
            if (Test-Path $installRoot) { Remove-Item $installRoot -Recurse -Force -ErrorAction SilentlyContinue }
            Move-Item $old $installRoot -Force
            throw
        }
    }
    else {
        Move-Item $stage $installRoot -Force
    }

    # ------------------------------------------------------------
    # Per-user writable state lives in LocalAppData, never Program Files.
    # ------------------------------------------------------------

    foreach ($dir in @(
        $dataRoot,
        (Join-Path $dataRoot 'cache'),
        (Join-Path $dataRoot 'cache\audio'),
        (Join-Path $dataRoot 'cache\artwork'),
        (Join-Path $dataRoot 'cache\video'),
        (Join-Path $dataRoot 'cache\visualizer'),
        (Join-Path $dataRoot 'cache\meta'),
        (Join-Path $dataRoot 'cache\gain'),
        (Join-Path $dataRoot 'cache\status'),
        (Join-Path $dataRoot 'state'),
        (Join-Path $dataRoot 'logs')
    )) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    $configPath = Join-Path $dataRoot 'config.json'
    if (-not (Test-Path $configPath)) {
        Copy-Item (Join-Path $installRoot 'app\default-config.json') $configPath -Force
        $fresh = Get-Content $configPath -Raw | ConvertFrom-Json
        $fresh.app_mode = $initialMode
        if (-not $installFfmpeg) { $fresh.visualizer_enabled = $false; $fresh.smart_artwork_crop = $false; $fresh.loudness_normalization = $false }
        $enc = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($configPath,($fresh | ConvertTo-Json -Depth 12),$enc)
    }

    # ------------------------------------------------------------
    # Explicit, narrow Windows Defender performance opt-in.
    # YOMI never adds broad folder, PowerShell, TEMP, mpv or Deno exclusions.
    # A marker is written only when YOMI itself owns the exact process exclusion.
    # ------------------------------------------------------------

    $defenderTarget = Join-Path $installRoot 'runtime\yt-dlp\yt-dlp.exe'
    $markerWasPresent = Test-Path $defenderMarker
    function Test-ExactDefenderProcessExclusion([string]$Path) {
        $items = @((Get-MpPreference -ErrorAction Stop).ExclusionProcess)
        foreach ($item in $items) {
            $expanded = [Environment]::ExpandEnvironmentVariables([string]$item)
            if ([string]::Equals($expanded,$Path,[StringComparison]::OrdinalIgnoreCase)) { return $true }
        }
        return $false
    }

    try {
        if ($enableDefenderExclusion) {
            if (-not (Test-ExactDefenderProcessExclusion $defenderTarget)) {
                Add-MpPreference -ExclusionProcess $defenderTarget -ErrorAction Stop
                if (-not (Test-ExactDefenderProcessExclusion $defenderTarget)) {
                    throw 'Windows Defender did not retain the requested yt-dlp process exclusion.'
                }
                Set-Content $defenderMarker $defenderTarget -Encoding Unicode
                Write-Host '      Defender performance exclusion: ADDED for YOMI yt-dlp only' -ForegroundColor Green
            }
            elseif ($markerWasPresent) {
                Set-Content $defenderMarker $defenderTarget -Encoding Unicode
                Write-Host '      Defender performance exclusion: existing YOMI-managed choice preserved' -ForegroundColor Green
            }
            else {
                Write-Host '      Defender performance exclusion: matching user-managed exclusion already exists; YOMI will not claim or remove it' -ForegroundColor DarkYellow
            }
        }
        elseif ($markerWasPresent) {
            $markedTarget = (Get-Content $defenderMarker -Raw -ErrorAction Stop).Trim()
            if ([string]::Equals($markedTarget,$defenderTarget,[StringComparison]::OrdinalIgnoreCase)) {
                if (Test-ExactDefenderProcessExclusion $defenderTarget) {
                    Remove-MpPreference -ExclusionProcess $defenderTarget -ErrorAction Stop
                    if (Test-ExactDefenderProcessExclusion $defenderTarget) {
                        throw 'Windows Defender did not remove the YOMI-managed yt-dlp exclusion.'
                    }
                }
                Remove-Item $defenderMarker -Force -ErrorAction Stop
                Write-Host '      Defender performance exclusion: removed by user choice' -ForegroundColor Green
            }
            else {
                throw 'The YOMI Defender ownership marker did not contain the expected yt-dlp path.'
            }
        }
        else {
            Write-Host '      Defender performance exclusion: not requested' -ForegroundColor DarkGray
        }
    }
    catch {
        Write-Host ('      Defender performance option could not be applied: ' + $_.Exception.Message) -ForegroundColor DarkYellow
        Write-Host '      Installation will continue; no broad fallback exclusion will be added.' -ForegroundColor DarkYellow
    }

    # ------------------------------------------------------------
    # GUI launcher and shortcuts.
    # YomiLauncher.exe is a Windows-subsystem executable: no console window,
    # but the PowerShell WinForms controller/settings remain fully visible.
    # ------------------------------------------------------------

    Write-Host '      Creating Start Menu shortcuts...' -ForegroundColor DarkCyan
    $wsh = New-Object -ComObject WScript.Shell
    $startFolder = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\YOMI'
    New-Item -ItemType Directory -Path $startFolder -Force | Out-Null

    function New-AppShortcut {
        param(
            [Parameter(Mandatory=$true)][string]$Path,
            [Parameter(Mandatory=$true)][string]$Target,
            [string]$ShortcutArguments = '',
            [string]$IconLocation = ''
        )

        $sc = $wsh.CreateShortcut($Path)
        $sc.TargetPath = [string]$Target
        $sc.Arguments = [string]$ShortcutArguments
        $sc.WorkingDirectory = [string]$installRoot
        if ($IconLocation) { $sc.IconLocation = [string]$IconLocation }
        $sc.Save()
    }

    $guiLauncher = Join-Path $installRoot 'app\YomiLauncher.exe'

    New-AppShortcut (Join-Path $startFolder 'YOMI.lnk') $guiLauncher 'controller' (Join-Path $installRoot 'assets\yomi-v408.ico')
    New-AppShortcut (Join-Path $startFolder 'YOMI Settings.lnk') $guiLauncher 'settings' (Join-Path $installRoot 'assets\yomi-settings-v408.ico')
    New-AppShortcut (Join-Path $startFolder 'Open YOMI Data Folder.lnk') 'explorer.exe' ('"' + $dataRoot + '"') (Join-Path $installRoot 'assets\yomi-v408.ico')
    New-AppShortcut (Join-Path $startFolder 'Shuffle Playlist.lnk') 'powershell.exe' ('-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installRoot 'app\shuffle.ps1') + '" -Interactive') (Join-Path $installRoot 'assets\yomi-v408.ico')
    New-AppShortcut (Join-Path $startFolder 'Easy README.lnk') "$env:WINDIR\System32\notepad.exe" ('"' + (Join-Path $installRoot 'README-EASY.txt') + '"')
    New-AppShortcut (Join-Path $startFolder 'Copy Diagnostics.lnk') 'powershell.exe' ('-NoProfile -ExecutionPolicy Bypass -NoExit -File "' + (Join-Path $installRoot 'app\diagnostics.ps1') + '"')
    New-AppShortcut (Join-Path $startFolder 'Uninstall YOMI.lnk') (Join-Path $installRoot 'Uninstall YOMI.cmd') '' (Join-Path $installRoot 'assets\yomi-settings-v408.ico')

    Add-Type -AssemblyName System.Windows.Forms
    $desktopAnswer = [System.Windows.Forms.MessageBox]::Show(
        'Create YOMI desktop shortcuts?',
        'YOMI Setup',
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Question
    )

    if ($desktopAnswer -eq [System.Windows.Forms.DialogResult]::Yes) {
        $desktopFolder = [Environment]::GetFolderPath('Desktop')
        New-AppShortcut (Join-Path $desktopFolder 'YOMI.lnk') $guiLauncher 'controller' (Join-Path $installRoot 'assets\yomi-v408.ico')
        New-AppShortcut (Join-Path $desktopFolder 'YOMI Settings.lnk') $guiLauncher 'settings' (Join-Path $installRoot 'assets\yomi-settings-v408.ico')
    }

    Set-InstallStage 8 8 'Final verification...'

    $required = @(
        (Join-Path $installRoot 'runtime\mpv\mpv.exe'),
        (Join-Path $installRoot 'runtime\yt-dlp\yt-dlp.exe'),
        (Join-Path $installRoot 'app\PriorityRun.exe'),
        (Join-Path $installRoot 'app\ArtworkEdgeDetector.exe'),
        (Join-Path $installRoot 'app\YomiLauncher.exe'),
        (Join-Path $installRoot 'VERSION.txt'),
        (Join-Path $installRoot 'app\music.lua'),
        (Join-Path $installRoot 'app\server.ps1'),
        (Join-Path $installRoot 'app\controller.ps1'),
        (Join-Path $installRoot 'app\shuffle.ps1'),
        (Join-Path $installRoot 'app\uninstall.ps1'),
        (Join-Path $installRoot 'app\update.ps1'),
        (Join-Path $installRoot 'Uninstall YOMI.cmd'),
        (Join-Path $installRoot 'web\overlay.html'),
        (Join-Path $installRoot 'web\director.html'),
        (Join-Path $installRoot 'assets\yomi-v408.ico'),
        (Join-Path $installRoot 'assets\yomi-settings-v408.ico'),
        (Join-Path $installRoot 'app\components.ps1')
    )

    foreach ($r in $required) {
        if (-not (Test-Path $r)) { throw "Final verification failed: $r" }
    }
    if ($installFfmpeg) { foreach ($r in @((Join-Path $installRoot 'runtime\ffmpeg\ffmpeg.exe'),(Join-Path $installRoot 'runtime\ffmpeg\ffprobe.exe'))) { if (-not (Test-Path $r)) { throw "Final verification failed: $r" } } }
    if ($installDeno -and -not (Test-Path (Join-Path $installRoot 'runtime\deno\deno.exe'))) { throw 'Final verification failed: Deno was selected but deno.exe is missing.' }


    Write-Host ''
    Set-Content (Join-Path $dataRoot 'install-status.txt') `
        ("PASS " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')) `
        -Encoding ASCII

    Set-Content $stageFile `
        ((Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' COMPLETE') `
        -Encoding ASCII

    $installedBytes = (
        Get-ChildItem $installRoot -File -Recurse -ErrorAction SilentlyContinue |
        Measure-Object -Property Length -Sum
    ).Sum

    Write-Host (
        "Installed program size: {0:N1} MB" -f ($installedBytes / 1MB)
    ) -ForegroundColor Green

    Write-Host 'INSTALL PASSED.' -ForegroundColor Green
    Write-Host 'Unrelated media-player installations were not modified.' -ForegroundColor Green
    Write-Host ''
    Write-Host 'Opening Settings now...' -ForegroundColor Yellow

    Start-Process (Join-Path $installRoot 'app\YomiLauncher.exe') -ArgumentList 'settings'
}
catch {
    try {
        Set-Content (Join-Path $dataRoot 'install-status.txt') `
            ("FAIL " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + " :: " + $_.Exception.Message) `
            -Encoding ASCII
    }
    catch {}

    Write-Host ''
    Write-Host 'INSTALL FAILED:' -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ''

    if (Test-Path $stageFile) {
        Write-Host 'Last installer stage:' -ForegroundColor Yellow
        Get-Content $stageFile | Write-Host
        Write-Host ''
    }

    Write-Host "Full installer log: $installLog" -ForegroundColor Yellow
    Write-Host "Failure status:     $(Join-Path $dataRoot 'install-status.txt')" -ForegroundColor Yellow
    exit 1
}
finally {
    try { Stop-Transcript | Out-Null } catch {}
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
