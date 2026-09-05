# Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [1.1.0] — 2026-07-07

### Added
- **Microsoft Store app updates** — Check for Updates now also scans `winget upgrade --source msstore`, so Store-sourced apps show up alongside regular winget packages
- Upgrading a Store-sourced package automatically appends `--source msstore` to the upgrade command (works for single-package and multi-select upgrades)
- **In-app self-update** — checks GitHub Releases for a newer version on startup, plus a manual "Check for App Updates" button in Settings; downloads and installs the new version automatically, no manual download required

### Fixed
- Installed Packages grid: the dimmed/read-only styling for "Local Registry" rows could be lost after sorting a column — styling is now recalculated per-row on every paint so it survives sorting

### Changed
- Release builds now produce a single `WingetManager.exe` with no companion `.pdb` or `.exe.config` file

---

## [1.0.0] — 2026-06-10

### Added
- **Available Updates** view — scans winget and lists upgradeable packages in a filterable grid
- **Batch upgrade** — select packages individually or via header checkbox, upgrade with progress tracking and cancellation
- **Installed Packages** view — full inventory of all packages known to winget
- **Live Log** view — real-time winget output streamed to a `RichTextBox`, persisted to `winget_manager.log`
- **Settings** tab:
  - Toggle Silent Mode (`--silent`)
  - Toggle Force Install (`--force`)
  - Toggle Accept Agreements (`--accept-package-agreements --accept-source-agreements`)
  - Reset winget sources (`source reset --force --accept-source-agreements`)
  - Clear log file and UI buffer
- Single-EXE distribution: Guna UI2 and Guna Charts DLLs embedded as managed resources via `AssemblyResolve`
- `app.manifest` — `requireAdministrator` so winget operations never fail due to missing elevation
- Custom icon (`winget_manager.ico`) — multi-resolution (16 / 32 / 48 / 256 px, PNG-in-ICO)
- `CancellationTokenSource` per view-switch to abort in-flight background loads when the user navigates away
- Hardware double-buffering on `DataGridView` via reflection to eliminate flicker during large list renders
- File I/O (`AppendAllText`) offloaded from the UI thread with `Task.Run`
