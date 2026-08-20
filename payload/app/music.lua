local mp = require 'mp'
local utils = require 'mp.utils'

local install_root = os.getenv("YOMI_INSTALL_ROOT")
if not install_root or install_root == "" then
    local program_files = os.getenv("ProgramFiles") or "C:\\Program Files"
    install_root = program_files .. "\\YOMI"
end

local localapp = os.getenv("LOCALAPPDATA") or "."
local data_root = localapp .. "\\YOMI"

local config_file = data_root .. "\\config.json"
local playlist_file = data_root .. "\\playlist.txt"
local resume_file = data_root .. "\\state\\resume-track.txt"
local current_file = data_root .. "\\state\\current.json"
local engine_status_file = data_root .. "\\state\\engine-status.json"
local history_file = data_root .. "\\state\\history.jsonl"

local audio_dir = data_root .. "\\cache\\audio"
local artwork_dir = data_root .. "\\cache\\artwork"
local video_dir = data_root .. "\\cache\\video"
local visualizer_dir = data_root .. "\\cache\\visualizer"
local meta_dir = data_root .. "\\cache\\meta"
local gain_dir = data_root .. "\\cache\\gain"
local status_dir = data_root .. "\\cache\\status"
local comment_dir = data_root .. "\\cache\\comments"
local telemetry_dir = data_root .. "\\cache\\telemetry"

local ytdlp = install_root .. "\\runtime\\yt-dlp\\yt-dlp.exe"
local ffmpeg = install_root .. "\\runtime\\ffmpeg\\ffmpeg.exe"
local ffmpeg_dir = install_root .. "\\runtime\\ffmpeg"
local runner = install_root .. "\\app\\PriorityRun.exe"
local deno = install_root .. "\\runtime\\deno\\deno.exe"

local function read_all(path)
    local f = io.open(path, "rb")
    if not f then return nil end
    local s = f:read("*a")
    f:close()
    return s
end

local function write_all(path, text)
    local temp = path .. ".tmp"
    local f = io.open(temp, "wb")
    if not f then return false end
    f:write(text or "")
    f:close()
    os.remove(path)
    return os.rename(temp, path) ~= nil
end

local function exists(path)
    local f = io.open(path, "rb")
    if f then f:close(); return true end
    return false
end

local function fsize(path)
    local f = io.open(path, "rb")
    if not f then return 0 end
    local n = f:seek("end") or 0
    f:close()
    return n
end

local function load_json(path)
    local s = read_all(path)
    if not s then return nil end

    -- Windows PowerShell 5.1 writes UTF-8 with a BOM by default.
    -- mp.utils.parse_json may reject that prefix, which made RC3.2 silently
    -- behave as if config.json were empty. Strip it defensively.
    if s:sub(1,3) == string.char(239,187,191) then
        s = s:sub(4)
    end

    local ok, value = pcall(utils.parse_json, s)
    if ok and value then return value end

    mp.msg.error("YOMI CONFIG PARSE FAILED: " .. tostring(path))
    return nil
end

local cfg = load_json(config_file) or {}
if cfg.app_mode == nil then cfg.app_mode = "Streamer / OBS" end
if cfg.player_video_quality == nil then cfg.player_video_quality = "Off (audio only)" end
if cfg.artwork_enabled == nil then cfg.artwork_enabled = true end
if cfg.smart_artwork_crop == nil then cfg.smart_artwork_crop = true end
if cfg.video_enabled == nil then cfg.video_enabled = true end
if cfg.visualizer_enabled == nil then cfg.visualizer_enabled = true end
if cfg.title_enabled == nil then cfg.title_enabled = true end
if cfg.channel_enabled == nil then cfg.channel_enabled = true end
if cfg.loudness_normalization == nil then cfg.loudness_normalization = true end
if cfg.director_mode == nil then cfg.director_mode = false end
if cfg.overlay_video_quality == nil then cfg.overlay_video_quality = "144p (fastest)" end
if cfg.video_preference == nil then cfg.video_preference = "Prefer selected maximum" end
if cfg.video_fps == nil then cfg.video_fps = "30 FPS" end
if cfg.audio_quality == nil then cfg.audio_quality = "Best available" end
if cfg.audio_preference == nil then cfg.audio_preference = "Prefer selected maximum" end
if cfg.visualizer_frequency_scale == nil then cfg.visualizer_frequency_scale = "Logarithmic" end
if cfg.visualizer_high_frequency_trim == nil then cfg.visualizer_high_frequency_trim = 20 end
if cfg.visualizer_fps == nil then cfg.visualizer_fps = "30 FPS" end

local function modules_contain(value, wanted)
    local hay = "," .. tostring(value or ""):lower():gsub("%s+","") .. ","
    return hay:find("," .. tostring(wanted):lower() .. ",",1,true) ~= nil
end

local function director_wants(wanted)
    if cfg.director_mode ~= true then return false end
    if type(cfg.director_fixed_sources) == "table" then
        for _,source in ipairs(cfg.director_fixed_sources) do
            if source.enabled == true and tostring(source.module):lower() == tostring(wanted):lower() then
                return true
            end
        end
    end
    if type(cfg.director_outputs) == "table" then
        for _,output in ipairs(cfg.director_outputs) do
            if output.enabled == true and modules_contain(output.modules,wanted) then return true end
        end
    end
    return false
end

local streamer_mode = tostring(cfg.app_mode) == "Streamer / OBS"
local director_mode = streamer_mode and cfg.director_mode == true
local ffmpeg_available = exists(ffmpeg)
local deno_available = exists(deno)
local want_art = streamer_mode and (cfg.artwork_enabled == true or director_wants("artwork"))
local want_video = streamer_mode and (cfg.video_enabled == true or director_wants("video"))
local want_viz = streamer_mode and (cfg.visualizer_enabled == true or director_wants("visualizer")) and ffmpeg_available
local want_comment = director_mode and cfg.featured_comment_enabled == true and director_wants("comment")
local want_telemetry = director_mode and cfg.telemetry_enabled == true
local want_probe = want_telemetry and cfg.telemetry_probe_enabled == true and ffmpeg_available
local want_history = director_mode and cfg.history_enabled == true
local smart_crop = want_art and cfg.smart_artwork_crop == true and ffmpeg_available
local loudness_enabled = cfg.loudness_normalization == true and ffmpeg_available
local player_stream_video = (not streamer_mode) and tostring(cfg.player_video_quality or "Off (audio only)") ~= "Off (audio only)"

mp.msg.info(
    "YOMI CONFIG mode=" .. tostring(cfg.app_mode) ..
    " art=" .. tostring(want_art) ..
    " video=" .. tostring(want_video) ..
    " visualizer=" .. tostring(want_viz) ..
    " director=" .. tostring(director_mode) ..
    " comments=" .. tostring(want_comment) ..
    " telemetry=" .. tostring(want_telemetry) ..
    " ffmpeg=" .. tostring(ffmpeg_available) ..
    " deno=" .. tostring(deno_available)
)

local workers = tonumber(cfg.cache_workers) or 1
if workers < 1 then workers = 1 end
if workers > 8 then workers = 8 end

local prefetch_ahead = tonumber(cfg.prefetch_ahead) or 4
if prefetch_ahead < 1 then prefetch_ahead = 1 end
if prefetch_ahead > 20 then prefetch_ahead = 20 end
local video_prefetch_ahead = prefetch_ahead
local video_cache_limit = math.max(64,tonumber(cfg.video_cache_limit_mb) or 512) * 1024 * 1024
local cache_priority = tostring(cfg.cache_priority or "idle")

local urls = {}
do
    local s = read_all(playlist_file) or ""
    for line in s:gmatch("[^\r\n]+") do
        line = line:match("^%s*(.-)%s*$")
        if line and line:match("^https?://") then
            table.insert(urls, line)
        end
    end
end

if #urls == 0 then
    mp.msg.error("YOMI: playlist is empty.")
    return
end

local function status_path(i, name)
    return status_dir .. "\\track-" .. i .. "." .. name
end

local function audio_path(i) return audio_dir .. "\\track-" .. i .. ".audio" end
local function meta_path(i) return meta_dir .. "\\track-" .. i .. ".info.json" end
local function gain_path(i) return gain_dir .. "\\track-" .. i .. ".gain" end
local function video_path(i) return video_dir .. "\\track-" .. i .. ".mp4" end
local function viz_path(i) return visualizer_dir .. "\\track-" .. i .. ".mp4" end
local function comment_path(i) return comment_dir .. "\\track-" .. i .. ".json" end
local function telemetry_audio_path(i) return telemetry_dir .. "\\track-" .. i .. ".audio.json" end
local function telemetry_video_path(i) return telemetry_dir .. "\\track-" .. i .. ".video.json" end

