# YOMI — YouTube OBS Music Interface

**A lightweight Windows YouTube playlist randomizer, music player, and modular OBS now-playing system.**

YOMI shuffles a YouTube playlist properly, preserves intentional duplicate occurrences, plays from a local prepared audio cache, and can feed OBS with artwork, synchronized tiny video, title, channel, retro audio visualization, technical telemetry, comments, history, and upcoming-track information—without keeping Chrome open.

If you were searching for a **YouTube playlist randomizer**, **lightweight YouTube music player for Windows**, **OBS YouTube music overlay**, **OBS now-playing overlay**, or a configurable collection of music Browser Sources, that is what YOMI is built to do.

## Download

The current public build is **YOMI v4.2.0.4**. Compatible improvements use the fourth version component while remaining within the 4.2.0 generation.

Download [`YOMI-v4.2.0.4.zip`](./YOMI-v4.2.0.4.zip), fully extract it, then double-click:

```text
INSTALL YOMI.cmd
```

The installer places application files in `C:\Program Files\YOMI` and writable user data in `%LOCALAPPDATA%\YOMI`.

## Core player

- Re-reads the latest YouTube playlist whenever you choose **Shuffle Playlist**
- Preserves every playlist occurrence, including intentional duplicates
- Uses a Fisher–Yates shuffle
- Prepares local audio bundles for reliable transitions
- Supports Player and Streamer / OBS modes
- Uses mpv and yt-dlp without requiring Chrome

Player video choices are Off/audio-only, 144p, 240p, 360p, 480p, 720p, and Best. Video can use a 30 FPS ceiling or prefer genuine 60 FPS when YouTube offers it; YOMI does not fake 60 FPS by duplicating frames.

## OBS overlay and Director Mode

The original combined Browser Source remains the default. It can contain:

- Artwork / YouTube thumbnail
- Tiny synchronized video
- Song title
- Channel name
- Retro audio visualizer

Director Mode is optional and adds a native source manager plus six configurable output groups. You can keep everything together, split each module into its own Browser Source, or shovel several modules into any output.

Available modules include artwork, video, title, channel, visualizer, progress, stats, technical telemetry, pipeline status, featured comment, history, Up Next, and mission metrics. All sources share one player clock and prepared media cache, so splitting the layout does not redownload the track.

Settings → **Sources** gives every individual module its own On switch, editable OBS dimensions, visible URL, Copy button, and live Preview button. Settings → **Groups 1–6** adds row-level Copy/Preview controls and an ordered checklist editor for choosing modules. Enabling an individual media source now activates its required preparation work instead of relying on a hidden classic-overlay or output-group dependency.

## Visualizer

The visualizer supports:

- 30 or 60 FPS generation
- Seven shapes: Spectrum, Center Mirror, Oscilloscope, Dots, Skyline, Particle Field, and Twin Rails
- Bottom, center, top, or source anchoring
- Solid, rainbow, and gradient colors
- Horizontal or vertical gradients
- Bar spacing and peak glow
- Logarithmic or linear frequency scale
- Live 0–60% high-frequency trim to remove an inactive far-right tail
- Generation resolutions from **Monolith (12×4)** to **Maximum Detail (192×48)**

The lower-resolution Monolith/Mega/Giant choices produce deliberately enormous pixels and normally reduce visualizer generation and cache cost. Ultra Fine, Microscopic, and Maximum Detail go aggressively in the other direction. Settings shows the exact sampled-pixel count and its cost relative to the default 40×10 source.

## Presets and customization

General / Player, the complete OBS Overlay page, Text & Style, Visualizer, Performance, Director Mode, Sources, and Groups 1–6 expose **Default**, named presets, and **Custom**. Changing any underlying option automatically marks only that page Custom.

Source presets can build Split Essentials, Text Only, Information Desk, Culture Desk, or Full Science individual-source packs. Group presets can build a minimal title source, six split essentials, a broadcast desk, or a full studio in one selection. Existing 4.2.0 installations preserve their hand-tuned values during migration.

