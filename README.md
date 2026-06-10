# Winget Manager

A modern, single-file Windows desktop GUI for **Windows Package Manager (winget)** — scan, batch-upgrade, and manage your installed packages without touching a terminal.

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Screenshots

> _Add screenshots here after first release._

---

## Features

| Feature | Description |
|---|---|
| **Available Updates** | Scans winget for all upgradeable packages and displays them in a sortable grid |
| **Batch Upgrade** | Select any subset of packages and upgrade them all with a single click |
| **Installed Packages** | Browse every package currently installed on the system |
| **Live Log** | Real-time streaming output from winget, persisted to a log file |
| **Settings** | Toggle silent mode, force-install, and auto-accept agreements; reset winget sources; clear the log |
| **Single EXE** | All UI libraries (Guna UI2, Guna Charts) are embedded — nothing to install alongside the binary |
| **Always Admin** | App manifest requests administrator elevation at launch so winget operations never fail silently |

---

## Requirements

| Requirement | Notes |
|---|---|
| Windows 10 (1903+) or Windows 11 | .NET 4.8 and winget are pre-installed |
| [Windows Package Manager (winget)](https://aka.ms/winget) | Included with App Installer; update via Microsoft Store if missing |
| .NET Framework 4.8 | Pre-installed on Win10 1903+ and all Win11 builds |
| Administrator privileges | The manifest enforces this automatically at launch |

---

## Installation

No installer needed.

1. Download `WingetManager.exe` from the [Releases](../../releases) page.
2. Double-click — UAC will prompt for elevation.
3. Done.

---

## Building from Source

**Prerequisites:** Visual Studio 2022 (or MSBuild 17+), .NET Framework 4.8 targeting pack.

```powershell
# Clone
git clone https://github.com/<your-username>/winget-manager.git
cd winget-manager

# Restore NuGet packages (Guna UI2 & Guna Charts)
nuget restore frm_winget_upgrade.slnx

# Release build — output: bin\Release\WingetManager.exe
msbuild frm_winget_upgrade.csproj /p:Configuration=Release
```

The release EXE is self-contained: Guna DLLs are embedded as managed resources and loaded at runtime via `AppDomain.AssemblyResolve`, so no extra files are needed beside the EXE.

---

## Tech Stack

- **C# / .NET Framework 4.8** — WinForms desktop
- **[Guna UI2 v2.0.4.8](https://gunaui.com/)** — modern control theming
- **[Guna Charts v1.1.0](https://gunaui.com/)** — charting controls
- `CancellationToken` cooperative cancellation for background loads
- `AssemblyResolve` + `EmbeddedResource` for single-EXE distribution
- `app.manifest` (`requireAdministrator`) for automatic elevation

---

## Settings Reference

| Setting | winget flag(s) added |
|---|---|
| Silent Mode | `--silent` |
| Force Install | `--force` |
| Accept Agreements | `--accept-package-agreements --accept-source-agreements` |

---

## License

[MIT](LICENSE) © 2026 Thanh Nguyen
