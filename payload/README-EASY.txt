YOMI 4.2.0.1 - 420 PATCH LINE / PRESETS EVERYWHERE / VISUALIZER RANGE
========
YOMI means YouTube OBS Music Interface.
YouTube playlist randomizer, player and modular OBS overlay for streamers

Created and designed by TheSensibleStreamer
Powered by mpv | yt-dlp | FFmpeg
Development assistance by ChatGPT

WHAT IS NEW IN 4.2.0.1
----------------------
- YOMI stays on the 4.2.0 / "420" generation. Compatible public updates use a
  fourth patch number such as 4.2.0.1.
- General / Player, OBS Overlay, Text & Style, Visualizer, Performance,
  Director Mode and Outputs 1-6 now provide Default, named presets and Custom.
- Changing an individual setting marks only its page Custom. Existing 4.2.0
  installations keep their settings and migrate newly tracked pages as Custom.
- Outputs presets can instantly build Minimal, Split Essentials, Broadcast Desk
  or Full Studio Browser Source arrangements across all six rows.
- Visualizer resolution now runs from Monolith 12x4 and Mega Blocks 16x5 through
  Microscopic 160x40 and Maximum Detail 192x48.
- The Visualizer page reports exact generated pixels, relative cost, FPS and a
  plain-language detail classification before you save.
- OBS Browser Source version queries follow the complete installed patch number,
  which makes future updater-installed pages less likely to remain stale.

WHAT IS NEW IN 4.2
------------------
- One Complete tracks ready ahead control replaces split audio/video look-ahead
  behavior. It can prepare 1-20 fully synchronized future tracks.
- Controller -> Show queue expands into previous-play history plus the next 20
  shuffled positions. Double-click or Play Selected jumps to any listed track.
- Visualizer high-frequency trim remaps the useful spectrum across the full
  width, with a live 0-60% adjustment for quiet upper-frequency tails.
- Visualizer generation supports true 30 or 60 FPS.
- Tiny/player video can prefer 60 FPS when YouTube provides it at the selected
  resolution. YOMI does not fake 60 FPS by duplicating 30 FPS frames.
- Browser Source Auto FPS follows the highest enabled video/visualizer rate.
- Maximum caching adds an opt-in 8-worker mode.
- Visualizer resolution originally ranged from Giant Blocks (20x6) through
  Ultra Fine (128x32); v4.2.0.1 expands both ends of that ladder.
- Text & Style, Visualizer and Performance introduced Default, named presets
  and Custom tracking; v4.2.0.1 extends that behavior to every useful page.
- Controller/Settings perform at most one automatic update check per day. A new
  build prompts before downloading; Settings -> Components also has Check for
  Updates. Downloaded ZIPs must match the public SHA-256 manifest before YOMI
  opens the normal interactive installer.
- Save Settings is silent; the button briefly says SAVED. Restore Default
  Settings preserves the playlist, history, installed components and Defender
  choice.

WHAT YOMI IS
------------
YOMI is a lightweight YouTube playlist randomizer/player for Windows. Streamer / OBS mode includes the original one-Browser-Source overlay plus an optional modular broadcast engine called Director Mode.

FIRST USE
---------
1. Open YOMI Settings.
2. Paste a YouTube playlist URL or playlist ID.
3. Choose Player or Streamer / OBS.
4. Save.
5. Open YOMI.

SHUFFLE PLAYLIST
----------------
This is the normal playlist update button. It re-reads the latest playlist from YouTube, preserves every playlist occurrence (NO dedupe), creates a fresh Fisher-Yates random order, clears track-number cache mapping and restarts from track 1.

PLAYER MODE
-----------
Default video quality is Off (audio only), which is the lowest-overhead mode. Optional normal video playback choices are 144p, 240p, 360p, 480p, 720p and Best. Player mode does not launch the OBS HTTP server and does not build artwork/tiny-video/visualizer presentation jobs.

STREAMER / OBS MODE
-------------------
The original default uses ONE Browser Source. Open Settings -> OBS Overlay or press OBS Setup in the Controller. Modules can be independently disabled; disabled modules do not consume preparation work unless an enabled Director output needs that same shared asset.

DIRECTOR MODE
-------------
Director Mode is opt-in. The original /overlay URL and its appearance remain the
default. Enabling Director Mode unlocks six configurable source groups plus
fixed one-module Browser Source URLs.

Available modules:
- artwork
- video
- title
- channel
- visualizer
- progress
- stats
- technical
- pipeline
- comment
- history
- upnext
- mission

