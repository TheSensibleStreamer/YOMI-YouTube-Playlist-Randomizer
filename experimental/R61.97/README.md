# YOMI 4.2.0.8 R61.97 — Experimental

**Experimental field-test build of YOMI.** This build is for testers already running a compatible 4.2.0.8 experimental WPF build. It is **not a fresh-install upgrade path from the public v4.2.0.7 package**.

## Download / install

The downloadable experimental package is being mirrored separately from the stable build. Do not install R61.97 over v4.2.0.7; it expects the newer experimental WPF runtime to already be installed.

## Highlights in the 4.2.0.8 experimental line

- New native WPF controller/workbench with a denser desktop-player layout.
- Compact-controller mode when the Queue is closed and a larger table-oriented workstation when it is open.
- Responsive small-window behavior with dedicated compact geometry instead of simply shrinking the full interface.
- Full table Queue designed for large playlists, with compact rows, improved scrolling, current-position navigation, filtering, search, multi-selection and richer row actions.
- Queue search can act as a temporary playback subset so matching tracks can be listened to in table order without rewriting the durable Queue.
- Favorites integrated into the player and Queue with deliberately low-key visual treatment.
- Queue row context actions include **Open source** for opening the original track page.
- Playlist Pool and source-discovery tools for working with larger collections and temporary listening sets.
- Audio-first, cached-first startup work intended to make playback available without waiting for every secondary surface to finish preparing.
- Artwork, video and visualizer presentation integrated into the player rather than treated as separate utility windows.
- Side-by-side artwork/video presentation with media geometry that follows the actual artwork aspect ratio.
- Sharper, scalable low-overhead visualizer presentation using nearest-neighbor scaling.
- Improved artwork edge trimming that can detect sufficiently uniform side bands in colors other than black while preserving the top and bottom of the image.
- More responsive resize handling, including separation between Queue-scrollbar grabbing and window-edge resizing.
- Condensed Queue command bar and reduced duplicated status/control text.
- Quieter everyday state presentation so normal waiting/preparation states do not fill the Queue with repetitive labels.
- Expanded dark/medium appearance work, stronger surface depth and improved typography/readability.
- Settings and other larger tools can occupy the main YOMI workspace while playback continues.
- Expanded diagnostic and Flight Recorder coverage for playback, media preparation, presentation, OBS and responsiveness problems.

## R61.97 focus

R61.97 concentrates on compact-player composition, live-resize stability, a one-row Queue command deck, a larger sharp visualizer and more general solid-color artwork side-band detection.

Use this build for testing and report reproducible problems through GitHub Issues.
