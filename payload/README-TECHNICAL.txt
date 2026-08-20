YOMI 4.2.0.3 - TECHNICAL NOTES / DIRECTOR SOURCE MANAGER
==========================
Product name: YOMI - YouTube OBS Music Interface
Program files: C:\Program Files\YOMI
Writable data: %LOCALAPPDATA%\YOMI
IPC pipe: \\.\pipe\yomi-v4
Overlay port: 8876 by default

ARCHITECTURE
------------
Player mode:
Controller -> Supervisor -> mpv + music.lua
No OBS HTTP server when app_mode=Player.
Audio-only playback uses the prepared local audio cache. Optional player video loads the YouTube URL through mpv's ytdl hook with a configured yt-dlp path and selected quality.

Streamer mode:
Controller -> Supervisor -> mpv/music.lua + range-enabled local HTTP server -> one Browser Source.
Presentation bundles are prepared before playback: audio/meta/gain, artwork, tiny video, visualizer. Future tracks are prefetched as complete bundles.
The unified prefetch_ahead value is clamped to 1-20 and applies to every enabled
essential requirement; video_prefetch_ahead remains only as a synchronized
legacy config field for older-version readability.

Director mode (opt-in):
The same server exposes /source/1 through /source/6 plus fixed module routes.
director_fixed_sources stores the enable state, label and OBS dimensions for
every one-module route. Merely enabling a fixed source now participates in the
same preparation-demand calculation as a numbered group or classic overlay.
All Browser Sources poll one current.json state object and one sampled mpv IPC
clock. Audio/art/video/visualizer assets are prepared once and shared by URL.
Each Browser Source owns its own decoder/render surface, so duplicate video
sources increase OBS decoding but never trigger duplicate downloads.

Essential bundle gate:
audio + metadata + gain + enabled artwork/video/visualizer

Decorative queue (never part of bundle_ready):
comment extraction + FFprobe telemetry

Decorative jobs receive very low priority after future playback bundles. A
failure writes a small status marker and cannot delay, skip or stop a track.

PAGE PRESETS
------------
General / Player, OBS Overlay, Text & Style, Visualizer, Performance, Director
Mode, Sources and Groups 1-6 expose Default, named presets and Custom. Event guards let
a preset populate all controls without renaming itself; changing any underlying
control outside the guarded operation marks only that page Custom. Existing
installations missing a page tracker migrate that page as Custom, preserving
hand-tuned values rather than falsely labeling them as factory settings.

The Groups page applies six rows atomically while the guard is active. Minimal,
Split Essentials, Broadcast Desk and Full Studio are configuration templates;
they do not duplicate downloads because every source retains the shared media
cache and clock architecture.

The Sources page applies one-module arrangements atomically. Each row exposes
enabled state, label, width, height, exact URL, Copy and Preview. Groups expose
the same Copy/Preview workflow and an ordered checked-list editor instead of a
raw module string. Selecting Comment or History also enables its required
decorative feature switch.

DIRECTOR SOURCE MANAGER / RESTART CONTRACT
------------------------------------------
The preparation contract is the union of:
1. enabled classic overlay modules,
2. enabled fixed one-module sources, and
3. modules present in enabled Groups 1-6.

Disabled systems schedule no asset work. Settings compares a compact signature
of pipeline-affecting values before and after Save. If YOMI is running and that
signature changes, it writes one restart request for the Controller, which
gracefully stops and relaunches the Supervisor/server/player. Browser-only
labels, dimensions, layout and styling do not restart the media pipeline.

All normal FFmpeg and FFprobe child work, including the Smart Crop compatibility
probe that previously bypassed the launcher, now goes through PriorityRun at
idle priority.

VISUALIZER GENERATION RESOLUTION
--------------------------------
The preset ladder maps to 12x4, 16x5, 20x6, 28x8, 40x10, 48x12, 64x18, 96x24,
128x32, 160x40 and 192x48 FFmpeg showfreqs frames. Browser rendering scales
those deliberately tiny frames to the configured overlay length. The engine
clamps arbitrary config input to 12-192 pixels wide and 4-48 pixels high.

