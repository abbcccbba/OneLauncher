using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Launcher.Strategys;
using OneLauncher.Core.Minecraft;
namespace OneLauncher.Core.Launcher;
public partial class LaunchCommandBuilder
{
    public VersionInfomations versionInfo;
    private readonly char separator;
    private readonly string versionId;
    private readonly string basePath;
    private readonly string versionPath;
    // 下面是外部注入的
    private string gamePath;
    private ModEnum modType = ModEnum.none;
    private ServerInfo? serverInfo = null;
    private UserModel loginUser;

    // 构造函数保持不变
    private LaunchCommandBuilder(
        string basePath,
        string versionId)
    {
        this.basePath = basePath;
        this.versionPath = Path.Combine(this.basePath, "versions", versionId);
        this.versionId = versionId;
#if WINDOWS
        separator = ';';
#else
    separator = ':';
#endif
        #region 对于未Set的默认值
        this.gamePath = this.basePath; // 默认游戏路径为根目录
        #endregion
    }
    #region 链式调用
    public static async Task<LaunchCommandBuilder> CreateAsync(string basePath, string versionId)
    {
        return new LaunchCommandBuilder(basePath, versionId)
        {
            versionInfo = new VersionInfomations(
            await File.ReadAllTextAsync(Path.Combine(basePath,"versions",versionId, "version.json")),
            basePath)
        };
    }
    public LaunchCommandBuilder SetGamePath(string gamePath)
    {
        this.gamePath = gamePath;
        return this;
    }
    public LaunchCommandBuilder SetModType(ModEnum modType)
    {
        this.modType = modType;
        return this;
    }
    public LaunchCommandBuilder SetServerInfo(ServerInfo? serverInfo)
    {
        this.serverInfo = serverInfo;
        return this;
    }
    public LaunchCommandBuilder SetLoginUser(UserModel user)
    {
        this.loginUser = user;
        return this;
    }
    #endregion
    public string GetJavaPath() =>
        Tools.IsUseOlansJreOrOssJdk(versionInfo.GetJavaVersion());

    public async Task<IEnumerable<string>> BuildCommand(IEnumerable<string>? extraJvmArgs = null)
    {
        IModStrategy? strategy = null; // 策略可以是null，代表原版
        if (modType != ModEnum.none)
            strategy = await CreateAndInitStrategy();

        // 确定主类：如果策略提供了覆盖，则使用策略的；否则用原版的
        string mainClass = strategy?.GetMainClassOverride() ?? versionInfo.GetMainClass();

        // 将策略传递给各个构建器
        List<string> rargs = new List<string>();
        if (extraJvmArgs != null)
            rargs.AddRange(extraJvmArgs);
        rargs.AddRange(BuildJvmArgs(strategy));
        rargs.Add(strategy?.GetMainClassOverride() ?? versionInfo.GetMainClass()); // 别把主类忘了
        rargs.AddRange(BuildGameArgs(gamePath ,strategy));
        return rargs;
    }

    // 策略方法
    private async Task<IModStrategy?> CreateAndInitStrategy()
    {
        IModStrategy strategy = modType switch
        {
            ModEnum.fabric => await FabricStrategy.CreateAsync(versionPath, basePath),
            ModEnum.quilt => await QuiltStrategy.CreateAsync(versionPath, basePath),
            ModEnum.neoforge => await NeoForgeStrategy.CreateAsync(versionPath, basePath, versionId),
            ModEnum.forge => await ForgeStrategy.CreateAsync(versionPath, basePath, versionId),
            _ => throw new OlanException("内部异常",$"不支持的模组加载器 {modType}")
        };
        return strategy;
    }
}