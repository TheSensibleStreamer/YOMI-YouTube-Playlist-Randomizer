#!/usr/bin/env python3
"""Exercise the production Lua visualizer filter with actual FFmpeg.
No user playlist, network, mpv, or Windows machine is required.
"""
import json
import subprocess
import tempfile
import time
from pathlib import Path

from lupa import LuaRuntime

ROOT = Path(__file__).resolve().parents[1]
SOURCE = (ROOT / "payload/app/music.lua").read_text(encoding="utf-8-sig")


def lua_function(name):
    start = SOURCE.find("function " + name + "(")
    assert start >= 0, f"Missing production Lua function {name}"
    end = SOURCE.find("\nend\n", start)
    assert end > start, f"Cannot extract Lua function {name}"
    return SOURCE[start : end + 5]


lua = LuaRuntime(unpack_returned_tuples=True)
lua.execute(
    """
cfg={visualizer_temporal_detail='Enhanced',visualizer_shape='Spectrum',
     visualizer_pixel_size='Extra Chunky',
     visualizer_color_mode='Solid',visualizer_activity='Active',
     visualizer_vertical_anchor='Source',visualizer_direction='Normal'}
function visualizer_fps() return cfg.visualizer_fps=='30 FPS' and 30 or 60 end
function visualizer_frequency_parameters()
  if cfg.visualizer_activity=='Subtle' then return 4,2048,'sqrt','log' end
  return 1,1024,'sqrt','log'
end
function visualizer_audio_prefix() return 'highpass=f=30' end
function visualizer_frequency_trim_filter() return '' end
function visualizer_spacing_filter() return '' end
"""
)
for name in ("visualizer_render_dimensions", "visualizer_color", "visualizer_binary_filter", "visualizer_profile", "visualizer_temporal_rate", "viz_filter"):
    lua.execute(lua_function(name))

assert tuple(lua.globals().visualizer_render_dimensions()) == (192, 8), "Coarse source has eight vertical cells and eight selectable frequency units"
assert lua.globals().visualizer_color() == "0xFFFFFF", "Every cached clip must use a neutral color"
assert "255,0" in lua.globals().visualizer_binary_filter(), "Cached FFmpeg frames must contain neutral white occupancy"
profile_before = str(lua.globals().visualizer_profile())
lua.globals().cfg.visualizer_color_mode = "Gradient"
lua.globals().cfg.visualizer_gradient_preset = "Rainbow"
lua.globals().cfg.visualizer_solid_color = "#AABBCC"
assert str(lua.globals().visualizer_profile()) == profile_before, "Color changes must never regenerate per-song cache"
for preset, dimensions in (("Chunky", (512, 16)), ("Fine", (768, 24)), ("Extra Fine", (1440, 36))):
    lua.globals().cfg.visualizer_pixel_size = preset
    assert tuple(lua.globals().visualizer_render_dimensions()) == dimensions, (preset, dimensions)
lua.globals().cfg.visualizer_pixel_size = "Extra Chunky"

def call_filter(fps, shape="Spectrum", mode="Enhanced", activity="Active"):
    lua.globals().cfg.visualizer_fps = f"{fps} FPS"
    lua.globals().cfg.visualizer_shape = shape
    lua.globals().cfg.visualizer_temporal_detail = mode
    lua.globals().cfg.visualizer_activity = activity
    return str(lua.globals().viz_filter())

def run(args):
    proc = subprocess.run(args, capture_output=True, text=True, timeout=45)
    if proc.returncode:
        raise AssertionError(f"Command failed: {' '.join(args)}\n{proc.stderr[-1600:]}")
    return proc.stdout

with tempfile.TemporaryDirectory(prefix="yomi-temporal-") as tmpdir:
    tmp = Path(tmpdir)
    # Include regular low-frequency sound; independent rate inputs prevent
    # a fixed 48 kHz assumption from silently drifting with 44.1 kHz music.
    for sr in (44100, 48000):
        input_wav = tmp / f"source-{sr}.wav"
        run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-f", "lavfi",
             "-i", f"sine=frequency=125:sample_rate={sr}:duration=3",
             "-c:a", "pcm_s16le", str(input_wav)])
        for fps, shape, activity in ((60, "Spectrum", "Active"),
                                     (30, "Spectrum", "Subtle"),
                                     (60, "Center Mirror", "Active")):
            expr = call_filter(fps, shape=shape, activity=activity)
            assert f":rate={2*fps}" in expr, expr
            assert "tblend=all_mode=lighten" in expr, expr
            assert "aresample=48000" in expr, expr
            assert f"fps={fps}" in expr, expr
            assert "setpts=N/(" in expr, expr
            output = tmp / f"{sr}-{fps}-{shape.replace(' ','-')}.mp4"
            start = time.monotonic()
            run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
                 "-i", str(input_wav), "-filter_complex", expr, "-map", "[v]",
                 "-an", "-r", str(fps), "-fps_mode", "cfr", "-c:v", "libx264",
                 "-preset", "ultrafast", "-qp", "0", "-g", "1",
                 "-bf", "0", "-threads", "1", "-filter_complex_threads", "1",
                 str(output)])
            info = json.loads(run(["ffprobe", "-v", "error", "-select_streams", "v:0",
                                   "-show_entries", "stream=nb_frames,duration,r_frame_rate,width,height",
                                   "-of", "json", str(output)]))["streams"][0]
            frames = int(info["nb_frames"])
            seconds = float(info["duration"])
            assert abs(frames - fps * 3) <= 1, (sr, fps, shape, frames)
            assert abs(seconds - 3) <= 1/fps + 0.001, (sr, fps, shape, seconds)
            # Extra Chunky retains eight rows even for the centered mirror.
            expected_height = 8
            assert (int(info["width"]), int(info["height"])) == (192, expected_height), (shape, info)
            print(f"PASS enhanced {shape} {sr} Hz, {fps} FPS: {frames} frames, {seconds:.3f}s, render {time.monotonic()-start:.3f}s")

    for fps in (30, 60):
        legacy = call_filter(fps, mode="Standard")
        assert "tblend=" not in legacy and "aresample=48000" not in legacy
        assert f":rate={fps}" in legacy
    scope = call_filter(60, shape="Oscilloscope")
    assert "showwaves=" in scope and "tblend=" not in scope
    assert "aresample=48000" not in scope

print("PASS: guarded standard mode, untouched oscilloscope, no output FPS increase")