Settings calculates source pixels relative to the 40x10 default (400 sampled
pixels per frame). This is an intentionally understandable workload indicator,
not a claim that total CPU usage scales perfectly linearly with pixel count.

UPDATER
-------
Controller and Settings launch the console-free update helper, which rate-limits
automatic checks to once per 24 hours. Manual checks bypass the timer. The helper
reads update.json only from the official public YOMI repository, accepts package
URLs only from that repository, verifies the downloaded ZIP against the manifest
SHA-256 and then opens the normal interactive installer. It never silently
replaces running program files.

COMPONENTS
----------
mpv and yt-dlp are core.
Deno and FFmpeg are optional runtime directories. Settings detects executable presence rather than trusting a separate database.

Deno path is explicitly supplied to yt-dlp subprocesses with --js-runtimes. The official yt-dlp Windows executable includes its matching EJS component.

VIDEO RETRY LADDER
------------------
Route 1 follows the requested 144/240/360/480/720/Best ceiling and maximum or
lowest-compatible policy. The default 144p route retains three delayed itag 160
attempts with player_client=web_embedded,default.
Higher ladders use known H.264 MP4 format IDs plus filtered MP4 selectors.
Every ladder can recover through 360p, the proven 144p route and a final
automatic/progressive <=360/480 selector.
Permanent unavailable/private/removed errors do not burn every retry.

OPTIONAL FEATURES
-----------------
No FFmpeg: gain scan writes 0 dB, smart crop is disabled, visualizer jobs are disabled. Raw artwork and tiny video remain possible.

OVERLAY
-------
Text styling and visualizer recoloring happen in-browser. Visualizer source remains a small white pixel video; solid/rainbow/gradient colors, seven shapes, anchor normalization, spacing, glow and 0-60% high-frequency trimming are applied to decoded pixels live. Activity, internal resolution, logarithmic/linear frequency scale or 30/60 FPS changes regenerate the cached source.

SECURITY / ISOLATION
--------------------
No Chrome dependency. No OBS WebSocket dependency. No hard-coded username. No user playlist/cache/logs are packaged. Unrelated mpv installations are never modified.


V4.0.1 PRESENTATION HOTFIX
--------------------------
- The visualizer's first VISIBLE pixel now begins directly at the media edge.
  Blank internal showfreqs source columns are normalized away before drawing.
- Mirrored bars and right-side layouts normalize against the correct media edge.
- Artwork/video borders are now painted by a dedicated top-layer pseudo-element,
  so the 2px frame cannot disappear behind image/video content.
- The existing shared 2px art/video seam behavior is preserved.


V4.0.2 SMART ARTWORK CROP HOTFIX
--------------------------------
The artwork crop detector now matches the previously earlier proven YOMI implementation
implementation:

FFmpeg:
  -loop 1
  -vf cropdetect=limit=16:round=2:reset=0
  -frames:v 10

YOMI uses the LAST crop result, probes the original thumbnail dimensions, then
computes:
  left   = X
  top    = Y
  right  = original_width  - (X + crop_width)
  bottom = original_height - (Y + crop_height)

The detected crop is only accepted if at least one border is 8 pixels or larger.
This avoids tiny cropdetect noise while correctly removing real black/letterbox
or pillarbox borders.

After the meaningful content crop, artwork is still scale/cover-cropped to the
selected YOMI media dimensions before the bundle becomes READY.


V4.0.3 ANY-COLOR ARTWORK CROPPING
---------------------------------
The black-biased FFmpeg cropdetect stage has been replaced.

