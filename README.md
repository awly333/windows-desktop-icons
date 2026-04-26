# Desktop Icons

[简体中文](README.zh-CN.md)

A small Windows utility that saves and restores desktop icon layouts per monitor configuration. Open-source replacement for ReIcon, with an Apple-style minimal interface.

## Features

- Save and restore named desktop layouts
- Layouts are stored per monitor configuration (auto-detected display fingerprint)
- System tray with quick controls
- Optional **Start with Windows**
- Optional **Auto-restore on display change** — re-applies the last layout when monitors are plugged/unplugged or resolution changes

## Requirements

- Windows 10 21H1 (build 19041) or later, x64
- No need to install .NET separately — the installer is self-contained

## Install

Download the latest `DesktopIcons-Setup-x.y.z.exe` from the [Releases page](https://github.com/awly333/windows-desktop-icons/releases) and run it.

The installer asks where to put the app:

- **Per-user** (default): `%LocalAppData%\Programs\Desktop Icons` — no admin required
- **Per-machine**: `Program Files\Desktop Icons` — admin required

You can change the install directory on the standard Inno Setup directory page.

User data lives in `%LocalAppData%\DesktopIcons\` (layouts and settings). Uninstalling does not delete user data.

## Build from source

```powershell
# Prerequisites
winget install Microsoft.DotNet.SDK.8
winget install JRSoftware.InnoSetup

# Run from source
dotnet build src\DesktopIcons.App\DesktopIcons.App.csproj -c Debug
.\src\DesktopIcons.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64\DesktopIcons.App.exe

# Build installer
powershell.exe -ExecutionPolicy Bypass -File tools\build-installer.ps1
# Output: installer-output\DesktopIcons-Setup-x.y.z.exe
```

## Project layout

```
src\
  DesktopIcons.Core\   # Shared library (desktop interop, fingerprint, storage)
  DesktopIcons.Cli\    # CLI: dump / apply / save / restore / list / delete
  DesktopIcons.App\    # WinUI 3 desktop app (unpackaged, self-contained)
installer\             # Inno Setup script
tools\                 # build-icon.ps1, build-installer.ps1
```

## License

MIT — see [LICENSE](LICENSE).
