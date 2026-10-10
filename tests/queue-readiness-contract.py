#!/usr/bin/env python3
"""Guard the real Queue projection's separation of playback readiness and media.
This is a source contract because compiling the whole WPF UI on Linux is not
available; the Windows controller smoke gate remains separately required.
"""
from pathlib import Path

source = Path("payload/app/YomiControllerWpf.cs").read_text(encoding="utf-8-sig")

def section(start, end):
    a = source.index(start)
    b = source.index(end, a + len(start))
    return source[a:b]

past = section("else if (delta < 0 && currentIndex >= 0)", "else if (runtime != null)")
assert 'row.Status = "PLAYED"' not in past, "Listening history is not a playback state"
assert "if (runtime != null)" in past, "Previous song must respect the runtime"
assert "ApplyRuntimeQueueStatus(row, runtime)" in past, "Previous READY is lost"

observer = section("private void ApplyQueueObservatoryState(", "private void ApplyQueueConfidence(")
assert 'row.ReadinessSummary = "PLAYBACK HISTORY"' not in observer
assert 'row.MediaSummary = "Audio ready"' not in observer
assert 'row.MediaSummary = "Ready"' not in observer
assert 'runtime.TransitionReady && !runtime.PresentationComplete' in observer
assert 'pending.Add("artwork")' in observer
assert 'pending.Add("video")' in observer
assert 'pending.Add("visualizer")' in observer
assert 'row.MediaSummary = "";' in observer

status = section("private void ApplyRuntimeQueueStatus(", "private string BuildQueueDetail(")
assert 'else if (runtime.TransitionReady)' in status
assert 'row.Status = "READY"' in status
assert 'row.Status = "AUDIO READY"' not in status
assert 'row.Status = "PLAYED"' not in status
print("PASS: previous song readiness remains authoritative; status and media cannot duplicate audio READY")
