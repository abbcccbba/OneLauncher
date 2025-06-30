using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Launcher.Strategys;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.Mod.ModLoader.fabric;
using OneLauncher.Core.Mod.ModLoader.fabric.quilt;
using OneLauncher.Core.Mod.ModLoader.forgeseries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OneLauncher.Core.Launcher;
public partial class LaunchCommandBuilder
{
    public VersionInfomations versionInfo;
    private readonly GameData gameData;
    private readonly string version;
    private readonly string basePath;
    private readonly ModEnum modType;
    private readonly string separator;
    private readonly string VersionPath;
    private readonly ServerInfo? serverInfo;

    // 构造函数保持不变
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
        this.separator = Path.PathSeparator.ToString();

        versionInfo = new VersionInfomations(
            File.ReadAllText(Path.Combine(VersionPath, "version.json")),
            this.basePath
        );
    }

    public string GetJavaPath() =>
        Tools.IsUseOlansJreOrOssJdk(versionInfo.GetJavaVersion());

    public async Task<IEnumerable<string>> BuildCommand(string OtherArgs = "", bool useRootLaunch = false)
    {
        // 等启动器重写完了再把这个逻辑丢过去
        await Init.AccountManager.GetUser(gameData.DefaultUserModelID).IntelligentLogin(Init.MsalAuthenticator);
        IModStrategy? strategy = null; // 策略可以是null，代表原版
        if (modType != ModEnum.none)
            strategy = await CreateAndInitStrategy();
        

        // 确定主类：如果策略提供了覆盖，则使用策略的；否则用原版的
        string mainClass = strategy?.GetMainClassOverride() ?? versionInfo.GetMainClass();

        // 将策略传递给各个构建器
        List<string> rargs = new List<string>();
        rargs.AddRange(BuildJvmArgs(strategy));
        rargs.Add(strategy?.GetMainClassOverride() ?? versionInfo.GetMainClass()); // 别把主类忘了
        rargs.AddRange(BuildGameArgs(useRootLaunch ? basePath : gameData.InstancePath ,strategy));
        return rargs;
    }

    // 策略方法
    private async Task<IModStrategy?> CreateAndInitStrategy()
    {
        IModStrategy strategy = modType switch
        {
            ModEnum.fabric => new FabricStrategy(VersionPath, basePath),
            ModEnum.quilt => new QuiltStrategy(VersionPath, basePath),
            ModEnum.neoforge => await NeoForgeStrategy.CreateAsync(VersionPath, basePath, version),
            ModEnum.forge => await ForgeStrategy.CreateAsync(VersionPath, basePath, version),
            _ => throw new OlanException("内部异常",$"不支持的模组加载器 {modType}")
        };
        return strategy;
    }
}