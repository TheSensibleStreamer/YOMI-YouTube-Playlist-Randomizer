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

# Audio output volume and measured track gain are independent. A saved
# percentage must never masquerade as a completed loudness measurement.
assert 'apply_measured_loudness(current_index)' in music
assert 'write_all(gain_path(i),"0")' not in music
assert 'function gain_job(job)' in music
assert 'ebur128=peak=true:framelog=quiet' in music

def production_function(name):
    start = music.index("function " + name + "(")
    end = music.index("\nend\n", start) + len("\nend")
    return music[start:end]

# Exercise the real static R128/peak gain function with the mpv-compatible Lua.
lua.execute(production_function("calculate_loudness_gain"))
assert abs(L.calculate_loudness_gain(-19.0,-8.0)-3.0)<0.001
assert abs(L.calculate_loudness_gain(-8.0,-0.1)-(-8.0))<0.001
assert abs(L.calculate_loudness_gain(-35.0,-1.0)-(-1.0))<0.001, "True peak must cap amplification"
assert L.calculate_loudness_gain(None,-1.0) is None, "A failed scan must not invent a gain"

lua.execute("""
    cfg={loudness_normalization=true}
    ffmpeg_available=true
    function audio_object_signature() return "sig1" end
    function audio_path(i) return "audio"..i end
    function gain_path(i) return "gain"..i end
    function gain_object_path(i) return "gainobj"..i end
    function fsize(path) return sizes[path] or 0 end
    function read_all(path) return data[path] end
    function hydrate_position(src,dst) data[dst]=data[src];return true end
    sizes={audio1=1234}
    data={gain1="0",gainobj1="r128-v1|sig1|1234|3.50"}
    os={remove=function(p) data[p]=nil end}
""")
lua.execute(production_function("measured_loudness_gain"))
lua.execute(production_function("apply_measured_loudness"))
assert abs(L.measured_loudness_gain(1)-3.5)<0.001, "Legacy zero placeholder must be replaced"
assert abs(L.apply_measured_loudness(1)-3.5)<0.001
assert abs(L.applied["volume-gain"]-3.5)<0.001
L.cfg.loudness_normalization=False
assert L.apply_measured_loudness(1)==0, "Turning leveling OFF must restore unmodified volume"
assert L.applied["volume-gain"]==0
L.cfg.loudness_normalization=True
L.sizes["audio1"]=4000
assert L.measured_loudness_gain(1) is None, "A replaced audio file must invalidate its gain"

# A filtered Listen queue must follow the edited authoritative queue, not its
# old membership CSV, even when the controller reasserts the filter after Next.
lane=LuaRuntime(unpack_returned_tuples=True)
lane.execute("""
  urls={"a","b","c","d","e"};order={1,2,3,4,5};playback_subset={1,2,3,5}
  playback_subset_active=true;playback_subset_cursor=1
  playback_subset_position={};transport_pending_target=0;desired_index=1;playing_index=1;current_index=1
  function rebuild_playback_subset_position()
      playback_subset_position={}
      for slot,occ in ipairs(playback_subset) do playback_subset_position[occ]=slot end
      if playback_subset_cursor>#playback_subset then playback_subset_cursor=#playback_subset end
  end
  function log(s) end
""")
lane.execute(production_function("reconcile_playback_subset_order"))
lane.execute(production_function("ensure_playback_subset"))
lane.execute("""
  order={1,3,5,2,4}
  reconcile_playback_subset_order()
  assert(table.concat(playback_subset,",")=="1,3,5,2")
  assert(playback_subset_cursor==1)
  assert(ensure_playback_subset("1,2,3,5","filtered"))
  assert(table.concat(playback_subset,",")=="1,3,5,2")
  -- Undo and redo also recalculate the filtered playback lane.
  order={1,2,3,4,5};reconcile_playback_subset_order()
  assert(table.concat(playback_subset,",")=="1,2,3,5")
  order={1,3,5,2,4};reconcile_playback_subset_order()
  assert(table.concat(playback_subset,",")=="1,3,5,2")
  desired_index=3;reconcile_playback_subset_order()
  assert(playback_subset_cursor==2)
""")
# YOMI 9042: visualizer loudness normalization replaces hard-coded gain,
# strictly within the separately prepared visualizer audio path.
analysis=music.split("function visualizer_audio_prefix()",1)[1].split("\nend",1)[0]
assert "dynaudnorm=f=700:g=15:p=" in analysis
assert "volume=" not in analysis, "Visualizer analysis must not introduce fixed gain clipping"
assert "mp.set_property_number" not in analysis and "mp.set_property_native" not in analysis

print("PASS: first launch and factory reset start at 50%")
print("PASS: saved 0/27/50/100% and saved mute remain untouched")
print("PASS: cached EBU R128 and true-peak leveling, OFF resets gain; visualizer remains separate")
print("PASS: filtered Listen order follows Queue moves and undo/redo without stale reassertion")
