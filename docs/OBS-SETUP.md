# YOMI OBS Setup

YOMI can run as a normal YouTube playlist randomizer/player without OBS. OBS integration is optional and is enabled through **Streamer / OBS** mode.

## Quick setup

1. Open **YOMI Settings**.
2. Set the application mode to **Streamer / OBS**.
3. Enable the overlay modules you want: artwork, tiny video, song title, channel name, and/or visualizer.
4. Save the settings.
5. Open **OBS Setup** from YOMI Settings or the YOMI Controller.
6. Add a single **Browser Source** in OBS using the URL and dimensions YOMI shows for the current configuration.

YOMI intentionally uses **one Browser Source** for the complete music overlay instead of requiring separate browser sources for artwork, video, text, and the visualizer.

## Overlay modules

- Artwork / YouTube thumbnail
- Tiny synchronized video
- Song title
- Channel / uploader name
- Retro pixel audio visualizer

Disabled modules do not perform their normal preparation work.

## Layout

YOMI supports Top Left, Top Right, Bottom Left, and Bottom Right placement. Right-side layouts mirror the presentation order so the text and media remain visually coherent.

Canvas size and media size are independent. Common canvas presets include 1280×720, 1920×1080, 2560×1440, and 3840×2160, with custom dimensions available.

## Browser Source FPS

- Auto
- 15 FPS
- 30 FPS

Auto uses 30 FPS when the visualizer is enabled in Streamer mode and 15 FPS otherwise.

## Performance

YOMI prepares upcoming media in the background:

- Gaming / Lowest overhead — 1 worker
- Balanced — 2 workers, default
- Fast caching — 4 workers

Balanced is intended to be the best general-purpose choice for streamers who want useful preloading without unnecessary background pressure.

## No browser or OBS WebSocket dependency

YOMI does not require Chrome to remain open and does not require OBS WebSocket. The overlay is served locally to the OBS Browser Source.