## Complete-track cache

YOMI can prepare 1–20 complete future tracks. “Complete” means audio, metadata, gain analysis, and every artwork/video/visualizer asset required by the currently enabled classic or Director outputs.

Background preparation modes:

- Gaming / Lowest overhead — 1 worker
- Balanced — 2 workers (default)
- Fast caching — 4 workers
- Maximum caching — 8 workers

Eight workers can fill a deep cache faster, but may heavily use CPU, network, disk, and Windows Defender simultaneously. Balanced remains the default.

## Controller queue navigator

**Show queue** expands the Controller into a table containing recent plays, the current position, and the next 20 shuffled positions. It includes readiness status, and double-clicking a row jumps to that exact playlist occurrence.

## Windows Defender performance opt-in

The first installer screen visibly offers an **unchecked, explicit opt-in** to reduce Defender CPU spikes during track changes.

It adds only this exact process exclusion:

```text
C:\Program Files\YOMI\runtime\yt-dlp\yt-dlp.exe
```

YOMI never excludes PowerShell, `%TEMP%`, mpv, Deno, a user profile, or broad YOMI folders. A YOMI-created exclusion is recorded as YOMI-owned and removed during uninstall. A matching pre-existing user-managed exclusion is never claimed or removed. Settings → Performance displays whether the YOMI-managed option is enabled.

## Built-in updater

Controller and Settings perform at most one low-overhead automatic update check every 24 hours. Settings → Components also provides **Check for Updates**.

When a newer build exists, YOMI asks before downloading anything. The package URL must remain inside the official repository, and the downloaded ZIP must match the SHA-256 in [`update.json`](./update.json) before YOMI opens the normal interactive installer. YOMI does not silently replace running program files.

## Settings behavior

Ordinary saves are silent; the button briefly changes to **SAVED**. When a saved change affects player or preparation behavior, Settings asks the existing Controller to perform a controlled automatic restart and reports **SAVED — RESTARTING**. Browser-only styling and source-dimension changes continue refreshing live without interrupting playback. **Restore Default Settings** resets YOMI options and layouts while preserving the playlist, playback history, installed components, and Defender choice.

## Smart artwork crop

YOMI normalizes thumbnails before using a compiled any-color edge detector. It can remove sufficiently uniform black, white, gray, blue, beige, red, and other edge bands while applying conservative crop safeguards. A proven FFmpeg dark-border detector remains as a fallback.

## Components

Core:

- **mpv** — playback engine
- **yt-dlp** — YouTube extraction and playlist handling

Optional:

- **Deno / YouTube Compatibility** — modern YouTube JavaScript challenge support
- **FFmpeg Media Tools** — loudness leveling, artwork crop, telemetry probes, and visualizer generation

Optional components can be installed or removed from Settings.

## Uninstalling

Use Settings → Components → Uninstall YOMI, the Start Menu shortcut, or:

```text
C:\Program Files\YOMI\Uninstall YOMI.cmd
```

The uninstaller can remove everything or preserve `config.json` and `playlist.txt`. It removes a Defender exclusion only when YOMI's ownership marker proves YOMI added that exact path.

## Privacy and isolation

YOMI does not package a user's playlist, cache, logs, playback history, or concrete YouTube video IDs. User data is stored separately under `%LOCALAPPDATA%\YOMI`.

YOMI uses a local IPC pipe and a local HTTP overlay server. It does not require OBS WebSocket.

YOMI is not affiliated with YouTube/Google, OBS Project, mpv, yt-dlp, Deno, FFmpeg, or their maintainers.

## Project status

YOMI is actively developed and tested with large real-world YouTube playlists and OBS. If something breaks, open a GitHub Issue with the YOMI version and a concise description.

## Credits

**Created and designed by TheSensibleStreamer**

Powered by **mpv**, **yt-dlp**, **Deno**, and **FFmpeg**.

Development assistance by **ChatGPT**.
