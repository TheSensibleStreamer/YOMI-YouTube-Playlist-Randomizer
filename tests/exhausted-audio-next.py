#!/usr/bin/env python3
"""Regression for the actual YOMI Next/Previous exhausted-audio state machine.

An extractor may report Video unavailable for a publicly playable song, so
a bounded audio extraction failure is never a permanent playlist deletion.
"""
from pathlib import Path
from lupa.lua51 import LuaRuntime

s=Path("payload/app/music.lua").read_text(encoding="utf-8-sig")
lua=LuaRuntime(unpack_returned_tuples=True)
lua.execute('assert(loadstring(...))',s)
def function(name,local=False):
    marker=("local function " if local else "function ")+name+"("
    i=s.index(marker)
    j=s.index("\nend\n",i)
    result=s[i:j+5]
    if local:result=result.replace("local function "+name+"(","function "+name+"(",1)
    return result
lua.execute(r'''
urls={'youtube-A','youtube-B','youtube-C','youtube-D'}
clock=1000
fs={}
ready={}
warnings={}
function status_path(i,suffix) return 'status-'..i..'-'..suffix end
function exists(p) return fs[p]~=nil end
function read_all(p) return fs[p] end
function source_identity(s) return s end
function ensure_position_binding(i) end
function audio_ready(i) return ready[i]==true end
function log(s) table.insert(warnings,s) end
os={time=function() return clock end,remove=function(p) fs[p]=nil end}
repeat_mode="off";order={1,2,3,4};order_position={[1]=1,[2]=2,[3]=3,[4]=4}
playback_subset_active=false;playback_subset={};playback_subset_position={}
stream_route_failures={js=0,['no-js']=0};stream_route_degraded_until={js=0,['no-js']=0}
music_endpoint_checked={}
function youtube_id(url) return url end
function music_endpoint_valid_id(v) return false end
function music_endpoint_override(i) return false end
''')
for name,local in [
    ("audio_failure_cooldown",False),
    ("known_bad",False),
    ("audio_prefetch_backoff",False),
    ("next_occurrence",True),
    ("explicit_audio_retry",False),
]:
    lua.execute(function(name,local))
L=lua.globals()
def assert_true(x,msg): assert bool(x),msg
def assert_false(x,msg): assert not bool(x),msg
def m(i,t="1000",key=None):
    if key is None:key=f"youtube-{chr(64+i)}"
    L.fs[f"status-{i}-audio.failed"]=f"extractor-failed-v2|{key}|{t}"
m(2);m(3)
assert_true(L.known_bad(2),"exhausted source must not be reattempted on Next")
assert_true(L.audio_prefetch_backoff(2),"prefetch and Next share the same cooldown")
assert L.next_occurrence(1,1,False)==4,"skip two exhausted sources to next potential song"
assert L.next_occurrence(1,1,True)==2,"explicit include should still see source"
assert L.next_occurrence(4,-1,False)==1,"Previous skips exhausted sources too"
assert L.next_occurrence(1,1,False)==4,"main queue order remains untouched"
assert len(list(L.order.values()))==4,"never remove or reorder actual playlist"
L.ready[2]=True
assert_false(L.known_bad(2),"downloaded playable audio outranks stale failed marker")
assert L.next_occurrence(1,1,False)==2,"cached audio plays normally"
L.ready[2]=False
L.explicit_audio_retry(2,"jump")
assert_false(L.known_bad(2),"Listen/Play now must immediately rearm manual retry")
assert L.next_occurrence(1,1,False)==2
m(2,t="1")
assert_false(L.known_bad(2),"15-minute expiry allows automatic retry")
L.clock=1000
m(2,t="1050")
assert_false(L.known_bad(2),"future clock skew cannot permanently suppress a source")
m(2,key="different-identity")
assert_false(L.known_bad(2),"changed URL/track ID invalidates stale marker")
assert L.fs["status-2-audio.failed"] is None,"stale marker was cleared"
L.fs["status-2-video.failed"]="failed"
assert_false(L.known_bad(2),"optional video failure must NEVER block music")
assert L.next_occurrence(1,1,False)==2
# The real transport source must guard against a late async exhaustion and
# stop the old mpv clock before presenting an uncached new track as playing.
a=s[s.index("local function commit_pending_transport("):s.index("local function advance(")]
assert "if known_bad(n) then" in a and "next_occurrence(n,1,false)" in a
assert 'explicit_audio_retry(n,"transport")' not in a
assert 'mp.commandv("stop")' in a and 'playing_index=0' in a
assert "audio_ready(n)" in a
print("PASS: actual music.lua parses under Lua 5.1")
print("PASS: Next/Previous skips only fresh six-route audio exhaustion; manually requested songs can retry")
print("PASS: cache overrides marker, changed identity invalidates marker, temporary cooldown expires")
print("PASS: optional video never excludes audio; source queue untouched")
print("PASS: transport stops old mpv audio/clock when destination is uncached")
