# YOMI OBS Setup

YOMI — YouTube OBS Music Interface can run as a normal playlist randomizer/player without OBS. OBS integration is optional and is enabled through **Streamer / OBS** mode.

## Classic combined overlay

The original combined overlay remains the default and normally needs one Browser Source.

1. Open **YOMI Settings**.
2. Choose a General / Player preset or set the application mode to **Streamer / OBS**.
3. On **OBS Overlay**, choose a page preset or configure artwork, tiny video, title, channel, and visualizer individually.
4. Save.
5. Open **OBS Setup** from Settings or Controller.
6. Add a Browser Source using the exact URL, dimensions, and FPS YOMI provides.

Classic overlay page presets include Default, Gaming Light, Artwork Radio, Full Strip, and Video Showcase. Editing an underlying field marks that page Custom.

## Director Mode and split sources

Director Mode is opt-in. It exposes fixed one-module routes through **Sources** and `/source/1` through `/source/6` through **Groups 1–6**.

### Individual-source workflow

1. Open **General / Player** and select **Streamer / OBS**.
2. Open **Director Mode** and enable it or choose a named preset.
3. Open **Sources** and choose a source preset or enable individual rows.
4. Set each Browser Source width and height if the recommended dimensions do not fit your scene.
5. Save. YOMI restarts itself only when the player/preparation pipeline needs to reload.
6. Use **Copy** or **Preview** beside the exact source you want.
7. In OBS, add a Browser Source with the copied URL and the width/height shown on that row.

Available individual routes are artwork, video, title, channel, visualizer, progress, stats, technical, pipeline, comment, history, Up Next, and mission. Enabling a row tells YOMI to prepare that source; disabled rows add no media work.

### Grouped-output workflow

Open **Groups 1–6**, choose a preset or edit any row, and use **Edit** to choose modules with an ordered checklist. Copy and Preview sit on the same row as its URL configuration.

Group presets can configure all six rows at once:

- Minimal — one title/channel source
- Split Essentials — artwork, video, title card, visualizer, culture, and stats sources
- Broadcast Desk — joined now-playing strip plus information/culture/engineering panels
- Full Studio — six enabled broadcast surfaces including a timeline and mission control

All sources share one local clock and one downloaded media cache. Splitting video into multiple Browser Sources does not redownload it, but every video source makes OBS perform another decode.

The Director page can copy one complete enabled-source pack or open a generated setup guide containing only enabled sources/groups with their exact URLs and dimensions.

## Layout and source FPS

Canvas presets include 1280×720, 1920×1080, 2560×1440, and 3840×2160. Media sizes and four screen corners are independently configurable.

Browser Source FPS choices are Auto, 15, 30, and 60 FPS. Auto resolves to the highest enabled video or visualizer rate. YOMI prefers genuine 60 FPS video only when YouTube provides it; it does not fabricate frames.

## Visualizer

Generated visualizer resolutions range from Monolith 12×4 to Maximum Detail 192×48. Lower choices stretch enormous blocks and reduce normal preparation/cache cost; higher choices provide progressively finer output. The Settings page shows exact generated pixels and their relative cost before saving.

## Performance

- Gaming / Lowest overhead — 1 worker
- Balanced — 2 workers (default)
- Fast caching — 4 workers
- Maximum caching — 8 workers

Maximum can fill a deep cache quickly but can hammer CPU, network, disk, and Defender together. Balanced is the intended general streaming default.

## No external browser or OBS WebSocket dependency

YOMI does not require Chrome to remain open and does not require OBS WebSocket. Browser Sources read YOMI's local HTTP server only.
