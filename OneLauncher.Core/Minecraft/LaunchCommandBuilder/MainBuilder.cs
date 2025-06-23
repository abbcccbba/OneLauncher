using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Mod.ModLoader.fabric;
using OneLauncher.Core.Mod.ModLoader.forgeseries;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace OneLauncher.Core.Minecraft;

/// <summary>
/// Minecraft 启动命令构造器，提供一个简单的方法来生成启动命令。
/// </summary>
public partial class LaunchCommandBuilder
{
    public VersionInfomations versionInfo;
    private readonly GameData gameData;
    private readonly string version;
    private readonly string basePath;
    private readonly ModEnum modType;
    private FabricVJParser fabricParser;
    private ForgeSeriesUsing neoForgeParser;
    private readonly string separator;
    private readonly string VersionPath;
    private readonly ServerInfo? serverInfo;
    public LaunchCommandBuilder(
        string basePath,
        GameData gameData,
        ServerInfo? serverInfo = null)
    {
        this.basePath = basePath;
        this.version = gameData.VersionId;
        this.modType = gameData.ModLoader;
        this.serverInfo = serverInfo;
        this.gameData = gameData;
        this.VersionPath = Path.Combine(this.basePath, "versions", this.version);
#if WINDOWS
        separator = ";";
#else
        separator = ":";
#endif
        versionInfo = new VersionInfomations(
            File.ReadAllText(Path.Combine(VersionPath, "version.json")),
            this.basePath
        );
    }
    public string GetJavaPath() =>
        Tools.IsUseOlansJreOrOssJdk(versionInfo.GetJavaVersion());
    public async Task<string> BuildCommand(string OtherArgs = "",bool useRootLaunch = false)
    {
        string MainClass;
        if (modType == ModEnum.fabric)
        {
            fabricParser = new FabricVJParser(
              Path.Combine(VersionPath, $"version.fabric.json"), basePath);
            MainClass = fabricParser.GetMainClass();
        }
        else if (modType == ModEnum.neoforge || modType == ModEnum.forge)
        {
            neoForgeParser = new ForgeSeriesUsing();
            await neoForgeParser.Init(basePath, version,(modType == ModEnum.forge ? true : false));
            MainClass = neoForgeParser.info.MainClass;
        }
        else MainClass = versionInfo.GetMainClass();
        string Args = $"{OtherArgs} {BuildJvmArgs()} {MainClass} {BuildGameArgs(useRootLaunch)}";
        Debug.WriteLine(Args);
        return Args;
    }
    private string BuildGameArgs(bool useRootLaunch)
    {
        string serverArgs = string.Empty;
        if(serverInfo != null)
        {
            serverArgs = $"--server \"{((ServerInfo)serverInfo).Ip}\" --port\"{((ServerInfo)serverInfo).Port}\" ";
            if(new Version(version) > new Version("1.20"))
            {
                serverArgs += $"--quickPlayMultiplayer \"{serverInfo.Value.Ip}:{serverInfo.Value.Port}\" ";
            }
        }
        string GameArgs =
            $"--username \"{gameData.DefaultUserModel.Name}\" " +
            $"--version \"{version}\" " +
            $"--gameDir \"{(useRootLaunch ? basePath : gameData.InstancePath)}\" " +
            $"--assetsDir \"{(Path.Combine(basePath, "assets"))}\" " +
            // 1.7版本及以上启用新用户验证机制
            (new Version(version) > new Version("1.7") ?
            $"--assetIndex \"{versionInfo.GetAssetIndexVersion()}\" " +
            $"--uuid \"{gameData.DefaultUserModel.uuid}\" " +
            $"--accessToken \"{gameData.DefaultUserModel.AccessToken.ToString()}\" " +
            $"--userType \"{(gameData.DefaultUserModel.IsMsaUser ? "msa" : "legacy")}\" " +
            $"--versionType \"OneLauncher\" " +
            serverArgs+
            "--userProperties {} "
            // 针对旧版用户验证机制
            : $"--session \"{gameData.DefaultUserModel.AccessToken}\" ");
        if (modType == ModEnum.neoforge || modType == ModEnum.forge)
            GameArgs +=
                string.Join(" ", neoForgeParser.info.Arguments.Game);
        Debug.WriteLine(GameArgs);
        return GameArgs;
    }
}
