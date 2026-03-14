# BaumLaunch

A graphical WinGet-based package manager for Windows that lives in your system tray, keeps your apps up to date, and makes reinstalling a new machine effortless.

---

## Features

### Package Management
- **Curated app catalog** — 33 hand-picked applications across 7 categories, all sourced from the official WinGet repository
- **Install & Update** — install apps you don't have, or update ones that are out of date, with a single click
- **Update All** — one button to update every outdated app on the system at once
- **Live status badges** — each app shows whether it is Up to Date, Update Available, or Not Installed

### System Tray
- **Runs silently in the background** — minimizes to the system tray instead of closing
- **Periodic update checks** — automatically scans for available updates every 6 hours
- **Tray notifications** — notified the moment updates are found without having to open the app

### Setup Profiles
- **Export your setup** — check the apps you want, export your selection to a JSON profile file
- **Import on a new machine** — import a profile after a clean Windows install to instantly know what to reinstall
- **Portable profiles** — share profiles with teammates or save them to cloud storage for disaster recovery

### UI
- **Category filter tabs** — quickly filter by Browsers, Runtimes, Dev Tools, Media & Tools, Game Launchers, Communication, or System Tools
- **Activity log** — live scrollable log of WinGet output so you can see exactly what's happening
- **Dark theme** — clean, modern borderless window with the same design language as BaumDash

---

## App Catalog

| Category | Apps |
|---|---|
| **Browsers** | Google Chrome, Mozilla Firefox, Brave, Opera |
| **Runtimes** | Python 3.13, .NET 8 Desktop Runtime, VC++ Redistributable x64, Node.js LTS |
| **Dev Tools** | Git, VS Code, PowerToys, Notepad++, Everything Search |
| **Media & Tools** | VLC, 7-Zip, OBS Studio, HandBrake, Audacity, GIMP |
| **Game Launchers** | Steam, EA Desktop, Epic Games Launcher, Ubisoft Connect, GOG Galaxy |
| **Communication** | Discord, Spotify, Zoom, Slack |
| **System Tools** | CrystalDiskInfo, Stream Deck, WinDirStat, HWiNFO, BleachBit |

---

## Requirements

- Windows 11 (22H2 / build 22621 or later)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [WinGet](https://learn.microsoft.com/windows/package-manager/winget/) (included with Windows 11 by default)

---

## Installation

1. Download `BaumLaunch-Setup-1.0.0.exe` from the [Releases](../../releases) page
2. Run the installer — no admin rights required (per-user install)
3. Optionally enable **Start with Windows** during setup
4. BaumLaunch will open and scan your installed apps automatically

---

## Usage

### Checking for updates
BaumLaunch checks for updates automatically every 6 hours. You can trigger a manual refresh with the **Refresh** button in the toolbar.

### Installing or updating an app
Click the **Install** or **Update** button on any app row. Progress and WinGet output are streamed to the log panel at the bottom.

### Update All
Click **Update All** in the toolbar to update every app that has an available upgrade. Updates run sequentially with live log output.

### Exporting a profile
1. Check the apps you want in your profile using the checkboxes on each row
2. Click **Export Profile** in the toolbar
3. Save the `.json` file — keep it on a USB drive, in OneDrive, or anywhere accessible

### Importing a profile
1. Click **Import Profile** in the toolbar
2. Select your previously exported `.json` file
3. Apps in the profile will be checked automatically — install any that are missing

---

## Building from Source

```bash
git clone https://github.com/Bruiserbaum/BaumLaunch.git
cd BaumLaunch/BaumLaunch
dotnet build
```

To publish and build the installer:

```bash
cd BaumLaunch/installer
build-installer.bat
```

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php) to be installed.

---

## Related Projects

- [BaumDash](https://github.com/Bruiserbaum/BaumDash) — Audio mixer, Discord integration, media controls, and smart home dashboard for Windows
