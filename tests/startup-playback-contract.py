#!/usr/bin/env python3
"""Exercise the actual Lua startup callbacks with a mocked mpv transport.

This catches the prewarmed first-track state that kept clock and play glyph at
STARTING despite audible playback. It does NOT replace a live Windows rehearsal.
"""
from pathlib import Path
from lupa import LuaRuntime

source = (Path(__file__).resolve().parents[1] / "payload/app/music.lua").read_text(encoding="utf-8-sig")
start = source.index("local function confirm_playback_started(origin)")
end = source.index("local function defer_eof_play(", start)
actual_callbacks = source[start:end]
assert 'mp.observe_property("time-pos","number"' in actual_callbacks
assert 'safe_register_event("playback-restart"' in actual_callbacks

lua = LuaRuntime(unpack_returned_tuples=True)
lua.execute("""
mp={properties={pause=true},observers={},events={}}
function mp.get_property_native(name) return mp.properties[name] end
function mp.observe_property(name,kind,callback) mp.observers[name]=callback end
function safe_register_event(name,callback) mp.events[name]=callback end
function meta_for(i) return {title='Powaz of Weed',channel='Midnite - Topic'} end
function set_engine_status(phase,message,i) current_phase=phase;status_index=i end
function sync_playback_subset_cursor(i) end
function write_current(i) end
function write_queue_runtime() end
function write_runtime_lease() end
function append_all(p,text) history_count=history_count+1 end
function schedule_ahead(i) end
function write_startup_flight(...) end
function startup_elapsed_ms() return 0 end
function log(s) last_log=s end
utils={format_json=function(_)return '{}' end}
history_file='test';history_count=0;current_phase='paused'
playing_index=255;current_index=255;desired_index=255
loaded_waiting_for_restart=255;shutting_down=false
transient_skip_streak=0;startup_first_sound=true
""")
lua.execute("local playback_clock_pending_index=0\nlocal playback_clock_baseline=nil\n" + actual_callbacks)
g = lua.globals()

# Reproduce user's first Play after a prewarmed, paused file:
g.mp.properties["pause"] = False
g.mp.observers["pause"]("pause", False)
assert g.current_phase == "starting", "Repro precondition changed"
g.mp.observers["time-pos"]("time-pos", .04)
g.mp.observers["time-pos"]("time-pos", .12)
assert g.current_phase == "starting", "Premature confirmation before advancing 180ms"
g.mp.observers["time-pos"]("time-pos", .29)
assert g.current_phase == "playing", "Audible first track stuck in STARTING"
assert g.loaded_waiting_for_restart == 0
assert g.history_count == 1
print("PASS prewarmed Play advances to PLAYING after confirmed clock movement")

# Pause/resume cannot switch the already-verified track back to STARTING.
g.mp.properties["pause"] = True
g.mp.observers["pause"]("pause", True)
assert g.current_phase == "paused"
g.mp.properties["pause"] = False
g.mp.observers["pause"]("pause", False)
assert g.current_phase == "playing"
g.mp.observers["time-pos"]("time-pos", .55)
assert g.history_count == 1
print("PASS subsequent Pause/Play maintains transport and single history entry")

# A paused loaded track must NOT be marked playing by a seek or clock sample.
g.playing_index=259;g.current_index=259;g.desired_index=259
g.loaded_waiting_for_restart=259
g.mp.properties["pause"] = True
g.mp.observers["time-pos"]("time-pos", 42.0)
g.mp.observers["time-pos"]("time-pos", 42.5)
assert g.loaded_waiting_for_restart == 259
g.mp.properties["pause"] = False
g.mp.observers["pause"]("pause", False)
g.mp.observers["time-pos"]("time-pos", 42.5)
g.mp.observers["time-pos"]("time-pos", 42.9)
assert g.current_phase == "playing" and g.loaded_waiting_for_restart == 0
print("PASS paused bookmark does not create false PLAYING; later resume does")

# The original mpv playback-restart path still works when available.
g.playing_index=272;g.current_index=272;g.desired_index=272
g.loaded_waiting_for_restart=272
g.mp.events["playback-restart"]()
assert g.current_phase == "playing" and g.loaded_waiting_for_restart == 0
assert g.history_count == 3
print("PASS original playback-restart remains authoritative")
