using OneLauncher.Core.Helper.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Global.ModelDataMangers;
[JsonSerializable(typeof(JvmArguments))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(UserModel))]
[JsonSerializable(typeof(UserVersion))]
[JsonSerializable(typeof(ModType))]
[JsonSerializable(typeof(ModEnum))]
public partial class OneLauncherAppConfigsJsonContext : JsonSerializerContext { }
public class AppSettings
{
    public JvmArguments MinecraftJvmArguments { get; set; } = JvmArguments.CreateFromMode();
    // 下载
    public int MaximumDownloadThreads { get; set; } = 24;
    public int MaximumSha1Threads { get; set; } = 24;
    public bool IsSha1Enabled { get; set; } = true;
    public bool IsAllowToDownloadUseBMLCAPI { get; set; } = false;
    public string? InstallPath { get; set; } 
    //public bool UseTempFileArguments { get; set; } = true;
}
public class AppConfig
{
    // 当前启动器已安装的所有版本列表，默认初始化为空列表
    public List<UserVersion> VersionList { get; set; } = new ();
    public string DefaultInstanceID { get; set; }
    // 除了系统自带的Java以外启动器安装的所有Java版本列表
    public List<int> AvailableJavaList { get; set; } = new ();
    public AppSettings OlanSettings { get; set; } = new AppSettings();
}
public class DBManager : BasicDataManager<AppConfig>
{
    public DBManager(string configPath)
        :base(configPath)
    {
    }

    protected override JsonSerializerContext GetJsonContext()
        => OneLauncherAppConfigsJsonContext.Default;
}