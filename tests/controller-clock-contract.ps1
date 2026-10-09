$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = [IO.File]::ReadAllText((Join-Path $repoRoot 'payload\app\YomiControllerWpf.cs'))
$start = $source.IndexOf('        private bool ConfirmPlaybackFromSampledMpvClock(',[StringComparison]::Ordinal)
$end = $source.IndexOf('        private void RefreshPlaybackState()', $start, [StringComparison]::Ordinal)
if($start -lt 0 -or $end -le $start){ throw 'Production controller clock method was not found' }
$method = $source.Substring($start, $end-$start)
$csharp = @'
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
public sealed class ClockContractTest
{
    private bool _running = true;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly object _playbackSnapshotSync = new object();
    private long _playbackSnapshotStampMs = -10000;
    private double _playbackSnapshotPosition = -1;
    private int _clockEvidenceOccurrence;
    private long _clockEvidenceSampleMs = -10000;
    private double _clockEvidenceStart = -1;
    private double _clockEvidenceLast = -1;
    private int _clockConfirmedOccurrence;
    private void WriteControllerPerformanceLogAsync(string detail) { }
    PRODUCTION_METHOD
    public bool Probe(int occurrence, double raw, bool paused, bool idle, bool have = true)
    {
        Thread.Sleep(23);
        lock (_playbackSnapshotSync)
        {
            _playbackSnapshotPosition = raw;
            _playbackSnapshotStampMs = _clock.ElapsedMilliseconds;
        }
        return ConfirmPlaybackFromSampledMpvClock(occurrence,have,idle,paused);
    }
    public void EngineStopped() { _running = false; Probe(255,42,false,false); _running=true; }
}
'@
$csharp = $csharp.Replace('PRODUCTION_METHOD', $method)
Add-Type -TypeDefinition $csharp -Language CSharp -ErrorAction Stop
$t = New-Object ClockContractTest
if($t.Probe(255,0.04,$false,$false)){throw 'False positive at first loaded sample'}
if($t.Probe(255,0.13,$false,$false)){throw 'False positive before confirmed advancing clock'}
if(-not $t.Probe(255,0.29,$false,$false)){throw 'Audible playback remains stuck in STARTING'}
if($t.Probe(255,0.29,$true,$false)){throw 'Paused clock reported playing'}
if(-not $t.Probe(255,0.32,$false,$false)){throw 'Resume failed to restore correct transport mode'}
if($t.Probe(256,4.00,$false,$false)){throw 'Previous track proof leaked into next occurrence'}
if(-not $t.Probe(256,4.27,$false,$false)){throw 'Next audible occurrence failed advancing clock proof'}
if($t.Probe(256,5.0,$false,$true)){throw 'Idle engine was reported playing'}
$t.EngineStopped()
if($t.Probe(255,0.01,$false,$false)){throw 'Old proof reused after engine restart on same track'}
if(-not $t.Probe(255,0.27,$false,$false)){throw 'Fresh engine does not recover after new advancing samples'}
Write-Host 'PASS production controller clock method: stale STARTING, pause/resume, occurrence switch, idle, engine restart'