Each output accepts any comma-separated module list in display order. Layouts
include Broadcast Strip, Horizontal, Stack, Cards, Terminal, Timeline and Single. A source can
contain one item or shovel several together. Every source reads the same local
track state and mpv clock, so video/progress/visualizer timing stays aligned.
Media is downloaded once and shared. Putting video in several Browser Sources
does make OBS decode that same file several times.

Director worlds:
Classic, Pirate Radio, Control Room, Cyberpunk Lab, Record Store,
Archive Terminal, Public Access, Museum Label, Arcade, Brutalist Industrial,
Spacecraft and Jazz Club.

Director also includes:
- Broadcast / Rapid / Cinematic scene rotation
- track-identity color palettes
- Off / Subtle / Full motion treatment
- Spectrum / Center Mirror / Oscilloscope / Dots / Skyline / Particle Field /
  Twin Rails visualizers
- rules for archive-age, short-form, portrait and returning transmissions
- Stats for Nerds and Completely Unhinged derived metrics
- synchronized progress and Up Next
- persistent recent-transmission history
- deterministic fictional broadcast classifications and mission statistics
- exact optional FFprobe codec/container/bitrate inspection
- optional low-priority Featured Comment retrieval

Press COPY ALL SOURCE URLS or OPEN DIRECTOR OBS GUIDE in Settings. Configured
groups use /source/1 through /source/6. Fixed module sources use URLs such as
/source/artwork, /source/video, /source/title and /source/stats.

FEATURED COMMENT SAFETY / COST
------------------------------
Featured Comment is off by default. When enabled and used by an output, YOMI
requests one relevance-sorted top-level YouTube comment only after essential
future bundles have priority. It strips URLs/control characters, limits length,
caches the result and never occupies an essential bundle-worker slot. Basic,
Strict and Off local word-filter modes are available. Controller -> Hide comment
immediately suppresses the current track's comment. The label says Featured Comment
because YouTube relevance sorting is algorithmic, not a literal highest-like
guarantee.

OVERLAY VIDEO QUALITY
---------------------
144p (fastest) remains the default. 240p, 360p, 480p, 720p and Best compatible
are opt-in. The large-video warning is hidden for 144p through 480p and appears
only for 720p or Best compatible. Those high modes can take longer to prepare
and use substantially more bandwidth, cache space and OBS decoding; long videos
can be tens or hundreds of MB. Complete tracks ready ahead and a hard video
cache limit are configurable; all quality ladders retain compatible MP4 routes.

Video and audio each have an independent selection policy: Prefer selected
maximum or Prefer lowest compatible. Audio targets are Low / Standard / High /
Best available. The default remains maximum-quality audio and the selected video
ceiling, preserving earlier YOMI behavior.

