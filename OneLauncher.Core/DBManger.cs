using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core;
[JsonSerializable(typeof(OneLauncher.Core.JvmArguments))]
[JsonSerializable(typeof(OneLauncher.Core.AppSettings))]
[JsonSerializable(typeof(OneLauncher.Core.AppConfig))]
[JsonSerializable(typeof(OneLauncher.Core.UserModel))]
[JsonSerializable(typeof(OneLauncher.Core.UserVersion))]
[JsonSerializable(typeof(OneLauncher.Core.ModType))]
[JsonSerializable(typeof(OneLauncher.Core.PreferencesLaunchMode))]
[JsonSerializable(typeof(OneLauncher.Core.ModEnum))]
public partial class OneLauncherAppConfigsJsonContext : JsonSerializerContext { }
public class AppSettings
{
    public JvmArguments MinecraftJvmArguments { get; set; }
    // 下载
    public int MaximumDownloadThreads { get; set; } = 24;
    public int MaximumSha1Threads { get; set; } = 24;
    public bool IsSha1Enabled { get; set; } = true;
}
public class AppConfig
{
    // 当前启动器已安装的所有版本列表，默认初始化为空列表
    public List<UserVersion> VersionList { get; set; } = new List<UserVersion>();
    // 当前启动器有的所有用户登入模型，默认初始化为空列表
    public List<UserModel> UserModelList { get; set; } = new List<UserModel>();
    // 默认用户模型，未指定下默认为 Zhi Wei
    public UserModel DefaultUserModel { get; set; } = new UserModel();
    // 默认版本（固定到仪表盘）
    public UserVersion DefaultVersion { get; set; }
    // 除了系统自带的Java以外启动器安装的所有Java版本列表
    public List<int> AvailableJavaList { get; set; } = new List<int>();
    public AppSettings OlanSettings { get; set; } = new AppSettings();
}

public class DBManger
{
    public AppConfig config;
    private readonly string ConfigFilePath;
    private readonly string BasePath;
    public DBManger(AppConfig FirstConfig, string BasePath)
    {
        this.BasePath = BasePath;
        ConfigFilePath = Path.Combine(BasePath, "config.json");
        if (File.Exists(ConfigFilePath))
        {
            Read(FirstConfig);
        }
        else
        {
            Write(FirstConfig).Wait();
        }
    }

    public Task Write(AppConfig config)
    {
        this.config = config;
        Directory.CreateDirectory(BasePath);
        return File.WriteAllTextAsync(ConfigFilePath, JsonSerializer.Serialize(config,OneLauncherAppConfigsJsonContext.Default.AppConfig));
    }
    public Task Save() => Write(this.config);
    public AppConfig Read(AppConfig Bk)
    {
        string jsonString = File.ReadAllText(ConfigFilePath);
        if (string.IsNullOrEmpty(jsonString))
        {
            Write(Bk);
            return config;
        }
        AppConfig readConfig = JsonSerializer.Deserialize<AppConfig>(jsonString, OneLauncherAppConfigsJsonContext.Default.AppConfig);
        this.config = readConfig;
        return readConfig;
    }
}
