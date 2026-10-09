$ErrorActionPreference = 'Stop'
$source = [IO.File]::ReadAllText((Resolve-Path 'payload/app/YomiControllerWpf.cs').Path)
function Extract-ProductionMethod([string] $signature) {
    $index = $source.IndexOf($signature, [StringComparison]::Ordinal)
    if ($index -lt 0 -or $source.IndexOf($signature, $index + 1, [StringComparison]::Ordinal) -ge 0) {
        throw "Production method missing or ambiguous: $signature"
    }
    $open = $source.IndexOf('{', $index)
    if ($open -lt 0) { throw "Method body not found: $signature" }
    $depth = 0
    for ($i = $open; $i -lt $source.Length; $i++) {
        if ($source[$i] -eq '{') { $depth++ }
        if ($source[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) {
                return $source.Substring($index, $i - $index + 1).Replace('private static ', 'public static ')
            }
        }
    }
    throw "Unbalanced production method: $signature"
}
$audio = Extract-ProductionMethod 'private static string QueueAudioStatus('
$media = Extract-ProductionMethod 'private static string QueueOptionalMediaSummary('
$issues = Extract-ProductionMethod 'private static bool QueueHasOptionalMediaIssue('

# Compile and CALL the actual three extracted C# methods, not Python copies or
# a reimplementation of their expected behavior.
$harness = @"
using System;
using System.Globalization;
public class QueueTruth
{
    public class RuntimeQueueItem
    {
        public string Audio = "", Artwork = "", Video = "", Visualizer = "";
        public bool TransitionReady;
    }
$audio
$media
$issues
}
"@
Add-Type -TypeDefinition $harness -Language CSharp -ErrorAction Stop
function Assert-Equal([object]$actual,[object]$expected,[string]$name) {
    if (-not [Object]::Equals($actual, $expected)) {
        throw "$name expected '$expected' but got '$actual'"
    }
    Write-Host "PASS $name"
}
$ready = New-Object 'QueueTruth+RuntimeQueueItem'
$ready.Audio = "READY"
$ready.TransitionReady = $true
$ready.Artwork = "READY"
$ready.Video = "READY"
$ready.Visualizer = "NOT_REQUIRED"
Assert-Equal ([QueueTruth]::QueueAudioStatus($ready)) "READY" "Previous track with cached audio stays READY"
Assert-Equal ([QueueTruth]::QueueOptionalMediaSummary($ready)) "2/2 ready" "Media reports only optional assets"
Assert-Equal ([QueueTruth]::QueueHasOptionalMediaIssue($ready)) $false "Fully ready visuals are not Issues"
$ready.Video = "ACTIVE"
Assert-Equal ([QueueTruth]::QueueAudioStatus($ready)) "READY" "Optional video work cannot demote ready audio"
Assert-Equal ([QueueTruth]::QueueOptionalMediaSummary($ready)) "1/2 ready" "Media reports partial readiness at all distances"
Assert-Equal ([QueueTruth]::QueueHasOptionalMediaIssue($ready)) $true "Issues still catches pending video"
$ready.Artwork = "NOT_REQUIRED"
$ready.Video = "FAILED_OPTIONAL"
Assert-Equal ([QueueTruth]::QueueOptionalMediaSummary($ready)) "0/1 ready" "Optional media failure reported without hiding playable audio"
Assert-Equal ([QueueTruth]::QueueHasOptionalMediaIssue($ready)) $true "Issues includes optional failures"
$ready.Video = "NOT_REQUIRED"
Assert-Equal ([QueueTruth]::QueueOptionalMediaSummary($ready)) "Off" "No requested presentation assets"
Assert-Equal ([QueueTruth]::QueueOptionalMediaSummary($null)) ([char]0x2014).ToString() "Missing snapshot is unknown, not failed"
$ready.Audio = "ACTIVE"
$ready.TransitionReady = $false
Assert-Equal ([QueueTruth]::QueueAudioStatus($ready)) "BUILDING" "Audio actively preparing"
$ready.Audio = "WAITING"
Assert-Equal ([QueueTruth]::QueueAudioStatus($ready)) "WAITING" "Not-yet-prepared audio"
$ready.Audio = "UNAVAILABLE"
Assert-Equal ([QueueTruth]::QueueAudioStatus($ready)) "RETRY" "Retryable extractor cooldown, not permanent deletion"
$ready.Audio = "FAILED_PERMANENT"
Assert-Equal ([QueueTruth]::QueueAudioStatus($ready)) "FAILED" "Legacy terminal failure distinguished"

if ($source.Contains('row.Status = "PLAYED"')) { throw 'PLAYED still replaces audio cache state' }
if ($source.Contains('row.ReadinessSummary = "PLAYBACK HISTORY"')) { throw 'Previous media state still blanked' }
if ($source.Contains('delta <= 3 && runtime.TransitionReady')) { throw 'Media still artificially stops after next three tracks' }
if (-not $source.Contains('return _currentSlot <= 0 || row.Slot >= _currentSlot;')) { throw 'Upcoming filter still depends on readiness labels' }
if (-not $source.Contains('return _currentSlot > 0 && row.Slot < _currentSlot;')) { throw 'History still depends on readiness labels' }

$xaml = [IO.File]::ReadAllText((Resolve-Path 'payload/app/YomiControllerWpf.xaml').Path)
foreach ($geometry in @(
    'QueueTableHeader" Height="30"',
    'QueueTableStateColumn" Width="100" MinWidth="72" MaxWidth="240"',
    'QueueTableMediaColumn" Width="94" MinWidth="72" MaxWidth="260"'
)) {
    if (-not $xaml.Contains($geometry)) { throw "Queue geometry changed unexpectedly: $geometry" }
}
Write-Host "PASS Previous/Upcoming scoping and all-distance media mapping"
Write-Host "PASS State/Media table geometry unchanged"
