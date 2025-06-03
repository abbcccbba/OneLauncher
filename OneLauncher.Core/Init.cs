using System.Runtime.InteropServices;

namespace OneLauncher.Core;
public static class Init
{
    public const string OneLauncherVersoin = "1.0.1";
    public const string AzureApplicationID = "53740b20-7f24-46a3-82cc-ea0376b9f5b5";
    public static string BasePath { get; private set; }
    public static string GameRootPath { get; private set; }
    public static DBManger ConfigManger { get; private set; }
    public static SystemType systemType { get; private set; }
    public static void Initialize()
    {
        // 初始化 BasePath
        BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneLauncher");
        GameRootPath = Path.Combine(BasePath, ".minecraft");
        Directory.CreateDirectory(BasePath); // 确保目录存在
        // 初始化 ConfigManger
        ConfigManger = new DBManger(new AppConfig()
        {
            DefaultUserModel =
            // 默认用户模型
            new UserModel("ZhiWei", Guid.NewGuid())
        }, BasePath);
        // 初始化系统信息
        systemType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? SystemType.windows :
                           RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? SystemType.linux :
                           RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? SystemType.osx : SystemType.linux;
    }
}
