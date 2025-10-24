using OneLauncher.Core.Downloader;
using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.Models;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Core.Mod.ModPack;

/// <summary>
/// 负责导入 Modrinth (.mrpack) 整合包的核心类。
/// 它实现了 IDisposable 接口，以确保临时文件能够被正确清理。
/// </summary>
public class ModpackImporter : IDisposable
{
    private readonly string _gameRootPath;
    private readonly GameDataManager _gameDataManager;
    private readonly DBManager _dbManager;
    private string _tempWorkDir; // 临时解压目录

    /// <summary>
    /// 私有构造函数，防止外部直接实例化。
    /// </summary>
    private ModpackImporter(string gameRootPath, GameDataManager gameDataManager, DBManager dbManager)
    {
        _gameRootPath = gameRootPath;
        _gameDataManager = gameDataManager;
        _dbManager = dbManager;
        _tempWorkDir = Path.Combine(Path.GetTempPath(), $"olan-import-{Path.GetRandomFileName()}");
        Directory.CreateDirectory(_tempWorkDir);
    }

    /// <summary>
    /// [静态入口] 从指定的 .mrpack 文件路径开始导入流程。
    /// </summary>
    /// <param name="packPath">.mrpack 文件的完整路径。</param>
    /// <param name="gameRoot">游戏根目录 (.minecraft) 的路径。</param>
    /// <param name="progress">用于报告进度的回调。</param>
    /// <param name="token">用于取消操作的 CancelToken。</param>
    public static async Task ImportFromMrpackAsync(
        string packPath,
        string gameRoot,
        IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress,
        CancellationToken token = default)
    {
        // 依赖注入所需的管理器
        var gameDataManager = Init.GameDataManager;
        var dbManager = Init.ConfigManager;

        // 使用 using 确保下载器和导入器资源被释放
        using var importer = new ModpackImporter(gameRoot, gameDataManager, dbManager);

        await importer.RunImportAsync(packPath, progress, token);
    }

    /// <summary>
    /// 执行整个导入流程的核心方法。
    /// </summary>
    private async Task RunImportAsync(string packPath, IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress, CancellationToken token)
    {
        // --- 步骤 1: 解压与解析清单文件 ---
        progress.Report((DownProgress.Meta, 0, 0, "正在解压整合包..."));
        ZipFile.ExtractToDirectory(packPath, _tempWorkDir);
        token.ThrowIfCancellationRequested();

        string manifestPath = Path.Combine(_tempWorkDir, "modrinth.index.json");
        if (!File.Exists(manifestPath))
            throw new OlanException("整合包无效", "压缩包内未找到 modrinth.index.json 文件。", OlanExceptionAction.Error);

        await using var manifestStream = File.OpenRead(manifestPath);
        var parser = new MrpackParser(manifestStream);

        // --- 步骤 2: 创建游戏实例 ---
        progress.Report((DownProgress.Meta, 0, 0, "正在创建游戏实例..."));
        (ModEnum loaderType, string loaderVersion) = parser.GetLoaderInfo();
        var packGameData = new GameData(
            name: parser.GetName(),
            versionId: parser.GetMinecraftVersion(),
            loader: loaderType,
            userModel: Init.AccountManager.GetDefaultUser().UserID // 默认为当前用户
        );
        Directory.CreateDirectory(packGameData.InstancePath);

        // --- 步骤 3: 安装基础游戏和Mod加载器 ---
        await InstallBaseGameAsync(packGameData, progress, token);
        token.ThrowIfCancellationRequested();

        // --- 步骤 4: 安装整合包特定文件 (Mods 和 Overrides) ---
        await InstallModpackFilesAsync(parser, packGameData, progress, token);
        token.ThrowIfCancellationRequested();

        // --- 步骤 5: 注册新的游戏实例并保存 ---
        await _gameDataManager.AddGameDataAsync(packGameData);
        progress.Report((DownProgress.Done, 1, 1, "整合包导入成功！"));
    }

    /// <summary>
    /// 负责下载和安装 Minecraft 基础版和指定的 Mod 加载器。
    /// </summary>
    private async Task InstallBaseGameAsync(GameData gameData, IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress, CancellationToken token)
    {
        var downInfo = await DownloadInfo.Create(
            versionId: gameData.VersionId,
            modType: new ModType { IsFabric = gameData.ModLoader == ModEnum.fabric, IsNeoForge = gameData.ModLoader == ModEnum.neoforge, IsForge = gameData.ModLoader == ModEnum.forge, IsQuilt = gameData.ModLoader == ModEnum.quilt },
            gameDataD: gameData
        );

        var mcDownloader = new DownloadMinecraft(
            downInfo,
            progress,
            token
        );

        // 调用核心下载方法
        await mcDownloader.MinecraftBasic(
        );
    }

    /// <summary>
    /// 负责下载所有 Mod 文件并应用覆盖文件 (overrides)。
    /// </summary>
    private async Task InstallModpackFilesAsync(MrpackParser parser, GameData gameData, IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress, CancellationToken token)
    {
        // --- 下载 Mods ---
        string modsDir = Path.Combine(gameData.InstancePath, "mods");
        Directory.CreateDirectory(modsDir);
        var filesToDownload = parser.GetModFiles(modsDir);

        if (filesToDownload.Any())
        {
            int totalMods = filesToDownload.Count;
            int downloadedMods = 0;
            var modProgress = new Progress<(int, string)>(p =>
            {
                downloadedMods++;
                progress.Report((DownProgress.DownAndInstModFiles, totalMods, downloadedMods, p.Item2));
            });

            await Init.Download.DownloadListAsync(modProgress, filesToDownload, _dbManager.GetConfig().OlanSettings.MaximumDownloadThreads, token);
        }

        // --- 应用 Overrides ---
        progress.Report((DownProgress.Meta, 0, 0, "正在应用覆盖文件..."));
        string overridesSourceDir = Path.Combine(_tempWorkDir, "overrides");
        if (Directory.Exists(overridesSourceDir))
        {
            // 使用你已经编写的健壮的目录复制方法
            await Tools.CopyDirectoryAsync(overridesSourceDir, gameData.InstancePath, token);
        }
    }

    /// <summary>
    /// 清理临时文件和目录。
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (!string.IsNullOrEmpty(_tempWorkDir) && Directory.Exists(_tempWorkDir))
            {
                Directory.Delete(_tempWorkDir, true);
            }
        }
        catch (IOException ex)
        {
            // 记录日志，但不抛出异常，以免影响程序关闭
            Debug.WriteLine($"[ModpackImporter] 清理临时目录 '{_tempWorkDir}' 失败: {ex.Message}");
        }
    }
}