using OneLauncher.Core.Helper;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core;
[JsonSerializable(typeof(LaunchOption))]
[JsonSerializable(typeof(JvmArguments))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(UserModel))]
[JsonSerializable(typeof(UserVersion))]
[JsonSerializable(typeof(ModType))]
[JsonSerializable(typeof(PreferencesLaunchMode))]
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
    public string? GameInstallPath { get; set; } 
}
public class AppConfig
{
    // 当前启动器已安装的所有版本列表，默认初始化为空列表
    public List<UserVersion> VersionList { get; set; } = new ();
    // 当前启动器有的所有用户登入模型，默认初始化为空列表
    public List<UserModel> UserModelList { get; set; } = new ();
    // 默认用户模型，未指定下默认为null
    public UserModel DefaultUserModel { get; set; } = new UserModel("Default",new Guid(UserModel.nullToken));
    // 除了系统自带的Java以外启动器安装的所有Java版本列表
    public List<int> AvailableJavaList { get; set; } = new ();
    // 用于在主页显示的一键启动选项列表
    public List<LaunchOption> LaunchOptionList { get; set; } = new ();
    public AppSettings OlanSettings { get; set; } = new AppSettings();
}
public class DBManger // 不再实现 IDisposable
{
    public AppConfig config;

    private readonly string _configPath;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private CancellationTokenSource? _delayedSaveCts;
    private const int SaveDelayMilliseconds = 500;

    // 构造函数和 CreateAsync 保持不变
    private DBManger(string configPath, AppConfig initialConfig)
    {
        _configPath = configPath;
        this.config = initialConfig;
    }

    public static async Task<DBManger> CreateAsync(AppConfig first, string basePath)
    {
        var configPath = Path.Combine(basePath, "config.json");
        Directory.CreateDirectory(basePath);
        AppConfig? loadedConfig = await Read(configPath);
        if (loadedConfig == null)
        {
            loadedConfig = new AppConfig();
            PerformSaveSync(configPath, loadedConfig);
        }
        var manager = new DBManger(configPath, loadedConfig);
        return manager;
    }

    // Write, Save, Read 等核心方法保持不变
    public Task Write(AppConfig config)
    {
        this.config = config;
        TriggerDelayedSave();
        return Task.CompletedTask;
    }
    public Task Save() => Write(this.config);

    private void TriggerDelayedSave()
    {
        _delayedSaveCts?.Cancel();
        _delayedSaveCts?.Dispose();
        _delayedSaveCts = new CancellationTokenSource();
        var token = _delayedSaveCts.Token;
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SaveDelayMilliseconds, token);
                await PerformSaveAsync();
            }
            catch (OperationCanceledException) { /* 正常取消 */ }
        });
    }

    private async Task PerformSaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            PerformSaveSync(_configPath, this.config);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private static void PerformSaveSync(string configPath, AppConfig configToSave)
    {
        string tempFilePath = configPath + ".tmp";
        try
        {
            byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(configToSave, OneLauncherAppConfigsJsonContext.Default.AppConfig);
            File.WriteAllBytes(tempFilePath, jsonUtf8Bytes);
            File.Move(tempFilePath, configPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    public static async Task<AppConfig?> Read(string configPath)
    {
        if (!File.Exists(configPath)) return null;
        try
        {
            await using var stream = File.OpenRead(configPath);
            if (stream.Length == 0) return null;
            return await JsonSerializer.DeserializeAsync<AppConfig>(stream, OneLauncherAppConfigsJsonContext.Default.AppConfig);
        }
        catch (Exception) { return null; }
    }

    /// <summary>
    /// **新增：用于在程序关闭前调用的方法。**
    /// 它会取消任何待处理的保存，并立即执行一次最终的原子保存，然后释放内部资源。
    /// </summary>
    public async Task ShutdownAsync()
    {
        _delayedSaveCts?.Cancel(); // 取消任何计划中的异步保存

        await _saveLock.WaitAsync(); // 异步等待锁
        try
        {
            // 调用纯同步的保存方法，快速且不会死锁
            PerformSaveSync(_configPath, this.config);
        }
        finally
        {
            _saveLock.Release();
        }

        // 清理内部资源
        _saveLock.Dispose();
        _delayedSaveCts?.Dispose();
    }

    // public void Dispose() 方法已被完全移除。
}