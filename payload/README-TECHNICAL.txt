YOMI 4.0 - TECHNICAL NOTES
==========================
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

COMPONENTS
----------
mpv and yt-dlp are core.
Deno and FFmpeg are optional runtime directories. Settings detects executable presence rather than trusting a separate database.

Deno path is explicitly supplied to yt-dlp subprocesses with --js-runtimes. The official yt-dlp Windows executable includes its matching EJS component.

VIDEO RETRY LADDER
------------------
Route 1: player_client=web_embedded,default, format 160, 3 delayed attempts (compatibility behavior).
Route 2: default,-web_safari low MP4 selector, 2 attempts.
Route 3: automatic/progressive <=360/480 fallback, 2 attempts.
Permanent unavailable/private/removed errors do not burn every retry.

OPTIONAL FEATURES
-----------------
No FFmpeg: gain scan writes 0 dB, smart crop is disabled, visualizer jobs are disabled. Raw artwork and tiny video remain possible.

OVERLAY
-------
Text styling and visualizer recoloring happen in-browser. Visualizer source remains a small white pixel video; solid/rainbow/gradient colors are applied to decoded pixels live, so changing color does not regenerate cache.

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


V4.0.9.4 DEFENDER PERFORMANCE OPT-IN
-------------------------------------
Defender Performance Analyzer traces showed the bundled yt-dlp process causing
nearly all scan time during concurrent artwork/video preparation. The official
single-file yt-dlp executable expands runtime files beneath TEMP, so excluding
only YOMI's Program Files and LocalAppData directories does not cover that work.

The installer now presents an unchecked, explicit performance option. When the
user selects it, the elevated installer adds exactly one process exclusion:
    C:\Program Files\YOMI\runtime\yt-dlp\yt-dlp.exe

YOMI never excludes PowerShell, TEMP, Deno, mpv, or the entire user profile.
Newly added exclusions are marked in LocalAppData and removed by the elevated
uninstaller. Pre-existing user-managed exclusions are detected but not claimed
or removed by YOMI.
