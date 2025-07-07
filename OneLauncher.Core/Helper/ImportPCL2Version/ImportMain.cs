using OneLauncher.Core.Compatible.ImportPCL2Version;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper.Models;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.ImportPCL2Version;

/// <summary>
/// 负责处理从 PCL2 实例文件夹导入游戏到 OneLauncher 的全部逻辑。
/// </summary>
public class PCL2Importer
{
    private readonly IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> _progress;
    private readonly CancellationToken _token;
    private readonly Download _downloader = Init.Download;

    // 采用您偏好的"伪依赖注入"模式
    private readonly GameDataManager _gameDataManager = Init.GameDataManager;
    private readonly DBManager _configManager = Init.ConfigManager;
    private readonly AccountManager _accountManager = Init.AccountManager;

    public PCL2Importer(IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress, CancellationToken? token = null)
    {
        _progress = progress;
        _token = token ?? CancellationToken.None;
    }

    /// <summary>
    /// [入口方法] 开始从 PCL2 实例文件夹进行导入。
    /// </summary>
    /// <param name="pclInstancePath">PCL2 的单个版本实例文件夹路径 (例如 E:\mc\.minecraft\versions\1.21.1-Fabric 0.16.14)。</param>
    public async Task ImportAsync(string pclInstancePath)
    {
        _progress?.Report((DownProgress.Meta, 0, 0, "分析PCL2实例..."));

        // 1. 解析PCL2的 version.json 获取基础信息
        string pclVersionJsonPath = FindPclVersionJson(pclInstancePath);
        using var fs = File.OpenRead(pclVersionJsonPath);
        var pclInfo = await JsonSerializer.DeserializeAsync(fs, PCL2VersionJsonContent.Default.PCL2VersionJsonModels, _token);

        string mcVersion = pclInfo.ClientVersionID;
        string customName = pclInfo.UserCustomName;
        ModEnum modLoader = Tools.MainClassToModEnum(pclInfo.MainClass);

        _progress?.Report((DownProgress.Meta, 0, 0, $"已识别: {customName} (MC: {mcVersion}, 加载器: {modLoader})"));

        // 2. 创建OneLauncher游戏实例
        var gameData = new GameData(customName, mcVersion, modLoader, _accountManager.GetDefaultUser().UserID);
        Directory.CreateDirectory(gameData.InstancePath);

        // 3. [!code focus-start]
        //  *** FIX: 明确获取父级的父级目录，确保拿到 .minecraft 文件夹 ***
        var versionsDir = new DirectoryInfo(pclInstancePath).Parent;
        string pclMinecraftRoot = versionsDir?.Parent?.FullName
                                  ?? throw new OlanException("导入失败", "无法确定PCL2的 .minecraft 根目录。");
        // [!code focus-end]

        // 4. 关键改动：首先迁移所有用户数据和Mod文件
        _progress?.Report((DownProgress.Meta, 0, 0, "正在迁移库、资源和用户数据..."));
        await MigratePclInstanceContentAsync(pclInstancePath, gameData.InstancePath);
        await MigratePclRootFoldersAsync(pclMinecraftRoot, Init.GameRootPath);


        // 5. 然后，调用您自己的下载和安装流程
        _progress?.Report((DownProgress.Meta, 0, 0, "正在验证和安装核心文件..."));
        var downloadInfo = await DownloadInfo.Create(
            gameData.VersionId,
            new ModType { IsFabric = gameData.ModLoader == ModEnum.fabric, IsNeoForge = gameData.ModLoader == ModEnum.neoforge, IsForge = gameData.ModLoader == ModEnum.forge, IsQuilt = gameData.ModLoader == ModEnum.quilt },
            _downloader, gameDataD: gameData);

        var finalInstaller = new DownloadMinecraft(_configManager, downloadInfo, _progress, _token);

        // 您的下载器将自动处理文件检查和后续安装步骤
        await finalInstaller.MinecraftBasic(
            maxDownloadThreads: _configManager.Data.OlanSettings.MaximumDownloadThreads,
            maxSha1Threads: _configManager.Data.OlanSettings.MaximumSha1Threads,
            IsSha1: false, // 不需要校验了
            useBMLCAPI: _configManager.Data.OlanSettings.IsAllowToDownloadUseBMLCAPI);


        // 6. 将实例添加到管理器并保存
        await _gameDataManager.AddGameDataAsync(gameData);
        _progress?.Report((DownProgress.Done, 1, 1, "导入成功！"));
    }

    /// <summary>
    /// 将PCL2实例文件夹内的所有内容（mods, saves, etc.）复制到OneLauncher的实例目录。
    /// </summary>
    private async Task MigratePclInstanceContentAsync(string sourceInstancePath, string destinationInstancePath)
    {
        await Tools.CopyDirectoryAsync(sourceInstancePath, destinationInstancePath, _token);
    }

    /// <summary>
    /// 将 PCL2 的 libraries 和 assets 目录整体迁移到 OneLauncher 的根目录，利用文件系统的能力跳过已存在的文件。
    /// </summary>
    private async Task MigratePclRootFoldersAsync(string pclRoot, string olanRoot)
    {
        string pclLibPath = Path.Combine(pclRoot, "libraries");
        string olanLibPath = Path.Combine(olanRoot, "libraries");
        if (Directory.Exists(pclLibPath))
        {
            await Tools.CopyDirectoryAsync(pclLibPath, olanLibPath, _token);
        }

        string pclAssetsPath = Path.Combine(pclRoot, "assets");
        string olanAssetsPath = Path.Combine(olanRoot, "assets");
        if (Directory.Exists(pclAssetsPath))
        {
            await Tools.CopyDirectoryAsync(pclAssetsPath, olanAssetsPath, _token);
        }
    }


    /// <summary>
    /// 在PCL2实例目录中找到权威的 version.json 文件。
    /// </summary>
    private string FindPclVersionJson(string versionPath)
    {
        string expectedJsonName = $"{Path.GetFileName(versionPath)}.json";
        string primaryJsonPath = Path.Combine(versionPath, expectedJsonName);

        if (File.Exists(primaryJsonPath)) return primaryJsonPath;

        var anyJson = Directory.EnumerateFiles(versionPath, "*.json").FirstOrDefault();
        if (anyJson != null) return anyJson;

        throw new OlanException("导入失败", $"在 '{versionPath}' 中未找到任何 .json 配置文件。");
    }
}