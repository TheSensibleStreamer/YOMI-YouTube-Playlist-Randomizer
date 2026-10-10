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
function visualizer_frequency_trim_filter() return '' end
function visualizer_spacing_filter() return '' end
"""
)
for name in ("visualizer_frequency_parameters", "visualizer_audio_prefix", "visualizer_spectrum_floor_filter", "visualizer_render_dimensions", "visualizer_color", "visualizer_binary_filter", "visualizer_profile", "visualizer_temporal_rate", "viz_filter"):
    lua.execute(lua_function(name))

assert tuple(lua.globals().visualizer_render_dimensions()) == (192, 8), "Coarse source has eight vertical cells and eight selectable frequency units"
assert lua.globals().visualizer_color() == "0xFFFFFF", "Every cached clip must use a neutral color"
assert "255,0" in lua.globals().visualizer_binary_filter(), "Cached FFmpeg frames must contain neutral white occupancy"
profile_before = str(lua.globals().visualizer_profile())
assert profile_before.startswith("r61106544-automatic-musical-detail|"), "Old pre-rendered clips must not bypass finer frequency preparation"
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

    # Reproduce the operator's permanently lit bottom rows from production
    # showfreqs, rather than passing a synthetic image through a test-only path.
    # Silence must generate zero visualizer occupancy; a narrow-band tone must
    # not fabricate a full-width bottom row. Broadband noise is intentionally
    # NOT used here: it genuinely contains energy across the entire spectrum.
    expr = call_filter(60, shape="Spectrum", activity="Active")
    assert "showfreqs=s=192x64" in expr, "Analyze eight times the visible vertical resolution"
    assert "crop=192:16:0:46,scale=192:8:flags=area" in expr, "Preserve fine musical amplitude detail but remove FFmpeg's false silence floor"
    assert "dynaudnorm=f=700:g=15:p=0.85:m=16" in expr, "One bounded analysis gain for every master volume"
    assert "ascale=cbrt" in expr and "fscale=log" in expr, "Perceptual octaves and high-resolution cube-root musical dynamics are automatic"
    for source, definition in (
        ("silence", "anullsrc=channel_layout=stereo:sample_rate=48000"),
        ("music", "sine=frequency=440:sample_rate=48000:duration=1"),
    ):
        target = tmp / (source + "-occupancy.rgb")
        run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
             "-f", "lavfi", "-i", definition, "-t", "1",
             "-filter_complex", expr, "-map", "[v]", "-an",
             "-pix_fmt", "rgb24", "-f", "rawvideo", str(target)])
        rgb = target.read_bytes()
        stride = 192 * 8 * 3
        assert len(rgb) >= stride * 40 and len(rgb) % stride == 0
        frames = len(rgb) // stride
        occupied = sum(rgb[i] > 128 for i in range(0, len(rgb), 3))
        low_row = sum(rgb[(f * 8 * 192 + 7 * 192 + x) * 3] > 128
                      for f in range(frames) for x in range(192))
        if source == "silence":
            assert occupied == 0, ("Silent sound must never illuminate a raster baseline", occupied)
        else:
            assert occupied > 0, "Normal music-level input must move the spectrum"
            assert 0 < low_row < frames * 192, "Permanent lower spectrum row returned"
        print(f"PASS {source}: {occupied} lit frequency cells across {frames} FFmpeg frames")

    # One Normal activity choice must cover different master-volume levels.
    # This test exercises the real filter on identical frequency content at
    # radically different levels. No per-song gain/preference is passed.
    lua.globals().cfg.visualizer_activity = "Normal"
    lua.globals().cfg.visualizer_shape = "Spectrum"
    stable = str(lua.globals().viz_filter())
    assert "dynaudnorm=f=700:g=15:p=0.85:m=16" in stable
    assert "ascale=cbrt" in stable and "fscale=log" in stable
    # Old manual settings from 9043 are intentionally non-authoritative.
    baseline=stable
    lua.globals().cfg.visualizer_frequency_scale="Linear"
    lua.globals().cfg.visualizer_adaptive_fill="Aggressive"
    lua.globals().cfg.visualizer_high_frequency_lift_db=12
    lua.globals().cfg.visualizer_high_frequency_trim=60
    subtle = call_filter(60, shape="Spectrum", activity="Subtle")
    assert subtle==baseline, "Old linear / activity / fill / trim / lift cannot defeat the one Automatic response"
    assert str(lua.globals().visualizer_profile()) != profile_before or baseline==subtle
    assert "volume=" not in stable, "No fixed post-normalizer gain or output clipping"
    assert "highpass=f=30" in stable
    for amplitude in (0.002, 0.02, 0.2):
        target = tmp / ("normal-" + str(amplitude) + ".rgb")
        source = "anoisesrc=color=pink:sample_rate=48000:amplitude=" + str(amplitude)
        run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
             "-f", "lavfi", "-i", source, "-t", "3",
             "-filter_complex", stable, "-map", "[v]", "-an",
             "-pix_fmt", "rgb24", "-f", "rawvideo", str(target)])
        rgb = target.read_bytes()
        assert len(rgb) % (192 * 8 * 3) == 0
        occupied = sum(rgb[n] > 128 for n in range(0, len(rgb), 3))
        assert occupied > 0, ("Normal activity should reveal actual quiet audio", amplitude)
        print(f"PASS single Normal activity at source amplitude {amplitude}: {occupied} spectral cells")

print("PASS: one Automatic musical response, 8x source definition, real silence, unchanged oscilloscope and 30/60 CFR")
