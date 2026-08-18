# YOMI Changelog

## v4.2.0

Current public Windows build.

### Identity and updates

- Expanded the product name as **YOMI — YouTube OBS Music Interface**
- Added automatic update availability checks, limited to once every 24 hours
- Added Settings → Components → Check for Updates
- Added an official-repository `update.json` contract
- Added mandatory SHA-256 verification before opening a downloaded installer
- Updates remain opt-in and use the normal interactive installer

### Modular OBS system

- Preserved the original combined overlay as the default
- Added six configurable Director output groups
- Added fixed single-module Browser Source URLs
- Added Broadcast Strip, Horizontal, Stack, Cards, Terminal, Timeline, and Single layouts
- Added synchronized progress, stats, technical telemetry, pipeline, comment, history, Up Next, and mission modules
- Added twelve broadcast-world themes and scene rotation

### Cache and Controller

- Unified split look-ahead behavior into 1–20 complete tracks ready ahead
- Added Maximum caching with 8 workers
- Added an expandable previous/current/next Controller table
- Added exact-occurrence double-click jumps without deduplicating the playlist

### Visualizer and video

- Added 30 or 60 FPS visualizer generation
- Added 30 FPS or prefer-genuine-60-FPS video selection
- Added automatic Browser Source FPS resolution
- Added live 0–60% high-frequency trim
- Added a visualizer resolution ladder from Giant Blocks (20×6) through Ultra Fine (128×32)
- Added seven visualizer shapes, anchoring, spacing, glow, and frequency scale

### Presets and settings

- Added Default, named, and auto-Custom presets to Text & Style, Visualizer, and Performance
- Existing hand-tuned visualizer/performance settings migrate as Custom
- Canvas and media dimensions switch their existing preset selectors to Custom when edited
- Removed the modal popup after ordinary saves
- Added Restore Default Settings while preserving playlist, history, components, and Defender choice

### Windows Defender performance

- Implemented the installer option that earlier documentation described but the installer failed to execute
- Added a visible unchecked opt-in on the first installer screen
- Adds only `C:\Program Files\YOMI\runtime\yt-dlp\yt-dlp.exe` as a process exclusion
- Records ownership only when YOMI itself creates the exclusion
- Preserves matching user-managed exclusions
- Removes only a YOMI-owned exact exclusion during opt-out or uninstall
- Stops uninstall safely if a YOMI-owned exclusion cannot be removed

### Artwork and media

- Carries forward canonical any-color Smart Crop and FFmpeg dark-border fallback
- Adds 240p and 480p choices alongside 144p, 360p, 720p, and Best
- Adds independent maximum/lowest-compatible video and audio policies
- Adds 64, 128, 160 kbps, and Best audio targets

## v4.0.9.4

- Published the complete public source and packaged installer
- Added canonical smart-artwork crop fixes
- Documented the narrow Windows Defender yt-dlp process-exclusion design

## v4.0.8

- Initial public YOMI 4 Windows build
- YouTube playlist randomizer and lightweight player
- Combined OBS artwork/video/title/channel/visualizer overlay
- Custom appearance, worker modes, component management, and uninstall workflow
