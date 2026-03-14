namespace BaumLaunch.Models;

public static class AppCatalog
{
    public static List<AppEntry> GetAll() => new()
    {
        // Browsers — ARP names match DisplayName exactly
        new() { WinGetId = "Google.Chrome",                           DisplayName = "Google Chrome",             Category = "Browsers" },
        new() { WinGetId = "Mozilla.Firefox",                         DisplayName = "Mozilla Firefox",           Category = "Browsers" },
        new() { WinGetId = "Brave.Brave",                             DisplayName = "Brave Browser",             Category = "Browsers" },
        new() { WinGetId = "Opera.Opera",                             DisplayName = "Opera",                     Category = "Browsers" },

        // Runtimes — ARP names often differ from catalog DisplayName
        new() { WinGetId = "Python.Python.3.13",                      DisplayName = "Python 3.13",               Category = "Runtimes",
                ArpNameHint = "Python 3.13" },                         // ARP: "Python 3.13.x (64-bit)"
        new() { WinGetId = "Microsoft.DotNet.DesktopRuntime.8",       DisplayName = ".NET 8 Desktop Runtime",    Category = "Runtimes",
                ArpNameHint = "Desktop Runtime - 8" },                 // ARP: "Microsoft Windows Desktop Runtime - 8.0.x (x64)"
        new() { WinGetId = "Microsoft.VCRedist.2015+.x64",            DisplayName = "VC++ Redistributable x64",  Category = "Runtimes",
                ArpNameHint = "Visual C++ 2015" },                     // ARP: "Microsoft Visual C++ 2015-2022 Redistributable (x64)"
        new() { WinGetId = "OpenJS.NodeJS.LTS",                       DisplayName = "Node.js LTS",               Category = "Runtimes",
                ArpNameHint = "Node.js" },                             // ARP: "Node.js" or "Node.js LTS"

        // AI Tools
        new() { WinGetId = "Anthropic.Claude",                        DisplayName = "Claude",                    Category = "AI Tools",
                ArpNameHint = "Claude" },
        new() { WinGetId = "OpenAI.ChatGPT",                          DisplayName = "ChatGPT",                   Category = "AI Tools",
                ArpNameHint = "ChatGPT" },
        new() { WinGetId = "Mintplex.AnythingLLMDesktop",             DisplayName = "AnythingLLM",               Category = "AI Tools",
                ArpNameHint = "AnythingLLM" },

        // Dev Tools
        new() { WinGetId = "Git.Git",                                 DisplayName = "Git",                       Category = "Dev Tools",
                ArpNameHint = "Git" },                                 // ARP: "Git" or "Git version X.XX.X"
        new() { WinGetId = "Microsoft.VisualStudioCode",              DisplayName = "VS Code",                   Category = "Dev Tools",
                ArpNameHint = "Visual Studio Code" },                  // ARP: "Microsoft Visual Studio Code"
        new() { WinGetId = "Microsoft.PowerToys",                     DisplayName = "PowerToys",                 Category = "Dev Tools",
                ArpNameHint = "PowerToys" },
        new() { WinGetId = "Notepad++.Notepad++",                     DisplayName = "Notepad++",                 Category = "Dev Tools",
                ArpNameHint = "Notepad++" },
        new() { WinGetId = "voidtools.Everything",                     DisplayName = "Everything Search",         Category = "Dev Tools",
                ArpNameHint = "Everything" },
        new() { WinGetId = "GitHub.GitHubDesktop",                    DisplayName = "GitHub Desktop",            Category = "Dev Tools",
                ArpNameHint = "GitHub Desktop" },
        new() { WinGetId = "Microsoft.WindowsTerminal",               DisplayName = "Windows Terminal",          Category = "Dev Tools",
                ArpNameHint = "Windows Terminal" },
        new() { WinGetId = "Microsoft.PowerBIDesktop",                DisplayName = "Power BI Desktop",          Category = "Dev Tools",
                ArpNameHint = "Power BI Desktop" },
        new() { WinGetId = "JGraph.Draw",                             DisplayName = "draw.io",                   Category = "Dev Tools",
                ArpNameHint = "draw.io" },
        new() { WinGetId = "RaspberryPiFoundation.RaspberryPiImager", DisplayName = "Raspberry Pi Imager",       Category = "Dev Tools",
                ArpNameHint = "Raspberry Pi Imager" },
        new() { WinGetId = "Microsoft.WSL",                           DisplayName = "WSL",                       Category = "Dev Tools",
                ArpNameHint = "Windows Subsystem for Linux" },         // ARP: "Windows Subsystem for Linux"
        new() { WinGetId = "Nmap.Nmap",                               DisplayName = "Nmap",                      Category = "Dev Tools",
                ArpNameHint = "Nmap" },

        // Media & Tools
        new() { WinGetId = "RODE.RodeCentral",                        DisplayName = "Rode Central",              Category = "Media & Tools",
                ArpNameHint = "RODE Central" },                        // ARP: "RODE Central"
        new() { WinGetId = "VideoLAN.VLC",                            DisplayName = "VLC Media Player",          Category = "Media & Tools",
                ArpNameHint = "VLC media player" },                    // ARP: "VLC media player"
        new() { WinGetId = "7zip.7zip",                               DisplayName = "7-Zip",                     Category = "Media & Tools",
                ArpNameHint = "7-Zip" },                               // ARP: "7-Zip 24.xx (x64 edition)"
        new() { WinGetId = "OBSProject.OBSStudio",                    DisplayName = "OBS Studio",                Category = "Media & Tools",
                ArpNameHint = "OBS Studio" },
        new() { WinGetId = "HandBrake.HandBrake",                     DisplayName = "HandBrake",                 Category = "Media & Tools",
                ArpNameHint = "HandBrake" },
        new() { WinGetId = "Audacity.Audacity",                       DisplayName = "Audacity",                  Category = "Media & Tools",
                ArpNameHint = "Audacity" },
        new() { WinGetId = "Gimp.Gimp",                               DisplayName = "GIMP",                      Category = "Media & Tools",
                ArpNameHint = "GIMP" },
        new() { WinGetId = "ShareX.ShareX",                           DisplayName = "ShareX",                    Category = "Media & Tools",
                ArpNameHint = "ShareX" },

        // Game Launchers
        new() { WinGetId = "Valve.Steam",                             DisplayName = "Steam",                     Category = "Game Launchers",
                ArpNameHint = "Steam" },
        new() { WinGetId = "ElectronicArts.EADesktop",                DisplayName = "EA Desktop",                Category = "Game Launchers",
                ArpNameHint = "EA" },                                  // ARP: "EA app" or "EA Desktop"
        new() { WinGetId = "EpicGames.EpicGamesLauncher",             DisplayName = "Epic Games Launcher",       Category = "Game Launchers",
                ArpNameHint = "Epic Games" },                          // ARP: "Epic Games Launcher"
        new() { WinGetId = "Ubisoft.Connect",                         DisplayName = "Ubisoft Connect",           Category = "Game Launchers",
                ArpNameHint = "Ubisoft Connect" },
        new() { WinGetId = "GOG.Galaxy",                              DisplayName = "GOG Galaxy",                Category = "Game Launchers",
                ArpNameHint = "GOG Galaxy" },
        new() { WinGetId = "Blizzard.BattleNet",                      DisplayName = "Battle.net",                Category = "Game Launchers",
                ArpNameHint = "Battle.net" },
        new() { WinGetId = "Wargaming.WorldOfTanks",                  DisplayName = "World of Tanks",            Category = "Game Launchers",
                ArpNameHint = "World of Tanks" },
        new() { WinGetId = "ebkr.r2modman",                           DisplayName = "r2modman",                  Category = "Game Launchers",
                ArpNameHint = "r2modman" },
        new() { WinGetId = "Overwolf.CurseForge",                     DisplayName = "CurseForge",                Category = "Game Launchers",
                ArpNameHint = "CurseForge" },

        // Communication
        new() { WinGetId = "Discord.Discord",                         DisplayName = "Discord",                   Category = "Communication",
                ArpNameHint = "Discord" },
        new() { WinGetId = "Spotify.Spotify",                         DisplayName = "Spotify",                   Category = "Communication",
                ArpNameHint = "Spotify" },
        new() { WinGetId = "Zoom.Zoom",                               DisplayName = "Zoom",                      Category = "Communication",
                ArpNameHint = "Zoom" },
        new() { WinGetId = "SlackTechnologies.Slack",                 DisplayName = "Slack",                     Category = "Communication",
                ArpNameHint = "Slack" },

        // System Tools
        new() { WinGetId = "Apache.OpenOffice",                       DisplayName = "OpenOffice",                Category = "System Tools",
                ArpNameHint = "OpenOffice" },                          // ARP: "Apache OpenOffice X.X"
        new() { WinGetId = "ASUS.ArmouryCrate",                       DisplayName = "Armoury Crate",             Category = "System Tools",
                ArpNameHint = "Armoury Crate" },
        new() { WinGetId = "MiniTool.PartitionWizard.Free",           DisplayName = "MiniTool Partition Wizard", Category = "System Tools",
                ArpNameHint = "MiniTool Partition Wizard" },
        new() { WinGetId = "Spreadsong.PDFgear",                      DisplayName = "PDFgear",                   Category = "System Tools",
                ArpNameHint = "PDFgear" },
        new() { WinGetId = "Wagnardsoft.DisplayDriverUninstaller",     DisplayName = "Display Driver Uninstaller",Category = "System Tools",
                ArpNameHint = "Display Driver Uninstaller" },
        new() { WinGetId = "CrystalDewWorld.CrystalDiskInfo",         DisplayName = "CrystalDiskInfo",           Category = "System Tools",
                ArpNameHint = "CrystalDiskInfo" },
        new() { WinGetId = "Elgato.StreamDeck",                       DisplayName = "Stream Deck",               Category = "System Tools",
                ArpNameHint = "Stream Deck" },                         // ARP: "Elgato Stream Deck" or "Stream Deck"
        new() { WinGetId = "WinDirStat.WinDirStat",                   DisplayName = "WinDirStat",                Category = "System Tools",
                ArpNameHint = "WinDirStat" },
        new() { WinGetId = "REALiX.HWiNFO",                          DisplayName = "HWiNFO",                    Category = "System Tools",
                ArpNameHint = "HWiNFO" },                              // ARP: "HWiNFO64"
        new() { WinGetId = "BleachBit.BleachBit",                     DisplayName = "BleachBit",                 Category = "System Tools",
                ArpNameHint = "BleachBit" },
        new() { WinGetId = "Logitech.GHUB",                           DisplayName = "Logitech G HUB",            Category = "System Tools",
                ArpNameHint = "LGHUB" },                               // ARP: "LGHUB"
        new() { WinGetId = "Corsair.iCUE.5",                          DisplayName = "iCUE",                      Category = "System Tools",
                ArpNameHint = "iCUE" },
        new() { WinGetId = "Rufus.Rufus",                             DisplayName = "Rufus",                     Category = "System Tools",
                ArpNameHint = "Rufus" },
        new() { WinGetId = "Microsoft.WindowsApp",                    DisplayName = "Windows App",               Category = "System Tools",
                ArpNameHint = "Windows App" },
        new() { WinGetId = "Creality.CrealityPrint",                  DisplayName = "Creality Print",            Category = "System Tools",
                ArpNameHint = "Creality Print" },
    };
}
