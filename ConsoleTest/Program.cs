using OneLauncher.Core.AboutMod.neoforge;
string GameRootPath = "C:\\Users\\wwwin\\OneLauncher\\.minecraft";
NeoForgeInstallTasker installTasker = new NeoForgeInstallTasker
                (
                    new OneLauncher.Core.Download(),
                    Path.Combine(GameRootPath, "libraries"),
                    Path.Combine(GameRootPath, "versions", "1.21.1"),
                    "1.21.1"
                );
await installTasker.StartReady("https://maven.neoforged.net/releases/net/neoforged/neoforge/21.1.173/neoforge-21.1.173-installer.jar");
await installTasker.ToRunProcessors("C:\\Users\\wwwin\\OneLauncher\\.minecraft\\versions\\1.21.1\\1.21.1.jar",null);
Console.ReadKey();
