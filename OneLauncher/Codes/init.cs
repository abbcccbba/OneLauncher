using OneLauncher.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace OneLauncher.Codes;
public static class Init
{
    public static string BasePath { get; private set; }
    public static DBManger ConfigManger { get; private set; }
    public static bool IsNetwork { get; private set; }
    public static async Task Initialize()
    {
        // 检查网络连接状态
        try
        {
            using (var ping = new Ping())
            {
                var reply = await ping.SendPingAsync("microsoft.com", 1000); 
                IsNetwork = reply.Status == IPStatus.Success;
            }
        }
        catch
        {
            IsNetwork = false; // 网络不可用
        }
        // 初始化 BasePath
        BasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/OneLauncher/";
        Directory.CreateDirectory(BasePath); // 确保目录存在

        // 初始化 ConfigManger
        ConfigManger = new DBManger(new AppConfig(), BasePath);
        Debug.WriteLine(IsNetwork);
    }
}