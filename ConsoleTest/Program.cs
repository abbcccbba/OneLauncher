using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Core.Serialization;
using System.Text.Json;

await JsonSerializer.DeserializeAsync<MinecraftVersionInfo>(
    File.OpenRead("C:\\Users\\wwwin\\OneLauncher\\.minecraft\\versions\\1.20.1\\1.20.1.json"),
    OneLauncherJsonContext.Default.VersionInformation
    );