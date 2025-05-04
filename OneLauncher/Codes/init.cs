using OneLauncher.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OneLauncher.Codes
{
    public static class Init
    {
        public static string BasePath { get; private set; }
        public static DBManger ConfigManger { get; private set; }

        public static async Task Initialize()
        {
            // 初始化 BasePath
            BasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/OneLauncher/";
            Directory.CreateDirectory(BasePath); // 确保目录存在

            // 初始化 ConfigManger
            ConfigManger = new DBManger(new AppConfig(), BasePath);
        }
    }
}