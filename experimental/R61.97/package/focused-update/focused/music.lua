-- R61.79 MEDIA COMPATIBILITY + FAILURE CONTAINMENT: use modern FFmpeg frame-sync semantics,
-- back off deterministic optional-media failures, and preserve R61.78 callback/volume/engine resilience.
-- run optional FFmpeg work directly with diagnostics, source-pass usable art/video when normalization fails,
-- and keep transport/media authority alive across recoverable callback faults.
-- R61.94 calm presentation: visualizer media is binary two-color at generation time and carries a new profile identity.
-- R61.75 runtime restoration: durable transport intent, live media demand, independent artwork/video preparation,
-- repeat acknowledgement support, and visualizer profile/cadence correctness over the sealed playback spine.
-- YOMI 4.2.0.8 R61.48 GEOMETRY CONTRACT; playback remains the sealed R61.45 resolver-health spine
-- Presentation-only iteration: resolver/cache/playback control flow is intentionally unchanged.
-- Fast-start extraction is now bounded and self-recovering: a wedged yt-dlp child cannot hold the entire player in PREPARING.
-- Startup now carries an explicit flight record from script load through first audible playback.
-- An alive mpv process is not considered a successful start until media actually reaches playback.
-- Cache misses resolve and begin a direct audio stream first; the durable full-track cache
-- is built behind active playback. Cached tracks remain local-first.
-- Playlist lines are no longer treated as permanent media identity. Cache reuse is
-- source-bound, and edited/reordered playlists preserve surviving queue intent by
-- stable source identity where possible. Audio remains authoritative.
if os.getenv("YOMI_SCRIPT_PROBE_ONLY") == "1" then return end

local mp = require 'mp'
local utils = require 'mp.utils'

local install_root = os.getenv("YOMI_INSTALL_ROOT")
if not install_root or install_root == "" then
    local program_files = os.getenv("ProgramFiles") or "C:\\Program Files"
    install_root = program_files .. "\\YOMI"
end
local localapp = os.getenv("LOCALAPPDATA") or "."
local data_root = localapp .. "\\YOMI"
local state_root = data_root .. "\\state"
local cache_root = data_root .. "\\cache"
local config_file = data_root .. "\\config.json"
controller_ui_file = state_root .. "\\controller-ui.json"
local playlist_file = data_root .. "\\playlist.txt"
local pool_file = data_root .. "\\pool-current.json"
local pool_reset_file = state_root .. "\\pool-reset.pending"
local resume_file = state_root .. "\\resume-track.txt"
local current_file = state_root .. "\\current.json"
local engine_status_file = state_root .. "\\engine-status.json"
local runtime_lease_file = state_root .. "\\runtime-lease.json"
local startup_flight_file = state_root .. "\\startup-flight.json"
local session_file = state_root .. "\\session.json"
local order_file = state_root .. "\\session-order.json"
local queue_file = state_root .. "\\queue-runtime.json"
local repeat_file = state_root .. "\\repeat-mode.txt"
local history_file = state_root .. "\\history.jsonl"
active_slot_file = data_root .. "\\active-slot.json"
cache_slot_context_file = state_root .. "\\cache-slot-context.txt"
object_root = cache_root .. "\\objects"
object_audio_dir = object_root .. "\\audio"
object_meta_dir = object_root .. "\\meta"
object_gain_dir = object_root .. "\\gain"
local audio_dir = cache_root .. "\\audio"
local artwork_dir = cache_root .. "\\artwork"
local video_dir = cache_root .. "\\video"
local visualizer_dir = cache_root .. "\\visualizer"
local meta_dir = cache_root .. "\\meta"
local gain_dir = cache_root .. "\\gain"
local status_dir = cache_root .. "\\status"
local ytdlp = install_root .. "\\runtime\\yt-dlp\\yt-dlp.exe"
local ffmpeg = install_root .. "\\runtime\\ffmpeg\\ffmpeg.exe"
local ffmpeg_dir = install_root .. "\\runtime\\ffmpeg"
local deno = install_root .. "\\runtime\\deno\\deno.exe"
local runner = install_root .. "\\app\\PriorityRun.exe"
local runtime_id = os.getenv("YOMI_RUNTIME_ID") or "focused-r19"

local function read_all(path)
    local f = io.open(path,"rb")
    if not f then return nil end
    local s = f:read("*a")
    f:close()
    return s
end

local function write_all(path,text)
    local temp = path .. ".tmp"
    local f = io.open(temp,"wb")
    if not f then return false end
    f:write(text or "")
    f:close()
    os.remove(path)
    return os.rename(temp,path) ~= nil
end

local function append_all(path,text)
    local f = io.open(path,"ab")
    if not f then return false end
    f:write(text or "")
    f:close()
    return true
end

local function exists(path)
    local f = io.open(path,"rb")
    if f then f:close(); return true end
    return false
end

local function fsize(path)
    local f = io.open(path,"rb")
    if not f then return 0 end
    local n = f:seek("end") or 0
    f:close()
    return n
end

local function load_json(path)
    local s = read_all(path)
    if not s then return nil end
    if s:sub(1,3) == string.char(239,187,191) then s=s:sub(4) end
    local ok,value = pcall(utils.parse_json,s)
    if ok and type(value)=="table" then return value end
    return nil
end

local function write_json(path,obj)
    local ok,text = pcall(utils.format_json,obj)
    if not ok then return false end
    return write_all(path,text)
end

local function log(text)
    mp.msg.info("YOMI R61.95 " .. tostring(text or ""))
end

-- R61.78: mpv destroys a Lua client when an uncaught callback throws. Keep one bad timer,
-- transport handler, event or subprocess completion from amputating the whole YOMI engine.
function yomi_callback_error(err)
    local text=tostring(err or "unknown callback error")
    if debug and debug.traceback then
        local ok,trace=pcall(debug.traceback,text,2)
        if ok and trace then text=tostring(trace) end
    end
    return text
end
function yomi_safe_invoke(label,fn,...)
    if type(fn)~="function" then return false end
    local args={...}
    local ok,result=xpcall(function() return fn(unpack(args)) end,yomi_callback_error)
    if not ok then log("CALLBACK RECOVERED "..tostring(label or "callback").." | "..tostring(result or "error")) end
    return ok,result
end
function safe_timeout(seconds,fn)
    return mp.add_timeout(seconds,function() yomi_safe_invoke("timeout",fn) end)
end
function safe_periodic_timer(seconds,fn)
    return mp.add_periodic_timer(seconds,function() yomi_safe_invoke("periodic",fn) end)
end
function safe_register_event(name,fn)
    mp.register_event(name,function(...) yomi_safe_invoke("event:"..tostring(name),fn,...) end)
end
function safe_register_script_message(name,fn)
    mp.register_script_message(name,function(...) yomi_safe_invoke("message:"..tostring(name),fn,...) end)
end

local active_slot = load_json(active_slot_file) or {}
local active_slot_id = tostring(active_slot.id or "")
local active_slot_name = tostring(active_slot.name or "Main")
local slot_bookmark_occurrence = math.max(0,math.floor(tonumber(active_slot.bookmark_occurrence) or 0))
local slot_bookmark_seconds = math.max(0,tonumber(active_slot.bookmark_seconds) or 0)
local explicit_play_start = os.getenv("YOMI_EXPLICIT_PLAY") == "1"
local prewarm_start = os.getenv("YOMI_PREWARM") == "1"
-- R61.94: prewarm is stronger than a saved play state. It may restore/load/prefetch the
-- last session, but it is never permitted to emit sound until the controller releases pause.
local slot_restore_paused = prewarm_start or (active_slot.paused == true and not explicit_play_start)
local slot_bookmark_pending = slot_bookmark_seconds > 0.5

-- R61.45 retains the R61.44 startup flight recorder. This is intentionally tiny and overwrite-only: it is
-- diagnostic state, not an ever-growing log. It lets the controller distinguish
-- "mpv exists" from the milestones that actually matter to first sound.
local startup_started_at = mp.get_time()
local startup_stage_seq = 0
local startup_first_sound = false
local startup_deadline_reported = false
local startup_deadline_seconds = 18
local startup_last_stage = "script_loaded"
local startup_last_occurrence = 0
local function startup_elapsed_ms()
    return math.max(0,math.floor(((mp.get_time() or startup_started_at)-startup_started_at)*1000+0.5))
end
local function write_startup_flight(stage,occurrence,detail,complete,deadline_exceeded,waiting_stage)
    startup_stage_seq=startup_stage_seq+1
    startup_last_stage=tostring(stage or startup_last_stage or "unknown")
    startup_last_occurrence=math.max(0,math.floor(tonumber(occurrence) or startup_last_occurrence or 0))
    write_json(startup_flight_file,{
        schema=1,runtime_id=runtime_id,slot_id=active_slot_id,slot_name=active_slot_name,
        stage_seq=startup_stage_seq,stage=startup_last_stage,waiting_stage=tostring(waiting_stage or ""),
        elapsed_ms=startup_elapsed_ms(),occurrence=startup_last_occurrence,detail=tostring(detail or ""),
        complete=complete==true,deadline_seconds=startup_deadline_seconds,deadline_exceeded=deadline_exceeded==true,unix=os.time()
    })
end
write_startup_flight("script_loaded",0,"Playback script loaded.",false,false)

local cfg = {}
local streamer_mode = true
local configured_art = true
local player_video_quality = "Off (audio only)"
local player_video_enabled = false
local overlay_video_enabled = true
local configured_video = true
local configured_viz = true
local controller_want_art = false
local controller_want_video = false
local controller_want_viz = false
local controller_demand_serial = 0
local controller_demand_unix = 0
local controller_demand_token = ""
local ffmpeg_available = exists(ffmpeg)
local deno_available = exists(deno)
local cache_priority = "idle"
local workers = 2
local prefetch_ahead = 4
optional_active_count = 0
optional_worker_limit = 1

local function apply_config(next_cfg)
    cfg = type(next_cfg)=="table" and next_cfg or {}
    streamer_mode = tostring(cfg.app_mode or "Streamer / OBS") == "Streamer / OBS"
    configured_art = streamer_mode and cfg.artwork_enabled ~= false
    player_video_quality = tostring(cfg.player_video_quality or "Off (audio only)")
    player_video_enabled = not player_video_quality:find("Off",1,true)
    overlay_video_enabled = streamer_mode and cfg.video_enabled ~= false
    configured_video = player_video_enabled or overlay_video_enabled
    configured_viz = streamer_mode and cfg.visualizer_enabled ~= false
    cache_priority = tostring(cfg.cache_priority or "idle")
    workers = math.max(1,math.min(8,tonumber(cfg.cache_workers) or 2))
    optional_worker_limit = workers<=1 and 1 or math.min(2,workers-1)
    prefetch_ahead = math.max(1,math.min(20,tonumber(cfg.prefetch_ahead) or 4))
end
apply_config(load_json(config_file) or {})
log("OPTIONAL MEDIA CONFIG art="..tostring(configured_art).." video="..tostring(configured_video).." viz="..tostring(configured_viz).." ffmpeg="..tostring(ffmpeg_available).." priority_runner="..tostring(exists(runner)).." cache_priority="..tostring(cache_priority).." workers="..tostring(workers))
local repeat_mode = tostring((read_all(repeat_file) or "all"):match("^%s*(.-)%s*$") or "all"):lower()
if repeat_mode~="off" and repeat_mode~="all" and repeat_mode~="one" then repeat_mode="all" end
local function persist_repeat() write_all(repeat_file,repeat_mode) end

local urls = {}
do
    local raw = read_all(playlist_file) or ""
    for line in raw:gmatch("[^\r\n]+") do
        line=line:match("^%s*(.-)%s*$")
        if line and line:match("^https?://") then table.insert(urls,line) end
    end
end
if #urls == 0 then
    write_startup_flight("playlist_empty",0,"No playable playlist URLs were found.",false,false)
    mp.msg.error("YOMI R19: playlist is empty")
    return
