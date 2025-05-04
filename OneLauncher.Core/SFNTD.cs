using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core
{
    public struct SFNTD
    {
        public SFNTD(string Url, string Sha1, string Path)
        {
            this.url = Url;
            this.sha1 = Sha1;
            this.path = Path;
        }
        public string url;
        public string sha1;
        public string path;
    }
    public struct aVersion
    {
        public VersionBasicInfo versionBasicInfo { get; set; }
        public string name { get; set; }
    }
    public struct VersionBasicInfo
    {
        public VersionBasicInfo(string name,string type,string url,string time)
        {
            this.name = name;
            this.type = type;
            this.time = time;
            this.url = url;
            this.DisInfo = $"""发布于{DateTimeOffset.Parse(time).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy年M月")} {((type == "release") ? "正式版" : "快照版")} """;
        }
        public string DisInfo { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string time { get; set; }
        public string url { get; set; }
    }
    public struct UserModel
    {
        public UserModel(string Name, string uuid, string accessToken)
        {
            this.Name = Name;
            this.uuid = uuid;
            this.accessToken = accessToken;
        }
        public string Name { get; set; }
        public string uuid { get; set; }
        public string accessToken { get; set; }
    }
    public struct StartArguments
    {
          
        public StartArguments(string Version,string VersionType,string BasePath,UserModel userModel)
        {
            // 确认系统信息
            string osName;
            string osVersion = Environment.OSVersion.Version.ToString(2); 
            if (OperatingSystem.IsWindows())
                osName = "Windows 10";
            else if (OperatingSystem.IsMacOS())
                osName = "Mac OS X"; 
            else if (OperatingSystem.IsLinux())
                osName = "Linux";
            else
            {
                osName = "Unknown";
                osVersion = "0.0";
            }

            // 格式化为 Minecraft 启动参数
            SystemInfo = $"-Dos.name=\"{osName}\" -Dos.version={osVersion}";
            this.D_Version = Version;
            this.D_Version_Type = VersionType;
            this.path = BasePath;
            this.userModel = userModel;
        }
        public static string GetArguments(StartArguments startInfo)
        {
            VersionInfomations a = new VersionInfomations(File.ReadAllText($"{startInfo.path}.minecraft/versions/{startInfo.D_Version}/{startInfo.D_Version}.json"));
            string cp = "";
            foreach (var i in a.GetLibrarys(startInfo.path))
            {
                // Windows 系统改为分号，其它系统改为冒号
//#if WINDOWS
                cp += i.path + ";";
//#elif MACOS || LINUX
//                cp += i.path + ":";
//#endif
            }
            return "" +
                // JVM 参数
                " -XX:+UseG1GC " +
                " -XX:-UseAdaptiveSizePolicy " +
                " -XX:-OmitStackTraceInFastThrow " +
                // MacOS请加上下面参数
#if MACOS
                " -XstartOnFirstThread "+
#else
#endif
                $" {startInfo.SystemInfo} " +
                $" -Dminecraft.launcher.brand=one-launcher -Dminecraft.launcher.version=1.0 " +
                // MacOS或者其它系统记得把这里改成冒号
//#if WINDOWS
                $" -cp \"{a.GetMainFile(startInfo.path, startInfo.D_Version).path};{cp}\" " +
//#elif MACOS || LINUX 
//                $" -cp \"{a.GetMainFile(startInfo.path, startInfo.D_Version).path}:{cp}\" " +
//#endif
                " net.minecraft.client.main.Main " +
                // 游戏参数
                $" --version {startInfo.D_Version} " +
                $" --gameDir \"{startInfo.path}.minecraft\" " +
                $" --assetsDir \"{startInfo.path}.minecraft/assets\" " +
                $" --assetIndex {a.GetAssetIndexVersion()} " +
                $" --username \"{startInfo.userModel.Name}\" " +
                $" --uuid \"{startInfo.userModel.uuid}\" " +
                $" --accessToken \"{startInfo.userModel.accessToken}\" " +
                $" --userType mojang " +
                $" --versionType {startInfo.D_Version_Type} ";
        }
        
        
        string SystemInfo;
        string D_Version;
        string D_Version_Type;
        string path;
        UserModel userModel;
    }
}
