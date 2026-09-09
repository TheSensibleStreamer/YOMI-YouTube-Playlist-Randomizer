# YOMI R61.97 — Experimental Build

This is an **experimental / field-test build** of YOMI 4.2.0.8. It is published separately from the stable/default `main` build and is not promoted as the primary release.

## Build

- Revision: `R61.97`
- Base revision: `R61.96.2`
- Theme of this build: Compact Controller Calibration
- EXE SHA-256: `56e4f634293ac54305965f85152eecea848057b8df136b81a25180bfb745c6e5`
- Focused updater SHA-256: `a01da6ee7b5455db4ea307e89bb50c8fa225ed7fbff2809e954cebd528b6596e`

## Major experimental changes

- Rebuilt compact/micro controller layout.
- Resize-mode handoff changed to prevent remembered workstation geometry from hijacking a live resize.
- Queue controls condensed into a single command row.
- Visualizer enlarged/repositioned with nearest-neighbor scaling.
- Artwork side-band cropping generalized from dark-only bars to near-uniform side colors.

Use this branch for testing only. The default `main` branch remains unchanged.
