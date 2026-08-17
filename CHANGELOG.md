# YOMI Changelog

## v4.0.8

Current public Windows build.

### Core
- YouTube playlist randomizer and lightweight playlist player
- Player mode with audio-only, 144p, 360p, 720p, and Best video options
- Streamer / OBS mode using one Browser Source
- Shuffle Playlist re-reads the current YouTube playlist, preserves duplicate occurrences, performs a Fisher-Yates shuffle, resets cache mapping, and restarts from track 1

### OBS overlay
- Artwork / thumbnail
- Tiny synchronized video
- Song title
- YouTube channel name
- Retro pixel audio visualizer
- Left/right mirrored layouts and four-corner positioning
- Independent canvas and media sizes

### Appearance
- Curated font selection
- Text and outline colors
- Text opacity, outline thickness, optional glow
- Media borders and corner styles
- Classic, Minimal, Retro, Neon, Pastel, and Arcade presets
- Solid, rainbow, and gradient visualizer modes
- Readable live overlay preview using generic sample content only

### Performance
- Gaming / Lowest overhead: 1 worker
- Balanced: 2 workers and the fresh-install default
- Fast caching: 4 workers
- Disabled OBS modules do not perform their normal preparation jobs

### Media handling
- Any-color smart thumbnail edge-band cropping with conservative safeguards
- Multiple tiny-video yt-dlp client / format retry routes
- Optional Deno compatibility component
- Optional FFmpeg media-tools component

### Windows UX
- Controller with Previous, Play/Pause, Next, Shuffle Playlist, Settings, OBS Setup, Data Folder, Stop, Hide to tray, and Exit
- Closing Controller stops playback; Hide to tray explicitly keeps it running
- Uninstall available from Settings, Start Menu, and the Program Files YOMI directory
- Root installer launcher is clearly named `INSTALL YOMI.cmd`

### Package cleanup
- Exactly two current multi-resolution ICO assets
- PNG-compressed icon layers
- Public package scrubbed for private development playlist/cache data and concrete YouTube IDs
