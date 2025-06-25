using OneLauncher.Core.Helper;
using OneLauncher.Core.Net.ConnectToolPower;
using OneLauncher.Core.Net.msa;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OneLauncher.Core.Global;
public static class Init
{
    public const string OneLauncherVersoin = "1.2.0.0";
    public const string ApplicationUUID = "com.onelauncher.lnetface";
    public const string AzureApplicationID = "53740b20-7f24-46a3-82cc-ea0376b9f5b5";
    public static Task<OlanException> InitTask;
    public static string BasePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneLauncher");
    public static string GameRootPath
#if DEBUG
    { get; set; }
#else
    { get; private set; }
#endif
    public static DBManger ConfigManger { get; private set; }
    public static GameDataManager GameDataManger { get; private set; }
    public static AccountManager AccountManager { get; private set; }
    public static SystemType SystemType { get; private set; }
    public static MsalAuthenticator MMA { get; private set; }
    public static List<VersionBasicInfo> MojangVersionList = null;
    public static async Task<OlanException> Initialize()
    {
        try
        {
            Directory.CreateDirectory(BasePath);                          
            ConfigManger = await DBManger.CreateAsync(new AppConfig(), BasePath);
            var installPath = ConfigManger.config.OlanSettings.InstallPath;
            GameRootPath = installPath == null ? Path.Combine(BasePath, "installed",".minecraft") : Path.Combine(installPath,".minecraft");
            // 初始化系统信息
            SystemType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? SystemType.windows :
                         RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? SystemType.linux :
                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? SystemType.osx : SystemType.linux;
            // 初始化微软验证系统
            MMA = await MsalAuthenticator.CreateAsync(AzureApplicationID);
            // 初始化游戏数据管理器
            GameDataManger = await GameDataManager.CreateAsync(GameRootPath);
            // 初始化用户管理器
            AccountManager = await AccountManager.InitializeAsync(BasePath);
            return null;
        }
        catch (ArgumentException ex)
        {
            return new OlanException("参数错误", $"参数未被正常传递：{ex.Message}", OlanExceptionAction.FatalError);
        }
        catch (PathTooLongException ex)
        {
            return new OlanException("路径过长", $"路径过长：{ex.Message}", OlanExceptionAction.FatalError);
        }
        catch (NotSupportedException ex)
        {
            return new OlanException("不支持的操作", $"当前操作不被支持：{ex.Message}", OlanExceptionAction.FatalError);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new OlanException("权限不足", $"当前用户没有足够的权限：{ex.Message}", OlanExceptionAction.FatalError);
        }
        catch (FileNotFoundException ex)
        {
            return new OlanException("文件未找到", $"所需文件不存在：{ex.Message}", OlanExceptionAction.FatalError);
        }
        catch (DirectoryNotFoundException ex)
        {
            return new OlanException("目录未找到", $"所需目录不存在：{ex.Message}", OlanExceptionAction.FatalError);
        }
        catch (InvalidOperationException ex)
        {
            return new OlanException("操作无效", $"当前操作无效：{ex.Message}", OlanExceptionAction.FatalError);
        }
        catch (Exception ex)
        {
            return new OlanException("未知错误", $"发生未知错误：{ex.Message}", OlanExceptionAction.FatalError);
        }
    }
}
