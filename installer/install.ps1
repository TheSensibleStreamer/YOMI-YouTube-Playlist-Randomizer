param([switch]$UpdateMode,[string]$UpdateStatusFile)
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


# Relaunch elevated because Program Files is protected.
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    if ($UpdateMode -and -not [string]::IsNullOrWhiteSpace($UpdateStatusFile)) {
        try {
            $statusParent=Split-Path $UpdateStatusFile -Parent
            if($statusParent){New-Item -ItemType Directory -Path $statusParent -Force|Out-Null}
            $statusObj=[ordered]@{schema=1;percent=61;state='waiting-admin';message='Waiting for Windows administrator approval...';updated_utc=[DateTime]::UtcNow.ToString('o')}
            $statusTmp=$UpdateStatusFile+'.tmp-'+[Guid]::NewGuid().ToString('N')
            [IO.File]::WriteAllText($statusTmp,($statusObj|ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
            Move-Item -LiteralPath $statusTmp -Destination $UpdateStatusFile -Force
        } catch {}
    }
    $args = @('-NoProfile')
    if ($UpdateMode) { $args += @('-WindowStyle','Hidden') }
    $args += @('-ExecutionPolicy','Bypass','-File',('"' + $PSCommandPath + '"'))
    if ($UpdateMode) {
        $args += '-UpdateMode'
        if (-not [string]::IsNullOrWhiteSpace($UpdateStatusFile)) {
            $args += @('-UpdateStatusFile',('"' + $UpdateStatusFile + '"'))
        }
    }

    try {
        $elevated = Start-Process powershell.exe `
            -Verb RunAs `
            -ArgumentList $args `
            -WindowStyle $(if($UpdateMode){'Hidden'}else{'Normal'}) `
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
$existingInstallAtStart = Test-Path -LiteralPath (Join-Path $installRoot 'VERSION.txt') -PathType Leaf
$dataRoot = Join-Path $env:LOCALAPPDATA 'YOMI'
$defenderMarker = Join-Path $dataRoot 'defender-yomi-exclusions.json'
$legacyDefenderMarker = Join-Path $dataRoot 'defender-yt-dlp-process-exclusion.txt'
$tempRoot = Join-Path $env:TEMP ('YOMI-Install-' + [Guid]::NewGuid().ToString('N'))

# Permanent installer log so a fast-closing admin window can never hide
# the actual failure again.
New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null
$installLog = Join-Path $dataRoot 'install.log'

try {
    Start-Transcript -Path $installLog -Append -Force | Out-Null
}
catch {}

Write-Host '===== YOMI 420.69.9002 - YOUTUBE OBS MUSIC INTERFACE =====' -ForegroundColor Cyan
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

function Write-InstallUpdateStatus([int]$Percent,[string]$State,[string]$Message) {
    if (-not $UpdateMode -or [string]::IsNullOrWhiteSpace($UpdateStatusFile)) { return }
    try {
        $parent = Split-Path $UpdateStatusFile -Parent
        if ($parent) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
        $obj = [ordered]@{schema=1;percent=[Math]::Max(0,[Math]::Min(100,$Percent));state=$State;message=$Message;updated_utc=[DateTime]::UtcNow.ToString('o')}
        $tmp = $UpdateStatusFile + '.tmp-' + [Guid]::NewGuid().ToString('N')
        [IO.File]::WriteAllText($tmp,($obj|ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $tmp -Destination $UpdateStatusFile -Force
    } catch {}
}
function Set-InstallStage {
    param(
        [int]$Number,
        [int]$Total,
        [string]$Text
    )

    $line = "[$Number/$Total] $Text"
    Write-Host ''
    Write-Host $line -ForegroundColor Cyan
    if ($UpdateMode) {
        $pct = 60 + [Math]::Floor((32.0 * $Number) / [Math]::Max(1,$Total))
        Write-InstallUpdateStatus $pct 'installing' ('Installing YOMI: ' + $Text)
    }

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
    $request.UserAgent = 'YOMI-420.69.9002-Installer'
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

    $headers = @{ 'User-Agent' = 'YOMI-420.69.9002-Installer' }

    # YOMI is one product. Normal installs and updates always carry the complete
    # runtime so behavior never depends on an old installer profile choice.
    $installFfmpeg = $true
    $installDeno = $true
    $initialMode = 'Streamer / OBS'
    $profileName = 'Full YOMI'
    $enableDefenderExclusion = $true
    Write-Host "      Profile: $profileName" -ForegroundColor Green
    Write-Host '      Defender performance protection: automatic for YOMI-owned files and processes' -ForegroundColor Green

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
    $runtimeStage = Join-Path $stage 'runtime'
    $assetsStage = Join-Path $stage 'assets'
    $mpvStage = Join-Path $runtimeStage 'mpv'
    $ffmpegStage = Join-Path $runtimeStage 'ffmpeg'
    $ytdlpStage = Join-Path $runtimeStage 'yt-dlp'
    $denoStage = Join-Path $runtimeStage 'deno'

    foreach ($dir in @($appStage,$assetsStage,$mpvStage,$ffmpegStage,$ytdlpStage,$denoStage)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    Copy-Item (Join-Path $payload 'app\*') $appStage -Recurse -Force
    Copy-Item (Join-Path $payload 'assets\*') $assetsStage -Recurse -Force
    Copy-Item (Join-Path $payload 'README-EASY.txt') (Join-Path $stage 'README-EASY.txt') -Force
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

    Write-Host '      Compiling native YOMI controller...' -ForegroundColor DarkCyan
    function Resolve-Csc {
        foreach($candidate in @(
            (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
            (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
        )) { if(Test-Path -LiteralPath $candidate){ return $candidate } }
        throw '.NET Framework C# compiler (csc.exe) was not found.'
    }
    function Resolve-FrameworkReferencePath([string]$Name) {
        $loaded=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq $Name -and $_.Location } | Select-Object -First 1
        if($loaded){ return [string]$loaded.Location }
        $dirs=@()
        try { $runtimeDir=[Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory(); if($runtimeDir){$dirs+=$runtimeDir;$dirs+=(Join-Path $runtimeDir 'WPF')} } catch {}
        foreach($frameworkRoot in @((Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'),(Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'))){$dirs+=$frameworkRoot;$dirs+=(Join-Path $frameworkRoot 'WPF')}
        $pf86=[Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFilesX86)
        if($pf86){$refRoot=Join-Path $pf86 'Reference Assemblies\Microsoft\Framework\.NETFramework';if(Test-Path $refRoot){foreach($v in @(Get-ChildItem $refRoot -Directory -ErrorAction SilentlyContinue|Where-Object{$_.Name -like 'v4*'}|Sort-Object Name -Descending)){$dirs+=$v.FullName;$dirs+=(Join-Path $v.FullName 'Facades')}}}
        foreach($dir in @($dirs|Where-Object{$_}|Select-Object -Unique)){$candidate=Join-Path $dir ($Name+'.dll');if(Test-Path $candidate){return $candidate}}
        throw ('Required .NET Framework reference assembly was not found: '+$Name)
    }
    $csc=Resolve-Csc
    $controllerSource=Join-Path $appStage 'YomiControllerWpf.cs'
    $controllerExe=Join-Path $appStage 'YomiControllerWpf.exe'
    $controllerManifest=Join-Path $appStage 'YomiControllerWpf.manifest'
    $controllerRefs=@('PresentationFramework','PresentationCore','WindowsBase','System.Xaml','WindowsFormsIntegration','System.Windows.Forms','System.Drawing','System.Web.Extensions','System.Xml','System','System.Core')
    $controllerArgs=@('/nologo','/noconfig','/codepage:65001','/target:winexe','/platform:anycpu','/optimize+','/debug-',('/win32manifest:"'+$controllerManifest+'"'),('/out:"'+$controllerExe+'"'))
    foreach($refName in $controllerRefs){$controllerArgs+=('/reference:"'+(Resolve-FrameworkReferencePath $refName)+'"')}
    $controllerArgs+=('"'+$controllerSource+'"')
    $controllerOutput=& $csc @controllerArgs 2>&1
    if($LASTEXITCODE -ne 0){throw ("YOMI controller compile failed:`r`n"+($controllerOutput -join "`r`n"))}
    if(-not(Test-Path $controllerExe)){throw 'YomiControllerWpf.exe failed to compile.'}

    Write-Host '      Compiling OBS server host...' -ForegroundColor DarkCyan
    $obsSource=Join-Path $appStage 'YomiObsServerHost.cs'
    $obsExe=Join-Path $appStage 'YomiObsServer.exe'
    $obsArgs=@('/nologo','/noconfig','/codepage:65001','/target:exe','/platform:anycpu','/optimize+','/debug-',('/out:"'+$obsExe+'"'),('/reference:"'+(Resolve-FrameworkReferencePath 'System')+'"'),('/reference:"'+(Resolve-FrameworkReferencePath 'System.Core')+'"'),('"'+$obsSource+'"'))
    $obsOutput=& $csc @obsArgs 2>&1
    if($LASTEXITCODE -ne 0){throw ("OBS server compile failed:`r`n"+($obsOutput -join "`r`n"))}
    if(-not(Test-Path $obsExe)){throw 'YomiObsServer.exe failed to compile.'}

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

    # Bootstrap lock escape for 4.2.0.9/420.69.9002.
    # Those builds can leave update.ps1 alive with Program Files\YOMI\app as its process CWD,
    # which prevents the installation directory from being atomically renamed.
    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object {
            $_.ProcessId -ne $installerPid -and
            $_.CommandLine -and
            (
                $_.CommandLine -like "*$installRoot\app\update.ps1*" -or
                $_.CommandLine -like "*$installRoot\app\update-deployment.ps1*" -or
                $_.CommandLine -like "*$installRoot\app\YomiUpdateHost.ps1*"
            )
        } |
        ForEach-Object {
            Write-Host ("      Releasing old updater lock PID " + $_.ProcessId + " " + $_.Name) -ForegroundColor DarkGray
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }

    Start-Sleep -Milliseconds 350

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
                        $_.CommandLine -like "*C:\Program Files\YOMI\app\update.ps1*" -or
                        $_.CommandLine -like "*C:\Program Files\YOMI\app\update-deployment.ps1*" -or
                        $_.CommandLine -like "*C:\Program Files\YOMI\app\YomiUpdateHost.ps1*" -or
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
        if (Test-Path $old) {
            Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path $old) {
            $old = "$installRoot.old-" + [Guid]::NewGuid().ToString('N')
        }

        $movedOld = $false
        $lastMoveError = $null
        for ($attempt = 1; $attempt -le 20; $attempt++) {
            try {
                [IO.Directory]::Move($installRoot, $old)
                $movedOld = $true
                break
            }
            catch {
                $lastMoveError = $_
                Start-Sleep -Milliseconds 250
            }
        }
        if (-not $movedOld) {
            throw $lastMoveError
        }

        try {
            Move-Item $stage $installRoot -Force
            Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
        }
        catch {
            if (Test-Path $installRoot) {
                Remove-Item $installRoot -Recurse -Force -ErrorAction SilentlyContinue
            }
            if (Test-Path $old) {
                [IO.Directory]::Move($old, $installRoot)
            }
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
    # Windows Defender performance protection.
    # Scope is deliberately broad *inside YOMI* and nowhere else:
    # Program Files\YOMI, LocalAppData\YOMI, and YOMI-bundled executables.
    # Never exclude PowerShell, TEMP, the user profile, Downloads, or unrelated apps.
    # The ownership marker records only exclusions YOMI itself added.
    # ------------------------------------------------------------

    $defenderPaths = @($installRoot,$dataRoot)
    $defenderProcesses = @(
        (Join-Path $installRoot 'runtime\yt-dlp\yt-dlp.exe'),
        (Join-Path $installRoot 'runtime\mpv\mpv.exe'),
        (Join-Path $installRoot 'runtime\ffmpeg\ffmpeg.exe'),
        (Join-Path $installRoot 'runtime\ffmpeg\ffprobe.exe'),
        (Join-Path $installRoot 'runtime\deno\deno.exe'),
        (Join-Path $installRoot 'app\YomiControllerWpf.exe'),
        (Join-Path $installRoot 'app\YomiObsServer.exe'),
        (Join-Path $installRoot 'app\YomiLauncher.exe'),
        (Join-Path $installRoot 'app\PriorityRun.exe'),
        (Join-Path $installRoot 'app\ArtworkEdgeDetector.exe')
    )
    $ownedPaths = New-Object System.Collections.ArrayList
    $ownedProcesses = New-Object System.Collections.ArrayList
    function Contains-Exact($List,[string]$Value) {
        foreach($item in @($List)){if([string]::Equals([Environment]::ExpandEnvironmentVariables([string]$item),$Value,[StringComparison]::OrdinalIgnoreCase)){return $true}}
        return $false
    }
    function Add-Owned($List,[string]$Value) {
        if(-not (Contains-Exact $List $Value)){[void]$List.Add($Value)}
    }
    function Test-ExactDefenderPathExclusion([string]$Path) {
        foreach($item in @((Get-MpPreference -ErrorAction Stop).ExclusionPath)){
            if([string]::Equals([Environment]::ExpandEnvironmentVariables([string]$item),$Path,[StringComparison]::OrdinalIgnoreCase)){return $true}
        }
        return $false
    }
    function Test-ExactDefenderProcessExclusion([string]$Path) {
        foreach($item in @((Get-MpPreference -ErrorAction Stop).ExclusionProcess)){
            if([string]::Equals([Environment]::ExpandEnvironmentVariables([string]$item),$Path,[StringComparison]::OrdinalIgnoreCase)){return $true}
        }
        return $false
    }
    function Save-YomiDefenderOwnership {
        try {
            $obj=[ordered]@{schema=2;added_paths=@($ownedPaths);added_processes=@($ownedProcesses);updated_utc=[DateTime]::UtcNow.ToString('o')}
            [IO.File]::WriteAllText($defenderMarker,($obj|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
        } catch {}
    }

    try {
        if(Test-Path -LiteralPath $defenderMarker -PathType Leaf){
            try {
                $previous=Get-Content -LiteralPath $defenderMarker -Raw -Encoding UTF8|ConvertFrom-Json
                foreach($p in @($previous.added_paths)){if($p){Add-Owned $ownedPaths ([string]$p)}}
                foreach($p in @($previous.added_processes)){if($p){Add-Owned $ownedProcesses ([string]$p)}}
            } catch {}
        }
        if(Test-Path -LiteralPath $legacyDefenderMarker -PathType Leaf){
            try {
                $legacy=(Get-Content -LiteralPath $legacyDefenderMarker -Raw -ErrorAction Stop).Trim()
                $legacyTarget=Join-Path $installRoot 'runtime\yt-dlp\yt-dlp.exe'
                if([string]::Equals($legacy,$legacyTarget,[StringComparison]::OrdinalIgnoreCase)){Add-Owned $ownedProcesses $legacyTarget}
            } catch {}
        }

        foreach($path in $defenderPaths){
            if(-not (Test-ExactDefenderPathExclusion $path)){
                Add-MpPreference -ExclusionPath $path -ErrorAction Stop
                if(-not (Test-ExactDefenderPathExclusion $path)){throw ('Windows Defender did not retain YOMI path exclusion: '+$path)}
                Add-Owned $ownedPaths $path
            }
        }
        foreach($processPath in $defenderProcesses){
            if(-not (Test-ExactDefenderProcessExclusion $processPath)){
                Add-MpPreference -ExclusionProcess $processPath -ErrorAction Stop
                if(-not (Test-ExactDefenderProcessExclusion $processPath)){throw ('Windows Defender did not retain YOMI process exclusion: '+$processPath)}
                Add-Owned $ownedProcesses $processPath
            }
        }
        Save-YomiDefenderOwnership
        Remove-Item -LiteralPath $legacyDefenderMarker -Force -ErrorAction SilentlyContinue
        Write-Host '      Defender performance protection: YOMI program, cache/data and runtime processes excluded' -ForegroundColor Green
    }
    catch {
        Save-YomiDefenderOwnership
        Write-Host ('      Defender performance protection could not be fully applied: ' + $_.Exception.Message) -ForegroundColor DarkYellow
        Write-Host '      Installation will continue; YOMI will never widen the exclusion outside its own files/processes.' -ForegroundColor DarkYellow
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

    # Windows Start caches shortcut icons aggressively. During an atomic YOMI update the
    # Program Files tree briefly moves away, so an icon that points into that tree can be
    # cached as broken. Publish icon bytes to a stable LocalAppData shell path and include
    # the content hash in the filename; a changed icon automatically gets a fresh cache key.
    $shellIconRoot = Join-Path $dataRoot 'shell'
    New-Item -ItemType Directory -Path $shellIconRoot -Force | Out-Null
    function Publish-YomiShellIcon([string]$Source,[string]$Prefix) {
        if(-not(Test-Path -LiteralPath $Source -PathType Leaf)){ return ($Source + ',0') }
        $hash=(Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash.ToLowerInvariant().Substring(0,12)
        $dest=Join-Path $shellIconRoot ($Prefix+'-'+$hash+'.ico')
        if(-not(Test-Path -LiteralPath $dest -PathType Leaf)){ Copy-Item -LiteralPath $Source -Destination $dest -Force }
        return ($dest + ',0')
    }
    $mainIconLocation = Publish-YomiShellIcon (Join-Path $installRoot 'app\yomi.ico') 'yomi'
    $settingsIconLocation = Publish-YomiShellIcon (Join-Path $installRoot 'assets\yomi-settings-v408.ico') 'yomi-settings'

    # Recreate Start Menu links rather than editing a stale .lnk in place.
    foreach($shortcutName in @('YOMI.lnk','YOMI Settings.lnk','Open YOMI Data Folder.lnk','Shuffle Playlist.lnk','Easy README.lnk','Copy Diagnostics.lnk','Uninstall YOMI.lnk')){
        Remove-Item -LiteralPath (Join-Path $startFolder $shortcutName) -Force -ErrorAction SilentlyContinue
    }
    New-AppShortcut (Join-Path $startFolder 'YOMI.lnk') $guiLauncher 'controller' $mainIconLocation
    New-AppShortcut (Join-Path $startFolder 'YOMI Settings.lnk') $guiLauncher 'settings' $settingsIconLocation
    New-AppShortcut (Join-Path $startFolder 'Open YOMI Data Folder.lnk') 'explorer.exe' ('"' + $dataRoot + '"') $mainIconLocation
    New-AppShortcut (Join-Path $startFolder 'Shuffle Playlist.lnk') 'powershell.exe' ('-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installRoot 'app\shuffle.ps1') + '" -Interactive') $mainIconLocation
    New-AppShortcut (Join-Path $startFolder 'Easy README.lnk') "$env:WINDIR\System32\notepad.exe" ('"' + (Join-Path $installRoot 'README-EASY.txt') + '"')
    New-AppShortcut (Join-Path $startFolder 'Copy Diagnostics.lnk') 'powershell.exe' ('-NoProfile -ExecutionPolicy Bypass -NoExit -File "' + (Join-Path $installRoot 'app\diagnostics.ps1') + '"')
    New-AppShortcut (Join-Path $startFolder 'Uninstall YOMI.lnk') (Join-Path $installRoot 'Uninstall YOMI.cmd') '' $settingsIconLocation

    $desktopFolder = [Environment]::GetFolderPath('Desktop')
    $desktopYomi = Join-Path $desktopFolder 'YOMI.lnk'
    $desktopSettings = Join-Path $desktopFolder 'YOMI Settings.lnk'
    $hadDesktopYomi = Test-Path -LiteralPath $desktopYomi -PathType Leaf
    $hadDesktopSettings = Test-Path -LiteralPath $desktopSettings -PathType Leaf

    if ($existingInstallAtStart) {
        # Preserve desktop-shortcut state. Existing shortcuts are rewritten so a
        # changed target or icon is picked up automatically; absent shortcuts stay absent.
        if ($hadDesktopYomi) { New-AppShortcut $desktopYomi $guiLauncher 'controller' $mainIconLocation }
        if ($hadDesktopSettings) { New-AppShortcut $desktopSettings $guiLauncher 'settings' $settingsIconLocation }
    }
    else {
        Add-Type -AssemblyName System.Windows.Forms
        $desktopAnswer = [System.Windows.Forms.MessageBox]::Show(
            'Create YOMI desktop shortcuts?',
            'YOMI Setup',
            [System.Windows.Forms.MessageBoxButtons]::YesNo,
            [System.Windows.Forms.MessageBoxIcon]::Question
        )
        if ($desktopAnswer -eq [System.Windows.Forms.DialogResult]::Yes) {
            New-AppShortcut $desktopYomi $guiLauncher 'controller' $mainIconLocation
            New-AppShortcut $desktopSettings $guiLauncher 'settings' $settingsIconLocation
        }
    }
    try {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class YomiShellIconRefresh {
    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
'@
        [YomiShellIconRefresh]::SHChangeNotify(0x08000000,0,[IntPtr]::Zero,[IntPtr]::Zero)
    } catch {}

    # Register focused update packages for future test/development updates.
    try {
        $classes='HKCU:\Software\Classes'
        New-Item -Path (Join-Path $classes '.yomiupdate') -Force | Out-Null
        Set-ItemProperty -Path (Join-Path $classes '.yomiupdate') -Name '(default)' -Value 'YOMI.UpdatePackage' -Force
        $commandKey=Join-Path $classes 'YOMI.UpdatePackage\shell\open\command'
        New-Item -Path $commandKey -Force | Out-Null
        $hostScript=Join-Path $installRoot 'app\YomiUpdateHost.ps1'
        $command='powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "'+$hostScript+'" "%1"'
        Set-ItemProperty -Path $commandKey -Name '(default)' -Value $command -Force
    } catch { Write-Host ('      Update package association skipped: '+$_.Exception.Message) -ForegroundColor DarkYellow }

    Set-InstallStage 8 8 'Final verification...'

    $required = @(
        (Join-Path $installRoot 'runtime\mpv\mpv.exe'),
        (Join-Path $installRoot 'runtime\yt-dlp\yt-dlp.exe'),
        (Join-Path $installRoot 'app\PriorityRun.exe'),
        (Join-Path $installRoot 'app\YomiControllerWpf.exe'),
        (Join-Path $installRoot 'app\YomiControllerWpf.cs'),
        (Join-Path $installRoot 'app\YomiControllerWpf.xaml'),
        (Join-Path $installRoot 'app\YomiDesign.xaml'),
        (Join-Path $installRoot 'app\YomiObsServer.exe'),
        (Join-Path $installRoot 'app\YomiObsServerHost.cs'),
        (Join-Path $installRoot 'app\YomiUpdateHost.ps1'),
        (Join-Path $installRoot 'app\YomiPublicUpdateHost.ps1'),
        (Join-Path $installRoot 'app\YomiDiagnosticBundle.ps1'),
        (Join-Path $installRoot 'app\FOCUSED-BUILD.txt'),
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
        (Join-Path $installRoot 'app\yomi.ico'),
        (Join-Path $installRoot 'assets\yomi-settings-v408.ico'),
        (Join-Path $installRoot 'app\components.ps1')
    )

    foreach ($r in $required) {
        if (-not (Test-Path $r)) { throw "Final verification failed: $r" }
    }
    if ($installFfmpeg) { foreach ($r in @((Join-Path $installRoot 'runtime\ffmpeg\ffmpeg.exe'),(Join-Path $installRoot 'runtime\ffmpeg\ffprobe.exe'))) { if (-not (Test-Path $r)) { throw "Final verification failed: $r" } } }
    if ($installDeno -and -not (Test-Path (Join-Path $installRoot 'runtime\deno\deno.exe'))) { throw 'Final verification failed: Deno was selected but deno.exe is missing.' }

    Write-Host '      Verifying current WPF control plane...' -ForegroundColor DarkCyan
    $controllerSelfTest = Start-Process -FilePath (Join-Path $installRoot 'app\YomiControllerWpf.exe') -ArgumentList '--install-probe' -WorkingDirectory (Join-Path $installRoot 'app') -PassThru -Wait
    if ($controllerSelfTest.ExitCode -ne 0) {
        throw ('Final verification failed: YomiControllerWpf install probe exit ' + $controllerSelfTest.ExitCode)
    }

    # If this install bootstrapped itself by terminating the old updater, close the transaction here.
    $updateTxFile = Join-Path $dataRoot 'state\update-transaction.json'
    if (Test-Path $updateTxFile) {
        try {
            $updateTx = Get-Content $updateTxFile -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($updateTx -and [string]$updateTx.to_version -eq '420.69.9002') {
                # Compatibility with 9.9.3/9.9.4 deployment scripts running under StrictMode:
                # their transaction schema omitted these fields, then verification tried to
                # assign them directly and failed with "Exception setting health".
                $updateTx | Add-Member -NotePropertyName health -NotePropertyValue $null -Force
                $updateTx | Add-Member -NotePropertyName rollback_health -NotePropertyValue $null -Force
                $updateTx.state = 'COMPLETE'
                $updateTx.reason = 'installer-verified-control-plane-after-bootstrap-lock-release'
                $updateTx.updated_utc = [DateTime]::UtcNow.ToString('o')
                [IO.File]::WriteAllText(
                    $updateTxFile,
                    ($updateTx | ConvertTo-Json -Depth 12),
                    [Text.UTF8Encoding]::new($false)
                )
            }
        }
        catch {}
    }


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
    if ($existingInstallAtStart) {
        if ($UpdateMode) {
            Write-InstallUpdateStatus 92 'installing' 'Program files installed. Returning to YOMI for final verification...'
            Write-Host 'Update install stage complete; the YOMI updater will verify and restart the app.' -ForegroundColor Yellow
        }
        else {
            Write-Host 'Opening YOMI now...' -ForegroundColor Yellow
            Start-Process (Join-Path $installRoot 'app\YomiLauncher.exe') -ArgumentList 'controller'
        }
    }
    else {
        Write-Host 'Opening Settings for first-time setup...' -ForegroundColor Yellow
        Start-Process (Join-Path $installRoot 'app\YomiLauncher.exe') -ArgumentList 'settings'
    }
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
