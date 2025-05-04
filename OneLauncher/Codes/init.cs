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
        public static VersionsList Versions { get; private set; }

        public static async Task Initialize()
        {
            // 初始化 BasePath
            BasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/OneLauncher/";
            Directory.CreateDirectory(BasePath); // 确保目录存在

            // 初始化 ConfigManger
            ConfigManger = new DBManger(new AppConfig(), BasePath);

            // 初始化 VersionsList
            try
            {
                Versions = new VersionsList(File.ReadAllText($"{BasePath}/version_manifest.json"));
            }
            catch (FileNotFoundException)
            {
                await Core.Download.DownloadToMinecraft(
                    "https://piston-meta.mojang.com/mc/game/version_manifest.json",
                    BasePath + "version_manifest.json"
                );
                Versions = new VersionsList(File.ReadAllText($"{BasePath}/version_manifest.json"));
            }
        }
    }
}