end
write_startup_flight("playlist_ready",0,"Loaded "..tostring(#urls).." playlist entries.",false,false)

local function audio_path(i) return audio_dir .. "\\track-" .. i .. ".audio" end
local function meta_path(i) return meta_dir .. "\\track-" .. i .. ".info.json" end
local function gain_path(i) return gain_dir .. "\\track-" .. i .. ".gain" end
local function video_path(i) return video_dir .. "\\track-" .. i .. ".mp4" end
local function viz_path(i) return visualizer_dir .. "\\track-" .. i .. ".mp4" end
local function status_path(i,suffix) return status_dir .. "\\track-" .. i .. "." .. suffix end

local function visualizer_fps()
    return tostring(cfg.visualizer_fps or "30 FPS"):find("60",1,true) and 60 or 30
end

function visualizer_render_dimensions()
    local base_w=math.max(16,math.min(256,tonumber(cfg.visualizer_internal_width) or 40))
    local h=math.max(4,math.min(192,tonumber(cfg.visualizer_internal_height) or 10))
    local length=math.max(1.0,math.min(8.0,tonumber(cfg.visualizer_length_multiplier) or 4.0))
    local w=math.max(16,math.min(2048,math.floor(base_w*length+0.5)))
    if w%2==1 then w=w+1 end
    if h%2==1 then h=h+1 end
    return w,h
end

function visualizer_profile()
    local w,h=visualizer_render_dimensions()
    return table.concat({
        "r6194-binary1",
        tostring(visualizer_fps()),
        tostring(w),tostring(h),
        tostring(cfg.visualizer_activity or "Active"),
        tostring(cfg.visualizer_adaptive_fill or "Adaptive"),
        tostring(cfg.visualizer_frequency_scale or "Logarithmic"),
        tostring(cfg.visualizer_high_frequency_trim or 0),
        tostring(cfg.visualizer_high_frequency_lift_db or 4),
        tostring(cfg.visualizer_color_mode or "Solid"),
        tostring(cfg.visualizer_solid_color or "#8A8A84"),
        tostring(cfg.visualizer_shape or "Spectrum"),
        tostring(cfg.visualizer_bar_spacing or "None"),
        tostring(cfg.visualizer_length_multiplier or 4.0),
        tostring(cfg.visualizer_direction or "Normal"),
        tostring(cfg.visualizer_vertical_anchor or "Source")
    },"|")
end

local function visualizer_profile_ready(i)
    if not (exists(viz_path(i)) and fsize(viz_path(i))>0) then return false end
    local recorded=read_all(status_path(i,"visualizer.profile"))
    return recorded~=nil and recorded==visualizer_profile()
end

-- R61.76: optional media is not READY merely because a non-empty file exists.
-- Every generated thumbnail/video/visualizer carries a decode-validation sidecar tied to
-- its exact byte length. Poisoned/truncated legacy cache is therefore rebuilt automatically.
function optional_validation_sidecar(kind,i) return status_path(i,tostring(kind)..".decode-ok") end
function optional_validation_media_path(kind,i)
    if kind=="art" then return artwork_path(i)
    elseif kind=="video" then return video_path(i)
    elseif kind=="viz" then return viz_path(i) end
    return nil
end
function optional_validation_ready(kind,i)
    local path=optional_validation_media_path(kind,i)
    if not path or not exists(path) then return false end
    local size=fsize(path)
    if size<=0 then return false end
    local stamp=read_all(optional_validation_sidecar(kind,i))
    if kind=="video" then
        -- Legacy decode-valid/source-pass cache remains PRESENTATION READY. R61.85 may
        -- opportunistically normalize it ahead of playback, but never blanks the current
        -- track just because its codec predates the native MediaElement path.
        return stamp==("r6185-h264|"..tostring(size)) or stamp==("r6185-source-pass|"..tostring(size))
            or stamp==("r6176-decode1|"..tostring(size)) or stamp==("r6178-source-pass|"..tostring(size))
    end
    return stamp==("r6176-decode1|"..tostring(size)) or stamp==("r6178-source-pass|"..tostring(size))
end
function legacy_video_validation_ready(i)
    local path=video_path(i)
    if not path or not exists(path) then return false end
    local size=fsize(path)
    if size<=0 then return false end
    local stamp=read_all(optional_validation_sidecar("video",i))
    return stamp==("r6176-decode1|"..tostring(size)) or stamp==("r6178-source-pass|"..tostring(size))
end
function mark_optional_validated(kind,i)
    local path=optional_validation_media_path(kind,i)
    if not path or not exists(path) or fsize(path)<=0 then return false end
    local prefix=kind=="video" and "r6185-h264|" or "r6176-decode1|"
    write_all(optional_validation_sidecar(kind,i),prefix..tostring(fsize(path)))
    return true
end
function mark_optional_source_pass(kind,i)
    local path=optional_validation_media_path(kind,i)
    if not path or not exists(path) or fsize(path)<=0 then return false end
    local prefix=kind=="video" and "r6185-source-pass|" or "r6178-source-pass|"
    write_all(optional_validation_sidecar(kind,i),prefix..tostring(fsize(path)))
    return true
end
function clear_optional_validation(kind,i) os.remove(optional_validation_sidecar(kind,i)) end

function youtube_id(raw)
    local u=tostring(raw or ""):match("^%s*(.-)%s*$") or ""
    return u:match("[Yy][Oo][Uu][Tt][Uu]%.?[Bb][Ee]/([%w_%-]+)")
        or u:match("[?&][Vv]=([%w_%-]+)")
        or u:match("[Yy][Oo][Uu][Tt][Uu][Bb][Ee]%.com/[Ss][Hh][Oo][Rr][Tt][Ss]/([%w_%-]+)")
        or u:match("[Yy][Oo][Uu][Tt][Uu][Bb][Ee]%.com/[Ee][Mm][Bb][Ee][Dd]/([%w_%-]+)")
        or ""
end

function source_identity(raw)
    local u=tostring(raw or ""):match("^%s*(.-)%s*$") or ""
    local id=youtube_id(u)
    -- YouTube video IDs are case-sensitive. Lowercasing here can make two distinct
    -- sources look identical and is therefore unsafe for positional-cache ownership.
    if id~="" then return "yt:"..id end
    return "url:"..u
end

-- R61.42 source-identity bridge. Keep the existing position cache as the hot/session
-- alias expected by the focused renderer/server, but back durable audio/metadata/gain
-- with source-keyed objects so switching Slots cannot bind track-N from one world to a
-- different source in another. Optional artwork/video/visualizer keep the R61.32 bounded
-- rolling-window policy. The hash mirrors ControllerHost.IdentityHash / ComputeSourceKey.
function hash32(text,seed)
    local h=tonumber(seed) or 0
    text=tostring(text or "")
    for n=1,#text do h=(h*65599+text:byte(n)+17)%4294967296 end
    return h
end
local hexchars="0123456789abcdef"
function hex32(value)
    local n=math.floor(tonumber(value) or 0)%4294967296
    local out={}
    for pos=8,1,-1 do
        local digit=n%16;n=math.floor(n/16)
        out[pos]=hexchars:sub(digit+1,digit+1)
    end
    return table.concat(out)
end
function identity_hash(text)
    return hex32(hash32(text,2166136261))..hex32(hash32(text,2246822519))
end
function source_cache_key(raw)
    local u=tostring(raw or ""):match("^%s*(.-)%s*$") or ""
    local id=youtube_id(u)
    if id~="" then return identity_hash("youtube:"..id) end
    return identity_hash("url:"..u)
end
function audio_object_signature()
    return identity_hash("audio:"..tostring(cfg.audio_quality or "Best available").."|"..tostring(cfg.audio_preference or "Prefer selected maximum"))
end
function audio_object_path(i) return object_audio_dir.."\\"..source_cache_key(urls[i]).."-audio-"..audio_object_signature()..".audio" end
function meta_object_path(i) return object_meta_dir.."\\"..source_cache_key(urls[i]).."-meta.info.json" end
function gain_object_path(i) return object_gain_dir.."\\"..source_cache_key(urls[i]).."-gain.gain" end

function ensure_dir(path)
    if utils.readdir(path,"files")~=nil then return true end
    local cmd=os.getenv("ComSpec") or "cmd.exe"
    local ok,result=pcall(utils.subprocess,{args={cmd,"/d","/c","mkdir",path},playback_only=false,capture_stdout=true,capture_stderr=true})
    return ok and result and tonumber(result.status or 1)==0 or utils.readdir(path,"files")~=nil
end
for _,dir in ipairs({object_root,object_audio_dir,object_meta_dir,object_gain_dir}) do ensure_dir(dir) end

function copy_file_atomic(src,dst)
    local expected=fsize(src);if expected<=0 then return false end
    local temp=dst..".tmp."..tostring(os.time()).."."..tostring(math.random(100000,999999))
    os.remove(temp)
    local input=io.open(src,"rb");if not input then return false end
    local output=io.open(temp,"wb");if not output then input:close();return false end
    local good=true
    while true do
        local chunk=input:read(1024*1024)
        if not chunk then break end
        if not output:write(chunk) then good=false;break end
    end
    input:close();output:close()
    if not good or fsize(temp)~=expected then os.remove(temp);return false end
    if exists(dst) and fsize(dst)>0 then os.remove(temp);return true end
    if os.rename(temp,dst) then return exists(dst) and fsize(dst)==expected end
    -- Another worker may have won the same source-object race. Never expose the
    -- temporary partial file as authoritative; accept only an already-complete target.
    os.remove(temp)
    return exists(dst) and fsize(dst)>0
end

function hardlink_or_copy(src,dst)
    local expected=fsize(src);if expected<=0 then return false end
    if exists(dst) and fsize(dst)>0 then return true end
    os.remove(dst)
    local cmd=os.getenv("ComSpec") or "cmd.exe"
    local ok,result=pcall(utils.subprocess,{args={cmd,"/d","/c","mklink","/H",dst,src},playback_only=false,capture_stdout=true,capture_stderr=true})
    if ok and result and tonumber(result.status or 1)==0 and exists(dst) and fsize(dst)==expected then return true end
    os.remove(dst)
    return copy_file_atomic(src,dst)
end

function promote_object(src,dst)
    if not src or not dst or not exists(src) or fsize(src)<=0 then return false end
    if exists(dst) and fsize(dst)>0 then return true end
    return hardlink_or_copy(src,dst)
end

function hydrate_position(src,dst)
    if exists(dst) and fsize(dst)>0 then return true end
    return hardlink_or_copy(src,dst)
end

function promote_position_objects(i)
    if i<1 or i>#urls then return end
    if exists(audio_path(i)) and fsize(audio_path(i))>0 then promote_object(audio_path(i),audio_object_path(i)) end
    if exists(meta_path(i)) and fsize(meta_path(i))>0 then promote_object(meta_path(i),meta_object_path(i)) end
    if exists(gain_path(i)) and fsize(gain_path(i))>0 then promote_object(gain_path(i),gain_object_path(i)) end
end

function artwork_path(i)
    if ensure_position_binding then ensure_position_binding(i) end
    for _,ext in ipairs({"jpg","jpeg","png","webp"}) do
        local p=artwork_dir .. "\\track-" .. i .. "." .. ext
        if exists(p) and fsize(p)>0 then return p end
    end
    return nil
end

function hydrate_supporting_objects(i)
    if not exists(meta_path(i)) then hydrate_position(meta_object_path(i),meta_path(i)) end
    if not exists(gain_path(i)) then hydrate_position(gain_object_path(i),gain_path(i)) end
end

function audio_ready(i)
    if not i or i<1 or i>#urls then return false end
    if ensure_position_binding then ensure_position_binding(i) end
    if not (exists(audio_path(i)) and fsize(audio_path(i))>0) then hydrate_position(audio_object_path(i),audio_path(i)) end
    if exists(audio_path(i)) and fsize(audio_path(i))>0 then
        hydrate_supporting_objects(i)
        promote_object(audio_path(i),audio_object_path(i))
        return true
    end
    return false
end

function known_bad(i)
    if ensure_position_binding then ensure_position_binding(i) end
    return exists(status_path(i,"audio.permanent"))
end

local pool_current=load_json(pool_file) or {}
local pool_tracks=type(pool_current.tracks)=="table" and pool_current.tracks or {}
local pool_name=tostring(pool_current.name or "")
local pool_reset_requested=exists(pool_reset_file)
local function pool_track_for(i)
    local raw=pool_tracks[i]
    if type(raw)~="table" then return nil end
    if source_identity(raw.url)~=source_identity(urls[i]) then return nil end
    return raw
end

local previous_session=load_json(session_file)
local previous_source_by_position={}
if previous_session and type(previous_session.occurrences)=="table" then
    for ordinal,raw in ipairs(previous_session.occurrences) do
        if type(raw)=="table" then
            local pos=math.floor(tonumber(raw.position or raw.source_index or ordinal) or ordinal)
            previous_source_by_position[pos]=source_identity(raw.url)
        end
    end
end

local playlist_changed=false
do
    local previous_count=0
    for _ in pairs(previous_source_by_position) do previous_count=previous_count+1 end
    if previous_count>0 then
        if previous_count~=#urls then playlist_changed=true end
        if not playlist_changed then
            for i=1,#urls do
                if previous_source_by_position[i]~=source_identity(urls[i]) then playlist_changed=true;break end
            end
        end
    end
end
if pool_reset_requested then playlist_changed=true end

local function remove_track_cache(dir,i)
    local prefix="track-"..tostring(i).."."
    local alt="track-"..tostring(i).."-"
    for _,name in ipairs(utils.readdir(dir,"files") or {}) do
        if name:sub(1,#prefix)==prefix or name:sub(1,#alt)==alt then os.remove(dir.."\\"..name) end
    end
end

local prior_cache_slot=(read_all(cache_slot_context_file) or ""):match("^%s*(.-)%s*$") or ""
local cache_slot_changed=active_slot_id~="" and prior_cache_slot~=active_slot_id
local position_binding_checked={}

-- R61.43.1 startup-critical repair.
-- Slot-aware cache safety is enforced lazily for the occurrence YOMI actually touches.
-- The R61.42 implementation synchronously reconciled/promoted every position in a
-- playlist during music.lua initialization. On large libraries that could run thousands
-- of filesystem probes and mklink subprocesses before mpv reached its first runtime
-- lease or first play request, leaving the supervisor alive while playback appeared
-- permanently stuck at "preparing the first playable track".
function ensure_position_binding(i)
    i=math.floor(tonumber(i) or 0)
    if i<1 or i>#urls or position_binding_checked[i] then return end
    position_binding_checked[i]=true

    local marker=status_path(i,"source")
    local current=source_identity(urls[i])
    local recorded=(read_all(marker) or ""):match("^%s*(.-)%s*$") or ""
    local prior=recorded~="" and recorded or (previous_source_by_position[i] or "")

    local has_art=false
    for _,ext in ipairs({"jpg","jpeg","png","webp"}) do
        local p=artwork_dir.."\\track-"..i.."."..ext
        if exists(p) and fsize(p)>0 then has_art=true;break end
    end
    local has_cache=(exists(audio_path(i)) and fsize(audio_path(i))>0) or has_art
        or (exists(video_path(i)) and fsize(video_path(i))>0)
        or (exists(viz_path(i)) and fsize(viz_path(i))>0)
        or (exists(meta_path(i)) and fsize(meta_path(i))>0)
        or (exists(gain_path(i)) and fsize(gain_path(i))>0)

    -- A recorded source marker is authoritative even across Slot switches. For older
    -- unmarked cache, trust the previous session mapping only when the Slot context has
    -- not changed. Otherwise the positional alias may belong to another listening world.
    local trusted=(recorded~="" and recorded==current)
        or (recorded=="" and not cache_slot_changed and prior~="" and prior==current)

    if not trusted then
        remove_track_cache(audio_dir,i);remove_track_cache(artwork_dir,i);remove_track_cache(video_dir,i)
        remove_track_cache(visualizer_dir,i);remove_track_cache(meta_dir,i);remove_track_cache(gain_dir,i);remove_track_cache(status_dir,i)
        if has_cache or recorded~="" or prior~="" then log("SOURCE CHANGED track "..i.."; stale position cache removed lazily") end
    elseif trusted then
        -- Promote only the occurrence currently being touched. This preserves the
        -- source-addressed durable cache without putting an O(playlist-size) migration
        -- on the Play critical path.
        promote_position_objects(i)
    end
    write_all(marker,current)
end

log("CACHE SOURCE RECONCILE lazy per-track"..(cache_slot_changed and " · slot changed" or ""))
if active_slot_id~="" then write_all(cache_slot_context_file,active_slot_id) end

local order = {}
local order_position = {}
local order_revision = 0
local order_command_serial = 0
local session_id = "focused-r19"
do
    local saved = pool_reset_requested and nil or load_json(order_file)
    local seen = {}
    if saved then
        session_id=tostring(saved.session_id or session_id)
        order_revision=tonumber(saved.revision) or 0
        order_command_serial=tonumber(saved.command_serial) or 0
    end
    if pool_reset_requested then
        session_id="pool-r6122-"..tostring(os.time())
        order_revision=0
        order_command_serial=0
    elseif playlist_changed and previous_session then
        session_id="focused-r59-"..tostring(os.time())
        order_revision=0
        order_command_serial=0
        local buckets={}
        for i=1,#urls do
            local key=source_identity(urls[i]);buckets[key]=buckets[key] or {};table.insert(buckets[key],i)
        end
        local cursors={}
        if saved and type(saved.order)=="table" then
            for _,raw in ipairs(saved.order) do
                local old=math.floor(tonumber(raw) or 0)
                local key=previous_source_by_position[old]
                local bucket=key and buckets[key] or nil
                if bucket then
                    local cursor=(cursors[key] or 0)+1
                    cursors[key]=cursor
                    local n=bucket[cursor]
                    if n and not seen[n] then seen[n]=true;table.insert(order,n) end
                end
            end
        end
    elseif saved and type(saved.order)=="table" then
        for _,raw in ipairs(saved.order) do
            local n=math.floor(tonumber(raw) or 0)
            if n>=1 and n<=#urls and not seen[n] then seen[n]=true;table.insert(order,n) end
        end
    end
    for i=1,#urls do if not seen[i] then seen[i]=true;table.insert(order,i) end end
    for slot,occ in ipairs(order) do order_position[occ]=slot end
end

local function current_slot(i)
    return tonumber(order_position[i]) or 0
end

-- R61.95 SEARCH PLAYLIST: the authoritative session order never changes. A temporary
-- filtered playback lane can sit over it, so search results behave like a little playlist
-- until the user clears the search.
playback_subset={}
playback_subset_position={}
playback_subset_active=false
playback_subset_label=""

function rebuild_playback_subset_position()
    playback_subset_position={}
    for slot,occ in ipairs(playback_subset) do playback_subset_position[occ]=slot end
end

local function next_occurrence(i,step)
    local sequence=playback_subset_active and playback_subset or order
    local positions=playback_subset_active and playback_subset_position or order_position
    if #sequence==0 then return nil end
    local direction=(tonumber(step) or 1)>=0 and 1 or -1
    local slot=tonumber(positions[i]) or 0
    if slot<1 then
        -- Entering a temporary lane from a track outside it starts at the closest edge.
        return direction>=0 and sequence[1] or sequence[#sequence]
    end
    for _=1,#sequence do
        local next_slot=slot+direction
        if repeat_mode=="off" and (next_slot>#sequence or next_slot<1) then return nil end
        slot=next_slot
        if slot>#sequence then slot=1 elseif slot<1 then slot=#sequence end
        local candidate=sequence[slot]
        if candidate and not known_bad(candidate) then return candidate end
    end
    return nil
end

local prior_resume_index = tonumber((read_all(resume_file) or "1"):match("%d+")) or 1
local current_index = prior_resume_index
if pool_reset_requested then
    current_index=order[1] or 1
elseif playlist_changed and previous_source_by_position[prior_resume_index] then
    local wanted=previous_source_by_position[prior_resume_index]
    current_index=0
    for i=1,#urls do if source_identity(urls[i])==wanted then current_index=i;break end end
end
if current_index<1 or current_index>#urls then current_index=order[1] or 1 end
local desired_index=current_index
local playing_index=0
local requested_index=0
local transport_pending_target=0
local transport_serial=0
local transport_timer=nil
local transport_settle_seconds=0.075
local transport_pending_attempts=0
local transport_max_attempts=2
local cancel_pending_transport
local work_generation=1
local shutting_down=false

-- R61.32: prefetch distance is not permission for optional media to accumulate forever.
-- Keep the current occurrence, the configured forward horizon, and two items behind.
-- Audio is intentionally excluded: durable audio cache still protects fast starts.
local optional_cache_keep_behind=2
local last_optional_prune_anchor=0

local function optional_retention_set(anchor)
    local keep={}
    anchor=math.floor(tonumber(anchor) or 0)
    if anchor<1 or anchor>#urls then return keep end
    keep[anchor]=true
    local cursor=anchor
    for _=1,prefetch_ahead do
        local n=next_occurrence(cursor,1)
        if not n or keep[n] then break end
        keep[n]=true;cursor=n
    end
    cursor=anchor
    for _=1,optional_cache_keep_behind do
        local n=next_occurrence(cursor,-1)
        if not n or keep[n] then break end
        keep[n]=true;cursor=n
    end
    if desired_index>0 then keep[desired_index]=true end
    if playing_index>0 then keep[playing_index]=true end
    if requested_index>0 then keep[requested_index]=true end
    return keep
end

local function remove_optional_track_cache(i)
    remove_track_cache(artwork_dir,i)
    remove_track_cache(video_dir,i)
    remove_track_cache(visualizer_dir,i)
    os.remove(status_path(i,"art.failed"))
    os.remove(status_path(i,"artwork.failed"))
    os.remove(status_path(i,"video.failed"))
    os.remove(status_path(i,"visualizer.failed"))
    os.remove(status_path(i,"visualizer.profile"))
    clear_optional_validation("art",i)
    clear_optional_validation("video",i)
    clear_optional_validation("viz",i)
end

local function optional_should_retain(i,anchor)
    return optional_retention_set(anchor)[i]==true
end

local function prune_optional_cache(anchor,force)
    anchor=math.floor(tonumber(anchor) or 0)
    if anchor<1 or anchor>#urls then return end
    if not force and anchor==last_optional_prune_anchor then return end
    last_optional_prune_anchor=anchor
    local keep=optional_retention_set(anchor)
    local removed={art=0,video=0,viz=0}
    for kind,dir in pairs({art=artwork_dir,video=video_dir,viz=visualizer_dir}) do
        for _,name in ipairs(utils.readdir(dir,"files") or {}) do
            local n=tonumber(name:match("^track%-(%d+)[%.%-]"))
            if n and not keep[n] then
                if os.remove(dir.."\\"..name) then removed[kind]=removed[kind]+1 end
            end
        end
    end
    if removed.art+removed.video+removed.viz>0 then
        log("OPTIONAL CACHE PRUNE anchor "..anchor.." keep current+"..prefetch_ahead.." ahead+"..optional_cache_keep_behind.." behind removed art="..removed.art.." video="..removed.video.." viz="..removed.viz)
    end
end

local function set_engine_status(phase,message,index)
    write_json(engine_status_file,{
        phase=phase or "",
        message=message or "",
        index=tonumber(index) or 0,
        count=#order,
        paused=mp.get_property_native("pause")==true,
        unix=os.time(),
        playback_spine="focused-r19",
        repeat_mode=repeat_mode,
        playback_subset_active=playback_subset_active,
        playback_subset_count=playback_subset_active and #playback_subset or 0,
        playback_subset_label=playback_subset_label,
        slot_id=active_slot_id,
        slot_name=active_slot_name,
        runtime_id=runtime_id,
        controller_demand={runtime_id=runtime_id,art=controller_want_art,video=controller_want_video,viz=controller_want_viz,serial=controller_demand_serial,unix=controller_demand_unix,token=controller_demand_token}
    })
end

local function meta_for(i)
    if ensure_position_binding then ensure_position_binding(i) end
    local info=load_json(meta_path(i)) or {}
    local pool=pool_track_for(i) or {}
    return {
        title=tostring(info.title or pool.title or ("Track "..tostring(i))),
        channel=tostring(info.channel or info.uploader or pool.channel or ""),
        duration=tonumber(info.duration) or tonumber(pool.duration) or 0,
        id=tostring(info.id or pool.id or "")
    }
end

local function state_for(i)
    local m=meta_for(i)
    local pool=pool_track_for(i) or {}
    return {
        index=i,
        occurrence_id=i,
        position=current_slot(i),
        playlist_count=#order,
        title=m.title,
        channel=m.channel,
        duration=m.duration,
        -- Optional presentation media is published only after the producer-side decode contract passes.
        -- Do not leak a merely-existing legacy/processing file into WPF or OBS.
        artwork=optional_validation_ready("art",i) and (artwork_path(i) or "") or "",
        video=optional_validation_ready("video",i) and video_path(i) or "",
        visualizer=(visualizer_profile_ready(i) and optional_validation_ready("viz",i)) and viz_path(i) or "",
        audio=audio_ready(i) and audio_path(i) or "",
        source_index=i,
        pool_name=pool_name,
        pool_source_indexes=type(pool.source_indexes)=="table" and pool.source_indexes or {},
        pool_source_labels=type(pool.source_labels)=="table" and pool.source_labels or {},
        unix=os.time(),
        playback_spine="focused-r19",
        repeat_mode=repeat_mode,
        slot_id=active_slot_id,
        slot_name=active_slot_name,
        runtime_id=runtime_id,
        controller_demand={runtime_id=runtime_id,art=controller_want_art,video=controller_want_video,viz=controller_want_viz,serial=controller_demand_serial,unix=controller_demand_unix,token=controller_demand_token}
    }
end

local function write_current(i)
    if i and i>=1 and i<=#urls then write_json(current_file,state_for(i)) end
end

function write_active_slot_bookmark()
    if active_slot_id=="" then return end
    local descriptor=load_json(active_slot_file) or {}
    if tostring(descriptor.id or "")~=active_slot_id then return end
    local i=(playing_index and playing_index>0 and playing_index) or current_index or slot_bookmark_occurrence
    descriptor.bookmark_occurrence=math.max(0,math.floor(tonumber(i) or 0))
    descriptor.bookmark_seconds=math.max(0,tonumber((mp.get_property_number("time-pos",0))) or 0)
    descriptor.paused=mp.get_property_native("pause")==true
    descriptor.engine_updated_unix=os.time()
    write_json(active_slot_file,descriptor)
end

local function write_queue_runtime()
    local base=(transport_pending_target>0 and transport_pending_target) or (playing_index>0 and playing_index) or (desired_index>0 and desired_index) or current_index
    local slot=current_slot(base)
    local items={}
    local start=math.max(1,slot-2)
    local finish=math.min(#order,slot+math.max(prefetch_ahead,4))
    for s=start,finish do
        local i=order[s]
        table.insert(items,{
            index=i,
            order_slot=s,
            playback_ready=audio_ready(i),
            state=(i==playing_index and "PLAYING") or (audio_ready(i) and "READY") or "WAITING",
            title=meta_for(i).title
        })
    end
    write_json(queue_file,{
        schema=4,
        count=#order,
        current_index=base,
        current_order_slot=slot,
        revision=order_revision,
        items=items,
        playback_spine="focused-r19",
        repeat_mode=repeat_mode,
        slot_id=active_slot_id,
        slot_name=active_slot_name,
        unix=os.time()
    })
end

local function ensure_projection_files()
    if playlist_changed or not exists(order_file) then
        write_json(order_file,{schema=3,session_id=session_id,revision=order_revision,command_serial=order_command_serial,last_command_status="idle",last_command_reason=playlist_changed and "playlist-source-change" or "focused-r19",order=order,inserted={},reason=playlist_changed and "playlist-source-change" or "focused-r19",slot_id=active_slot_id,slot_name=active_slot_name})
    end
    if playlist_changed or not exists(session_file) then
        local occurrences={}
        for i=1,#urls do
            local pool=pool_track_for(i) or {}
            table.insert(occurrences,{
                position=i,source_index=i,url=urls[i],source_key=source_cache_key(urls[i]),
                id=tostring(pool.id or ""),title=tostring(pool.title or ("Track "..i)),channel=tostring(pool.channel or ""),duration=tonumber(pool.duration) or 0,
                pool_name=pool_name,pool_source_indexes=type(pool.source_indexes)=="table" and pool.source_indexes or {},pool_source_labels=type(pool.source_labels)=="table" and pool.source_labels or {}
            })
        end
        write_json(session_file,{schema=3,session_id=session_id,count=#urls,occurrences=occurrences,playlist_changed=playlist_changed,pool_name=pool_name,slot_id=active_slot_id,slot_name=active_slot_name})
    else
        local existing=load_json(session_file)
        if existing then
            existing.slot_id=active_slot_id;existing.slot_name=active_slot_name
            if type(existing.occurrences)=="table" then
                for i,occ in ipairs(existing.occurrences) do
                    if type(occ)=="table" and urls[i] then occ.source_key=source_cache_key(urls[i]) end
                end
            end
            write_json(session_file,existing)
        end
    end
    if playlist_changed then write_all(resume_file,tostring(current_index)) end
    if pool_reset_requested then os.remove(pool_reset_file) end
end
write_startup_flight("session_ready",current_index,"Session/order state restored.",false,false)

local function write_runtime_lease()
    if shutting_down then return end
    write_json(runtime_lease_file,{
        schema=1,
        runtime_id=runtime_id,
        session_id=session_id,
        unix=os.time(),
        occurrence=(playing_index>0 and playing_index) or current_index,
        order_slot=current_slot((playing_index>0 and playing_index) or current_index),
        order_revision=order_revision,
        work_generation=work_generation,
        safe_mode=false,
        failure_domain="FOCUSED_PLAYBACK",
        slot_id=active_slot_id,
        slot_name=active_slot_name
    })
end

local function subprocess_argv(priority,exe,args,bypass_runner)
    local argv={}
    if not bypass_runner and exists(runner) then
        table.insert(argv,runner);table.insert(argv,tostring(priority or "idle"));table.insert(argv,exe)
    else
        table.insert(argv,exe)
    end
    for _,v in ipairs(args or {}) do table.insert(argv,tostring(v)) end
    return argv
end

local function run(priority,exe,args,callback)
    mp.command_native_async({
        name="subprocess",playback_only=false,capture_stdout=true,capture_stderr=true,args=subprocess_argv(priority,exe,args,false)
    },function(success,result,error_text)
        yomi_safe_invoke("subprocess",callback,success,result or {},error_text or "")
    end)
end

-- R61.45: every yt-dlp lane that can occupy playback/cache authority is wall-clock bounded. mpv's async subprocess API has no wall-clock timeout of its own. yt-dlp's
-- --socket-timeout only covers individual network operations, so an extractor/JS child can
-- still live forever. This wrapper owns a real deadline and aborts the async subprocess.
-- Fast-start resolution bypasses PriorityRun as well: first sound must not depend on a
-- secondary wrapper process whose child lifetime cannot be authoritatively observed here.
local function run_bounded(priority,exe,args,timeout_seconds,bypass_runner,callback)
    local finished=false
    local request_id=nil
    local timer=nil
    local function finish(success,result,error_text,timed_out)
        if finished then return end
        finished=true
        if timer then timer:kill();timer=nil end
        yomi_safe_invoke("bounded-subprocess",callback,success,result or {},error_text or "",timed_out==true)
    end
    local submitted,id_or_error=pcall(mp.command_native_async,{
        name="subprocess",playback_only=false,capture_stdout=true,capture_stderr=true,args=subprocess_argv(priority,exe,args,bypass_runner==true)
    },function(success,result,error_text)
        finish(success,result,error_text,false)
    end)
    if not submitted then
        finish(false,{status=-1,stdout="",stderr=tostring(id_or_error or "subprocess submission failed")},"submit",false)
        return
    end
    request_id=id_or_error
    local seconds=math.max(1,tonumber(timeout_seconds) or 1)
    timer=safe_timeout(seconds,function()
        if finished then return end
        finished=true
        if request_id then pcall(mp.abort_async_command,request_id) end
        yomi_safe_invoke("bounded-timeout",callback,false,{status=-1,stdout="",stderr="YOMI subprocess wall-clock timeout after "..tostring(seconds).." seconds"},"timeout",true)
    end)
end

-- R61.88: bounded cache producers use the job-owning PriorityRun helper. R61.78 had to
-- bypass the old helper because aborting its parent could leave FFmpeg alive. PriorityRun R61.88
-- launches the child suspended, assigns it to a KILL_ON_JOB_CLOSE Windows Job Object, then resumes
-- it. mpv can therefore abort the wrapper without orphaning the producer tree, while cache_priority
-- finally becomes an actual process-priority authority. Fast first-sound stream resolution remains
-- direct because it is latency-critical rather than background cache work.
function media_error_summary(result,error_text,timed_out)
    local status=tonumber((result or {}).status or -1) or -1
    local text=tostring((result or {}).stderr or "").." "..tostring(error_text or "")
    text=text:gsub("[\r\n\t]+"," "):gsub("%s+"," "):match("^%s*(.-)%s*$") or ""
    if #text>700 then text=text:sub(1,700).."..." end
    return "status="..tostring(status).." timeout="..tostring(timed_out==true)..(text~="" and (" stderr="..text) or "")
end
function run_ffmpeg_media(args,timeout_seconds,expected_path,callback)
    run_bounded(cache_priority,ffmpeg,args,timeout_seconds,false,function(success,result,error_text,timed_out)
        local status=tonumber((result or {}).status or -1) or -1
        local output_ok=(not expected_path) or (exists(expected_path) and fsize(expected_path)>0)
        if not (success and status==0 and output_ok) then
            log("FFMPEG MEDIA FAIL "..media_error_summary(result,error_text,timed_out).." output="..tostring(expected_path or ""))
        end
        callback(success,result,error_text,timed_out,false)
    end)
end

local function ytdlp_common()
    local a={"--no-playlist","--quiet","--no-warnings","--socket-timeout","20","--retries","2","--fragment-retries","2"}
    if deno_available then
        table.insert(a,"--js-runtimes");table.insert(a,"deno:"..deno)
    end
    if ffmpeg_available then
        table.insert(a,"--ffmpeg-location");table.insert(a,ffmpeg_dir)
    end
    return a
end

local function ytdlp_fast_common(use_js)
    local a={"--no-playlist","--quiet","--no-warnings","--socket-timeout","6","--retries","0","--fragment-retries","0","--extractor-retries","0"}
    if use_js and deno_available then
        table.insert(a,"--js-runtimes");table.insert(a,"deno:"..deno)
    end
    return a
end

local function audio_selector()
    local quality=tostring(cfg.audio_quality or "Best available")
    local low=tostring(cfg.audio_preference or "Prefer selected maximum")=="Prefer lowest compatible"
    local cap=nil
    if quality:find("64",1,true) then cap=64 elseif quality:find("128",1,true) then cap=128 elseif quality:find("160",1,true) then cap=160 end
    if low then
        return cap and ("worstaudio[abr<="..cap.."]/worstaudio/bestaudio/best") or "worstaudio/bestaudio/best"
    end
    return cap and ("bestaudio[abr<="..cap.."]/bestaudio/best") or "bestaudio/best"
end

local function permanent_error(raw)
    local s=tostring(raw or ""):lower()
    return s:find("video unavailable",1,true) or s:find("private video",1,true) or s:find("has been removed",1,true)
        or s:find("account associated with this video has been terminated",1,true) or s:find("copyright",1,true)
end

local jobs={}
local queued={}
local active={}
local active_count=0
local audio_failures={}
local stream_resolving={}
local stream_failures={}
local stream_consecutive_failures=0
stream_route_degraded_until={js=0,["no-js"]=0}
stream_route_failures={js=0,["no-js"]=0}
function stream_route_mark(mode,success,timed_out)
    mode=tostring(mode or "no-js")
    if success then
        stream_route_failures[mode]=0
        stream_route_degraded_until[mode]=0
        return
    end
    stream_route_failures[mode]=(stream_route_failures[mode] or 0)+1
    if timed_out or stream_route_failures[mode]>=2 then
        stream_route_degraded_until[mode]=os.time()+(mode=="js" and 300 or 90)
    end
end
function stream_route_order()
    local now=os.time()
    local nojs=(stream_route_degraded_until["no-js"] or 0)<=now
    local js=(stream_route_degraded_until.js or 0)<=now
    if nojs and js then return {"no-js","js"} end
    if nojs then return {"no-js"} end
    if js then return {"js"} end
    return {"no-js"}
end
local playing_from_cache=true
local pump
local request_bundle
local play_index
local start_fast_stream

local function job_key(kind,i) return kind..":"..tostring(i) end

local function enqueue(kind,i,priority)
    local key=job_key(kind,i)
    if active[key] or queued[key] then return end
    queued[key]=true
    table.insert(jobs,{key=key,kind=kind,i=i,priority=tonumber(priority) or 100,generation=work_generation})
end

-- R61.63: loading media is itself a bounded transition. Deferring EOF handoff prevents
-- re-entrant loadfile calls, but a decoder/network/cache load that never reaches file-loaded
-- must not strand transport authority forever. One retry is allowed; then YOMI fails forward.
local load_watchdog_timer=nil
local load_watchdog_serial=0
local load_watchdog_target=0
local load_watchdog_failures={}

local function cancel_load_watchdog()
    load_watchdog_serial=load_watchdog_serial+1
    load_watchdog_target=0
    if load_watchdog_timer then load_watchdog_timer:kill();load_watchdog_timer=nil end
end

local function arm_load_watchdog(i,route)
    i=math.floor(tonumber(i) or 0)
    if i<1 or i>#urls then return end
    cancel_load_watchdog()
    load_watchdog_target=i
    local serial=load_watchdog_serial
    local ceiling=(route=="direct") and 12.0 or 7.0
    load_watchdog_timer=safe_timeout(ceiling,function()
        load_watchdog_timer=nil
        if shutting_down or serial~=load_watchdog_serial or load_watchdog_target~=i or desired_index~=i or playing_index==i then return end
        load_watchdog_target=0
        requested_index=0
        load_watchdog_failures[i]=(load_watchdog_failures[i] or 0)+1
        local attempt=load_watchdog_failures[i]
        log("LOAD WATCHDOG track "..i.." route "..tostring(route).." attempt "..attempt)
        pcall(function() mp.commandv("stop") end)

        if route=="direct" then
            set_engine_status("preparing","Stream load stalled; switching to durable cache for track "..i.."...",i)
            if audio_ready(i) then
                safe_timeout(0.08,function() if not shutting_down and desired_index==i then play_index(i) end end)
            else
                enqueue("audio",i,0);pump()
            end
            return
        end

        if attempt<=1 then
            set_engine_status("starting","Local audio load stalled; retrying track "..i.."...",i)
            safe_timeout(0.08,function() if not shutting_down and desired_index==i then play_index(i) end end)
            return
        end

        local n=next_occurrence(i,1)
        if transport_pending_target==i and cancel_pending_transport then cancel_pending_transport() end
        if n and n~=i then
            desired_index=n;requested_index=0;work_generation=work_generation+1
            set_engine_status("advancing","Track "..i.." would not open; moving to track "..n.."...",n)
            log("LOAD WATCHDOG fail-forward "..i.." -> "..n)
            safe_timeout(0.08,function() if not shutting_down and desired_index==n then play_index(n) end end)
        else
            set_engine_status("error","The current track would not open after a bounded retry. Transport remains available; press Next or Play to retry.",i)
            write_queue_runtime()
        end
    end)
end

local function optional_ready(kind,i)
    if ensure_position_binding then ensure_position_binding(i) end
    if kind=="art" then return optional_validation_ready("art",i)
    elseif kind=="video" then return optional_validation_ready("video",i)
    elseif kind=="viz" then return visualizer_profile_ready(i) and optional_validation_ready("viz",i) end
    return false
end

-- R61.79: a periodic controller-demand lease is a liveness mechanism, not an instruction to
-- hammer a deterministic media failure every four seconds. Failure markers carry their own
-- revision/timestamp and suppress automatic retries for a bounded interval. User/config state
-- changes can explicitly clear the marker and retry immediately.
yomi_optional_backoff_log_token={}
function optional_failure_path(kind,i)
    if kind=="art" then return status_path(i,"artwork.failed") end
    if kind=="video" then return status_path(i,"video.failed") end
    if kind=="viz" then return status_path(i,"visualizer.failed") end
    return nil
end
function clear_optional_failure(kind,i)
    local p=optional_failure_path(kind,i)
    if p then os.remove(p) end
    yomi_optional_backoff_log_token[job_key(kind,i)]=nil
end
function mark_optional_failure(kind,i,reason)
    local p=optional_failure_path(kind,i)
    if not p then return end
    write_all(p,"r6179|"..tostring(os.time()).."|"..tostring(reason or "failure"))
    yomi_optional_backoff_log_token[job_key(kind,i)]=nil
end
function optional_failure_blocked(kind,i)
    local p=optional_failure_path(kind,i)
    if not p or not exists(p) then return false end
    local raw=tostring(read_all(p) or "")
    local stamp=tonumber(raw:match("^r6179|(%d+)|"))
    if not stamp then return false end -- pre-R61.79 markers are allowed one fresh retry
    local age=math.max(0,os.time()-stamp)
    local current=(i==playing_index or i==desired_index or i==current_index)
    local cooldown=current and 45 or 180
    if age>=cooldown then return false end
    local key=job_key(kind,i)
    local token=tostring(stamp)..":"..tostring(cooldown)
    if yomi_optional_backoff_log_token[key]~=token then
        yomi_optional_backoff_log_token[key]=token
        log("OPTIONAL BACKOFF kind="..kind.." track="..i.." retry_in="..math.max(1,cooldown-age).."s")
    end
    return true
end

local function job_done(job)
    active[job.key]=nil
    active_count=math.max(0,active_count-1)
    if job.kind~="audio" then optional_active_count=math.max(0,optional_active_count-1) end
    if job.kind~="audio" then
        local anchor=(playing_index>0 and playing_index) or desired_index or current_index
        if not optional_should_retain(job.i,anchor) then remove_optional_track_cache(job.i) end
    end
    if job.kind=="audio" and audio_ready(job.i) then
        request_bundle(job.i,job.priority)
        if desired_index==job.i and playing_index~=job.i and requested_index~=job.i then play_index(job.i) end
    end
    if playing_index==job.i then write_current(job.i) end
    write_queue_runtime()
    pump()
end

local function cleanup_prefix(dir,prefix)
    for _,name in ipairs(utils.readdir(dir,"files") or {}) do
        if name:sub(1,#prefix)==prefix then os.remove(dir.."\\"..name) end
    end
end

local function audio_job(job)
    local i=job.i
    if audio_ready(i) then job_done(job);return end
    local prefix="track-"..i..".focused."
    cleanup_prefix(audio_dir,prefix)
    local template=audio_dir.."\\track-"..i..".focused.%(ext)s"
    local a=ytdlp_common()
    table.insert(a,"--format");table.insert(a,audio_selector())
    table.insert(a,"--write-info-json")
    table.insert(a,"--no-part")
    table.insert(a,"--output");table.insert(a,template)
    table.insert(a,urls[i])
    log("AUDIO START track "..i)
    run_bounded(cache_priority,ytdlp,a,75,false,function(success,result,error_text,timed_out)
        local stderr=tostring(result.stderr or "").." "..tostring(error_text or "")
        local media=nil
        for _,name in ipairs(utils.readdir(audio_dir,"files") or {}) do
            if name:sub(1,#prefix)==prefix and not name:match("%.info%.json$") then
                local p=audio_dir.."\\"..name
                if fsize(p)>0 then media=p;break end
            end
        end
        local info=audio_dir.."\\track-"..i..".focused.info.json"
        if success and tonumber(result.status or 0)==0 and media then
            os.remove(audio_path(i));os.rename(media,audio_path(i))
            if exists(info) then os.remove(meta_path(i));os.rename(info,meta_path(i)) end
            if not exists(gain_path(i)) then write_all(gain_path(i),"0") end
            promote_object(audio_path(i),audio_object_path(i))
            if exists(meta_path(i)) then promote_object(meta_path(i),meta_object_path(i)) end
            if exists(gain_path(i)) then promote_object(gain_path(i),gain_object_path(i)) end
            audio_failures[i]=nil
            os.remove(status_path(i,"audio.failed"));os.remove(status_path(i,"audio.permanent"))
            log("AUDIO READY track "..i)
            job_done(job)
            return
        end
        cleanup_prefix(audio_dir,prefix)
        audio_failures[i]=(audio_failures[i] or 0)+1
        if permanent_error(stderr) then
            write_all(status_path(i,"audio.permanent"),"1")
            log("AUDIO PERMANENT track "..i)
            if desired_index==i then
                local n=next_occurrence(i,1)
                if n then desired_index=n;requested_index=0;safe_timeout(0.05,function() play_index(n) end) end
            end
        elseif audio_failures[i]<3 then
            log("AUDIO RETRY track "..i.." attempt "..(audio_failures[i]+1))
            safe_timeout(1.0,function() enqueue("audio",i,job.priority);pump() end)
        else
            write_all(status_path(i,"audio.failed"),"1")
            log("AUDIO FAILED track "..i)
            if desired_index==i then
                local n=next_occurrence(i,1)
                if n then desired_index=n;requested_index=0;safe_timeout(0.05,function() play_index(n) end) end
            end
        end
        job_done(job)
    end)
end

start_fast_stream=function(i)
    i=math.floor(tonumber(i) or 0)
    if i<1 or i>#urls or known_bad(i) then return end
    if audio_ready(i) then play_index(i);return end
    if stream_resolving[i] then return end
    stream_resolving[i]=true
    if not startup_first_sound then write_startup_flight("stream_resolving",i,"Resolving the direct audio stream.",false,false) end
    set_engine_status("preparing","Resolving stream for track "..i.."...",i)
    log("STREAM RESOLVE track "..i)

    local routes=stream_route_order()
    local route_pos=1
    local stderr_parts={}
    local timed_out_any=false
    local function finish_failure()
        stream_resolving[i]=nil
        stream_failures[i]=(stream_failures[i] or 0)+1
        local stderr=table.concat(stderr_parts," ")
        local permanent=permanent_error(stderr)
        if permanent then
            write_all(status_path(i,"audio.permanent"),"1")
            stream_consecutive_failures=0
            log("STREAM PERMANENT track "..i.."; advancing")
        else
            stream_consecutive_failures=stream_consecutive_failures+1
            log("STREAM RESOLVE FAIL track "..i..(timed_out_any and " timeout" or "").."; background cache + advance")
            -- Preserve the durable-cache opportunity without holding first sound hostage.
            enqueue("audio",i,30);pump()
        end
        if desired_index~=i then return end
        if not startup_first_sound then
            write_startup_flight("stream_resolve_failed",i,permanent and "Track is unavailable; advancing." or "Direct stream could not resolve inside the bounded fast-start window.",false,false)
        end
        local n=next_occurrence(i,1)
        if n and n~=i and (permanent or stream_consecutive_failures<=2) then
            desired_index=n;requested_index=0
            set_engine_status("preparing","Track "..i.." did not resolve; trying track "..n.."...",n)
            safe_timeout(0.08,function() if desired_index==n then play_index(n) end end)
        elseif not permanent then
            set_engine_status("error","YouTube stream resolution failed across multiple tracks. Resolver routes are cooling down; check the connection/runtime, then press Next or Play to retry.",i)
        end
    end

    local function attempt()
        local mode=routes[route_pos] or "no-js"
        local use_js=(mode=="js")
        local timeout_seconds=use_js and 8 or 4
        local a=ytdlp_fast_common(use_js)
        table.insert(a,"--format");table.insert(a,audio_selector())
        table.insert(a,"--get-url")
        table.insert(a,urls[i])
        if not startup_first_sound then write_startup_flight("stream_route",i,"Trying "..mode.." direct-stream resolver ("..timeout_seconds.."s ceiling).",false,false) end
        log("STREAM ROUTE track "..i.." "..mode.." timeout "..timeout_seconds.."s")
        run_bounded(cache_priority,ytdlp,a,timeout_seconds,true,function(success,result,error_text,timed_out)
            if not stream_resolving[i] then return end
            local raw=tostring(result.stdout or "")
            local direct=raw:match("([^\r\n]+)") or ""
            direct=direct:match("^%s*(.-)%s*$") or ""
            if success and tonumber(result.status or 0)==0 and direct:match("^https?://") then
                stream_route_mark(mode,true,false)
                stream_resolving[i]=nil
                stream_failures[i]=nil
                stream_consecutive_failures=0
                if desired_index==i and playing_index~=i then
                    current_index=i;requested_index=i;playing_from_cache=false
                    set_engine_status("starting","Starting streamed track "..i.."...",i)
                    write_current(i);write_queue_runtime()
                    if not startup_first_sound then write_startup_flight("loadfile_stream",i,"Direct audio stream handed to mpv via "..mode.." resolver.",false,false) end
                    mp.commandv("loadfile",direct,"replace")
                    arm_load_watchdog(i,"direct")
                    enqueue("audio",i,0);pump()
                else
                    enqueue("audio",i,20);pump()
                end
                log("STREAM READY track "..i.." "..mode)
                return
            end
            stream_route_mark(mode,false,timed_out)
            local diagnostic="["..mode.."] "..tostring(result.stderr or "").." "..tostring(error_text or "")
            table.insert(stderr_parts,diagnostic)
            if timed_out then timed_out_any=true end
            route_pos=route_pos+1
            if route_pos<=#routes then
                if desired_index==i and not startup_first_sound then write_startup_flight("stream_retry",i,"Direct resolver did not complete; trying alternate route.",false,false) end
                safe_timeout(0.05,attempt)
            else
                finish_failure()
            end
        end)
    end
    attempt()
end

function promote_art_source_pass(i,found)
    if not found or not exists(found) or fsize(found)<=0 then return false end
    local ext=(tostring(found):match("%.([%w]+)$") or ""):lower()
    if ext~="jpg" and ext~="jpeg" and ext~="png" and ext~="webp" then return false end
    local final=artwork_dir.."\\track-"..i.."."..ext
    for _,old_ext in ipairs({"jpg","jpeg","png","webp"}) do
        local old=artwork_dir.."\\track-"..i.."."..old_ext
        if old~=final then os.remove(old) end
    end
    os.remove(final)
    local moved=os.rename(found,final)~=nil
    if not moved then moved=copy_file_atomic(found,final) end
    if moved and mark_optional_source_pass("art",i) then
        clear_optional_failure("art",i);log("ART READY SOURCE-PASS track "..i.." ext="..ext);return true
    end
    return false
end

local function art_job(job)
    local i=job.i
    if optional_ready("art",i) then clear_optional_failure("art",i);job_done(job);return end
    clear_optional_failure("art",i);clear_optional_validation("art",i)
    local prefix="track-"..i..".focused-art"
    cleanup_prefix(artwork_dir,prefix)
    local a=ytdlp_common()
    table.insert(a,"--skip-download");table.insert(a,"--write-thumbnail")
    table.insert(a,"--output");table.insert(a,artwork_dir.."\\"..prefix)
    table.insert(a,urls[i])
    log("ART START track "..i)
    run_bounded(cache_priority,ytdlp,a,45,false,function(success,result,error_text,timed_out)
        local found=nil
        for _,name in ipairs(utils.readdir(artwork_dir,"files") or {}) do
            if name:sub(1,#prefix)==prefix and not name:match("%.part$") then
                local p=artwork_dir.."\\"..name
                if fsize(p)>0 then found=p;break end
            end
        end
        if not (success and tonumber(result.status or 0)==0 and found) then
            local summary=media_error_summary(result,error_text,timed_out)
            local permanent=permanent_error(tostring(result.stderr or "").." "..tostring(error_text or ""))
            if permanent then
                write_all(status_path(i,"audio.permanent"),"1")
                log("ART PERMANENT SOURCE track "..i.." "..summary)
            end
            cleanup_prefix(artwork_dir,prefix);mark_optional_failure("art",i,permanent and "permanent-source" or "download");log("ART OPTIONAL FAIL track "..i.." "..summary);job_done(job);return
        end
        if not ffmpeg_available then
            if not promote_art_source_pass(i,found) then mark_optional_failure("art",i,"source-pass") end
            cleanup_prefix(artwork_dir,prefix);job_done(job);return
        end
        local normalized=artwork_dir.."\\track-"..i..".focused-art-normalized.png"
        os.remove(normalized)
        run_ffmpeg_media({"-y","-hide_banner","-loglevel","error","-i",found,"-frames:v","1","-vf","format=rgba",normalized},25,normalized,function(ok,probe,probe_error,probe_timeout,direct_fallback)
            local final=artwork_dir.."\\track-"..i..".png"
            if ok and tonumber(probe.status or 0)==0 and exists(normalized) and fsize(normalized)>0 then
                for _,ext in ipairs({"jpg","jpeg","png","webp"}) do os.remove(artwork_dir.."\\track-"..i.."."..ext) end
                os.rename(normalized,final)
                if mark_optional_validated("art",i) then clear_optional_failure("art",i);log("ART READY+DECODED track "..i)
                else mark_optional_failure("art",i,"validate");log("ART VALIDATION FAIL track "..i) end
            elseif not promote_art_source_pass(i,found) then
                os.remove(normalized);mark_optional_failure("art",i,"decode");log("ART DECODE+SOURCE FAIL track "..i)
            end
            cleanup_prefix(artwork_dir,prefix);job_done(job)
        end)
    end)
end

local function video_quality_rank(quality)
    quality=tostring(quality or "")
    if quality:find("Best",1,true) then return 10000 end
    local h=tonumber(quality:match("(%d+)%s*p")) or 0
    return h
end

local function effective_video_quality()
    local overlay=tostring(cfg.overlay_video_quality or "240p")
    if player_video_enabled and overlay_video_enabled then
        return video_quality_rank(player_video_quality)>=video_quality_rank(overlay) and player_video_quality or overlay
    elseif player_video_enabled then
        return player_video_quality
    end
    return overlay
end

local function video_formats()
    local quality=effective_video_quality()
    if quality:find("240p",1,true) then
        return {"bestvideo[height=240][ext=mp4]/133","18","bestvideo[height=144][ext=mp4]/160"}
    elseif quality:find("360p",1,true) then
        return {"18/bestvideo[height=360][ext=mp4]/134","bestvideo[height=240][ext=mp4]/133","bestvideo[height=144][ext=mp4]/160"}
    elseif quality:find("480p",1,true) then
        return {"bestvideo[height=480][ext=mp4]/135","18","bestvideo[height=240][ext=mp4]/133","bestvideo[height=144][ext=mp4]/160"}
    elseif quality:find("720p",1,true) then
        return {"22/bestvideo[height=720][ext=mp4]/136","18","bestvideo[height=240][ext=mp4]/133","bestvideo[height=144][ext=mp4]/160"}
    elseif quality:find("Best",1,true) then
        return {"bestvideo[ext=mp4]/best[ext=mp4]","18","bestvideo[height=240][ext=mp4]/133","bestvideo[height=144][ext=mp4]/160"}
    elseif quality:find("144p",1,true) then
        return {"bestvideo[height=144][ext=mp4]/160","18"}
    end
    return {"bestvideo[height=144][ext=mp4]/160","18"}
end

local function video_job(job)
    local i=job.i
    local legacy_ready=legacy_video_validation_ready(i)
    if optional_ready("video",i) and not legacy_ready then job_done(job);return end
    local quality=effective_video_quality()
    local formats=video_formats()
    local temp=video_dir.."\\track-"..i..".focused-video.mp4"
    local normalized=video_dir.."\\track-"..i..".focused-video-normalized.mp4"
    local legacy_backup=video_dir.."\\track-"..i..".legacy-video-backup.mp4"
    clear_optional_failure("video",i)

    -- R61.85: do not redownload good legacy cache merely because its codec predates the
    -- native MediaElement path. Ahead-of-current jobs migrate those bytes to H.264 once;
    -- the current track stays presentation-ready and is never held behind transcoding.
    -- The original survives until replacement is proven; a failed migration becomes a
    -- new source-pass marker so the same file never enters an infinite conversion loop.
    if legacy_ready then
        if not ffmpeg_available then
            if mark_optional_source_pass("video",i) then
                log("VIDEO LEGACY NATIVE KEEP track "..i.." reason ffmpeg-unavailable")
                job_done(job);return
            end
        else
            os.remove(normalized);os.remove(legacy_backup)
            log("VIDEO LEGACY NATIVE MIGRATE START track "..i)
            local migrate={"-y","-hide_banner","-loglevel","error","-i",video_path(i),"-map","0:v:0","-an","-sn","-dn","-c:v","libx264","-preset","ultrafast","-tune","fastdecode","-crf","20","-pix_fmt","yuv420p","-g","60","-bf","0","-fps_mode","passthrough","-movflags","+faststart","-threads","1",normalized}
            run_ffmpeg_media(migrate,180,normalized,function(ok,vr,ve,vt,direct_fallback)
                if ok and tonumber(vr.status or 0)==0 and exists(normalized) and fsize(normalized)>0 then
                    local backed=os.rename(video_path(i),legacy_backup)~=nil
                    if backed then
                        local replaced=os.rename(normalized,video_path(i))~=nil
                        if replaced and mark_optional_validated("video",i) then
                            os.remove(legacy_backup);clear_optional_failure("video",i)
                            log("VIDEO LEGACY NATIVE MIGRATE READY track "..i)
                            job_done(job);return
                        end
                        os.remove(video_path(i));os.rename(legacy_backup,video_path(i))
                    end
                end
                os.remove(normalized)
                if mark_optional_source_pass("video",i) then
                    clear_optional_failure("video",i);log("VIDEO LEGACY NATIVE KEEP track "..i.." reason migrate-failed")
                    job_done(job);return
                end
                clear_optional_validation("video",i)
                safe_timeout(0.2,function() video_job(job) end)
            end)
            return
        end
    end

    clear_optional_validation("video",i)
    log("VIDEO START track "..i.." quality "..quality)
    local route=1
    local function source_pass()
        if not (exists(temp) and fsize(temp)>0) then return false end
        os.remove(video_path(i))
        local moved=os.rename(temp,video_path(i))~=nil
        if not moved then moved=copy_file_atomic(temp,video_path(i)) end
        if moved and mark_optional_source_pass("video",i) then
            clear_optional_failure("video",i);log("VIDEO READY SOURCE-PASS track "..i.." route "..route.." quality "..quality);return true
        end
        return false
    end
    local function attempt()
        os.remove(temp);os.remove(normalized)
        local a=ytdlp_common()
        table.insert(a,"--format");table.insert(a,formats[route])
        table.insert(a,"--no-part");table.insert(a,"--output");table.insert(a,temp);table.insert(a,urls[i])
        run_bounded(cache_priority,ytdlp,a,120,false,function(success,result,error_text,timed_out)
            if success and tonumber(result.status or 0)==0 and exists(temp) and fsize(temp)>0 then
                if not ffmpeg_available then
                    if source_pass() then job_done(job);return end
                else
                    local trans={"-y","-hide_banner","-loglevel","error","-i",temp,"-map","0:v:0","-an","-sn","-dn","-c:v","libx264","-preset","ultrafast","-tune","fastdecode","-crf","20","-pix_fmt","yuv420p","-g","60","-bf","0","-fps_mode","passthrough","-movflags","+faststart","-threads","1",normalized}
                    run_ffmpeg_media(trans,180,normalized,function(ok,vr,ve,vt,direct_fallback)
                        if ok and tonumber(vr.status or 0)==0 and exists(normalized) and fsize(normalized)>0 then
                            os.remove(video_path(i));os.rename(normalized,video_path(i));os.remove(temp)
                            if mark_optional_validated("video",i) then clear_optional_failure("video",i);log("VIDEO READY+DECODED track "..i.." route "..route.." quality "..quality);job_done(job);return end
                        end
                        os.remove(normalized)
                        if source_pass() then job_done(job);return end
                        os.remove(temp);route=route+1
                        if route<=#formats then safe_timeout(0.2,attempt) else mark_optional_failure("video",i,"decode");log("VIDEO OPTIONAL FAIL track "..i);job_done(job) end
                    end)
                    return
                end
            end
            local summary=media_error_summary(result,error_text,timed_out)
            local permanent=permanent_error(tostring(result.stderr or "").." "..tostring(error_text or ""))
            log("VIDEO ROUTE FAIL track "..i.." route "..route.." "..summary)
            os.remove(temp);os.remove(normalized)
            if permanent then
                write_all(status_path(i,"audio.permanent"),"1")
                mark_optional_failure("video",i,"permanent-source")
                log("VIDEO PERMANENT SOURCE track "..i.." route "..route)
                job_done(job);return
            end
            route=route+1
            if route<=#formats then safe_timeout(0.2,attempt)
            else mark_optional_failure("video",i,"routes");log("VIDEO OPTIONAL FAIL track "..i);job_done(job) end
        end)
    end
    attempt()
end

function visualizer_audio_prefix()
    local activity=tostring(cfg.visualizer_activity or "Active")
    local gain=activity=="Subtle" and 1 or (activity=="Normal" and 3 or 5)
    local fill=tostring(cfg.visualizer_adaptive_fill or "Adaptive")
    if fill=="Aggressive" then gain=gain+3 elseif fill=="Off" then gain=math.max(0,gain-2) end
    local trim=math.max(0,math.min(60,tonumber(cfg.visualizer_high_frequency_trim) or 0))
    local lift=math.max(0,math.min(12,tonumber(cfg.visualizer_high_frequency_lift_db) or 4))
    local chain={"highpass=f=30"}
    if trim>0 then
        local cutoff=math.max(3500,math.floor(20000*(1-trim/100)+0.5))
        table.insert(chain,"lowpass=f="..tostring(cutoff))
    end
    if lift>0 then table.insert(chain,"highshelf=f=4200:g="..tostring(lift)) end
    if gain>0 then table.insert(chain,"volume="..tostring(gain).."dB") end
    return table.concat(chain,",")
end

function visualizer_frequency_parameters()
    local activity=tostring(cfg.visualizer_activity or "Active")
    local averaging,win=1,1024
    if activity=="Subtle" then averaging,win=4,2048 elseif activity=="Normal" then averaging,win=2,1024 end
    local fill=tostring(cfg.visualizer_adaptive_fill or "Adaptive")
    local ascale=fill=="Off" and "sqrt" or (fill=="Aggressive" and "log" or "cbrt")
    local fscale=tostring(cfg.visualizer_frequency_scale or "Logarithmic")=="Linear" and "lin" or "log"
    return averaging,win,ascale,fscale
end

function visualizer_color()
    local color=tostring(cfg.visualizer_solid_color or "#8A8A84")
    if not color:match("^#%x%x%x%x%x%x$") then color="#8A8A84" end
    return "0x"..color:sub(2)
end

function visualizer_binary_filter()
    local color=tostring(cfg.visualizer_solid_color or "#8A8A84")
    if not color:match("^#%x%x%x%x%x%x$") then color="#8A8A84" end
    local r=tonumber(color:sub(2,3),16) or 138
    local g=tonumber(color:sub(4,5),16) or 138
    local b=tonumber(color:sub(6,7),16) or 132
    return ",format=rgb24,lutrgb=r='if(gt(val,3),"..r..",0)':g='if(gt(val,3),"..g..",0)':b='if(gt(val,3),"..b..",0)'"
end

function visualizer_spacing_filter(shape,spacing,w)
    if spacing=="None" then return "" end
    if shape=="Oscilloscope" or shape=="Dots" or shape=="Particle Field" or shape=="Skyline" or shape=="Twin Rails" then return "" end
    local cell=spacing=="Wide" and 7 or 4
    if w<48 then cell=spacing=="Wide" and 5 or 3 end
    return ",drawgrid=w="..tostring(cell)..":h=100000:t=1:c=black:replace=1"
end

function viz_filter()
    local w,h=visualizer_render_dimensions()
    local fps=visualizer_fps()
    local shape=tostring(cfg.visualizer_shape or "Spectrum")
    local spacing=tostring(cfg.visualizer_bar_spacing or "None")
    local direction=tostring(cfg.visualizer_direction or "Normal")
    local anchor=tostring(cfg.visualizer_vertical_anchor or "Source")
    local averaging,win,ascale,fscale=visualizer_frequency_parameters()
    local color=visualizer_color()
    local render_w=w
    if spacing=="Light" then render_w=math.max(8,math.floor(w/1.5)) elseif spacing=="Wide" then render_w=math.max(8,math.floor(w/2.5)) end
    if render_w%2==1 then render_w=render_w+1 end
    local audio=visualizer_audio_prefix()
    local tail=""
    if render_w~=w then tail=tail..",scale="..w..":"..h..":flags=neighbor" end
    if direction=="Mirrored" then tail=tail..",hflip" end

    if shape=="Oscilloscope" then
        local mode=anchor=="Top" and "cline" or "line"
        local out="[0:a]"..audio..",showwaves=s="..render_w.."x"..h..":mode="..mode..":scale=cbrt:rate="..fps..":colors="..color
        if render_w~=w then out=out..",scale="..w..":"..h..":flags=neighbor" end
        if direction=="Mirrored" then out=out..",hflip" end
        return out..visualizer_binary_filter()..",format=yuv420p[v]"
    end

    local mode="bar"
    if shape=="Dots" or shape=="Particle Field" then mode="dot" elseif shape=="Skyline" or shape=="Twin Rails" then mode="line" end
    local mirror=(shape=="Center Mirror" or shape=="Twin Rails" or anchor=="Center")
    if mirror then
        local half=math.max(2,math.floor(h/2));if half%2==1 then half=half+1 end
        local base="[0:a]"..audio..",showfreqs=s="..render_w.."x"..half..":mode="..mode..":ascale="..ascale..":fscale="..fscale..":cmode=combined:rate="..fps..":colors="..color..":averaging="..averaging..":win_size="..win.."[base];"
        local post="[base]split=2[up][down];[up]vflip[top];[top][down]vstack=inputs=2"
        if render_w~=w then post=post..",scale="..w..":"..h..":flags=neighbor" end
        if direction=="Mirrored" then post=post..",hflip" end
        post=post..visualizer_spacing_filter(shape,spacing,w)
        return base..post..visualizer_binary_filter()..",format=yuv420p[v]"
    end

    local out="[0:a]"..audio..",showfreqs=s="..render_w.."x"..h..":mode="..mode..":ascale="..ascale..":fscale="..fscale..":cmode=combined:rate="..fps..":colors="..color..":averaging="..averaging..":win_size="..win
    if render_w~=w then out=out..",scale="..w..":"..h..":flags=neighbor" end
    if anchor=="Top" then out=out..",vflip" end
    if direction=="Mirrored" then out=out..",hflip" end
    out=out..visualizer_spacing_filter(shape,spacing,w)
    if shape=="Particle Field" then out=out..",gblur=sigma=0.35" end
    return out..visualizer_binary_filter()..",format=yuv420p[v]"
end

function viz_filter_compat()
    local w,h=visualizer_render_dimensions()
    return string.format("[0:a]showfreqs=s=%dx%d:mode=bar:ascale=cbrt:rate=%d:colors=%s",w,h,visualizer_fps(),visualizer_color())..visualizer_binary_filter()..",format=yuv420p[v]"
end

local function viz_job(job)
    local i=job.i
    if optional_ready("viz",i) then clear_optional_failure("viz",i);job_done(job);return end
    clear_optional_failure("viz",i);clear_optional_validation("viz",i)
    if not ffmpeg_available or not audio_ready(i) then job_done(job);return end
    local temp=visualizer_dir.."\\track-"..i..".focused-processing.mp4"
    os.remove(temp)
    local fps=visualizer_fps()
    local profile=visualizer_profile()
    local function render_args(filter)
        return {"-y","-hide_banner","-loglevel","error","-i",audio_path(i),"-filter_complex",filter,"-map","[v]","-an","-r",tostring(fps),"-fps_mode","cfr","-c:v","libx264","-preset","ultrafast","-tune","fastdecode","-qp","0","-pix_fmt","yuv420p","-g","1","-keyint_min","1","-sc_threshold","0","-bf","0","-movflags","+faststart","-threads","1","-filter_complex_threads","1",temp}
    end
    local function validate_render()
        run_bounded(cache_priority,ffmpeg,{"-hide_banner","-loglevel","error","-i",temp,"-frames:v","1","-f","null","NUL"},25,false,function(ok,probe,probe_error,probe_timeout)
            if ok and tonumber(probe.status or 0)==0 then
                os.remove(viz_path(i));os.rename(temp,viz_path(i));write_all(status_path(i,"visualizer.profile"),profile)
                if mark_optional_validated("viz",i) then clear_optional_failure("viz",i);log("VISUALIZER READY+DECODED track "..i.." "..fps.."fps")
                else os.remove(status_path(i,"visualizer.profile"));mark_optional_failure("viz",i,"validate") end
            else
                os.remove(temp);os.remove(status_path(i,"visualizer.profile"));mark_optional_failure("viz",i,"decode");log("VISUALIZER DECODE FAIL track "..i.." "..media_error_summary(probe,probe_error,probe_timeout))
            end
            job_done(job)
        end)
    end
    local function compatibility_render()
        os.remove(temp)
        log("VISUALIZER COMPAT RETRY track "..i)
        run_ffmpeg_media(render_args(viz_filter_compat()),180,temp,function(ok,result,error_text,timed_out,direct_fallback)
            if ok and tonumber(result.status or 0)==0 and exists(temp) and fsize(temp)>0 then validate_render();return end
            os.remove(temp);os.remove(status_path(i,"visualizer.profile"));mark_optional_failure("viz",i,"render");log("VISUALIZER OPTIONAL FAIL track "..i);job_done(job)
        end)
    end
    log("VISUALIZER START track "..i.." "..fps.."fps profile "..profile)
    run_ffmpeg_media(render_args(viz_filter()),180,temp,function(success,result,error_text,timed_out,direct_fallback)
        if success and tonumber(result.status or 0)==0 and exists(temp) and fsize(temp)>0 then validate_render();return end
        compatibility_render()
    end)
end

local function start_job(job)
    queued[job.key]=nil;active[job.key]=job;active_count=active_count+1
    if job.kind~="audio" then optional_active_count=optional_active_count+1 end
    if job.kind=="audio" then audio_job(job)
    elseif job.kind=="art" then art_job(job)
    elseif job.kind=="video" then video_job(job)
    elseif job.kind=="viz" then viz_job(job)
    else job_done(job) end
end

pump=function()
    if #jobs==0 then return end
    table.sort(jobs,function(a,b) if a.priority==b.priority then return a.i<b.i end return a.priority<b.priority end)
    while active_count<workers and #jobs>0 do
        local pick=1
        if optional_active_count>=optional_worker_limit and jobs[pick].kind~="audio" then
            pick=nil
            for n,candidate in ipairs(jobs) do if candidate.kind=="audio" then pick=n;break end end
            if not pick then break end
        end
        local job=table.remove(jobs,pick)
        if queued[job.key] then start_job(job) end
    end
end

request_bundle=function(i,priority)
    if not i or i<1 or i>#urls or known_bad(i) then return end
    local p=tonumber(priority) or 10
    if not audio_ready(i) then enqueue("audio",i,p) end
    local is_current=(i==playing_index or i==desired_index)
    if (configured_art or controller_want_art) and not optional_ready("art",i) and not optional_failure_blocked("art",i) then enqueue("art",i,p+(is_current and 4 or 35)) end
    local video_ready_now=optional_ready("video",i)
    local video_migrate_ahead=(not is_current) and legacy_video_validation_ready(i)
    if (configured_video or controller_want_video) and (not video_ready_now or video_migrate_ahead) and not optional_failure_blocked("video",i) then enqueue("video",i,p+(is_current and 6 or 45)) end
    -- The visualizer is the only optional media lane that actually requires cached audio.
    if audio_ready(i) and (configured_viz or controller_want_viz) and not optional_ready("viz",i) and not optional_failure_blocked("viz",i) then enqueue("viz",i,p+(is_current and 8 or 40)) end
end

local function schedule_ahead(i)
    prune_optional_cache(i,false)
    local cursor=i
    for n=1,prefetch_ahead do
        local next_i=next_occurrence(cursor,1)
        if not next_i or next_i==i then break end
        request_bundle(next_i,n)
        cursor=next_i
    end
    if i then request_bundle(i,0) end
    pump()
end

play_index=function(i)
    i=math.floor(tonumber(i) or 0)
    if i<1 or i>#urls then return end
    if known_bad(i) then
        local n=next_occurrence(i,1)
        if n then desired_index=n;requested_index=0;play_index(n) end
        return
    end
    desired_index=i
    current_index=i
    write_all(resume_file,tostring(i))
    if not startup_first_sound then write_startup_flight("cache_check",i,"Checking cache identity for the requested track.",false,false) end
    if not audio_ready(i) then
        requested_index=0
        set_engine_status("preparing","Preparing fast start for track "..i.."...",i)
        write_queue_runtime();start_fast_stream(i);return
    end
    requested_index=i
    playing_from_cache=true
    set_engine_status("starting","Starting track "..i.."...",i)
    write_current(i);write_queue_runtime()
    if not startup_first_sound then write_startup_flight("loadfile_cache",i,"Cached audio handed to mpv.",false,false) end
    mp.commandv("loadfile",audio_path(i),"replace")
    arm_load_watchdog(i,"cache")
end

cancel_pending_transport=function()
    transport_serial=transport_serial+1
    transport_pending_target=0
    transport_pending_attempts=0
    if transport_timer then transport_timer:kill();transport_timer=nil end
end

local function peek_pending_transport_target()
    local n=math.floor(tonumber(transport_pending_target) or 0)
    return n>0 and n or nil
end

local function acknowledge_pending_transport(i)
    local n=peek_pending_transport_target()
    if n and n==math.floor(tonumber(i) or 0) then
        log("TRANSPORT ACK track "..n)
        cancel_pending_transport()
        return true
    end
    return false
end

local function commit_pending_transport(serial)
    if serial~=transport_serial then return end
    local n=peek_pending_transport_target()
    transport_timer=nil
    if not n then return end
    transport_pending_attempts=transport_pending_attempts+1
    desired_index=n;requested_index=0;work_generation=work_generation+1
    log("TRANSPORT COMMIT track "..n.." attempt "..transport_pending_attempts)
    play_index(n)
end

local function advance(step)
    local direction=(tonumber(step) or 1)>=0 and 1 or -1
    local base=(transport_pending_target>0 and transport_pending_target) or (desired_index>0 and desired_index) or (playing_index>0 and playing_index) or current_index
    local n=next_occurrence(base,direction)
    if not n then
        log("TRANSPORT boundary "..(direction>0 and "next" or "previous").." from track "..tostring(base))
        return
    end
    desired_index=n
    transport_pending_target=n
    transport_pending_attempts=0
    transport_serial=transport_serial+1
    local serial=transport_serial
    if transport_timer then transport_timer:kill() end
    set_engine_status("advancing","Moving to track "..n.."...",n)
    write_queue_runtime()
    transport_timer=safe_timeout(transport_settle_seconds,function() commit_pending_transport(serial) end)
end

local order_undo={}
local order_redo={}
local original_order={}
for _,v in ipairs(order) do table.insert(original_order,v) end

local function clone_order(src)
    local out={};for _,v in ipairs(src) do table.insert(out,v) end;return out
end

local function orders_equal(a,b)
    if #a~=#b then return false end
    for i=1,#a do if a[i]~=b[i] then return false end end
    return true
end

local function rebuild_order_position()
    order_position={};for slot,occ in ipairs(order) do order_position[occ]=slot end
end

local function find_order_slot(occ)
    for slot,value in ipairs(order) do if value==occ then return slot end end
    return 0
end

local function write_order_projection(reason,status)
    rebuild_order_position()
    write_json(order_file,{
        schema=3,session_id=session_id,revision=order_revision,command_serial=order_command_serial,
        last_command_status=status or "committed",last_command_reason=reason or "focused-r19",
        order=order,inserted={},reason=reason or "focused-r19",slot_id=active_slot_id,slot_name=active_slot_name
    })
    write_queue_runtime()
    prune_optional_cache((playing_index>0 and playing_index) or desired_index or current_index,true)
end

local function publish_order_receipt(reason,status)
    order_command_serial=order_command_serial+1
    write_order_projection(reason,status or "no-op")
end

local function save_order(reason)
    order_revision=order_revision+1
    order_command_serial=order_command_serial+1
    write_order_projection(reason,"committed")
end

local function mutate_order(mutator,reason)
    local before=clone_order(order)
    mutator()
    if orders_equal(before,order) then
        rebuild_order_position()
        publish_order_receipt(reason,"no-op")
        return false
    end
    table.insert(order_undo,before);if #order_undo>20 then table.remove(order_undo,1) end
    order_redo={}
    save_order(reason)
    return true
end

local function move_occurrence(occ,target_slot)
    local from=find_order_slot(occ);if from<1 then return false end
    table.remove(order,from)
    target_slot=math.max(1,math.min(#order+1,target_slot))
    table.insert(order,target_slot,occ);return true
end

local function parse_occurrence_csv(raw,exclude_current)
    local wanted={}
    local seen={}
    local current=(playing_index>0 and playing_index) or current_index
    local invalid=false
    for token in tostring(raw or ""):gmatch("[^,]+") do
        local occ=math.floor(tonumber(token) or 0)
        if occ<=0 or seen[occ] or find_order_slot(occ)<=0 or (exclude_current and occ==current) then
            invalid=true
        else
            seen[occ]=true;table.insert(wanted,occ)
        end
    end
    if #wanted==0 then invalid=true end
    return wanted,invalid
end

local function selected_in_order(wanted)
    local set={};for _,occ in ipairs(wanted) do set[occ]=true end
    local out={};for _,occ in ipairs(order) do if set[occ] then table.insert(out,occ) end end
    return out,set
end

local function move_occurrence_block(wanted,target_boundary)
    local block,set=selected_in_order(wanted)
    if #block==0 then return false end
    local remaining={}
    local removed_before=0
    target_boundary=math.max(1,math.min(#order+1,math.floor(tonumber(target_boundary) or 1)))
    for slot,occ in ipairs(order) do
        if set[occ] then
            if slot<target_boundary then removed_before=removed_before+1 end
        else table.insert(remaining,occ) end
    end
    local adjusted=math.max(1,math.min(#remaining+1,target_boundary-removed_before))
    for i=#block,1,-1 do table.insert(remaining,adjusted,block[i]) end
    order=remaining
    return true
end

safe_register_script_message("yomi-next",function() advance(1) end)
safe_register_script_message("yomi-prev",function() advance(-1) end)
safe_register_script_message("yomi-jump",function(raw) local n=math.floor(tonumber(raw) or 0);if n>=1 and n<=#urls then cancel_pending_transport();desired_index=n;requested_index=0;work_generation=work_generation+1;play_index(n) end end)
safe_register_script_message("yomi-playback-subset",function(raw,label)
    local seen={}
    local subset={}
    for token in tostring(raw or ""):gmatch("[^,]+") do
        local n=math.floor(tonumber(token) or 0)
        if n>=1 and n<=#urls and not seen[n] then seen[n]=true;table.insert(subset,n) end
    end
    if #subset<1 then return end
    playback_subset=subset
    playback_subset_active=true
    playback_subset_label=tostring(label or "")
    rebuild_playback_subset_position()
    local active=(playing_index>0 and playing_index) or current_index
    log("PLAYBACK SUBSET ON count="..tostring(#subset).." label="..playback_subset_label)
    if not playback_subset_position[active] then
        cancel_pending_transport();desired_index=subset[1];requested_index=0;work_generation=work_generation+1;play_index(subset[1])
    end
    write_queue_runtime()
end)
safe_register_script_message("yomi-playback-subset-clear",function()
    playback_subset={}
    playback_subset_position={}
    playback_subset_active=false
    playback_subset_label=""
    log("PLAYBACK SUBSET OFF")
    write_queue_runtime()
end)
safe_register_script_message("yomi-repeat-mode",function(raw)
    local v=tostring(raw or "all"):lower()
    if v=="off" or v=="all" or v=="one" then repeat_mode=v;persist_repeat();write_current((playing_index>0 and playing_index) or current_index);write_queue_runtime();log("REPEAT "..repeat_mode) end
end)
safe_register_script_message("yomi-repeat-cycle",function()
    repeat_mode=(repeat_mode=="all" and "one") or (repeat_mode=="one" and "off") or "all"
    persist_repeat();write_current((playing_index>0 and playing_index) or current_index);write_queue_runtime();log("REPEAT "..repeat_mode)
end)
safe_register_script_message("yomi-prepare",function(raw) local n=math.floor(tonumber(raw) or current_index);if n>=1 and n<=#urls then request_bundle(n,0);pump() end end)
local function controller_demand_bool(raw)
    local v=tostring(raw or ""):lower()
    return v=="1" or v=="true" or v=="on" or v=="yes"
end
local function publish_controller_demand(reason,token)
    controller_demand_serial=controller_demand_serial+1
    controller_demand_unix=os.time()
    if token~=nil then controller_demand_token=tostring(token or "") end
    local i=(playing_index>0 and playing_index) or current_index
    if i>0 and (controller_want_art or controller_want_video or controller_want_viz) then
        request_bundle(i,0);pump()
        log("OPTIONAL MEDIA KICK track="..tostring(i).." active="..tostring(active_count).." queued="..tostring(#jobs).." ffmpeg="..tostring(ffmpeg_available).." runner="..tostring(exists(runner)))
    end
    write_current(i)
    log("CONTROLLER DEMAND "..tostring(reason or "update").." art="..tostring(controller_want_art).." video="..tostring(controller_want_video).." viz="..tostring(controller_want_viz).." token="..controller_demand_token)
end
safe_register_script_message("yomi-controller-demand",function(raw_art,raw_video,raw_viz,raw_token)
    local next_art=controller_demand_bool(raw_art)
    local next_video=controller_demand_bool(raw_video)
    local next_viz=controller_demand_bool(raw_viz)
    local token=tostring(raw_token or "")
    local token_changed=token~="" and token~=controller_demand_token
    local i=(playing_index>0 and playing_index) or current_index
    if i>0 then
        if token_changed or (next_art and not controller_want_art) then clear_optional_failure("art",i) end
        if token_changed or (next_video and not controller_want_video) then clear_optional_failure("video",i) end
        if token_changed or (next_viz and not controller_want_viz) then clear_optional_failure("viz",i) end
    end
    controller_want_art=next_art
    controller_want_video=next_video
    controller_want_viz=next_viz
    publish_controller_demand("lease",raw_token)
end)
safe_register_script_message("yomi-controller-visualizer",function(raw)
    local next_viz=controller_demand_bool(raw)
    local i=(playing_index>0 and playing_index) or current_index
    if i>0 and next_viz and not controller_want_viz then clear_optional_failure("viz",i) end
    controller_want_viz=next_viz
    publish_controller_demand("legacy-viz",controller_demand_token)
end)
safe_register_script_message("yomi-controller-media",function(raw_art,raw_video)
    local next_art=controller_demand_bool(raw_art)
    local next_video=controller_demand_bool(raw_video)
    local i=(playing_index>0 and playing_index) or current_index
    if i>0 and next_art and not controller_want_art then clear_optional_failure("art",i) end
    if i>0 and next_video and not controller_want_video then clear_optional_failure("video",i) end
    controller_want_art=next_art
    controller_want_video=next_video
    publish_controller_demand("legacy-media",controller_demand_token)
end)
safe_register_script_message("yomi-reload-config",function()
    local next_cfg=load_json(config_file) or {}
    local before_profile=visualizer_profile()
    apply_config(next_cfg)
    local after_profile=visualizer_profile()
    local i=(playing_index>0 and playing_index) or current_index
    if before_profile~=after_profile and i>0 then
        clear_optional_failure("viz",i)
    end
    if i>0 then request_bundle(i,0);schedule_ahead(i);pump();write_current(i) end
    write_queue_runtime()
    log("CONFIG RELOADED workers="..workers.." prefetch="..prefetch_ahead.." viz="..after_profile)
end)
safe_register_script_message("yomi-order-play-next",function(raw)
    local occ=math.floor(tonumber(raw) or 0)
    local current=(playing_index>0 and playing_index) or current_index
    local here=find_order_slot(current)
    local from=find_order_slot(occ)
    if occ>0 and occ~=current and here>0 and from>0 then
        -- R61.33 target semantics retained: if the requested occurrence is before
        -- current, its removal shifts current left before insertion.
        local target=here+1
        if from<here then target=here end
        mutate_order(function() move_occurrence(occ,target) end,"play-next")
    else
        publish_order_receipt("play-next-invalid","rejected")
    end
end)
safe_register_script_message("yomi-order-move-later",function(raw,delta)
    local occ=math.floor(tonumber(raw) or 0);local d=math.max(1,math.floor(tonumber(delta) or 1));local from=find_order_slot(occ)
    if from>0 then mutate_order(function() move_occurrence(occ,from+d) end,"move-later")
    else publish_order_receipt("move-later-invalid","rejected") end
end)
safe_register_script_message("yomi-order-move-block",function(raw,boundary)
    local wanted,invalid=parse_occurrence_csv(raw,true)
    local target=math.floor(tonumber(boundary) or 0)
    if invalid or target<1 then publish_order_receipt("move-block-invalid","rejected");return end
    mutate_order(function() move_occurrence_block(wanted,target) end,"move-block")
end)
safe_register_script_message("yomi-order-play-next-block",function(raw)
    local wanted,invalid=parse_occurrence_csv(raw,true)
    local current=(playing_index>0 and playing_index) or current_index
    local here=find_order_slot(current)
    if invalid or here<1 then publish_order_receipt("play-next-block-invalid","rejected");return end
    mutate_order(function() move_occurrence_block(wanted,here+1) end,"play-next-block")
end)
safe_register_script_message("yomi-order-move-later-many",function(raw,delta)
    local wanted,invalid=parse_occurrence_csv(raw,true)
    local d=math.max(1,math.min(25,math.floor(tonumber(delta) or 1)))
    if invalid then publish_order_receipt("move-later-many-invalid","rejected");return end
    local block=selected_in_order(wanted)
    mutate_order(function()
        for i=#block,1,-1 do
            local occ=block[i]
            local from=find_order_slot(occ)
            if from>0 then move_occurrence(occ,from+d) end
        end
    end,"move-later-many")
end)
safe_register_script_message("yomi-order-remove",function(raw)
    local occ=math.floor(tonumber(raw) or 0);local from=find_order_slot(occ)
    local current=(playing_index>0 and playing_index) or current_index
    if from>0 and #order>1 and occ~=current then mutate_order(function() table.remove(order,find_order_slot(occ)) end,"remove")
    else publish_order_receipt("remove-invalid","rejected") end
end)
safe_register_script_message("yomi-order-remove-block",function(raw)
    local wanted,invalid=parse_occurrence_csv(raw,true)
    if invalid then publish_order_receipt("remove-block-invalid","rejected");return end
    local _,set=selected_in_order(wanted)
    if #order-#wanted<1 then publish_order_receipt("remove-block-would-empty-order","rejected");return end
    mutate_order(function()
        local remaining={};for _,occ in ipairs(order) do if not set[occ] then table.insert(remaining,occ) end end;order=remaining
    end,"remove-block")
end)
safe_register_script_message("yomi-order-undo",function()
    local prev=table.remove(order_undo)
    if prev then
        table.insert(order_redo,clone_order(order));order=prev;save_order("undo")
    else publish_order_receipt("undo-empty","no-op") end
end)
safe_register_script_message("yomi-order-redo",function()
    local next_order=table.remove(order_redo)
    if next_order then
        table.insert(order_undo,clone_order(order));order=next_order;save_order("redo")
    else publish_order_receipt("redo-empty","no-op") end
end)
safe_register_script_message("yomi-order-restore",function()
    mutate_order(function() order=clone_order(original_order) end,"restore")
end)
safe_register_script_message("yomi-order-apply-sequence",function(raw)
    local proposed={};local seen={}
    for token in tostring(raw or ""):gmatch("[^,]+") do
        local occ=math.floor(tonumber(token) or 0)
        if occ>0 and not seen[occ] then seen[occ]=true;table.insert(proposed,occ) else publish_order_receipt("apply-sequence-invalid","rejected");return end
    end
    if #proposed~=#order then publish_order_receipt("apply-sequence-count-mismatch","rejected");return end
    for _,occ in ipairs(order) do if not seen[occ] then publish_order_receipt("apply-sequence-set-mismatch","rejected");return end end
    mutate_order(function() order=clone_order(proposed) end,"apply-sequence")
end)
safe_register_script_message("yomi-order-reshuffle-unprepared",function()
    local here=find_order_slot((playing_index>0 and playing_index) or current_index)
    if here<1 or here>=#order then publish_order_receipt("reshuffle-tail-empty","no-op");return end
    mutate_order(function()
        math.randomseed(os.time()+order_revision+order_command_serial)
        for n=#order,here+2,-1 do local j=math.random(here+1,n);order[n],order[j]=order[j],order[n] end
    end,"reshuffle-tail")
end)
safe_register_script_message("yomi-hide-comment",function() end)
safe_register_script_message("yomi-rehearse",function() end)
safe_register_script_message("yomi-freeze-toggle",function() end)

safe_register_event("file-loaded",function()
    local loaded=current_index
    local requested_before_load=requested_index
    cancel_load_watchdog()
    if loaded and loaded>0 then load_watchdog_failures[loaded]=nil end
    requested_index=0
    -- Any successfully loaded media proves the resolver/cache path is healthy again.
    stream_consecutive_failures=0
    if not startup_first_sound then write_startup_flight("file_loaded",current_index,"mpv loaded the requested media; waiting for playback.",false,false) end
    local pending=peek_pending_transport_target()
    if pending then
        if pending==current_index or pending==requested_before_load then
            acknowledge_pending_transport(pending)
        elseif transport_pending_attempts<transport_max_attempts then
            log("TRANSPORT supersede loaded track "..current_index.." -> "..pending)
            transport_serial=transport_serial+1
            local serial=transport_serial
            if transport_timer then transport_timer:kill() end
            transport_timer=safe_timeout(0.03,function() commit_pending_transport(serial) end)
            return
        else
            log("TRANSPORT target "..pending.." did not acknowledge after bounded retries; failing forward")
            cancel_pending_transport()
            local fallback=next_occurrence(current_index,1)
            if fallback and fallback~=current_index then desired_index=fallback;work_generation=work_generation+1;play_index(fallback);return end
        end
    end
    playing_index=current_index
    if slot_bookmark_pending then
        if slot_bookmark_occurrence==current_index and slot_bookmark_seconds>0.5 then
            mp.commandv("seek",tostring(slot_bookmark_seconds),"absolute+exact")
            log("SLOT BOOKMARK restore "..active_slot_name.." item "..current_index.." @ "..string.format("%.2f",slot_bookmark_seconds).."s")
        end
        slot_bookmark_pending=false
    end
    hydrate_supporting_objects(current_index)
    local gain=tonumber((read_all(gain_path(current_index)) or "0"):match("[%+%-]?[%d%.]+")) or 0
    mp.set_property_number("volume-gain",gain)
    local m=meta_for(current_index)
    set_engine_status("playing","Playing: "..m.title,current_index)
    write_current(current_index);write_queue_runtime();write_runtime_lease()
    append_all(history_file,(utils.format_json({unix=os.time(),index=current_index,title=m.title,channel=m.channel}) or "{}").."\n")
    schedule_ahead(current_index)
    log("PLAYING track "..current_index)
    if not startup_first_sound and mp.get_property_native("pause") == true then
        startup_first_sound=true
        write_startup_flight("media_ready_paused",current_index,"Requested media is loaded and intentionally paused.",true,false)
        log("FIRST SOUND CONTRACT ready-paused track "..current_index.." @ "..tostring(startup_elapsed_ms()).."ms")
    end
end)

safe_register_event("playback-restart",function()
    if startup_first_sound or shutting_down then return end
    if mp.get_property_native("pause") == true then return end
    local i=(playing_index>0 and playing_index) or current_index
    startup_first_sound=true
    write_startup_flight("first_sound",i,"Playback reached mpv playback-restart.",true,false)
    log("FIRST SOUND track "..tostring(i).." @ "..tostring(startup_elapsed_ms()).."ms")
end)

mp.observe_property("pause","bool",function(_,paused)
    if playing_index>0 then
        local m=meta_for(playing_index)
        set_engine_status(paused and "paused" or "playing",(paused and "Paused: " or "Playing: ")..m.title,playing_index)
    end
end)


local function defer_eof_play(target,label)
    target=math.floor(tonumber(target) or 0)
    if target<1 or target>#urls then return end
    desired_index=target
    work_generation=work_generation+1
    local generation=work_generation
    safe_timeout(0.01,function()
        if shutting_down or generation~=work_generation or desired_index~=target then return end
        log("EOF HANDOFF "..tostring(label or "next").." -> track "..target)
        play_index(target)
    end)
end

safe_register_event("end-file",function(e)
    cancel_load_watchdog()
    if shutting_down then return end
    local reason=tostring(e and e.reason or "")
    if reason=="eof" then
        local ended=playing_index>0 and playing_index or current_index
        playing_index=0;requested_index=0
        local pending=peek_pending_transport_target()
        if pending then
            set_engine_status("advancing","Track ended during transport; moving to requested track...",pending)
            defer_eof_play(pending,"requested")
        elseif repeat_mode=="one" then
            set_engine_status("advancing","Track ended; repeating current track...",ended)
            log("END track "..ended.." eof -> repeat one")
            defer_eof_play(ended,"repeat-one")
        else
            set_engine_status("advancing","Track ended; advancing...",ended)
            log("END track "..ended.." eof -> next")
            local n=next_occurrence(ended,1)
            if n then defer_eof_play(n,"next")
            else set_engine_status("complete","End of queue.",ended);write_current(ended);write_queue_runtime() end
        end
    elseif reason=="error" then
        local ended=playing_index>0 and playing_index or current_index
        local failed_from_cache=playing_from_cache
        playing_index=0;requested_index=0
        if not failed_from_cache then
            -- A direct fast-start stream can occasionally expire or reject a request.
            -- Keep the user's requested track authoritative and fall back to the durable
            -- cache path instead of silently skipping to another song.
            set_engine_status("preparing","Fast stream interrupted; finishing local cache for track "..ended.."...",ended)
            if audio_ready(ended) then defer_eof_play(ended,"error-cache-retry") else enqueue("audio",ended,0);pump() end
            return
        end
        os.remove(audio_path(ended))
        local pending=peek_pending_transport_target()
        if pending then
            set_engine_status("advancing","Playback error during transport; moving to requested track...",pending)
            log("END track "..ended.." error -> requested "..pending)
            defer_eof_play(pending,"error-requested")
        else
            set_engine_status("advancing","Playback error; advancing...",ended)
            log("END track "..ended.." error -> next")
            local n=next_occurrence(ended,1)
            if n then defer_eof_play(n,"error-next") else set_engine_status("complete","End of queue.",ended);write_current(ended);write_queue_runtime() end
        end
    end
end)

safe_register_event("shutdown",function()
    shutting_down=true
    cancel_load_watchdog()
    if cancel_pending_transport then cancel_pending_transport() end
    if not startup_first_sound then write_startup_flight("shutdown_before_first_sound",(playing_index>0 and playing_index) or current_index,"Playback engine stopped before first sound confirmation.",false,false,startup_last_stage) end
    write_active_slot_bookmark()
    write_all(resume_file,tostring(current_index))
    os.remove(runtime_lease_file)
    set_engine_status("stopped","YOMI stopped.",current_index)
end)

function apply_startup_volume()
    local ui=load_json(controller_ui_file) or {}
    local volume=tonumber(ui.volume)
    if volume==nil then volume=100 end
    volume=math.max(0,math.min(130,volume))
    local muted=ui.muted==true
    local ok_max=pcall(mp.set_property_number,"volume-max",130)
    local ok_volume=pcall(mp.set_property_number,"volume",volume)
    local ok_mute=pcall(mp.set_property_native,"mute",muted)
    log("STARTUP VOLUME preplay="..tostring(volume).." muted="..tostring(muted).." applied="..tostring(ok_max and ok_volume and ok_mute))
end

ensure_projection_files()
persist_repeat()
apply_startup_volume()
write_runtime_lease()
write_startup_flight("runtime_ready",current_index,"Runtime lease armed; scheduling first play request.",false,false)
safe_periodic_timer(5,write_runtime_lease)
safe_periodic_timer(1,function()
    if shutting_down or startup_first_sound or startup_deadline_reported then return end
    if startup_elapsed_ms() >= startup_deadline_seconds*1000 then
        startup_deadline_reported=true
        local waiting=startup_last_stage
        write_startup_flight("deadline_exceeded",(playing_index>0 and playing_index) or current_index,"First sound has not been confirmed within the startup deadline.",false,true,waiting)
        log("FIRST SOUND DEADLINE exceeded @ "..tostring(startup_elapsed_ms()).."ms waiting="..tostring(waiting))
    end
end)
safe_periodic_timer(5,write_active_slot_bookmark)
safe_periodic_timer(2,function()
    local i=(playing_index>0 and playing_index) or current_index
    if i>0 then write_current(i);write_queue_runtime() end
end)
if slot_restore_paused then mp.set_property_native("pause",true) end
log("SLOT ACTIVE "..(active_slot_name~="" and active_slot_name or "Main").." ["..active_slot_id.."]"..(prewarm_start and " prewarm-paused" or (slot_restore_paused and " paused" or (explicit_play_start and " explicit-play" or ""))))
set_engine_status("starting","Preparing focused playback...",current_index)
safe_timeout(0.10,function() play_index(current_index) end)
