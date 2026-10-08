param(
    [string]$DataRoot = (Join-Path $env:LOCALAPPDATA 'YOMI')
)
# YOMI optional font pack: local application data only, never system-wide font installation.
# All faces come unmodified from the Google Fonts repository under the SIL Open Font License.
$ErrorActionPreference = 'Stop'
$fontsRoot = Join-Path $DataRoot 'fonts'
if (-not (Test-Path -LiteralPath $fontsRoot)) { New-Item -ItemType Directory -Path $fontsRoot -Force | Out-Null }

$fonts = @(
    @{ Name='Press Start 2P'; Directory='pressstart2p'; Source='PressStart2P-Regular.ttf'; File='PressStart2P-Regular.ttf'; GitBlob='39adf42efa597906e53be689474ac82214112124' }
    @{ Name='Pixelify Sans'; Directory='pixelifysans'; Source='PixelifySans[wght].ttf'; File='PixelifySans-Variable.ttf'; GitBlob='2d7eb388ee499fbcf3e992723ec409c095640104' }
    @{ Name='Audiowide'; Directory='audiowide'; Source='Audiowide-Regular.ttf'; File='Audiowide-Regular.ttf'; GitBlob='8b50bedc0f99bcfcb5686a3dbeb5b51d1c0190d5' }
    @{ Name='Righteous'; Directory='righteous'; Source='Righteous-Regular.ttf'; File='Righteous-Regular.ttf'; GitBlob='07fc0b45132b1d7039c202e6d09c91b1a137ec26' }
    @{ Name='Black Ops One'; Directory='blackopsone'; Source='BlackOpsOne-Regular.ttf'; File='BlackOpsOne-Regular.ttf'; GitBlob='672d8e285231ab0ff58fac66ec21ef3f5a005a48' }
    @{ Name='Teko'; Directory='teko'; Source='Teko[wght].ttf'; File='Teko-Variable.ttf'; GitBlob='1a9168e40358ab6ce3886270f8b0f091d13a6974' }
    @{ Name='Barlow Condensed'; Directory='barlowcondensed'; Source='BarlowCondensed-Regular.ttf'; File='BarlowCondensed-Regular.ttf'; GitBlob='ecbe0cc84118decc6a16216ad53d83d39888b175' }
    @{ Name='IBM Plex Sans Condensed'; Directory='ibmplexsanscondensed'; Source='IBMPlexSansCondensed-Regular.ttf'; File='IBMPlexSansCondensed-Regular.ttf'; GitBlob='0aa292f2954da6755b61ca9c67234e1b1e9c41d3' }
    @{ Name='UnifrakturCook'; Directory='unifrakturcook'; Source='UnifrakturCook-Bold.ttf'; File='UnifrakturCook-Bold.ttf'; GitBlob='cf9a8f9d6f089a380e917fa7f557e6075b3f260c' }
    @{ Name='Great Vibes'; Directory='greatvibes'; Source='GreatVibes-Regular.ttf'; File='GreatVibes-Regular.ttf'; GitBlob='a1327ff3b862246f1f253639040edcc473e5f3a9' }
)
$base = 'https://raw.githubusercontent.com/google/fonts/main/ofl/'
try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch {}
$added = 0
$failed = 0
function Test-YomiGitBlobSha([string]$File, [string]$Expected)
{
    $bytes = [IO.File]::ReadAllBytes($File)
    $head = [Text.Encoding]::ASCII.GetBytes('blob ' + $bytes.Length.ToString([Globalization.CultureInfo]::InvariantCulture) + [char]0)
    $both = New-Object byte[] ($head.Length + $bytes.Length)
    [Buffer]::BlockCopy($head, 0, $both, 0, $head.Length)
    [Buffer]::BlockCopy($bytes, 0, $both, $head.Length, $bytes.Length)
    $sha = [Security.Cryptography.SHA1]::Create()
    try { $digest = [BitConverter]::ToString($sha.ComputeHash($both)).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
    return ($digest -eq $Expected)
}
try {
    foreach ($font in $fonts) {
        $destination = Join-Path $fontsRoot $font.File
        if ((Test-Path -LiteralPath $destination -PathType Leaf) -and (Test-YomiGitBlobSha $destination $font.GitBlob)) {
            continue
        }
        $download = $base + $font.Directory + '/' + [uri]::EscapeDataString($font.Source)
        $temp = $destination + '.download'
        try {
            if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Force }
            Invoke-WebRequest -Uri $download -OutFile $temp -UseBasicParsing -TimeoutSec 12 | Out-Null
            if (-not (Test-YomiGitBlobSha $temp $font.GitBlob)) { throw 'Font download failed integrity check.' }
            Move-Item -LiteralPath $temp -Destination $destination -Force
            $added++
        }
        catch {
            $failed++
            if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue }
            Write-Warning ('Could not prepare '+ $font.Name +': '+ $_.Exception.Message)
        }
        # Keep the original license notice with each downloaded typeface.
        $license = Join-Path $fontsRoot ('OFL-' + $font.Directory + '.txt')
        if ((Test-Path -LiteralPath $destination -PathType Leaf) -and -not (Test-Path -LiteralPath $license)) {
            try { Invoke-WebRequest -Uri ($base + $font.Directory + '/OFL.txt') -OutFile $license -UseBasicParsing -TimeoutSec 12 | Out-Null }
            catch { Write-Warning ('Could not get the '+$font.Name+' font license.'); }
        }
    }
}
finally { }
Write-Output ('YOMI font pack: '+$added+' added, '+$failed+' unavailable')
