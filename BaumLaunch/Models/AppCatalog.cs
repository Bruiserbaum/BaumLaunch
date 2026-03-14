namespace BaumLaunch.Models;

public static class AppCatalog
{
    public static List<AppEntry> GetAll() => new()
    {
        // Browsers
        new() { WinGetId = "Google.Chrome",                          DisplayName = "Google Chrome",             Category = "Browsers" },
        new() { WinGetId = "Mozilla.Firefox",                        DisplayName = "Mozilla Firefox",           Category = "Browsers" },
        new() { WinGetId = "Brave.Brave",                            DisplayName = "Brave Browser",             Category = "Browsers" },
        new() { WinGetId = "Opera.Opera",                            DisplayName = "Opera",                     Category = "Browsers" },

        // Runtimes
        new() { WinGetId = "Python.Python.3.13",                     DisplayName = "Python 3.13",               Category = "Runtimes" },
        new() { WinGetId = "Microsoft.DotNet.DesktopRuntime.8",      DisplayName = ".NET 8 Desktop Runtime",    Category = "Runtimes" },
        new() { WinGetId = "Microsoft.VCRedist.2015+.x64",           DisplayName = "VC++ Redistributable x64",  Category = "Runtimes" },
        new() { WinGetId = "OpenJS.NodeJS.LTS",                      DisplayName = "Node.js LTS",               Category = "Runtimes" },

        // AI Tools
        new() { WinGetId = "Anthropic.Claude",                       DisplayName = "Claude",                    Category = "AI Tools" },
        new() { WinGetId = "OpenAI.ChatGPT",                         DisplayName = "ChatGPT",                   Category = "AI Tools" },
        new() { WinGetId = "Mintplex.AnythingLLMDesktop",            DisplayName = "AnythingLLM",               Category = "AI Tools" },

        // Dev Tools
        new() { WinGetId = "Git.Git",                                DisplayName = "Git",                       Category = "Dev Tools" },
        new() { WinGetId = "Microsoft.VisualStudioCode",             DisplayName = "VS Code",                   Category = "Dev Tools" },
        new() { WinGetId = "Microsoft.PowerToys",                    DisplayName = "PowerToys",                 Category = "Dev Tools" },
        new() { WinGetId = "Notepad++.Notepad++",                    DisplayName = "Notepad++",                 Category = "Dev Tools" },
        new() { WinGetId = "voidtools.Everything",                   DisplayName = "Everything Search",         Category = "Dev Tools" },
        new() { WinGetId = "GitHub.GitHubDesktop",                   DisplayName = "GitHub Desktop",            Category = "Dev Tools" },
        new() { WinGetId = "Microsoft.WindowsTerminal",              DisplayName = "Windows Terminal",          Category = "Dev Tools" },
        new() { WinGetId = "Microsoft.PowerBIDesktop",               DisplayName = "Power BI Desktop",          Category = "Dev Tools" },
        new() { WinGetId = "JGraph.Draw",                            DisplayName = "draw.io",                   Category = "Dev Tools" },
        new() { WinGetId = "RaspberryPiFoundation.RaspberryPiImager", DisplayName = "Raspberry Pi Imager",     Category = "Dev Tools" },

        // Media & Tools
        new() { WinGetId = "VideoLAN.VLC",                           DisplayName = "VLC Media Player",          Category = "Media & Tools" },
        new() { WinGetId = "7zip.7zip",                              DisplayName = "7-Zip",                     Category = "Media & Tools" },
        new() { WinGetId = "OBSProject.OBSStudio",                   DisplayName = "OBS Studio",                Category = "Media & Tools" },
        new() { WinGetId = "HandBrake.HandBrake",                    DisplayName = "HandBrake",                 Category = "Media & Tools" },
        new() { WinGetId = "Audacity.Audacity",                      DisplayName = "Audacity",                  Category = "Media & Tools" },
        new() { WinGetId = "Gimp.Gimp",                              DisplayName = "GIMP",                      Category = "Media & Tools" },
        new() { WinGetId = "ShareX.ShareX",                          DisplayName = "ShareX",                    Category = "Media & Tools" },

        // Game Launchers
        new() { WinGetId = "Valve.Steam",                            DisplayName = "Steam",                     Category = "Game Launchers" },
        new() { WinGetId = "ElectronicArts.EADesktop",               DisplayName = "EA Desktop",                Category = "Game Launchers" },
        new() { WinGetId = "EpicGames.EpicGamesLauncher",            DisplayName = "Epic Games Launcher",       Category = "Game Launchers" },
        new() { WinGetId = "Ubisoft.Connect",                        DisplayName = "Ubisoft Connect",           Category = "Game Launchers" },
        new() { WinGetId = "GOG.Galaxy",                             DisplayName = "GOG Galaxy",                Category = "Game Launchers" },
        new() { WinGetId = "Blizzard.BattleNet",                     DisplayName = "Battle.net",                Category = "Game Launchers" },
        new() { WinGetId = "Wargaming.WorldOfTanks",                 DisplayName = "World of Tanks",            Category = "Game Launchers" },
        new() { WinGetId = "ebkr.r2modman",                          DisplayName = "r2modman",                  Category = "Game Launchers" },
        new() { WinGetId = "Overwolf.CurseForge",                    DisplayName = "CurseForge",                Category = "Game Launchers" },

        // Communication
        new() { WinGetId = "Discord.Discord",                        DisplayName = "Discord",                   Category = "Communication" },
        new() { WinGetId = "Spotify.Spotify",                        DisplayName = "Spotify",                   Category = "Communication" },
        new() { WinGetId = "Zoom.Zoom",                              DisplayName = "Zoom",                      Category = "Communication" },
        new() { WinGetId = "SlackTechnologies.Slack",                DisplayName = "Slack",                     Category = "Communication" },

        // System Tools
        new() { WinGetId = "CrystalDewWorld.CrystalDiskInfo",        DisplayName = "CrystalDiskInfo",           Category = "System Tools" },
        new() { WinGetId = "Elgato.StreamDeck",                      DisplayName = "Stream Deck",               Category = "System Tools" },
        new() { WinGetId = "WinDirStat.WinDirStat",                  DisplayName = "WinDirStat",                Category = "System Tools" },
        new() { WinGetId = "REALiX.HWiNFO",                         DisplayName = "HWiNFO",                    Category = "System Tools" },
        new() { WinGetId = "BleachBit.BleachBit",                    DisplayName = "BleachBit",                 Category = "System Tools" },
        new() { WinGetId = "Logitech.GHUB",                          DisplayName = "Logitech G HUB",            Category = "System Tools" },
        new() { WinGetId = "Corsair.iCUE.5",                         DisplayName = "iCUE",                      Category = "System Tools" },
        new() { WinGetId = "Rufus.Rufus",                            DisplayName = "Rufus",                     Category = "System Tools" },
        new() { WinGetId = "Microsoft.WindowsApp",                   DisplayName = "Windows App",               Category = "System Tools" },
        new() { WinGetId = "Creality.CrealityPrint",                 DisplayName = "Creality Print",            Category = "System Tools" },
    };
}