YOMI now uses a Windows-native edge-band detector that:
- samples each image edge independently
- estimates the edge band's actual RGB color
- tolerates JPEG/WebP compression noise
- crops black, white, gray, blue, beige, red, or other solid-color padding
- requires at least 93% of sampled pixels in a candidate line to match
- requires at least 8 pixels of meaningful border
- requires a content transition immediately inside the proposed border
- caps cropping to 30% from any individual edge
- preserves at least 40% of original width and height
- leaves uncertain images untouched

The detected rectangle is then passed to FFmpeg, followed by the ordinary final
scale/cover crop to the selected YOMI media dimensions.


V4.0.4 SHUFFLE HOTFIX
----------------------
- Fixed Controller shuffle child launch from C:\Program Files\YOMI.
  The PowerShell script path is now explicitly quoted through -Command instead
  of passing an unquoted Program Files path to -File.
- Shuffle Playlist in Settings now performs the real operation instead of only
  opening Controller and telling the user to click another button.
- Settings writes a tiny approved shuffle request file. The existing Controller
  consumes it; if Controller is not running, Settings launches Controller first.
- Both buttons now use the same safe workflow:
  stop -> reload latest YouTube playlist -> preserve duplicates -> Fisher-Yates
  shuffle -> clear track-number cache -> reset to track 1 -> restart.
- Shuffle failures remain visible in Controller status instead of disappearing
  inside a hidden PowerShell process.


V4.0.5 CONTROLLER / UNINSTALL UX
--------------------------------
- Clicking the Controller X now stops YOMI and exits the Controller.
- Exit Controller from the button or tray menu does the same.
- YOMI gets up to two seconds for a graceful MPV shutdown, then only its
  recorded runtime processes are forced if necessary.
- Hide to tray remains the explicit way to keep music playing with the
  Controller window hidden.

UNINSTALL
---------
- Settings -> Components includes an UNINSTALL YOMI... button.
- A root launcher is installed at:
    C:\Program Files\YOMI\Uninstall YOMI.cmd
- Start Menu -> Uninstall YOMI points to the same root launcher.
- app\uninstall.ps1 remains the self-elevating uninstall engine.
- The elevated uninstaller closes YOMI Controller/Settings/runtime processes
  before removing Program Files.


V4.0.6 UNINSTALLER PACKAGING HOTFIX
-----------------------------------
v4.0.5 contained Uninstall YOMI.cmd in the ZIP and checked for it after
installation, but accidentally omitted it from the staging copy list.

v4.0.6:
- requires payload\Uninstall YOMI.cmd during installer preflight
- explicitly copies it into the Program Files staging root
- still refuses final verification if the installed launcher is missing
- lets Settings fall back directly to app\uninstall.ps1 if needed


V4.0.7 DEFAULT / PREVIEW / PUBLIC-RELEASE POLISH
-------------------------------------------------
PERFORMANCE
- Fresh installs default to Balanced (2 workers).
- Gaming (1 worker) remains available for absolute lowest background overhead.
- Fast caching (4 workers) remains opt-in.
- Upgrades preserve an already-saved performance choice.

LIVE PREVIEW
- The old preview shrank the entire output canvas into a small panel, making a
  90px overlay microscopic at 1440p/4K.
- The new preview renders a readable overlay close-up plus a separate mini-map
  for canvas/corner placement.
- Preview content is generic sample text and never reads playback metadata,
  playlist contents, current track state or cache files.

PUBLIC RELEASE HYGIENE
- Private development track names, usernames, concrete YouTube IDs and
  development-only path references are rejected by the package build check.


V4.0.8 PACKAGE / ICON / INSTALLER CLEANUP
----------------------------------------
ICONS
- The public package now contains exactly two current icon files: one for YOMI
  and one for Settings.
- Historical duplicate aliases are no longer shipped.
- Current icon filenames remain version-specific so Windows sees a fresh icon
  path after an upgrade instead of reusing an older cached shortcut image.
