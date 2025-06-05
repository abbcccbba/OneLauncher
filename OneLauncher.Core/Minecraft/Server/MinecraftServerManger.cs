using OneLauncher.Core.Downloader;
using OneLauncher.Core.Minecraft.JsonModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core.Minecraft.Server;

public class MinecraftServerManger
{
    public static async Task<int> Init(
        string version,
        bool IsVI
        )
    {
        MinecraftVersionInfo serverGetInfo;
        string versionPath = Path.Combine(Core.Init.GameRootPath,"versions",version);
        serverGetInfo = await JsonSerializer.DeserializeAsync<MinecraftVersionInfo>
            (File.OpenRead(Path.Combine(versionPath,$"{version}.json")),MinecraftJsonContext.Default.MinecraftVersionInfo);
        if (serverGetInfo.Downloads.Server == null)
            throw new OlanException("无法初始化服务端","当前版本不支持服务端",OlanExceptionAction.Error);
        using (Download t = new Download())
        {
            await t.DownloadFileAndSha1(
                serverGetInfo.Downloads.Server.Url,
                Path.Combine(versionPath,"server.jar"),
                serverGetInfo.Downloads.Server.Sha1
                );
        }
        Directory.CreateDirectory(
            (IsVI)
            ? Path.Combine(versionPath, "servers")
            : Path.Combine(Core.Init.GameRootPath, "servers")
            );
        await File.WriteAllTextAsync((
            (IsVI)
            ? Path.Combine(versionPath,"servers","eula.txt")
            : Path.Combine(Core.Init.GameRootPath,"servers","eula.txt"))
            ,"eula=true");
        return serverGetInfo.JavaVersion.MajorVersion;
    }
    public static void Run(string version,string sarg,int java,bool IsVI)
    {
        using (Process serverProcess = new Process())
        {
            const string JvmArgsForServer = "-XX:+UseG1GC -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M -XX:+DisableExplicitGC -XX:+ParallelRefProcEnabled";
            string versionPath = Path.Combine(Core.Init.GameRootPath, "versions", version);
            serverProcess.StartInfo = new ProcessStartInfo()
            {
                FileName = Tools.IsUseOlansJreOrOssJdk(java, OneLauncher.Core.Init.BasePath),
                Arguments = JvmArgsForServer + $" {sarg} " + $"-jar {(Path.Combine(versionPath, "server.jar"))}",
                WorkingDirectory =
                (IsVI)
                ? Path.Combine(versionPath, "servers")
                : Path.Combine(Core.Init.GameRootPath,"servers")
            };
            serverProcess.Start();
        }
    }
}