CUSTOMIZATION
-------------
YOMI 4 includes a deliberately broad but curated style system instead of hundreds of overwhelming choices:
- 12 fonts from understated to loud/playful/serif/mono
- text color and outline color
- outline thickness and text opacity
- subtle glow
- square / soft rounded / rounded media corners
- border on/off, width and color (2px #252525 remains the default)
- Classic / Minimal / Retro / Neon / Pastel / Arcade presets
- live preview

VISUALIZER
----------
Activity: Subtle / Normal / Active / Punchy
Opacity: 5-80%
Pixels: Extra Chunky / Chunky / Fine
Length: Short / Medium / Wide / Extra Wide
Color: solid / rainbow / gradient
Gradient presets: Sunset / Ocean / Pastel / Fire / Forest / Mono
Gradient orientation: Horizontal / Vertical
Bars: Normal / Mirrored
Layer: Behind text / Above text
Shape: Spectrum / Center Mirror / Oscilloscope / Dots / Skyline /
       Particle Field / Twin Rails
Vertical anchor: Source / Bottom / Center / Top
Bar spacing: None / Light / Wide
Peak glow: Off / Subtle / Strong
Frequency scale: Logarithmic / Linear
High-frequency trim: 0-60%
Visualizer FPS: 30 / 60

The visualizer begins immediately after the final pixel of the artwork/video media area. It does NOT inherit the title/channel padding gap.

PERFORMANCE
-----------
Gaming / Lowest overhead (1 worker)
Balanced (2 workers)
Fast caching (4 workers)
Maximum caching (8 workers)

A worker is one background track-bundle preparation slot. More workers preload
faster but can use more CPU/network/disk. Eight workers can be aggressive and
is not the streaming-safe default. Browser FPS can be Auto, 15, 30 or 60. Auto
uses the highest enabled video/visualizer rate, otherwise 15.

Complete tracks ready ahead can be set from 1-20. In Streamer mode, complete
means audio, metadata, gain, artwork, video and visualizer according to enabled
classic/Director modules. The configured video disk ceiling still wins if high
quality clips cannot all fit.

CONTROLLER NAVIGATOR
--------------------
Controller remains compact until Show queue is clicked. The expanded table
shows the last 20 actual plays, current/resume track and next 20 playlist
positions with WAITING / BUILDING / READY status. Double-click a row or use
Play Selected. An uncached selection enters the normal preparation gate.

Video selection and audio selection are independent. "Prefer lowest compatible"
can save transfer/cache space but can visibly or audibly reduce quality.

COMPONENTS
----------
Core: mpv + yt-dlp.
Optional: Deno / YouTube Compatibility and FFmpeg Media Tools.

Deno is recommended because modern yt-dlp YouTube extraction uses an external JavaScript runtime for full format support. FFmpeg Media Tools unlock loudness leveling, smart thumbnail crop and the retro visualizer.

Open Settings -> Components to install/remove optional components later. Features that require a missing component are visibly disabled. Removing a component does not delete your playlist/config/cache.

ARTWORK
-------
Smart Crop uses FFmpeg cropdetect to remove actual black/letterbox borders before producing the exact finished YOMI artwork asset. If Media Tools are absent, YOMI can still use the raw thumbnail.

TINY VIDEO RELIABILITY
----------------------
YOMI 4 carries the proven compatibility behavior back into the public version: preferred 144p web_embedded/default downloads get delayed retries before YOMI changes client/format routes. Deno support also improves modern YouTube extraction. If every tiny-video route fails, the rest of the synchronized bundle can still play.

UNINSTALL
---------
Uninstall asks whether to remove everything or keep only config.json + playlist.txt. Keeping data still removes disposable caches, logs and installer downloads.

YOMI does not modify unrelated mpv installations.


V4 FINAL INTEGRATION NOTES
--------------------------
- The curated font list intentionally spans understated, serif, heavy display,
  playful, handwritten/print and monospace styles rather than one aesthetic.
- Missing FFmpeg now grays out the full visualizer control group, smart crop and
  loudness leveling; install/remove it from the Components tab.
- The Settings icon asset has no rounded-square/drop-shadow shell: unused pixels
  are transparent and the gear/music/dice design is tightly fit to the canvas.


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


V4.0.9.4 DEFENDER PERFORMANCE OPT-IN
------------------------------------
- The first installer screen visibly offers an unchecked, explicit Windows
  Defender performance adjustment below the installation-profile choices.
- It adds only the bundled yt-dlp executable as a process exclusion.
- It never excludes PowerShell, TEMP, mpv, Deno or broad YOMI folders.
- Uninstall removes only an exclusion YOMI itself added.
- Reinstalling preserves an existing YOMI-managed choice; unchecking it during
  reinstall removes only the exclusion recorded as YOMI-owned.


V4.1 DIRECTOR MODE
------------------
- Six configurable modular OBS outputs and fixed single-module URLs.
- One shared clock/state/cache across every Browser Source.
- Twelve broadcast worlds, scene rotation and four audio-reactive render shapes.
- Stats, exact technical telemetry, pipeline state, progress, history, Up Next,
  Featured Comment, rule badges and deliberately absurd mission metrics.
- 144p/360p/720p/Best overlay quality with prefetch and disk-budget controls.
- Decorative comments and probes are low priority and never gate playback.
- Original combined overlay remains the default and requires no setup changes.


V4.1.1 DIRECTOR REFINEMENT
--------------------------
- Broadcast Strip is a polished 90px layout with joined media, stacked
  title/channel information and a full-height visualizer.
- The loose Horizontal layout now gives modules a consistent media-height frame.
- Default Output 1 migrates from the prototype 2560x180 row to 2560x90 only
  when that exact untouched prototype preset is detected.
- Added 240p and 480p to both player and overlay quality controls.
- Added independent maximum/lowest-compatible video and audio policies plus
  64/128/160 kbps audio ceilings and Best available.
- The large-video warning appears only for 720p or Best compatible.
- Added seven visualizer shapes, vertical anchoring, spacing, peak glow and
  logarithmic/linear frequency scaling.
- OBS setup URLs carry a version query so upgraded Browser Sources are easier
  to refresh without stale-page cache behavior.
