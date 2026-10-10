#!/usr/bin/env python3
"""Apply the reviewed 9044 single-activity UI patch to the giant WPF source.

GitHub's content API is size-limited for a 1.8 MB file; this small, guarded
one-time transformer runs on the Windows QA branch, never directly on main.
"""
from pathlib import Path
import sys

p=Path("payload/app/YomiControllerWpf.cs")
s=p.read_text(encoding="utf-8-sig")
old=s

def fix(before,after):
    global s
    n=s.count(before)
    if n!=1:
        if n==0 and after in s:
            return  # allow rerun on an already patched branch
        raise RuntimeError(f"Expected one production anchor; found {n}: {before[:110]}")
    s=s.replace(before,after)

for a,b in [
('FillCombo(_settingsVisualizerActivity, "Subtle", "Normal", "Active");','FillCombo(_settingsVisualizerActivity, "Automatic");'),
('FillCombo(_settingsVisualizerFill, "Off", "Adaptive", "Aggressive");','FillCombo(_settingsVisualizerFill, "Off");'),
('FillCombo(_settingsVisualizerFrequencyScale, "Logarithmic", "Linear");','FillCombo(_settingsVisualizerFrequencyScale, "Logarithmic");'),
('SetComboPreserve(_settingsVisualizerActivity, GetString(c, "visualizer_activity", "Active"));','SetComboPreserve(_settingsVisualizerActivity, "Automatic");'),
('SetCombo(_settingsVisualizerFill, GetString(c, "visualizer_adaptive_fill", "Off"));','SetCombo(_settingsVisualizerFill, "Off");'),
('SetComboPreserve(_settingsVisualizerFrequencyScale, GetString(c, "visualizer_frequency_scale", "Logarithmic"));','SetComboPreserve(_settingsVisualizerFrequencyScale, "Logarithmic");'),
('SetCombo(_settingsVisualizerTrim, GetInt(c, "visualizer_high_frequency_trim", 0).ToString(CultureInfo.InvariantCulture));','SetCombo(_settingsVisualizerTrim, "0");'),
('SetCombo(_settingsVisualizerLift, GetInt(c, "visualizer_high_frequency_lift_db", 0).ToString(CultureInfo.InvariantCulture));','SetCombo(_settingsVisualizerLift, "0");'),
('c["visualizer_adaptive_fill"] = ComboText(_settingsVisualizerFill, "Off");','c["visualizer_adaptive_fill"] = "Off";'),
('c["visualizer_activity"] = ComboText(_settingsVisualizerActivity, "Active");','c["visualizer_activity"] = "Automatic";'),
('c["visualizer_frequency_scale"] = ComboText(_settingsVisualizerFrequencyScale, "Logarithmic");','c["visualizer_frequency_scale"] = "Logarithmic";'),
('c["visualizer_high_frequency_trim"] = ComboInt(_settingsVisualizerTrim, 0);','c["visualizer_high_frequency_trim"] = 0;'),
('c["visualizer_high_frequency_lift_db"] = ComboInt(_settingsVisualizerLift, 0);','c["visualizer_high_frequency_lift_db"] = 0;'),
('config["visualizer_adaptive_fill"] = ComboText(_settingsVisualizerFill, "Off");','config["visualizer_adaptive_fill"] = "Off";'),
('config["visualizer_activity"] = ComboText(_settingsVisualizerActivity, "Active");','config["visualizer_activity"] = "Automatic";'),
('config["visualizer_frequency_scale"] = ComboText(_settingsVisualizerFrequencyScale, "Logarithmic");','config["visualizer_frequency_scale"] = "Logarithmic";'),
('config["visualizer_high_frequency_trim"] = ComboInt(_settingsVisualizerTrim, 0);','config["visualizer_high_frequency_trim"] = 0;'),
('config["visualizer_high_frequency_lift_db"] = ComboInt(_settingsVisualizerLift, 0);','config["visualizer_high_frequency_lift_db"] = 0;'),
('                addTextChoices("Activity", "visualizer_activity", new[] { "Subtle", "Normal", "Active" });\n',''),
('                addTextChoices("Fill quiet ranges", "visualizer_adaptive_fill", new[] { "Off", "Adaptive", "Aggressive" });\n',''),
('                addTextChoices("Frequency scale", "visualizer_frequency_scale", new[] { "Logarithmic", "Linear" });\n',''),
]:
    fix(a,b)

if old!=s:
    p.write_text(s,encoding="utf-8",newline="")
    print(f"PASS updated {p}: {len(old)} -> {len(s)} bytes")
else:
    print("PASS controller already uses Automatic only")

# If any route can still present legacy quick settings, the patch is incomplete.
for forbidden in [
    'addTextChoices("Activity", "visualizer_activity"',
    'addTextChoices("Frequency scale", "visualizer_frequency_scale"',
    'addTextChoices("Fill quiet ranges", "visualizer_adaptive_fill"',
]:
    assert forbidden not in s,forbidden
assert 'c["visualizer_activity"] = "Automatic";' in s
assert 'config["visualizer_activity"] = "Automatic";' in s
