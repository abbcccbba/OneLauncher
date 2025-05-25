using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core;
public static class Init
{
    public static string BasePath { get; private set; }
    public static string GameRootPath { get; private set; }
    public static DBManger ConfigManger { get; private set; }
    public static SystemType systemType { get; private set; }
    public static int CPUPros { get; private set; }
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
        // 初始化 CPU 线程数
        CPUPros = Environment.ProcessorCount;
    }
}
