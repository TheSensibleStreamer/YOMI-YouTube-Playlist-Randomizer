# YOMI — YouTube Playlist Randomizer & Player

**A lightweight Windows YouTube playlist randomizer and music player with optional OBS integration for streamers.**

YOMI is built for people who want to take a YouTube playlist, shuffle it properly, and play it without keeping a full browser open. It can run as a low-overhead audio-first YouTube playlist player, or switch into **Streamer / OBS mode** and drive a single OBS Browser Source with artwork, tiny synchronized video, song title, channel name, and a retro pixel audio visualizer.

If you were looking for a **YouTube playlist randomizer**, **YouTube playlist player for Windows**, **OBS YouTube music overlay**, **OBS now-playing overlay**, or a lightweight way to play a large YouTube music playlist on stream, that is the problem YOMI is designed to solve.

## Download

The current public build is **YOMI v4.0.9.4**.

Download [`YOMI-v4.0.9.4.zip`](./YOMI-v4.0.9.4.zip), fully extract it, then double-click:

```text
INSTALL YOMI.cmd
```

The installer keeps application files in `C:\Program Files\YOMI` and writable user data in `%LOCALAPPDATA%\YOMI`.

## What YOMI does

### YouTube playlist randomizer

Paste a YouTube playlist URL or playlist ID. **Shuffle Playlist** re-reads the latest playlist from YouTube, preserves every playlist occurrence including intentional duplicates, creates a fresh Fisher-Yates random order, resets track-number cache mapping, and starts again from track 1.

This means you do not have to separately refresh the playlist whenever you add songs on YouTube. Shuffle is the normal update action.

### Lightweight Player mode

Player mode is designed to work without OBS. Audio-only playback is the lowest-overhead option, with optional normal video playback at:

- Off / audio only
- 144p
- 360p
- 720p
- Best

Player mode does not launch the OBS overlay server or build artwork, tiny-video, or visualizer presentation jobs.

### Streamer / OBS mode

Streamer mode adds an optional **one-Browser-Source OBS overlay**. Available modules include:

- YouTube thumbnail / artwork
- Tiny synchronized video
- Song title
- Channel name
- Retro pixel audio visualizer

Modules can be disabled independently, and disabled modules do not consume their normal preparation work.

Open **YOMI Settings → OBS Overlay** or use **OBS Setup** in the Controller for the exact Browser Source URL, canvas dimensions, and recommended FPS for your current configuration.

## Overlay customization

YOMI 4 includes a deliberately broad but manageable customization system rather than hundreds of tiny settings.

- Multiple curated fonts spanning understated, serif, heavy, playful, handwritten, and monospace styles
- Text color and outline color
- Outline thickness and text opacity
- Optional subtle text glow
- Square, soft-rounded, or rounded media corners
- Media border on/off, width, and color
- Style presets: Classic, Minimal, Retro, Neon, Pastel, Arcade
- Live overlay preview
- Left/right mirrored layouts
- Top-left, top-right, bottom-left, and bottom-right placement
- Independent canvas and media-size presets

The artwork and tiny-video boxes use a 2px dark frame by default, and the visualizer begins at the actual media edge rather than inheriting the text padding.

## Retro visualizer

The OBS visualizer can be adjusted without regenerating the entire presentation cache.

- Activity: Subtle / Normal / Active / Punchy
- Opacity: 5–80%
- Pixels: Extra Chunky / Chunky / Fine
- Length: Short / Medium / Wide / Extra Wide
- Color: Solid / Rainbow / Gradient
- Gradient presets: Sunset / Ocean / Pastel / Fire / Forest / Mono
- Gradient orientation: Horizontal / Vertical
- Bars: Normal / Mirrored
- Layer: Behind text / Above text

## Performance modes

YOMI prepares future tracks in the background so transitions can be ready before the current song ends.

- **Gaming / Lowest overhead — 1 worker**
- **Balanced — 2 workers** *(fresh-install default)*
- **Fast caching — 4 workers**