local image_exts = {"jpg","jpeg","webp","png"}
local audio_exts = {"webm","m4a","mp4","opus","ogg","aac","mp3","mka"}

local function artwork_path(i)
    -- Smart Crop has ONE canonical final artifact. This prevents a legacy/raw
    -- WEBP or PNG from both bypassing crop preparation and winning the server
    -- lookup over the processed JPG.
    if smart_crop then
        local p = artwork_dir .. "\\track-" .. i .. ".jpg"
        if exists(p) and fsize(p) > 0 then return p,"jpg" end
        return nil,nil
    end

    for _,ext in ipairs(image_exts) do
        local p = artwork_dir .. "\\track-" .. i .. "." .. ext
        if exists(p) and fsize(p) > 0 then return p,ext end
    end
    return nil,nil
end

local function mark(path)
    write_all(path,"1")
end

local function playback_ready(i)
    return exists(audio_path(i))
        and fsize(audio_path(i)) > 0
        and exists(meta_path(i))
        and fsize(meta_path(i)) > 0
        and exists(gain_path(i))
end

local bad = {}
local function known_bad(i)
    return bad[i] or exists(status_path(i,"audio.permanent"))
end

local function set_engine_status(phase,message,index)
    local obj = {
        phase = phase or "",
        message = message or "",
        index = tonumber(index) or 0,
        count = #urls,
        paused = mp.get_property_native("pause") == true,
        unix = os.time()
    }
    local ok,text = pcall(utils.format_json,obj)
    if ok then write_all(engine_status_file,text) end
end

local function log(msg)
    mp.msg.info("YOMI " .. msg)
end

-- Smart Crop cache format v3:
-- Older public builds could leave uncropped .webp/.png/.jpg finals behind,
-- and server lookup order could display those instead of the newly processed
-- JPG. Invalidate artwork only once; audio/video/meta caches are untouched.
local smartcrop_cache_version = "3"
local smartcrop_marker = artwork_dir .. "\\smartcrop-cache-version.txt"

if smart_crop then
    local old_version = (read_all(smartcrop_marker) or ""):match("^%s*(.-)%s*$")

    if old_version ~= smartcrop_cache_version then
        for _,name in ipairs(utils.readdir(artwork_dir,"files") or {}) do
            if name:match("^track%-%d+%.") then
                os.remove(artwork_dir .. "\\" .. name)
            end
        end

        for _,name in ipairs(utils.readdir(status_dir,"files") or {}) do
            if name:match("^track%-%d+%.artwork%.failed$") then
                os.remove(status_dir .. "\\" .. name)
            end
        end

        write_all(smartcrop_marker,smartcrop_cache_version)
        log("SMART CROP CACHE RESET v" .. smartcrop_cache_version)
    end
end

local function lower(s)
    return tostring(s or ""):lower()
end

local function permanent_error(stderr)
    local s = lower(stderr)
    return s:find("video unavailable",1,true)
        or s:find("account associated with this video has been terminated",1,true)
        or s:find("private video",1,true)
        or s:find("has been removed",1,true)
        or s:find("removed by the uploader",1,true)
        or s:find("this video is unavailable",1,true)
        or s:find("copyright",1,true)
        or s:find("members-only",1,true)
end