- Each ICO retains 16, 24, 32, 48, 64, 128 and 256px layers.
- Layers use PNG compression plus light palette optimization, preserving
  transparency and virtually identical appearance at icon sizes.

INSTALLER
- The extracted package root has one obvious launcher:
    INSTALL YOMI.cmd
- The implementation script is tucked away at:
    installer\install.ps1
- There is no second INSTALL.ps1 or RUN-INSTALL.cmd beside the launcher.

PACKAGE CLEANUP
- Development icon-preview images and release-test notes are omitted from the
  public ZIP.


V4.0.9 SMART-CROP FORMAT FIX
----------------------------
The any-color detector itself was color-independent, but its input format was
not reliable.

yt-dlp can provide thumbnails as JPG, PNG or WebP. Windows PowerShell 5.1 /
.NET Framework System.Drawing does not reliably decode WebP. That meant some
thumbnails could reach the detector as an unreadable WebP and silently fall
through as "no crop", while FFmpeg later displayed the uncropped image normally.

v4.0.9 canonicalizes every Smart Crop thumbnail first:

  yt-dlp source thumbnail
  -> FFmpeg lossless PNG normalization
  -> any-color edge-band detector
  -> detected crop
  -> exact YOMI media-size cover crop
  -> final cached JPG

This makes crop detection independent of both border color AND YouTube thumbnail
file format.


V4.0.9.1 SMART-CROP DETECTOR REWRITE
------------------------------------
Runtime logs from v4.0.8 proved Smart Crop was entered correctly, but the
detector itself crashed.

Observed failures:
- image loader: "Parameter is not valid."
- PowerShell arithmetic: System.Object[] has no op_Subtraction

The detector is rewritten around normalized PNG input and scalar-safe math:
- Image.FromFile + Bitmap(Image)
- typed int[] sample positions
- scalar PSCustomObject RGB references
- explicitly parenthesized numeric channel subtraction
- no Measure-Object/pipeline arithmetic
- explicit int edge widths before geometry subtraction
- line-number diagnostics on future detector errors
- runtime marker: ARTWORK EDGE DETECT V2


V4.0.9.2 WINDOWS POWERSHELL 5.1 SYNTAX HOTFIX
------------------------------------------------
v4.0.9.1 used inline type annotations on foreach iterator variables. Windows
PowerShell 5.1 rejects that syntax before the detector can run.

v4.0.9.2 uses ordinary foreach variables and casts each value inside the loop
body. The normalized-PNG pipeline and scalar-safe detector math are otherwise
unchanged.


V4.0.9.3 SMART-CROP PIPELINE RESTORE
------------------------------------
A second bug existed outside the detector.

Older working artwork logic produced one processed JPG and served that exact
file. Public YOMI later allowed final JPG/PNG/WebP files, while the HTTP server
looked for WebP before JPG. A legacy/raw WebP could therefore:
1. make the scheduler think artwork was already complete, and
2. be served instead of a newly processed/cropped JPG.

v4.0.9.3 restores one canonical Smart Crop artifact:
    cache\artwork\track-N.jpg

Changes:
- Smart Crop ignores non-JPG legacy finals when deciding whether art is ready.
- One-time Smart Crop cache v3 migration removes old artwork finals/status only.
- The server now prefers JPG before JPEG/PNG/WebP.
- Successful Smart Crop deletes competing legacy PNG/WebP/JPEG files.
- Pixel detection is moved from PowerShell to compiled strongly-typed C#.
- C# any-color detection runs first on FFmpeg-normalized PNG input.
- If color detection returns no crop or errors, YOMI runs the older proven
  FFmpeg static-image cropdetect method as a black/dark-border fallback.
- Audio, video, metadata, gain and playlist caches are not reset by this
  artwork-only migration.


V4.1 DIRECTOR MODE ARCHITECTURE
-------------------------------
ROUTES
  /overlay              original combined compatibility overlay
  /source/1..6          configured modular output groups
  /source/<module>      fixed single-module sources
  /state                clock + enriched current track state
  /history              bounded JSON-lines broadcast history

