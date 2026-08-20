# YOMI Changelog

## v4.2.0.5

### Sources header geometry

- Removed the invisible overlap between the Sources column-header labels and the first source row
- Assigned the headings a compact 18-pixel header band with a visible gap before the interactive controls
- Rebalanced the row and footer spacing so all thirteen sources remain cleanly inside the page

### Title and channel spacing

- Expanded the Text & Style spacing choices with Extra Loose and Maximum
- Made Loose, Extra Loose, and Maximum separation scale upward with larger media layouts
- Kept Tight and Normal behavior stable so existing default overlays retain their established appearance

### Automatic updater startup check

- Normal Controller/player startup now checks the tiny public manifest every session instead of suppressing newly published releases behind an earlier 24-hour no-update check
- After a user declines an update, that same version stays quiet for 30 days; any newer version still prompts immediately
- Added a cross-process check lock so opening Settings and Controller together cannot create duplicate prompts

## v4.2.0.4

### Video quality selection

- Corrected the 30 FPS maximum-quality ladder so YouTube's requested quality label, literal height, and compatible capped MP4 formats are tried before lower-resolution recovery
- Handles nonstandard stored dimensions such as a YouTube-labeled 240p format whose actual frame is 352×288
- Expanded the 144p/240p primary extraction route across the proven embedded and default YouTube clients
- Added the selected format ID, height, frame rate, and container to successful video-cache log entries
- Preserved 144p as the final reliability recovery when a requested higher format genuinely is not available

### Settings layout polish

- Widened the Sources-page name column so Featured Comment and other module names stay clear of the dimension fields
- Shortened the Sources guidance line and added bounded-label overflow protection for Windows font and DPI variations
- Added breathing room between the Visualizer explanation and its bordered workload readout

## v4.2.0.3

Director Mode usability and correctness release for the YOMI 4.2.0 generation.

### Source manager

- Added a dedicated Sources page covering all thirteen individual module routes
- Added per-source enable switches, editable OBS dimensions, visible URLs, Copy buttons, and live Preview buttons
- Added Default, Split Essentials, Text Only, Information Desk, Culture Desk, Full Science, and auto-Custom source presets
- Added enabled-source packs with exact OBS dimensions to the generated setup guide

### Grouped outputs

- Renamed the advanced page to Groups 1–6 and added Copy and Preview beside every row
- Replaced raw module-string editing with an ordered checklist and Move Up / Move Down controls
- Automatically enables Featured Comment and History prerequisites when an active source/group selects those modules

### Correctness and lifecycle

- Individual artwork, video, visualizer, and comment routes now activate their own required preparation work
- Disabled individual routes remain genuinely disabled and do not silently add cache work
- Pipeline-affecting saves now request a controlled stop/restart through the existing Controller; browser-only changes remain live
- Routed the last direct Smart Crop FFprobe fallback through `PriorityRun.exe idle`
- Expanded diagnostics with every individual source, enable state, dimensions, and URL

## v4.2.0.1

Preset and visualizer expansion for the YOMI 4.2.0 generation.

### Presets everywhere useful

- Added Default, named, and auto-Custom page presets to General / Player
- Added complete-page presets to the classic OBS Overlay page
- Added Director Mode presets for Pirate Radio, Culture Desk, Control Room, and Full Science
- Added six-row Outputs presets for Minimal, Split Essentials, Broadcast Desk, and Full Studio arrangements
- Existing installations preserve their values and migrate newly tracked preset pages as Custom

### Visualizer range and feedback

- Extended generated resolution below Giant Blocks to Monolith 12×4 and Mega Blocks 16×5
- Extended high-detail options through Microscopic 160×40 and Maximum Detail 192×48
- Added live sampled-pixel count, default-relative cost, frame-rate, and detail classification
- Added Monolith Efficiency and Signal Analyzer visualizer presets
- Disabled irrelevant solid/gradient controls automatically according to the selected color mode

### Patch-line and interface polish

- Centralized the installed version used by Controller, Settings, diagnostics, status, component downloads, and OBS instructions
- Browser Source cache-busting URLs now derive from the complete patch version
- Kept the full **YOMI — YouTube OBS Music Interface** identity throughout the product
- Refreshed public OBS documentation for Director Mode, 60 FPS, and 8-worker caching

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
