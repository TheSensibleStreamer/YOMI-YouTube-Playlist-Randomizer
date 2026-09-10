param([switch]$Interactive)
# Compatibility wrapper: v4 exposes one user action, Shuffle Playlist.
& (Join-Path $PSScriptRoot 'shuffle.ps1') -Interactive:$Interactive
exit $LASTEXITCODE
