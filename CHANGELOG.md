# YOMI Changelog

## 4.2.0.9.2

- Fixed the in-app updater locking its own Program Files app directory during installation.
- Update execution now moves to LocalAppData before activation, and the installer can bootstrap out of the broken 4.2.0.9/4.2.0.9.1 update path.
- Added retry-safe atomic installation and an installer-side WPF self-test.
- Preserves the WPF Settings routing fix from 4.2.0.9.1.

## Previous versions

See [Previous Versions](./previous/README.md).