local function find_stage(dir,i,extensions)
    local files = utils.readdir(dir,"files") or {}
    local prefix = "track-" .. i .. ".downloading."
    for _,name in ipairs(files) do
        if name:sub(1,#prefix) == prefix then
            local l = name:lower()
            for _,ext in ipairs(extensions) do
                if l:sub(-#ext-1) == "." .. ext then
                    return dir .. "\\" .. name,ext
                end
            end
        end
    end
    return nil,nil
end

local function move_replace(src,dst)
    if not src or not exists(src) then return false end
    os.remove(dst)
    return os.rename(src,dst) ~= nil
end

local function run(priority,exe,args,callback)
    local a = {runner,priority,exe}
    for _,v in ipairs(args) do table.insert(a,tostring(v)) end
    mp.command_native_async({
        name="subprocess",
        playback_only=false,
        capture_stdout=true,
        capture_stderr=true,
        args=a
    },callback)
end

local function ytdlp_common(client_spec)
    local args = {
        "--no-playlist",
        "--quiet",
        "--no-warnings",
        "--socket-timeout","20"
    }
    if client_spec ~= false then
        table.insert(args,"--extractor-args")
        table.insert(args,"youtube:player_client=" .. tostring(client_spec or "web_embedded,default"))
    end
    if deno_available then
        table.insert(args,"--js-runtimes")
        table.insert(args,"deno:" .. deno)
    end
    if ffmpeg_available then
        table.insert(args,"--ffmpeg-location")
        table.insert(args,ffmpeg_dir)
    end
    return args
end

local function audio_format_selector()
    local quality = tostring(cfg.audio_quality or "Best available")
    local prefer_low = tostring(cfg.audio_preference or "Prefer selected maximum") == "Prefer lowest compatible"
    local cap = nil
    if quality:find("64",1,true) then cap = 64
    elseif quality:find("128",1,true) then cap = 128
    elseif quality:find("160",1,true) then cap = 160 end

    if prefer_low then
        if cap then return "worstaudio[abr<=" .. cap .. "]/worstaudio/best" end
        return "worstaudio/bestaudio/best"
    end
    if cap then return "bestaudio[abr<=" .. cap .. "]/bestaudio/best" end
    return "bestaudio/best"
end

local function parse_gain(stderr)
    local gain = tonumber((stderr or ""):match("track_gain%s*=%s*([%+%-]?[%d%.]+)%s*dB")) or 0
    local peak = tonumber((stderr or ""):match("track_peak%s*=%s*([%d%.]+)")) or 0

    if gain > 6 then gain = 6 end
    if gain < -12 then gain = -12 end

    if gain > 0 and peak > 0 then
        local projected = peak * (10 ^ (gain / 20))
        if projected > 0.98 then
            local safe = 20 * (math.log(0.98 / peak) / math.log(10))
            if safe < gain then gain = safe end
        end
    end

    if gain < -20 then gain = -20 end
    return gain
end

local function viz_filter()
    local activity = tostring(cfg.visualizer_activity or "Active")
    local averaging,win,boost = 1,1024,9

    if activity == "Subtle" then averaging,win,boost = 3,2048,3
    elseif activity == "Normal" then averaging,win,boost = 2,1024,6
    elseif activity == "Punchy" then averaging,win,boost = 1,512,12 end

    local w = math.max(12,math.min(192,tonumber(cfg.visualizer_internal_width) or 40))
    local h = math.max(4,math.min(48,tonumber(cfg.visualizer_internal_height) or 10))
    local fps = tostring(cfg.visualizer_fps or "30 FPS"):find("60",1,true) and 60 or 30

    local fscale = tostring(cfg.visualizer_frequency_scale or "Logarithmic") == "Linear" and "lin" or "log"
    return string.format(
        "[0:a]highpass=f=30,volume=%ddB,showfreqs=s=%dx%d:mode=bar:ascale=cbrt:fscale=%s:cmode=combined:rate=%d:colors=white:averaging=%d:win_size=%d,format=yuv420p[v]",
        boost,w,h,fscale,fps,averaging,win
    )
end

local current_index = tonumber((read_all(resume_file) or "1"):match("%d+")) or 1
if current_index < 1 or current_index > #urls then current_index = 1 end

local desired_index = current_index
local requested_index = 0
local playing_index = 0
local jobs = {}
local queued = {}
local active = {}
local active_count = 0
local decorative_active_count = 0
local audio_retries = {}
local bundle_priority = {}
local metrics_by_track = {}
local bundle_cache_hit = {}
local bundle_ready

local function next_candidate(from,step)
    step = step or 1
    local i = from
    for _=1,#urls do
        i = i + step
        if i > #urls then i = 1 end
        if i < 1 then i = #urls end
        if not known_bad(i) then return i end
    end
    return nil
end

local function read_gain(i)
    return tonumber((read_all(gain_path(i)) or "0"):match("[%+%-]?[%d%.]+")) or 0
end

local function ready_ahead_count(i)
    if not bundle_ready then return 0 end
    local count = 0
    local cursor = i
    for _=1,prefetch_ahead do
        local n = next_candidate(cursor,1)
        if not n or n == i then break end
        if bundle_ready(n) then count = count + 1 else break end
        cursor = n
    end
    return count
end

local function state_for(i)
    local info = load_json(meta_path(i)) or {}
    local art = artwork_path(i)
    local comment = load_json(comment_path(i)) or {}
    if exists(status_path(i,"comment.hidden")) then comment={} end
    local audio_probe = load_json(telemetry_audio_path(i))
    local video_probe = load_json(telemetry_video_path(i))
    local next_i = next_candidate(i,1)
    local next_info = next_i and (load_json(meta_path(next_i)) or {}) or {}
    local next_art = next_i and artwork_path(next_i) or nil
    local m = metrics_by_track[i] or {}
    local prepare_seconds = 0
    for _,seconds in pairs(m) do prepare_seconds = prepare_seconds + (tonumber(seconds) or 0) end
    local decorative_active = 0
    for _,running in pairs(active) do
        if type(running)=="table" and running.decorative then decorative_active=decorative_active+1 end
    end
    local audio_bytes = fsize(audio_path(i))
    local art_bytes = art and fsize(art) or 0
    local video_bytes = fsize(video_path(i))
    local viz_bytes = fsize(viz_path(i))

    return {
        index = i,
        playlist_count = #urls,
        id = info.id or "",
        extractor = info.extractor_key or info.extractor or "YouTube",
        webpage_url = info.webpage_url or info.original_url or urls[i],
        title = info.title or ("Track " .. i),
        channel = info.channel or info.uploader or "",
        uploader = info.uploader or "",
        duration = tonumber(info.duration) or 0,
        upload_date = info.upload_date or info.release_date or "",
        release_date = info.release_date or "",
        view_count = tonumber(info.view_count),
        like_count = tonumber(info.like_count),
        comment_count = tonumber(info.comment_count),
        channel_follower_count = tonumber(info.channel_follower_count),
        channel_is_verified = info.channel_is_verified == true,
        categories = info.categories or {},
        tags = info.tags or {},
        live_status = info.live_status or "",
        age_limit = tonumber(info.age_limit) or 0,
        artwork = (want_art and art) and ("/media/artwork/" .. i) or "",
        video = (want_video and exists(video_path(i))) and ("/media/video/" .. i) or "",
        visualizer = (want_viz and exists(viz_path(i))) and ("/media/visualizer/" .. i) or "",
        video_quality = tostring(cfg.overlay_video_quality or "144p (fastest)"),
        video_preference = tostring(cfg.video_preference or "Prefer selected maximum"),
        video_fps = tostring(cfg.video_fps or "30 FPS"),
        audio_quality = tostring(cfg.audio_quality or "Best available"),
        audio_preference = tostring(cfg.audio_preference or "Prefer selected maximum"),
        visualizer_fps = tostring(cfg.visualizer_fps or "30 FPS"),
        visualizer_high_frequency_trim = tonumber(cfg.visualizer_high_frequency_trim) or 20,
        audio = {
            ext = info.ext or info.audio_ext or "",
            format_id = info.format_id or "",
            format_note = info.format_note or "",
            acodec = info.acodec or "",
            abr = tonumber(info.abr),
            asr = tonumber(info.asr),
            audio_channels = tonumber(info.audio_channels),
            gain_db = read_gain(i)
        },
        media = {
            audio_bytes = audio_bytes,
            artwork_bytes = art_bytes,
            video_bytes = video_bytes,
            visualizer_bytes = viz_bytes,
            total_bytes = audio_bytes + art_bytes + video_bytes + viz_bytes
        },
        telemetry = {
            audio = audio_probe,
            video = video_probe
        },
        comment = comment,
        up_next = {
            index = next_i or 0,
            title = next_info.title or "",
            channel = next_info.channel or next_info.uploader or "",
            artwork = (next_i and next_art) and ("/media/artwork/" .. next_i) or ""
        },
        pipeline = {
            bundle_ready = bundle_ready and bundle_ready(i) or false,
            cache_hit = bundle_cache_hit[i] == true,
            active_jobs = active_count,
            decorative_jobs = decorative_active,
            queued_jobs = #jobs,
            workers = workers,
            prefetch_ahead = prefetch_ahead,
            video_prefetch_ahead = video_prefetch_ahead,
            complete_prefetch_ahead = prefetch_ahead,
            ready_ahead = ready_ahead_count(i),
            prepare_seconds = prepare_seconds,
            stages = m
        }
    }
end

local function write_state(i)
    if not i or i < 1 then return end
    local ok,text = pcall(utils.format_json,state_for(i))
    if ok then write_all(current_file,text) end
end

local function record_history(i)
    if not i or i<1 then return end
    local info = load_json(meta_path(i)) or {}
    local entry = {
        index = i,
        id = info.id or "",
        title = info.title or ("Track " .. i),
        channel = info.channel or info.uploader or "",
        duration = tonumber(info.duration) or 0,
        upload_date = info.upload_date or info.release_date or "",
        unix = os.time()
    }
    local ok,line = pcall(utils.format_json,entry)
    if not ok then return end
    local max_entries = math.max(10,math.min(1000,tonumber(cfg.history_max_entries) or 100))
    local lines = {}
    local raw = read_all(history_file) or ""
    for old in raw:gmatch("[^\r\n]+") do table.insert(lines,old) end
    while #lines >= max_entries do table.remove(lines,1) end
    table.insert(lines,line)
    write_all(history_file,table.concat(lines,"\r\n") .. "\r\n")
end

local function job_key(kind,i)
    return kind .. ":" .. i
end

local function optional_done(kind,i)
    if kind == "art" then
        return (not want_art) or artwork_path(i) ~= nil or exists(status_path(i,"artwork.failed"))
    elseif kind == "video" then
        return (not want_video) or exists(video_path(i)) or exists(status_path(i,"video.failed"))
    elseif kind == "viz" then
        return (not want_viz) or exists(viz_path(i)) or exists(status_path(i,"visualizer.failed"))
    end
    return true
end

local function decorative_done(kind,i)
    if kind == "comment" then
        return (not want_comment) or exists(comment_path(i)) or exists(status_path(i,"comment.failed")) or exists(status_path(i,"comment.hidden"))
    elseif kind == "probe" then
        if not want_probe then return true end
        local audio_done = exists(telemetry_audio_path(i)) or exists(status_path(i,"probe-audio.failed"))
        local video_done = (not exists(video_path(i))) or exists(telemetry_video_path(i)) or exists(status_path(i,"probe-video.failed"))
        return audio_done and video_done
    end
    return true
end

bundle_ready = function(i)
    if not i or known_bad(i) then return false end
    if not playback_ready(i) then return false end
    if not optional_done("art",i) then return false end
    if not optional_done("video",i) then return false end
    if not optional_done("viz",i) then return false end
    return true
end

local function bundle_stage(i)
    if not playback_ready(i) then return "audio" end
    if not optional_done("art",i) then return "artwork" end
    if not optional_done("video",i) then return "tiny video" end
    if not optional_done("viz",i) then return "visualizer" end
    return "ready"
end

local function enqueue(kind,i,priority)
    if not i or i < 1 or i > #urls then return end

    if kind == "audio" then
        if playback_ready(i) or known_bad(i) then return end
    elseif kind == "comment" or kind == "probe" then
        if not playback_ready(i) or decorative_done(kind,i) then return end
    else
        if not playback_ready(i) or optional_done(kind,i) then return end
    end

    local key = job_key(kind,i)
    if queued[key] or active[key] then return end

    queued[key] = true
    table.insert(jobs,{
        kind=kind,
        i=i,
        priority=priority or 100,
        key=key
    })
end

local function prune_old_optional(current)
    local out = {}
    for _,j in ipairs(jobs) do
        if j.kind == "audio" or j.i == current then
            table.insert(out,j)
        else
            queued[j.key] = nil
        end
    end
    jobs = out
end

local pump
local start_play
local schedule_for_current
local request_bundle

local function job_done(job)
    if job.started_at then
        if not metrics_by_track[job.i] then metrics_by_track[job.i] = {} end
        metrics_by_track[job.i][job.kind] = math.max(0,mp.get_time()-job.started_at)
    end
    active[job.key] = nil
    if job.decorative then decorative_active_count=math.max(0,decorative_active_count-1)
    else active_count = math.max(0,active_count-1) end

    -- Every completed step immediately unlocks the next missing pieces of the
    -- same presentation bundle.
    if request_bundle and bundle_priority[job.i] and not known_bad(job.i) then
        request_bundle(job.i,bundle_priority[job.i])
    end

    if playing_index>0 then
        local next_i = next_candidate(playing_index,1)
        if job.i==playing_index or job.i==next_i then write_state(playing_index) end
    end

    -- The desired track is not allowed to start until every enabled visual
    -- asset is READY or has explicitly FAILED.
    if start_play
        and desired_index == job.i
        and playing_index ~= job.i
        and requested_index ~= job.i
        and bundle_ready(job.i) then
        mp.add_timeout(0,function()
            if desired_index == job.i
                and playing_index ~= job.i
                and requested_index ~= job.i
                and bundle_ready(job.i) then
                start_play(job.i)
            end
        end)
    end

    pump()
end

local function cleanup_cache(center)
    local keep = {[center]=true}
    local cursor = center
    for _=1,prefetch_ahead+3 do
        cursor = next_candidate(cursor,1)
        if not cursor or keep[cursor] then break end
        keep[cursor] = true
    end
    cursor = center
    for _=1,2 do
        cursor = next_candidate(cursor,-1)
        if not cursor or keep[cursor] then break end
        keep[cursor] = true
    end
    local dirs = {audio_dir,artwork_dir,video_dir,visualizer_dir,meta_dir,gain_dir,status_dir,comment_dir,telemetry_dir}

    for _,dir in ipairs(dirs) do
        for _,name in ipairs(utils.readdir(dir,"files") or {}) do
            local n = tonumber(name:match("track%-(%d+)"))
            if n and not keep[n] then
                os.remove(dir .. "\\" .. name)
            end
        end
    end

    -- A quality-independent hard ceiling prevents 720p/Best mode from turning
    -- a long playlist window into an unbounded video cache. Preserve the
    -- playing track and its immediate successor; evict the farthest prepared
    -- videos first and let the ordinary scheduler recreate them if needed.
    local video_files = {}
    local video_total = 0
    local protected_next = next_candidate(center,1)
    for _,name in ipairs(utils.readdir(video_dir,"files") or {}) do
        local n = tonumber(name:match("^track%-(%d+)%.mp4$"))
        if n then
            local path = video_dir .. "\\" .. name
            local size = fsize(path)
            video_total = video_total + size
            local direct = math.abs(n-center)
            table.insert(video_files,{n=n,path=path,size=size,distance=math.min(direct,#urls-direct)})
        end
    end
    table.sort(video_files,function(a,b) return a.distance > b.distance end)
    for _,item in ipairs(video_files) do
        if video_total <= video_cache_limit then break end
        if item.n ~= center and item.n ~= protected_next then
            os.remove(item.path)
            os.remove(telemetry_video_path(item.n))
            video_total = math.max(0,video_total-item.size)
            log("VIDEO CACHE EVICT track " .. item.n .. " limit " .. math.floor(video_cache_limit/1024/1024) .. " MB")
        end
    end
end

local function audio_failure(job,stderr,is_permanent)
    local i = job.i
    bad[i] = true

    if is_permanent then
        mark(status_path(i,"audio.permanent"))
        log("AUDIO PERMANENT track " .. i)
    else
        log("AUDIO FAILED track " .. i)
    end

    if desired_index == i and playing_index ~= i then
        local n = next_candidate(i,1)
        if n then
            desired_index = n
            requested_index = 0
            set_engine_status(
                "skipping",
                "Skipping unavailable track " .. i .. "; preparing track " .. n .. "...",
                n
            )
            bundle_priority[n] = 0
            request_bundle(n,0)
            pump()
        else
            set_engine_status("error","No playable tracks remain.",i)
        end
    elseif playing_index > 0 then
        -- If the immediate prefetched successor died, promote the next known
        -- candidate before any decorative current-track work.
        schedule_for_current(playing_index)
    end

    job_done(job)
end

local function finish_audio_ready(job)
    local i = job.i
    audio_retries[i] = nil
    log("AUDIO READY track " .. i)

    -- Audio readiness is only one part of a synchronized presentation bundle.
    -- job_done() will unlock artwork -> video -> visualizer in priority order.
    job_done(job)
end

local function gain_scan(job)
    local i = job.i
    if exists(gain_path(i)) then
        finish_audio_ready(job)
        return
    end

    if not loudness_enabled then
        write_all(gain_path(i),"0.000")
        log("GAIN BYPASS track " .. i)
        finish_audio_ready(job)
        return
    end

    if desired_index == i and playing_index ~= i then
        set_engine_status("gain","Scanning loudness for track " .. i .. "...",i)
    end

    log("GAIN START track " .. i)

    run("idle",ffmpeg,{
        "-hide_banner","-nostats",
        "-i",audio_path(i),
        "-af","replaygain",
        "-f","null","NUL"
    },function(success,result)
        local gain = 0
        if result then gain = parse_gain(result.stderr or "") end
        write_all(gain_path(i),string.format("%.3f",gain))
        log("GAIN READY track " .. i)
        finish_audio_ready(job)
    end)
end

local function source_download(job)
    local i = job.i

    if playback_ready(i) then
        finish_audio_ready(job)
        return
    end

    if exists(audio_path(i)) and exists(meta_path(i)) then
        gain_scan(job)
        return
    end

    if desired_index == i and playing_index ~= i then
        set_engine_status("download","Downloading audio for track " .. i .. "...",i)
    end

    log("AUDIO START track " .. i)

    local base = audio_dir .. "\\track-" .. i .. ".downloading.%(ext)s"
    local args = ytdlp_common()

    table.insert(args,"--ignore-errors")
    table.insert(args,"--format")
    table.insert(args,audio_format_selector())
    table.insert(args,"--write-info-json")
    table.insert(args,"--no-write-playlist-metafiles")
    table.insert(args,"--output")
    table.insert(args,base)
    table.insert(args,urls[i])

    run(cache_priority,ytdlp,args,function(success,result)
        local stderr = result and result.stderr or ""
        local src_audio = find_stage(audio_dir,i,audio_exts)
        local good = success and result and result.status == 0 and src_audio ~= nil

        if not good then
            local permanent = permanent_error(stderr)
            audio_retries[i] = (audio_retries[i] or 0) + 1

            if permanent then
                mp.msg.warn("YOMI audio " .. i .. ": " .. stderr)
                audio_failure(job,stderr,true)
                return
            end

            if audio_retries[i] < 2 then
                log("AUDIO RETRY track " .. i)
                if desired_index == i and playing_index ~= i then
                    set_engine_status("retry","Retrying audio for track " .. i .. "...",i)
                end
                mp.add_timeout(1.0,function() source_download(job) end)
                return
            end

            mp.msg.warn("YOMI audio " .. i .. ": " .. stderr)
            audio_failure(job,stderr,false)
            return
        end

        move_replace(src_audio,audio_path(i))

        local info_stage = audio_dir .. "\\track-" .. i .. ".downloading.info.json"
        if exists(info_stage) then move_replace(info_stage,meta_path(i)) end

        if not exists(meta_path(i)) then
            audio_failure(job,"metadata missing after audio download",false)
            return
        end

        gain_scan(job)
    end)
end

local function art_job(job)
    local i = job.i
    if optional_done("art",i) then job_done(job); return end

    log("ARTWORK START track " .. i)

    if desired_index == i and playing_index ~= i then
        set_engine_status("artwork","Preparing artwork for track " .. i .. "...",i)
    end

    local base = artwork_dir .. "\\track-" .. i .. ".downloading.%(ext)s"
    local args = ytdlp_common(nil)

    table.insert(args,"--skip-download")
    table.insert(args,"--write-thumbnail")
    table.insert(args,"--output")
    table.insert(args,base)
    table.insert(args,urls[i])

    run(cache_priority,ytdlp,args,function(success,result)
        local src,ext = find_stage(artwork_dir,i,image_exts)

        if not success or not result or result.status ~= 0
            or not src or fsize(src) <= 0 then

            if src then os.remove(src) end
            mark(status_path(i,"artwork.failed"))
            log("ARTWORK FAILED track " .. i)
            job_done(job)
            return
        end

        if not smart_crop then
            local final = artwork_dir .. "\\track-" .. i .. "." .. tostring(ext or "jpg")
            move_replace(src,final)
            log("ARTWORK READY track " .. i .. " raw")

            if playing_index == i then write_state(i) end
            job_done(job)
            return
        end

        local w = math.max(20,tonumber(cfg.media_width) or 160)
        local h = math.max(20,tonumber(cfg.media_height) or 90)

        -- Smart Crop's ONLY final file.
        local final = artwork_dir .. "\\track-" .. i .. ".jpg"
        local temp = artwork_dir .. "\\track-" .. i .. ".cropping.jpg"
        local normalized = artwork_dir .. "\\track-" .. i .. ".detector.png"

        os.remove(final)
        os.remove(temp)
        os.remove(normalized)

        -- Kill any legacy/raw final formats for this same track.
        for _,legacy_ext in ipairs({"jpeg","webp","png"}) do
            os.remove(artwork_dir .. "\\track-" .. i .. "." .. legacy_ext)
        end

        log("ARTWORK SOURCE track " .. i .. " ." .. tostring(ext or "unknown"))
        log("ARTWORK NORMALIZE track " .. i .. " -> PNG")

        run("idle",ffmpeg,{
            "-y",
            "-hide_banner",
            "-loglevel","error",
            "-i",src,
            "-frames:v","1",
            normalized
        },function(norm_ok,norm_res)

            if not norm_ok or not norm_res or norm_res.status ~= 0
                or not exists(normalized) or fsize(normalized) <= 0 then

                os.remove(normalized)
                os.remove(src)
                mark(status_path(i,"artwork.failed"))
                log("ARTWORK NORMALIZE FAILED track " .. i)
                job_done(job)
                return
            end

            log("ARTWORK NORMALIZED track " .. i)

            local finished=false

            local function finish_art(crop_expr,reason)
                if finished then return end
                finished=true

                local filters={}

                if crop_expr and crop_expr ~= "" then
                    table.insert(filters,"crop=" .. crop_expr)
                    log("ARTWORK APPLY CROP track " .. i .. " " .. crop_expr .. " via " .. tostring(reason))
                else
                    log("ARTWORK NO BORDER CROP track " .. i)
                end

                table.insert(
                    filters,
                    string.format(
                        "scale=%d:%d:force_original_aspect_ratio=increase,crop=%d:%d",
                        w,h,w,h
                    )
                )

                run("idle",ffmpeg,{
                    "-y",
                    "-hide_banner",
                    "-loglevel","error",
                    "-i",normalized,
                    "-vf",table.concat(filters,","),
                    "-frames:v","1",
                    "-q:v","2",
                    temp
                },function(ok,res)

                    os.remove(src)
                    os.remove(normalized)

                    if ok and res and res.status==0 and exists(temp) and fsize(temp)>0 then
                        move_replace(temp,final)

                        -- Final canonicalization: there can be no competing
                        -- raw WEBP/PNG/JPEG file after Smart Crop succeeds.
                        for _,legacy_ext in ipairs({"jpeg","webp","png"}) do
                            os.remove(artwork_dir .. "\\track-" .. i .. "." .. legacy_ext)
                        end

                        log("ARTWORK READY CROPPED-CACHE track " .. i)
                    else
                        os.remove(temp)
                        mark(status_path(i,"artwork.failed"))
                        log("ARTWORK FAILED track " .. i)
                    end

                    if playing_index == i then write_state(i) end
                    job_done(job)
                end)
            end

            local function black_fallback()
                log("ARTWORK BLACK FALLBACK track " .. i)

                -- This is the exact static-image cropdetect pattern that worked
                -- in the earlier YOMI/personal artwork pipeline.
                run("idle",ffmpeg,{
                    "-hide_banner",
                    "-nostats",
                    "-loglevel","info",
                    "-loop","1",
                    "-i",normalized,
                    "-vf","cropdetect=limit=16:round=2:reset=0",
                    "-frames:v","10",
                    "-f","null",
                    "NUL"
                },function(_,detect_result)

                    local stderr=detect_result and detect_result.stderr or ""
                    local cw,ch,cx,cy=nil,nil,nil,nil

                    -- Last cropdetect result is the stabilized result.
                    for w1,h1,x1,y1 in stderr:gmatch("crop=(%d+):(%d+):(%d+):(%d+)") do
                        cw=tonumber(w1)
                        ch=tonumber(h1)
                        cx=tonumber(x1)
                        cy=tonumber(y1)
                    end

                    if not cw or not ch or not cx or not cy then
                        log("ARTWORK BLACK FALLBACK NONE track " .. i)
                        finish_art(nil,"none")
                        return
                    end

                    local ffprobe = install_root .. "\\runtime\\ffmpeg\\ffprobe.exe"
                    run("idle",ffprobe,{
                        "-v","error",
                        "-select_streams","v:0",
                        "-show_entries","stream=width,height",
                        "-of","csv=s=x:p=0",
                        normalized
                    },function(probe_success,probe)
                        local ow,oh=nil,nil

                        if probe_success and probe and probe.status==0 and probe.stdout then
                            local a,b=probe.stdout:match("(%d+)%s*x%s*(%d+)")
                            ow=tonumber(a)
                            oh=tonumber(b)
                        end

                        if not ow or not oh then
                            log("ARTWORK BLACK FALLBACK PROBE FAILED track " .. i)
                            finish_art(nil,"none")
                            return
                        end

                        local left=cx
                        local top=cy
                        local right=ow-(cx+cw)
                        local bottom=oh-(cy+ch)

                        local meaningful =
                            left >= 8 or top >= 8 or right >= 8 or bottom >= 8

                        local within_caps =
                            left <= ow*0.30 and
                            right <= ow*0.30 and
                            top <= oh*0.30 and
                            bottom <= oh*0.30 and
                            cw >= ow*0.40 and
                            ch >= oh*0.40

                        if meaningful and within_caps then
                            local crop_expr=string.format("%d:%d:%d:%d",cw,ch,cx,cy)

                            log(
                                "ARTWORK BLACK CROP track "..i.." "..crop_expr..
                                " borders L"..left..
                                " T"..top..
                                " R"..right..
                                " B"..bottom
                            )

                            finish_art(crop_expr,"black-fallback")
                        else
                            log(
                                "ARTWORK BLACK FALLBACK REJECT track "..i..
                                " borders L"..left..
                                " T"..top..
                                " R"..right..
                                " B"..bottom
                            )

                            finish_art(nil,"none")
                        end
                    end)
                end)
            end

            local detector = install_root .. "\\app\\ArtworkEdgeDetector.exe"
            log("ARTWORK COLOR DETECT track " .. i)

            run("idle",detector,{normalized,tostring(w),tostring(h)},function(det_success,det_result)
                local stdout = det_result and det_result.stdout or ""
                local stderr = det_result and det_result.stderr or ""

                local cw,ch,cx,cy,left,top,right,bottom =
                    stdout:match(
                        "CROP%s+(%d+):(%d+):(%d+):(%d+)%s+L(%d+)%s+T(%d+)%s+R(%d+)%s+B(%d+)"
                    )

                if det_success and det_result and det_result.status==0
                    and cw and ch and cx and cy then

                    local crop_expr=
                        tostring(cw)..":"..
                        tostring(ch)..":"..
                        tostring(cx)..":"..
                        tostring(cy)

                    log(
                        "ARTWORK COLOR CROP track "..i.." "..crop_expr..
                        " borders L"..tostring(left)..
                        " T"..tostring(top)..
                        " R"..tostring(right)..
                        " B"..tostring(bottom)
                    )

                    finish_art(crop_expr,"color-detector")
                    return
                end

                local reason=stdout:gsub("[\r\n]+"," "):match("^%s*(.-)%s*$")

                if reason=="" and stderr~="" then
                    reason=stderr:gsub("[\r\n]+"," "):match("^%s*(.-)%s*$")
                end

                if reason=="" then reason="no confident color edge band" end

                log("ARTWORK COLOR NONE track "..i.." "..reason)

                -- Guaranteed compatibility fallback for black/dark bars:
                -- the older FFmpeg method that was previously working.
                black_fallback()
            end)
        end)
    end)
end

local function video_job(job)
    local i = job.i
    if optional_done("video",i) then job_done(job); return end
    log("VIDEO START track " .. i)
    if desired_index == i and playing_index ~= i then set_engine_status("video","Preparing tiny video for track " .. i .. "...",i) end
    local temp = video_dir .. "\\track-" .. i .. ".downloading.mp4"

    -- Resolution is a ceiling. The independent preference chooses the top or
    -- bottom of that compatible range. Each compatibility format is a real
    -- download route so an in-transit failure advances instead of reselecting
    -- the same broken stream and then jumping immediately to 144p.
    local quality = tostring(cfg.overlay_video_quality or "144p (fastest)")
    local prefer_low = tostring(cfg.video_preference or "Prefer selected maximum") == "Prefer lowest compatible"
    local cap = 144
    if quality:find("240p",1,true) then cap = 240
    elseif quality:find("360p",1,true) then cap = 360
    elseif quality:find("480p",1,true) then cap = 480
    elseif quality:find("720p",1,true) then cap = 720
    elseif quality:find("Best",1,true) then cap = 0 end

    local prefer_60 = tostring(cfg.video_fps or "30 FPS"):find("60",1,true) ~= nil
    local function filtered_video(base,height,high_fps_only,exact_height)
        local h = ""
        if height and height > 0 then
            h = exact_height and ("[height=" .. height .. "]") or ("[height<=" .. height .. "]")
        end
        local fps
        if prefer_60 then fps = high_fps_only and "[fps>30][fps<=60]" or "[fps<=60]"
        else fps = "[fps<=30]" end
        return base .. h .. fps .. "[vcodec^=avc][ext=mp4]/" .. base .. h .. fps .. "[ext=mp4]"
    end
    local function labeled_video(base,height,high_fps_only)
        local fps
        if prefer_60 then fps = high_fps_only and "[fps>30][fps<=60]" or "[fps<=60]"
        else fps = "[fps<=30]" end
        local label = "[format_note^=" .. height .. "p]"
        return base .. label .. fps .. "[vcodec^=avc][ext=mp4]/" .. base .. label .. fps .. "[ext=mp4]"
    end
    local max_formats = {}
    for _,height in ipairs({144,240,360,480,720}) do
        if prefer_60 then
            max_formats[height] =
                labeled_video("bestvideo",height,true) .. "/" ..
                filtered_video("bestvideo",height,true,true) .. "/" ..
                filtered_video("bestvideo",height,true,false) .. "/" ..
                labeled_video("bestvideo",height,false) .. "/" ..
                filtered_video("bestvideo",height,false,true) .. "/" ..
                filtered_video("bestvideo",height,false,false)
        else
            -- YouTube's nominal quality label is authoritative here. A video
            -- can be labeled 240p while its stored frame is 352x288, so a
            -- literal height<=240 filter rejects the correct format. Try the
            -- label first, then literal/capped MP4. Fixed itags are separate
            -- download routes because yt-dlp's slash fallback happens during
            -- selection, not after a selected stream fails in transit.
            max_formats[height] =
                labeled_video("bestvideo",height,false) .. "/" ..
                filtered_video("bestvideo",height,false,true) .. "/" ..
                filtered_video("bestvideo",height,false,false)
        end
    end
    local primary
    if prefer_low then
        if prefer_60 then primary = filtered_video("worstvideo",cap,true) .. "/" .. filtered_video("worstvideo",cap,false) .. "/160"
        else primary = "160/" .. filtered_video("worstvideo",cap,false) end
    elseif cap > 0 then
        primary = max_formats[cap]
    else
        if prefer_60 then primary = filtered_video("bestvideo",0,true) .. "/" .. filtered_video("bestvideo",0,false)
        else primary = filtered_video("bestvideo",0,false) end
    end

    local label_cap = cap > 0 and (tostring(cap) .. "p") or "best"
    local label_pref = prefer_low and "lowest compatible" or "selected maximum"
    local label_fps = prefer_60 and "60 FPS when available" or "30 FPS"
    local minimum_heights = {[144]=100,[240]=180,[360]=300,[480]=400,[720]=600}
    local routes = {}
    local function add_route(label,clients,format,repeats,delay,min_height)
        table.insert(routes,{label=label,clients=clients,format=format,repeats=repeats or 1,delay=delay or 1.0,min_height=min_height or 0})
    end

    add_route(
        label_pref .. " " .. label_cap .. " " .. label_fps .. " MP4",
        prefer_60 and "default,-web_safari" or (((cap > 0 and cap <= 240) or prefer_low) and "web_embedded,default" or "default,-web_safari"),
        primary,
        (cap == 144 and 3 or 2),
        (cap == 144 and 1.5 or 1.0),
        (not prefer_low and cap > 0) and minimum_heights[cap] or 0
    )

    if not prefer_low then
        if cap == 240 then
            add_route("240p fixed-format default-client recovery","default,-web_safari","133",2,1.0,minimum_heights[240])
            add_route("240p fixed-format embedded-client recovery","web_embedded","133",2,1.0,minimum_heights[240])
        elseif cap == 360 then
            add_route("360p progressive compatibility recovery","default,-web_safari","18",2,1.0,minimum_heights[360])
            add_route("360p adaptive embedded-client recovery","web_embedded,default","134",2,1.0,minimum_heights[360])
        elseif cap == 480 then
            add_route("480p fixed-format recovery","default,-web_safari","135",2,1.0,minimum_heights[480])
            add_route("360p progressive resolution recovery","default,-web_safari","18",2,1.0,minimum_heights[360])
        elseif cap == 720 then
            add_route("720p progressive compatibility recovery","default,-web_safari","22",2,1.0,minimum_heights[720])
            add_route("720p adaptive embedded-client recovery","web_embedded,default","136",2,1.0,minimum_heights[720])
            add_route("360p progressive resolution recovery","default,-web_safari","18",2,1.0,minimum_heights[360])
        elseif cap == 0 then
            add_route("360p progressive compatibility recovery","default,-web_safari","18",2,1.0,minimum_heights[360])
        end
    end

    local fallback_fps = prefer_60 and "[fps<=60]" or "[fps<=30]"
    local automatic_cap=cap
    if automatic_cap==0 then automatic_cap=480 end
    local automatic_format="best[height<="..automatic_cap.."]"..fallback_fps.."[ext=mp4]/bestvideo[height<="..automatic_cap.."]"..fallback_fps.."[ext=mp4]"
    local automatic_min=0
    if not prefer_low then
        if cap==240 then automatic_min=minimum_heights[240]
        elseif cap==360 then automatic_min=minimum_heights[360]
        elseif cap==480 or cap==720 or cap==0 then automatic_min=minimum_heights[360]
        elseif cap==144 then automatic_min=minimum_heights[144] end
    end
    add_route("automatic capped compatibility recovery",false,automatic_format,2,1.0,automatic_min)
    if cap == 144 or prefer_low then
        add_route("proven 144p compatibility recovery","web_embedded,default","160",3,1.5,100)
    else
        add_route("proven 144p final recovery","web_embedded,default","160",3,1.5,100)
    end
    local last_stderr=""; local try_route
    local function compact_error(raw)
        local s=tostring(raw or ""):gsub("[\r\n]+"," "):gsub("%s+"," "):match("^%s*(.-)%s*$") or ""
        if #s>360 then s=s:sub(1,360).."..." end
        if s=="" then s="yt-dlp returned no diagnostic text" end
        return s
    end
    local function finish_route_failure(rn,attempt,reason)
        local spec=routes[rn]
        os.remove(temp)
        local detail=compact_error(reason)
        log("VIDEO ATTEMPT FAILED track "..i.." route "..rn.." try "..attempt.." "..spec.label.." reason "..detail)
        if permanent_error(reason or "") then mark(status_path(i,"video.failed"));log("VIDEO DEAD track "..i);job_done(job);return end
        if attempt < spec.repeats then
            log("VIDEO RETRY track "..i.." same route")
            if desired_index==i and playing_index~=i then set_engine_status("video","Tiny video retry "..(attempt+1).."/"..spec.repeats.." for track "..i.."...",i) end
            mp.add_timeout(spec.delay,function() try_route(rn,attempt+1) end);return
        end
        if rn < #routes then
            log("VIDEO FALLBACK track "..i.." route "..(rn+1))
            mp.add_timeout(0.4,function() try_route(rn+1,1) end);return
        end
        mark(status_path(i,"video.failed"));log("VIDEO FAILED track "..i.." after all routes");if detail~="" then mp.msg.warn("YOMI video "..i..": "..detail) end;if playing_index==i then write_state(i) end;job_done(job)
    end
    local function accept_video(rn,attempt,selected,height)
        move_replace(temp,video_path(i))
        log("VIDEO READY track "..i.." route "..rn.." try "..attempt.." selected "..selected.." verified-height "..tostring(height or "unknown"))
        if playing_index==i then write_state(i) end
        job_done(job)
    end
    try_route=function(rn,attempt)
        os.remove(temp); local spec=routes[rn]; local args=ytdlp_common(spec.clients)
        table.insert(args,"--retries");table.insert(args,"1");table.insert(args,"--fragment-retries");table.insert(args,"1")
        table.insert(args,"--format");table.insert(args,spec.format);table.insert(args,"--output");table.insert(args,temp);table.insert(args,"--no-part");table.insert(args,urls[i])
        table.insert(args,"--print");table.insert(args,"after_move:YOMI_FORMAT=%(format_id)s|%(height)s|%(fps)s|%(ext)s")
        log("VIDEO ATTEMPT track "..i.." route "..rn.." try "..attempt.."/"..spec.repeats.." "..spec.label)
        run(cache_priority,ytdlp,args,function(success,result)
            last_stderr=(result and result.stderr) or ""
            if success and result and result.status==0 and exists(temp) and fsize(temp)>0 then
                mp.add_timeout(0.5,function()
                    if exists(temp) and fsize(temp)>0 then
                        local selected = result and result.stdout and result.stdout:match("YOMI_FORMAT=([^\r\n]+)") or "unknown"
                        local printed_height=tonumber(selected:match("^[^|]*|([^|]+)|"))
                        if printed_height and printed_height>0 then
                            if spec.min_height>0 and printed_height<spec.min_height then
                                finish_route_failure(rn,attempt,"selected stream height "..printed_height.." is below route minimum "..spec.min_height)
                            else
                                accept_video(rn,attempt,selected,printed_height)
                            end
                        elseif ffmpeg_available then
                            local ffprobe=install_root.."\\runtime\\ffmpeg\\ffprobe.exe"
                            run("idle",ffprobe,{"-v","error","-select_streams","v:0","-show_entries","stream=height","-of","default=noprint_wrappers=1:nokey=1",temp},function(probe_ok,probe_result)
                                local measured=probe_result and probe_result.stdout and tonumber(probe_result.stdout:match("(%d+)")) or nil
                                if not probe_ok or not probe_result or probe_result.status~=0 or not measured then
                                    finish_route_failure(rn,attempt,"FFprobe could not validate completed video")
                                elseif spec.min_height>0 and measured<spec.min_height then
                                    finish_route_failure(rn,attempt,"completed stream height "..measured.." is below route minimum "..spec.min_height)
                                else
                                    accept_video(rn,attempt,selected,measured)
                                end
                            end)
                        else
                            accept_video(rn,attempt,selected,nil)
                        end
                    else
                        finish_route_failure(rn,attempt,"downloaded temporary file disappeared before validation")
                    end
                end);return
            end
            finish_route_failure(rn,attempt,last_stderr)
        end)
    end
    try_route(1,1)
end

local function viz_job(job)
    local i = job.i
    if optional_done("viz",i) then job_done(job); return end

    log("VISUALIZER START track " .. i)
    if desired_index == i and playing_index ~= i then
        set_engine_status("visualizer","Preparing visualizer for track " .. i .. "...",i)
    end
    local temp = visualizer_dir .. "\\track-" .. i .. ".processing.mp4"
    os.remove(temp)

    local args = {
        "-y","-hide_banner","-loglevel","error",
        "-i",audio_path(i),
        "-filter_complex",viz_filter(),
        "-map","[v]","-an",
        "-c:v","libx264",
        "-preset","ultrafast",
        "-tune","fastdecode",
        "-crf","10",
        "-g","1",
        "-keyint_min","1",
        "-sc_threshold","0",
        "-bf","0",
        "-movflags","+faststart",
        "-threads","1",
        "-filter_complex_threads","1",
        temp
    }

    run("idle",ffmpeg,args,function(success,result)
        if success and result and result.status == 0 and exists(temp) and fsize(temp) > 0 then
            move_replace(temp,viz_path(i))
            log("VISUALIZER READY track " .. i)
        else
            os.remove(temp)
            mark(status_path(i,"visualizer.failed"))
            log("VISUALIZER FAILED track " .. i)
            if result and result.stderr and result.stderr ~= "" then
                mp.msg.warn("YOMI visualizer " .. i .. ": " .. result.stderr)
            end
        end

        if playing_index == i then write_state(i) end
        job_done(job)
    end)
end

local function sanitize_comment(text)
    local s = tostring(text or "")
    s = s:gsub("https?://%S+","")
    s = s:gsub("[%z\1-\8\11\12\14-\31]","")
    s = s:gsub("%s+"," "):match("^%s*(.-)%s*$") or ""
    local max_chars = math.max(40,math.min(500,tonumber(cfg.comment_max_chars) or 220))
    local count,cut = 0,nil
    for pos in s:gmatch("()[%z\1-\127\194-\244]") do
        count = count + 1
        if count > max_chars then cut = pos-1; break end
    end
    if cut then s = s:sub(1,cut):gsub("%s+%S*$","") .. "…" end
    return s
end

local function comment_allowed(text)
    local mode = tostring(cfg.comment_filter_mode or "Basic safety")
    if mode == "Off" then return true end
    local s = tostring(text or ""):lower()
    local basic_words = {"nigger","faggot","kike","chink","spic"}
    local strict_words = {"fuck","shit","bitch","cunt","cock","dick","pussy","whore","slut"}
    for _,word in ipairs(basic_words) do
        if s:find("%f[%w]" .. word .. "%f[%W]") then return false end
    end
    if s:find("kill yourself",1,true) or s:find("rape you",1,true) then return false end
    if mode == "Strict" then
        for _,word in ipairs(strict_words) do
            if s:find("%f[%w]" .. word .. "%f[%W]") then return false end
        end
    end
    return true
end

local function comment_job(job)
    local i = job.i
    if decorative_done("comment",i) then job_done(job); return end
    log("COMMENT START track " .. i)
    local temp_info = comment_dir .. "\\track-" .. i .. ".comments.info.json"
    os.remove(temp_info)
    local args = ytdlp_common(false)
    table.insert(args,"--skip-download")
    table.insert(args,"--write-info-json")
    table.insert(args,"--write-comments")
    table.insert(args,"--extractor-retries")
    table.insert(args,"1")
    table.insert(args,"--extractor-args")
    table.insert(args,"youtube:player_client=web_embedded,default;comment_sort=top;max_comments=1,1,0,0,1")
    table.insert(args,"--output")
    table.insert(args,comment_dir .. "\\track-" .. i .. ".comments")
    table.insert(args,urls[i])

    run("idle",ytdlp,args,function(success,result)
        local info = load_json(temp_info) or {}
        local comments = info.comments
        local c = type(comments)=="table" and comments[1] or nil
        local text = c and sanitize_comment(c.text) or ""
        if success and result and result.status==0 and c and text~="" and comment_allowed(text) and not exists(status_path(i,"comment.hidden")) then
            local slim = {
                text = text,
                author = c.author or c.author_id or "YouTube viewer",
                author_id = c.author_id or "",
                like_count = tonumber(c.like_count),
                timestamp = tonumber(c.timestamp),
                source = "YouTube relevance sorting"
            }
            local ok,json = pcall(utils.format_json,slim)
            if ok then write_all(comment_path(i),json) end
            log("COMMENT READY track " .. i)
        else
            if not exists(status_path(i,"comment.hidden")) then mark(status_path(i,"comment.failed")) end
            log("COMMENT UNAVAILABLE track " .. i)
        end
        os.remove(temp_info)
        if playing_index==i then write_state(i) end
        job_done(job)
    end)
end

local function probe_one(i,label,path,out_path,failed_path,callback)
    if not exists(path) or fsize(path)<=0 then callback(); return end
    local ffprobe = install_root .. "\\runtime\\ffmpeg\\ffprobe.exe"
    run("idle",ffprobe,{
        "-v","error",
        "-show_entries","format=format_name,duration,size,bit_rate:stream=index,codec_name,codec_long_name,codec_type,profile,width,height,pix_fmt,level,r_frame_rate,avg_frame_rate,sample_rate,channels,channel_layout,bit_rate",
        "-of","json",
        path
    },function(success,result)
        local raw = result and result.stdout or ""
        local parsed = nil
        if raw~="" then
            local ok,value = pcall(utils.parse_json,raw)
            if ok then parsed=value end
        end
        if success and result and result.status==0 and parsed then
            write_all(out_path,raw)
            log("PROBE " .. label .. " READY track " .. i)
        else
            mark(failed_path)
            log("PROBE " .. label .. " FAILED track " .. i)
        end
        callback()
    end)
end

local function probe_job(job)
    local i = job.i
    if decorative_done("probe",i) then job_done(job); return end
    log("PROBE START track " .. i)
    probe_one(i,"AUDIO",audio_path(i),telemetry_audio_path(i),status_path(i,"probe-audio.failed"),function()
        probe_one(i,"VIDEO",video_path(i),telemetry_video_path(i),status_path(i,"probe-video.failed"),function()
            if playing_index==i then write_state(i) end
            job_done(job)
        end)
    end)
end

local function start_job(job)
    queued[job.key] = nil
    job.decorative = (job.kind=="comment" or job.kind=="probe")
    active[job.key] = job
    if job.decorative then decorative_active_count=decorative_active_count+1
    else active_count = active_count + 1 end
    job.started_at = mp.get_time()

    if job.kind == "audio" then source_download(job)
    elseif job.kind == "art" then art_job(job)
    elseif job.kind == "video" then video_job(job)
    elseif job.kind == "viz" then viz_job(job)
    elseif job.kind == "comment" then comment_job(job)
    elseif job.kind == "probe" then probe_job(job)
    else job_done(job) end
end

pump = function()
    if #jobs == 0 then return end

    table.sort(jobs,function(a,b)
        if a.priority == b.priority then return a.i < b.i end
        return a.priority < b.priority
    end)

    while active_count < workers and #jobs > 0 do
        local job = table.remove(jobs,1)
        if queued[job.key] then
            if (job.kind=="comment" or job.kind=="probe") and decorative_active_count>=2 then
                table.insert(jobs,1,job)
                break
            end
            start_job(job)
        end
    end
end

request_bundle = function(i,base_priority)
    if not i or i < 1 or i > #urls or known_bad(i) then return end

    local base = tonumber(base_priority) or 0
    local old = bundle_priority[i]
    if old == nil or base < old then
        bundle_priority[i] = base
    else
        base = old
    end

    if not playback_ready(i) then
        enqueue("audio",i,base)
        return
    end

    -- Once audio + metadata + gain are complete, build the presentation
    -- pieces in a deterministic order. With one Gaming worker this is:
    -- artwork -> tiny video -> visualizer.
    if want_art then enqueue("art",i,base+1) end
    if want_video then enqueue("video",i,base+2) end
    if want_viz then enqueue("viz",i,base+3) end
end

schedule_for_current = function(i)
    if not i or i < 1 then return end

    -- Prepare COMPLETE future bundles, not a pile of future audio files.
    -- Track N+1 is completely prepared before N+2 receives worker time.
    local cursor = i
    for n=1,prefetch_ahead do
        local candidate = next_candidate(cursor,1)
        if not candidate or candidate == i then break end

        request_bundle(candidate,n*10)
        cursor = candidate
    end

    pump()
end

start_play = function(i)
    if not i then
        set_engine_status("error","No playable tracks remain.",0)
        return
    end

    if known_bad(i) then
        local n = next_candidate(i,1)
        if n then
            desired_index = n
            requested_index = 0
            bundle_priority[n] = 0
            set_engine_status(
                "skipping",
                "Skipping unavailable track " .. i .. "; preparing track " .. n .. "...",
                n
            )
            request_bundle(n,0)
            pump()
        else
            set_engine_status("error","No playable tracks remain.",i)
        end
        return
    end

    desired_index = i
    bundle_priority[i] = 0
    if bundle_cache_hit[i] == nil then bundle_cache_hit[i] = bundle_ready(i) end

    if not bundle_ready(i) then
        local stage = bundle_stage(i)

        if stage == "audio" then
            set_engine_status("preparing","Preparing audio for track " .. i .. " of " .. #urls .. "...",i)
        elseif stage == "artwork" then
            set_engine_status("artwork","Preparing artwork for track " .. i .. "...",i)
        elseif stage == "tiny video" then
            set_engine_status("video","Preparing tiny video for track " .. i .. "...",i)
        elseif stage == "visualizer" then
            set_engine_status("visualizer","Preparing visualizer for track " .. i .. "...",i)
        else
            set_engine_status("preparing","Preparing track " .. i .. "...",i)
        end

        request_bundle(i,0)
        pump()
        return
    end

    current_index = i
    desired_index = i
    requested_index = i
    bundle_priority[i] = nil

    write_all(resume_file,tostring(i))
    mp.set_property_number("volume-gain",read_gain(i))
    set_engine_status("starting","Starting synchronized track " .. i .. " of " .. #urls .. "...",i)

    cleanup_cache(i)
    log("BUNDLE READY track " .. i)
    if player_stream_video then
        log("PLAYER VIDEO STREAM track " .. i .. " quality " .. tostring(cfg.player_video_quality))
        mp.commandv("loadfile",urls[i],"replace")
    else
        mp.commandv("loadfile",audio_path(i),"replace")
    end
end

local player_rescue_active = false

local function advance(step)
    player_rescue_active = false
    local base = current_index
    local n = next_candidate(base,step or 1)

    if not n then
        set_engine_status("error","No playable tracks remain.",base)
        return
    end

    desired_index = n
    requested_index = 0
    start_play(n)
end

mp.register_script_message("yomi-next",function() advance(1) end)
mp.register_script_message("yomi-prev",function() advance(-1) end)
mp.register_script_message("yomi-jump",function(raw_index)
    local n = math.floor(tonumber(raw_index) or 0)
    if n < 1 or n > #urls then return end
    player_rescue_active = false
    desired_index = n
    requested_index = 0
    start_play(n)
end)
mp.register_script_message("yomi-hide-comment",function()
    local i = playing_index>0 and playing_index or current_index
    if not i or i<1 then return end
    os.remove(comment_path(i))
    mark(status_path(i,"comment.hidden"))
    log("COMMENT HIDDEN track " .. i)
    if playing_index==i then write_state(i) end
end)

mp.register_event("file-loaded",function()
    playing_index = current_index
    requested_index = 0

    mp.set_property_number("volume-gain",read_gain(current_index))
    record_history(current_index)
    write_state(current_index)

    local info = load_json(meta_path(current_index)) or {}
    local title = info.title or ("Track " .. current_index)
    set_engine_status("playing","Playing: " .. title,current_index)

    log("PLAYING track " .. current_index)
    schedule_for_current(current_index)
    if want_probe then enqueue("probe",current_index,9000) end
    if want_comment then enqueue("comment",current_index,9010) end
    pump()
end)

mp.observe_property("pause","bool",function(_,paused)
    if playing_index > 0 then
        local info = load_json(meta_path(playing_index)) or {}
        local title = info.title or ("Track " .. playing_index)
        if paused then
            set_engine_status("paused","Paused: " .. title,playing_index)
        else
            set_engine_status("playing","Playing: " .. title,playing_index)
        end
    end
end)

mp.register_event("end-file",function(e)
    if player_stream_video and e.reason == "error" and not player_rescue_active and exists(audio_path(current_index)) then
        player_rescue_active = true
        log("PLAYER VIDEO ERROR - RESCUE TO LOCAL AUDIO track " .. current_index)
        set_engine_status("playing","Video stream failed; continuing with cached audio.",current_index)
        mp.commandv("loadfile",audio_path(current_index),"replace")
        return
    end
    if player_rescue_active and (e.reason == "eof" or e.reason == "error") then player_rescue_active = false end
    if e.reason == "eof" or e.reason == "error" then
        local ended = current_index
        playing_index = 0
        requested_index = 0

        set_engine_status("advancing","Track ended; finding the next playable track...",ended)
        log("END track " .. ended .. " reason " .. tostring(e.reason))
        advance(1)
    end
end)

mp.register_event("shutdown",function()
    write_all(resume_file,tostring(current_index))
    set_engine_status("stopped","YOMI stopped.",current_index)
end)

set_engine_status("starting","Preparing playlist...",current_index)

mp.add_timeout(0.15,function()
    start_play(current_index)
end)
