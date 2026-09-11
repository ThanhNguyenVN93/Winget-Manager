# Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [1.2.3] — 2026-09-11

### Added
- **"Include beta/pre-release versions" setting** (off by default) — packages on a Beta, Dev, Canary, Nightly, Insider, Alpha, or Preview channel (e.g. Google Chrome Beta) are now excluded from the update scan unless this is turned on

---

## [1.2.2] — 2026-09-11

### Changed
- Startup order reversed: the app now checks for its own update first and waits for that to resolve before scanning for winget package updates. Previously the package scan ran first while the self-update check fired in the background unawaited, so the two could run concurrently — pointless work if the user accepted a self-update (the scan's results were discarded on restart anyway), and a contributing factor in the `FormClosing`/`Application.Exit()` crash fixed in 1.2.1

---

## [1.2.1] — 2026-09-11

### Fixed
- Edge components (`Microsoft Edge`, `Microsoft Edge For Game Bar`, WebView/MSIX variants) could surface as upgradeable via the new deep-check fallback and then fail — they're serviced by Windows Update, not winget. Now filtered out by both Name and Id, not just the exact `Microsoft.Edge` id
- The "Support the developer" donate prompt was shown as a modal dialog inside `FormClosing`, which also fires during the in-app self-update's `Application.Exit()` — closing that dialog mid-teardown could throw. It's no longer shown automatically on close

### Changed
- Donate prompt moved to a manual **"💖 Support the Developer"** button in Settings instead of appearing automatically when the app closes

---

## [1.2.0] — 2026-09-11

### Added
- **Deep-check fallback for missed updates** — winget's bulk `upgrade` scan silently drops a package when it hits an ambiguous name match (e.g. multiple "qBittorrent" listings) or when the app is only known locally via its ARP registry key rather than a real winget Id; those packages are now re-queried individually (by exact Id, or by display name for ARP-only entries) to recover the update
- **Track column** in Available Updates — flags packages whose Id names a fixed version line (e.g. `OpenJS.NodeJS.22`) as "🔒 Version-locked"; these are excluded from "Select All" by default but can still be ticked individually
- **Elapsed-time readout** next to the progress bar during scans, upgrades, and uninstalls
- **"Open Log File" button** in Settings — opens `upgrade.log` on demand instead of only when an upgrade fails

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
