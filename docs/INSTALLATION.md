# Installing YOMI on Windows

## Download

Use the latest packaged ZIP from this repository's **Releases** section. The packaged release is separate from GitHub's automatically generated source-code ZIP.

## Install

1. Download the latest `YOMI-vX.X.X.zip` release asset.
2. Fully extract the ZIP to a normal folder.
3. Double-click `INSTALL YOMI.cmd`.
4. Approve the Windows administrator prompt. YOMI installs application files under `C:\Program Files\YOMI`.
5. Open YOMI Settings, paste a YouTube playlist URL or playlist ID, and choose Player or Streamer / OBS mode.

Writable settings, playlist state, cache, and logs are stored separately under `%LOCALAPPDATA%\YOMI`.

## Installation profiles and components

YOMI can install the functionality needed for normal player use or the fuller streamer/OBS feature set. Core playback uses mpv and yt-dlp. Deno compatibility and FFmpeg media tools are optional components that can also be installed or removed later through Settings → Components.

## Optional Windows Defender performance setting

The installer offers an unchecked option named **Reduce Windows Defender CPU spikes during track changes**. If selected, it adds only `C:\Program Files\YOMI\runtime\yt-dlp\yt-dlp.exe` as a Defender process exclusion. This slightly reduces antivirus coverage for files opened by yt-dlp. YOMI does not exclude PowerShell, `%TEMP%`, mpv, Deno, or the entire user profile.

The uninstaller removes an exclusion created by YOMI. If that exact exclusion existed before installation, YOMI leaves it under the user's control.

## Updating

New YOMI versions can be installed over the existing installation. User configuration is stored separately from Program Files.

## Uninstalling

Use **Settings → Components → Uninstall YOMI**, the Start Menu uninstaller, or:

```text
C:\Program Files\YOMI\Uninstall YOMI.cmd
```

The uninstaller can remove everything or preserve playlist/config data for a future reinstall while clearing disposable cache and log data.