MODULES
  artwork video title channel visualizer progress stats technical pipeline
  comment history upnext mission

STATE ENRICHMENT
current.json now exposes selected yt-dlp metadata, media byte counts, gain,
playlist position/count, next-track metadata, worker/queue state, ready-ahead,
per-stage preparation timings, optional featured comment and optional FFprobe
audio/video JSON.

COMMENTS
yt-dlp runs with skip-download, write-info-json, write-comments, YouTube
comment_sort=top and max_comments=1,1,0,0,1. YOMI retains only one sanitized,
length-limited parent comment. Comment work is opt-in, cached and decorative.
Basic/Strict/Off local filters run before the comment enters current.json. The
controller's yomi-hide-comment script message persists a per-track hidden marker.

TELEMETRY
FFprobe uses selective stream/format fields for codec, profile, dimensions,
frame rate, pixel format, sample rate, channel layout, bitrate, duration and
container. Raw compact probe JSON is cached separately for audio and video.

VIDEO QUALITY / DISK BUDGET
Quality ladders start at 144p, 240p, 360p, 480p, 720p or Best compatible MP4
and retain a proven 144p recovery route. video_preference independently chooses
the selected maximum or lowest compatible stream. video_fps chooses <=30 FPS or
prefers >30 through 60 FPS when YouTube exposes it, then falls back safely.
prefetch_ahead schedules every enabled bundle requirement for 1-20 future
tracks. video_cache_limit_mb evicts the farthest prepared video first while
preserving the playing track and its immediate successor.

audio_quality applies approximate 64/128/160 kbps targets or Best available,
with a reliability fallback when the requested metadata/format is unavailable.
audio_preference independently chooses the selected maximum or lowest compatible
audio-only format. A changed audio selector schedules a safe next-start reset of
audio, metadata, gain, audio telemetry and dependent visualizer caches.

PRESENTATION ENGINE
director.html implements layout groups, twelve theme worlds, deterministic
track palette hashing, scene phases, rule badges, transitions, seven render
shapes from the cached audio-reactive visualizer, persistent history, Up Next,
technical/pipeline panels and derived/fictional metrics. Text is assigned with
textContent; featured comments are never injected as HTML.

BROADCAST STRIP
Broadcast Strip uses the configured media height as its exact row height.
Artwork/video share their configured frame seam, title/channel form one stacked
information group, and the visualizer uses the configured length. The original
4.1 prototype Output 1 is migrated from 2560x180 Horizontal to 2560x90 Broadcast
Strip only when every identifying field still matches the untouched prototype.


V4.2 DEEP CACHE / NAVIGATOR / 60 FPS
------------------------------------
- One complete-bundle prefetch distance (1-20) replaces the split video-ahead
  behavior. Audio/meta/gain/art/video/visualizer follow the same track window.
- The Controller records a bounded lightweight play history even when the
  Director history module is hidden. Show queue displays previous plays,
  current/resume state and 20 upcoming positions. yomi-jump accepts an exact
  shuffled playlist occurrence index.
- Cache readiness in Controller mirrors enabled classic and Director modules.
- cache_workers is clamped to 1-8; the 8-worker preset stays opt-in and below
  normal priority.
- showfreqs rate is configurable at 30/60. Browser render loops follow the same
  configured rate and Auto Browser FPS resolves to the highest enabled visual
  rate.
- visualizer_high_frequency_trim is a live 0-60% browser remap of the frequency
  X axis. It removes quiet upper bins without modifying audio.
- video_fps prefers >30 to 60 FPS formats when requested and available; the
  compatibility ladder still recovers through reliable 30 FPS MP4 routes.
- Restore Defaults preserves playlist/history/components and schedules safe
  next-start rebuilds of media affected by restored settings.
- Ordinary Save Settings no longer opens a modal dialog.
