using OneLauncher.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OneLauncher.Codes;
public static class Init
{
    public static string BasePath { get; private set; }
    public static DBManger ConfigManger { get; private set; }
    public static SystemType systemType { get; private set; }
    public static async Task Initialize()
    {
        // 初始化 BasePath
        BasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/OneLauncher/";
        Directory.CreateDirectory(BasePath); // 确保目录存在
        // 初始化 ConfigManger
        ConfigManger = new DBManger(new AppConfig() { DefaultUserModel =
            // 默认用户模型
            new UserModel ("ZhiWei",Guid.NewGuid().ToString())}, BasePath);
        // 初始化系统信息
        systemType =       RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? SystemType.windows :
                           RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? SystemType.linux :
                           RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? SystemType.osx : SystemType.linux;
    }
}