A worker is one background track-bundle preparation slot for yt-dlp / FFmpeg work. Balanced is intended to provide useful preparation headroom without pushing background work as aggressively as Fast caching.

Browser Source FPS can be Auto, 15, or 30. Auto uses 30 FPS with the visualizer enabled in Streamer mode and 15 FPS otherwise.

## Smart artwork cropping

YOMI can detect and remove solid or near-solid padding around YouTube thumbnails before creating the final artwork asset. The detector is not limited to black letterboxing: it samples all four image edges, tolerates normal JPEG/WebP compression noise, and can crop black, white, gray, blue, beige, red, or other sufficiently uniform edge bands while using conservative safeguards to avoid eating real artwork.

## Tiny-video reliability

Tiny OBS video is prepared separately from audio. YOMI uses multiple yt-dlp client/format routes with retries, including low-resolution and progressive fallbacks. If every tiny-video route fails for a particular YouTube item, the rest of the prepared presentation can still play instead of wedging the entire playlist.

## Windows Defender performance option

The installer offers an **unchecked, explicit opt-in** to reduce Windows Defender CPU spikes during track changes. It adds only YOMI's bundled `yt-dlp.exe` as a process exclusion; it never excludes PowerShell, `%TEMP%`, mpv, Deno, or the whole user profile. An exclusion added by YOMI is recorded and removed by the YOMI uninstaller. Pre-existing user-managed exclusions are left alone.

## Components

Core components:

- **mpv** — playback engine
- **yt-dlp** — YouTube extraction / playlist handling

Optional components:

- **Deno / YouTube Compatibility** — improves modern yt-dlp YouTube extraction support
- **FFmpeg Media Tools** — enables loudness leveling, smart artwork crop, and the retro visualizer

Components can be installed or removed from **Settings → Components**. Features that require a missing optional component are visibly disabled.

## Common use cases

YOMI is useful for:

- Randomizing a very large YouTube playlist without browser shuffle behavior
- Running background YouTube music on Windows with low overhead
- Adding a YouTube music now-playing display to OBS
- Showing song artwork, title, uploader/channel, and tiny video on stream
- Adding a customizable retro spectrum visualizer to an OBS music overlay
- Keeping a streamer music player separate from Chrome and normal browser tabs

## Controller

The YOMI Controller provides:

- Previous
- Play / Pause
- Next
- Shuffle Playlist
- Settings
- OBS Setup
- Open Data Folder
- Stop YOMI
- Hide to tray
- Exit

Closing the Controller normally stops YOMI. **Hide to tray** is the explicit option for keeping playback running while hiding the window.

## Uninstalling

YOMI can be uninstalled from **Settings → Components → Uninstall YOMI**, from the Start Menu, or directly with:

```text
C:\Program Files\YOMI\Uninstall YOMI.cmd
```

The uninstaller can either remove everything or preserve the user's playlist/config for a future reinstall while clearing disposable cache/log data.

## Architecture

Player mode:

```text
Controller → Supervisor → mpv + music.lua
```

Streamer mode:

```text
Controller → Supervisor → mpv/music.lua
                         → local range-enabled HTTP server
                         → one OBS Browser Source
```

Streamer presentation bundles are prepared before playback: audio/metadata/gain, artwork, tiny video, and visualizer assets as enabled. Future tracks are prefetched in the background according to the selected worker count.

YOMI uses a local IPC pipe and local HTTP overlay server. It does **not** require Chrome or OBS WebSocket.

## Privacy and isolation

YOMI does not package a user's playlist, cache, logs, or playback history. User data is stored separately from Program Files under `%LOCALAPPDATA%\YOMI`.

YOMI is not affiliated with YouTube/Google, OBS Project, mpv, yt-dlp, FFmpeg, or their respective maintainers.

## Project status

YOMI is a new public project and is still being actively tested across real-world YouTube playlists and OBS setups. If something behaves strangely, open a GitHub Issue with the YOMI version and a concise description of what happened.

## Credits

**Created and designed by TheSensibleStreamer**

Powered by **mpv**, **yt-dlp**, and **FFmpeg**.

Development assistance by **ChatGPT**.
