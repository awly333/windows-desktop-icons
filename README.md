# Desktop Icons

[简体中文](README.zh-CN.md)

A small Windows utility that saves and restores desktop icon layouts per monitor configuration. Open-source replacement for ReIcon, with an Apple-style minimal interface.

## Features

- Save and restore named desktop layouts
- Layouts are stored per monitor configuration (auto-detected display fingerprint)
- System tray with quick controls
- Optional **Start with Windows**
- Optional **Auto-restore when icons move** - re-applies the last restored layout when desktop icons drift from their saved positions

## Requirements

- Windows 10 21H1 (build 19041) or later, x64
- No need to install .NET separately - the installer is self-contained

## Install

Download the latest `DesktopIcons-Setup-x.y.z.exe` from the [Releases page](https://github.com/awly333/windows-desktop-icons/releases) and run it.

The installer asks where to put the app:

- **Per-user** (default): `%LocalAppData%\Programs\Desktop Icons` - no admin required
- **Per-machine**: `Program Files\Desktop Icons` - admin required

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

```text
src\
  DesktopIcons.Core\   # Shared library (desktop interop, fingerprint, storage)
  DesktopIcons.Cli\    # CLI: dump / apply / save / restore / list / delete
  DesktopIcons.App\    # WinUI 3 desktop app (unpackaged, self-contained)
installer\             # Inno Setup script
tools\                 # build-icon.ps1, build-installer.ps1
```

## CLI

A standalone command-line tool is available alongside the GUI app.

**Install via Scoop (no admin required):**

```powershell
scoop install https://raw.githubusercontent.com/awly333/windows-desktop-icons/main/scoop/desktop-icons.json
```

**Commands:**

```text
di save <name>        Save the current desktop layout
di restore <name>     Restore a saved layout
di list               List saved layouts for the current monitor config
di list --all         List layouts across all monitor configs
di delete <name>      Delete a layout
di --version          Print version
```

Layout names can be anything (spaces allowed). Layouts are stored per monitor configuration - switching setups keeps each config's layouts isolated.

**Advanced:**

```text
di dump <path>        Export current layout to a specific JSON file
di apply <path>       Apply layout from a specific JSON file
```

## License

MIT - see [LICENSE](LICENSE).
