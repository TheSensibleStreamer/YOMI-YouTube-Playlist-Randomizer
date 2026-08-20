# Installing YOMI on Windows

## Download

Use the current packaged `YOMI-v4.2.0.x.zip` linked from the repository README. The packaged installer is separate from GitHub's automatically generated source-code ZIP.

## Install

1. Download the latest `YOMI-v4.2.0.x.zip` package.
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

YOMI uses a fourth version component for compatible improvements within the 4.2.0 generation. Controller and Settings check the tiny official `update.json` manifest at startup. Declining a version suppresses that same prompt for 30 days, while a newer version can still prompt immediately. Settings → Components also provides a manual check. YOMI asks before downloading, verifies the ZIP's SHA-256, then opens the normal interactive installer. User configuration is stored separately from Program Files and is preserved by an in-place update.

## Uninstalling

Use **Settings → Components → Uninstall YOMI**, the Start Menu uninstaller, or:

```text
C:\Program Files\YOMI\Uninstall YOMI.cmd
```

The uninstaller can remove everything or preserve playlist/config data for a future reinstall while clearing disposable cache and log data.
