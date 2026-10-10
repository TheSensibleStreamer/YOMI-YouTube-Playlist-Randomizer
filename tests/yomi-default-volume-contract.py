#!/usr/bin/env python3
"""Verify YOMI's new-install/factory-reset volume contract without touching saved values."""
from pathlib import Path
from lupa.lua51 import LuaRuntime

music = Path("payload/app/music.lua").read_text(encoding="utf-8-sig")
controller = Path("payload/app/YomiControllerWpf.cs").read_text(encoding="utf-8-sig")
begin = music.index("function apply_startup_volume()")
end = music.index("\nend\n", begin) + len("\nend")
func = music[begin:end]
lua = LuaRuntime(unpack_returned_tuples=True)
lua.execute('''
    stored={}
    applied={}
    controller_ui_file='controller-ui.json'
    function load_json(path) return stored end
    function log(s) end
    mp={
        set_property_number=function(name,value) applied[name]=value end,
        set_property_native=function(name,value) applied[name]=value end
    }
''')
lua.execute(func)
L=lua.globals()

def sample(settings):
    L.stored=lua.table_from(settings)
    L.applied=lua.table_from({})
    L.apply_startup_volume()
    return L.applied

assert sample({})["volume"] == 50, "brand-new YOMI must start at 50%"
assert sample({"muted": False})["volume"] == 50, "mute-only preferences preserve 50% default"
assert sample({"volume":100})["volume"] == 100, "existing 100% preference must not be overwritten"
assert sample({"volume":27, "muted":True})["volume"] == 27, "existing custom volume must be preserved"
assert sample({"volume":27, "muted":True})["mute"] is True, "saved mute must be preserved"
assert sample({"volume":0})["volume"] == 0, "saved zero/mute-like volume must not be changed"
assert sample({"volume":50})["volume"] == 50, "saved default preserved"
assert controller.count('private double _volume = 50.0;') == 1
assert '_volume = 50.0; _muted = false; _volumePreferenceLoaded = true; ApplyVolumeUi();' in controller
assert 'SendMpv("set_property", "volume", "50");' in controller
assert 'GetDouble(map, "volume", 50.0)' in controller
assert 'GetDouble(map, "volume", 100.0)' not in controller
assert 'SendMpv("set_property", "volume", "100");' not in controller

# 100 is a normal mpv percentage; keep separate audio gain policy unchanged.
assert 'mp.set_property_number("volume-gain",gain)' in music
assert 'write_all(gain_path(i),"0")' in music
# YOMI 9042: visualizer loudness normalization replaces hard-coded gain,
# strictly within the separately prepared visualizer audio path.
analysis=music.split("function visualizer_audio_prefix()",1)[1].split("\nend",1)[0]
assert "dynaudnorm=f=700:g=15:p=" in analysis
assert "volume=" not in analysis, "Visualizer analysis must not introduce fixed gain clipping"
assert "mp.set_property_number" not in analysis and "mp.set_property_native" not in analysis

print("PASS: first launch and factory reset start at 50%")
print("PASS: saved 0/27/50/100% and saved mute remain untouched")
print("PASS: default track gain stays zero; visualizer gain affects analysis only")
