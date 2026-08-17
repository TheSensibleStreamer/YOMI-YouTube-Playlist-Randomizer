YOMI 4.0
========
YouTube Playlist Randomizer & Player
Optional OBS integration for streamers

Created and designed by TheSensibleStreamer
Powered by mpv | yt-dlp | FFmpeg
Development assistance by ChatGPT

WHAT YOMI IS
------------
YOMI is a lightweight YouTube playlist randomizer/player for Windows. Streamer / OBS mode adds an optional one-Browser-Source overlay with artwork, tiny synchronized video, title/channel and a retro pixel visualizer.

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
Default video quality is Off (audio only), which is the lowest-overhead mode. Optional normal video playback choices are 144p, 360p, 720p and Best. Player mode does not launch the OBS HTTP server and does not build artwork/tiny-video/visualizer presentation jobs.

STREAMER / OBS MODE
-------------------
Uses ONE Browser Source. Open Settings -> OBS Overlay or press OBS Setup in the Controller. Modules can be independently disabled; disabled modules do not consume preparation work.

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

The visualizer begins immediately after the final pixel of the artwork/video media area. It does NOT inherit the title/channel padding gap.

PERFORMANCE
-----------
Gaming / Lowest overhead (1 worker)
Balanced (2 workers)
Fast caching (4 workers)

A worker is one background track-bundle preparation slot. More workers preload faster but can use more CPU/network/disk. Browser FPS can be Auto, 15 or 30. Auto = 30 with visualizer in Streamer mode, otherwise 15.

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
-------------------------------------
The installer now offers an unchecked, clearly explained option to reduce
Windows Defender CPU spikes during track changes.

When selected, YOMI adds only this process exclusion:
    C:\Program Files\YOMI\runtime\yt-dlp\yt-dlp.exe

The option does not exclude PowerShell, the Windows TEMP folder, or the user's
profile. YOMI records exclusions it adds and removes them during uninstall.
An exclusion that already existed before installation is left under the user's
control and is not claimed as YOMI-managed.
