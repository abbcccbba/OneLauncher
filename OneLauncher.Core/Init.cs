using OneLauncher.Core.Helper;
using OneLauncher.Core.Net.ConnectToolPower;
using OneLauncher.Core.Net.msa;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OneLauncher.Core;
public static class Init
{
    public const string OneLauncherVersoin = "1.3.0";
    public const string ApplicationUUID = "com.onelauncher.qustellar";
    public const string AzureApplicationID = "53740b20-7f24-46a3-82cc-ea0376b9f5b5";
    public static Task<OlanException> InitTask { get; } = Task.Run(() =>Init.Initialize());
    public static string BasePath { get; private set; }
    public static string GameRootPath { get; private set; }
    public static DBManger ConfigManger { get; private set; }
    public static SystemType systemType { get; private set; }
    public static MsalMicrosoftAuthenticator MMA { get; private set; }
    public static MainPower ConnentToolPower { get; private set; }
    public static async Task<OlanException> Initialize()
    {
        try
        {
            BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneLauncher");
            Directory.CreateDirectory(BasePath); 
                                                 
            ConfigManger = await DBManger.CreateAsync(new AppConfig()
            {
                DefaultUserModel =
                // 默认用户模型
                new UserModel("ZhiWei", Guid.NewGuid()),
                // 应用设置
                OlanSettings = new AppSettings()
                {
                    // 使用标准参数
                    MinecraftJvmArguments = JvmArguments.CreateFromMode(OptimizationMode.Standard)
                }
            }, BasePath);
            GameRootPath = ConfigManger.config.OlanSettings.GameInstallPath ?? Path.Combine(BasePath, "installed",".minecraft");
            // 初始化系统信息
            systemType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? SystemType.windows :
                         RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? SystemType.linux :
                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? SystemType.osx : SystemType.linux;
            // 初始化微软验证系统
            MMA = await MsalMicrosoftAuthenticator.CreateAsync(Init.AzureApplicationID);
            // 初始化联机组件
            ConnentToolPower = await MainPower.InitializationAsync();
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
