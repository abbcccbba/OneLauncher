using OneLauncher.Core.Helper;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core;
[JsonSerializable(typeof(JvmArguments))]
[JsonSerializable(typeof(OneLauncher.Core.AppSettings))]
[JsonSerializable(typeof(OneLauncher.Core.AppConfig))]
[JsonSerializable(typeof(UserModel))]
[JsonSerializable(typeof(UserVersion))]
[JsonSerializable(typeof(ModType))]
[JsonSerializable(typeof(PreferencesLaunchMode))]
[JsonSerializable(typeof(ModEnum))]
public partial class OneLauncherAppConfigsJsonContext : JsonSerializerContext { }
public class AppSettings
{
    public JvmArguments MinecraftJvmArguments { get; set; }
    // 下载
    public int MaximumDownloadThreads { get; set; } = 24;
    public int MaximumSha1Threads { get; set; } = 24;
    public bool IsSha1Enabled { get; set; } = true;
    public bool IsAllowToDownloadUseBMLCAPI { get; set; } = false;
    public string? GameInstallPath { get; set; } 
}
public class AppConfig
{
    // 当前启动器已安装的所有版本列表，默认初始化为空列表
    public List<UserVersion> VersionList { get; set; } = new List<UserVersion>();
    // 当前启动器有的所有用户登入模型，默认初始化为空列表
    public List<UserModel> UserModelList { get; set; } = new List<UserModel>();
    // 默认用户模型，未指定下默认为null
    public UserModel DefaultUserModel { get; set; } = null;
    // 默认版本（固定到仪表盘）
    public UserVersion DefaultVersion { get; set; }
    // 除了系统自带的Java以外启动器安装的所有Java版本列表
    public List<int> AvailableJavaList { get; set; } = new List<int>();
    [JsonInclude]
    public AppSettings OlanSettings { get; set; } = new AppSettings();
}

public class DBManger : IDisposable
{
    public AppConfig config;
    private readonly FileStream configStream;
    private DBManger(string configBasePath)
    {
        configStream = File.Open(configBasePath,FileMode.OpenOrCreate,FileAccess.ReadWrite);
    }
    public static async Task<DBManger> CreateAsync(AppConfig first, string basePath)
    {
        var configBasePath = Path.Combine(basePath, "config.json");
        var r = new DBManger(configBasePath);
        r.config = first;
        Directory.CreateDirectory(basePath);
        if (File.Exists(configBasePath))
            await r.Read();
        else
            await r.Write(first);
        return r;
    }
    public Task Write(AppConfig config)
    {
        this.config = config;
        configStream.Seek(0, SeekOrigin.Begin);
        return JsonSerializer.SerializeAsync(configStream,config,OneLauncherAppConfigsJsonContext.Default.AppConfig);
    }
    public Task Save() => Write(this.config);
    public async Task<AppConfig> Read()
    {
        configStream.Seek(0, SeekOrigin.Begin);
        AppConfig? r = await JsonSerializer.DeserializeAsync<AppConfig>(configStream, OneLauncherAppConfigsJsonContext.Default.AppConfig);
        if(r == null) 
            await Write(config);
        else
            config = r;
        return config;
    }

    public void Dispose()
        => configStream.Dispose();
}
