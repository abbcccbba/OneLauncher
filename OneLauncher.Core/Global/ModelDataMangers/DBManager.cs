using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
using OneLauncher.Core.Helper.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

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
    public DownloadSourceStrategy DownloadMinecraftSourceStrategy { get; set; } = DownloadSourceStrategy.OfficialOnly;
    public string? InstallPath { get; set; }
    //public bool UseTempFileArguments { get; set; } = true;
    public string DefaultInstanceID { get; set; }
    // 每天一更新
    public DateTimeOffset LastVersionManifestRefreshTime { get; set; } = DateTimeOffset.UtcNow;
}
public class AppConfig
{
    // 除了系统自带的Java以外启动器安装的所有Java版本列表
    public Dictionary<int,string?> AvailableJavas { get; set; } = new();
    // 当前启动器已安装的所有版本列表，默认初始化为空列表
    public List<UserVersion> VersionList { get; set; } = new ();
    public AppSettings OlanSettings { get; set; } = new AppSettings();
}
public class DBManager : BasicDataManager<AppConfig>
{
    public AppConfig GetConfig()
        => base.Data;
    public event Action? OnConfigChanged;
    public AppConfig Data 
    { 
        get => base.Data; 
        set
        {
            base.Data = value;
            OnConfigChanged?.Invoke(); // BUG 事件无法正常触发
        }
    }
    public Task EditSettings(AppSettings config)
    {
        base.Data.OlanSettings = config;
        OnConfigChanged?.Invoke();
        return Save();
    }
    public Task AddVersion(UserVersion value)
    {
        base.Data.VersionList.Add(value);
        OnConfigChanged?.Invoke();
        return Save();
    }
    public Task RemoveVersion(UserVersion value)
    {
        base.Data.VersionList.Remove(value);
        OnConfigChanged?.Invoke();
        return Save();
    }
    public Task AddJava(int key,string value)
    {
        base.Data.AvailableJavas.Add(key,value);
        OnConfigChanged?.Invoke();
        return Save();
    }
    public Task RemoveJava(int key)
    {
        base.Data.AvailableJavas.Remove(key);
        OnConfigChanged?.Invoke();
        return Save();
    }

    public DBManager(string configPath)
        :base(configPath)
    {
    }
    protected override JsonSerializerContext GetJsonContext()
        => OneLauncherAppConfigsJsonContext.Default;
}