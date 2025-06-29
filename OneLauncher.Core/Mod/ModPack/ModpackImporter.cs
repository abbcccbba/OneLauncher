using OneLauncher.Core.Downloader;
using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Core.Mod.ModPack;
public class ModpackImporter : IDisposable
{
    private readonly Download downloadTool;
    private readonly string gameRootPath;
    private string tempWorkDir;

    private ModpackImporter(Download download, string gameRootPath)
    {
        this.downloadTool = download;
        this.gameRootPath = gameRootPath;
    }

    public static async Task ImportFromMrpackAsync(
        string packPath,
        string gameRoot,
        CancellationToken token = default)
    {
        //using var downloader = new Download();
        //using var importer = new ModpackImporter(downloader, gameRoot);
        //await importer.RunImportAsync(packPath, token);
    }

    //private async Task RunImportAsync(string packPath, CancellationToken token)
    //{
    //    // 解压与解析
    //    tempWorkDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    //    Directory.CreateDirectory(tempWorkDir);
    //    Download.ExtractFile(packPath, tempWorkDir);
    //    token.ThrowIfCancellationRequested();

    //    string manifestPath = Path.Combine(tempWorkDir, "modrinth.index.json");
    //    if (!File.Exists(manifestPath))
    //        throw new OlanException("整合包无效", "压缩包内未找到 modrinth.index.json 文件。", OlanExceptionAction.Error); //

    //    await using var manifestStream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read);
    //    var parser = new MrpackParser(manifestStream);

    //    // 获取整合包信息
    //    string mcVersion = parser.GetMinecraftVersion();
    //    (ModEnum loaderType, string loaderVersion) = parser.GetLoaderInfo();

    //    var packGameData = new GameData(
    //        parser.GetName(),
    //        mcVersion,
    //        loaderType,
    //        null // 默认
    //    );

    //    Directory.CreateDirectory(packGameData.InstancePath);

    //    await InstallBaseGameAsync(packGameData, token);
    //    token.ThrowIfCancellationRequested();

    //    await InstallModpackFilesAsync(parser, packGameData, token);
    //    token.ThrowIfCancellationRequested();

    //    await Init.GameDataManger.AddGameDataAsync(packGameData);
    //}

    //private async Task InstallBaseGameAsync(GameData gameData, CancellationToken token)
    //{
    //    using var downTool = new Download();
    //    var loaderType = gameData.ModLoader;
    //    var mcVersion = gameData.VersionId;

    //    var downInfo = await DownloadInfo.Create(
    //            mcVersion, new ModType()
    //            {
    //                IsFabric = loaderType == ModEnum.fabric,
    //                IsNeoForge = loaderType == ModEnum.neoforge,
    //                IsForge = loaderType == ModEnum.forge
    //            }, downTool, false, true, true, true, null, gameData
    //        );

    //    var mcDownloader = new DownloadMinecraft(
    //        downInfo,
    //        null,
    //        token
    //    );

    //    await mcDownloader.MinecraftBasic(
    //        maxDownloadThreads: Init.ConfigManager.config.OlanSettings.MaximumDownloadThreads,
    //        maxSha1Threads: Init.ConfigManager.config.OlanSettings.MaximumSha1Threads,
    //        IsSha1: Init.ConfigManager.config.OlanSettings.IsSha1Enabled,
    //        useBMLCAPI: Init.ConfigManager.config.OlanSettings.IsAllowToDownloadUseBMLCAPI
    //    );
    //    await Init.ConfigManager.Save();
    //}

    //private async Task InstallModpackFilesAsync(MrpackParser parser, GameData gameData, CancellationToken token)
    //{
    //    string modsDir = Path.Combine(gameData.InstancePath, "mods");
    //    Directory.CreateDirectory(modsDir);
    //    var filesToDownload = parser.GetLibraries(gameData.Name, modsDir);

    //    if (filesToDownload.Any())
    //    {
    //        await downloadTool.DownloadListAsync(null, filesToDownload, Init.ConfigManager.config.OlanSettings.MaximumDownloadThreads, token); //
    //    }

    //    string overridesSourceDir = Path.Combine(tempWorkDir, "overrides");
    //    if (Directory.Exists(overridesSourceDir))
    //    {
    //        await Tools.CopyDirectoryAsync(overridesSourceDir, gameData.InstancePath, token); //
    //    }
    //}

    public void Dispose()
    {
        //if (!string.IsNullOrEmpty(tempWorkDir) && Directory.Exists(tempWorkDir))
        //{
        //    try
        //    {
        //        Directory.Delete(tempWorkDir, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"清理整合包临时目录 '{tempWorkDir}' 失败: {ex.Message}");
        //    }
        //}
        //downloadTool?.Dispose();
    }
}