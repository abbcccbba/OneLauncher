using OneLauncher.Core.Downloader;
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
        using var downloader = new Download();
        using var importer = new ModpackImporter(downloader, gameRoot);
        await importer.RunImportAsync(packPath, token);
    }

    private async Task RunImportAsync(string packPath, CancellationToken token)
    {
        // 解压与解析
        tempWorkDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempWorkDir);
        Download.ExtractFile(packPath, tempWorkDir); 
        token.ThrowIfCancellationRequested();

        string manifestPath = Path.Combine(tempWorkDir, "modrinth.index.json");
        if (!File.Exists(manifestPath))
            throw new OlanException("整合包无效", "压缩包内未找到 modrinth.index.json 文件。", OlanExceptionAction.Error); //

        await using var manifestStream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read);
        var parser = new MrpackParser(manifestStream); 

        // 获取整合包信息
        string mcVersion = parser.GetMinecraftVersion(); 
        (ModEnum loaderType, string loaderVersion) = parser.GetLoaderInfo(); 

        var packGameData = new GameData( 
            parser.GetName(), 
            mcVersion,
            loaderType,
            null // 默认
        );

        Directory.CreateDirectory(packGameData.InstancePath);

        await InstallBaseGameAsync(packGameData, token);
        token.ThrowIfCancellationRequested();

        await InstallModpackFilesAsync(parser, packGameData, token);
        token.ThrowIfCancellationRequested();

        await Init.GameDataManger.AddGameDataAsync(packGameData); 
    }

    private async Task InstallBaseGameAsync(GameData gameData, CancellationToken token)
    {
        var loaderType = gameData.ModLoader; 
        var mcVersion = gameData.VersionId; 

        var existingVersion = Init.ConfigManger.config.VersionList
            .FirstOrDefault(v => v.VersionID == mcVersion);

        ModType finalModType;
        if (existingVersion != null)
        {
            finalModType = existingVersion.modType;
            if (loaderType == ModEnum.fabric && !finalModType.IsFabric) finalModType.IsFabric = true;
            if (loaderType == ModEnum.neoforge && !finalModType.IsNeoForge) finalModType.IsNeoForge = true;
            existingVersion.modType = finalModType;
        }
        else
        {
            finalModType = new ModType 
            {
                IsFabric = loaderType == ModEnum.fabric,
                IsNeoForge = loaderType == ModEnum.neoforge,
            };
        }

        var versionBasicInfo = Init.MojangVersionList.FirstOrDefault(x => x.ID == mcVersion);
        if (versionBasicInfo == null)
            throw new OlanException("整合包错误", $"未知的 Minecraft 版本: {mcVersion}", OlanExceptionAction.Error); //

        var userVersionForDownloader = new UserVersion 
        {
            VersionID = mcVersion,
            modType = finalModType,
            AddTime = DateTime.Now,
            preferencesLaunchMode = new PreferencesLaunchMode { LaunchModType = loaderType }
        };

        var mcDownloader = new DownloadMinecraft( 
            downloadTool,
            userVersionForDownloader,
            versionBasicInfo,
            gameData,
            gameRootPath,
            null,
            token
        );

        await mcDownloader.MinecraftBasic( 
            maxDownloadThreads: Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads, 
            maxSha1Threads: Init.ConfigManger.config.OlanSettings.MaximumSha1Threads, 
            IsSha1: Init.ConfigManger.config.OlanSettings.IsSha1Enabled, 
            AndJava: true,
            IsDownloadFabricWithAPI: false
        );

        if (existingVersion == null)
        {
            Init.ConfigManger.config.VersionList.Add(userVersionForDownloader);
        }
        await Init.ConfigManger.Save(); 
    }

    private async Task InstallModpackFilesAsync(MrpackParser parser, GameData gameData, CancellationToken token)
    {
        string modsDir = Path.Combine(gameData.InstancePath, "mods"); 
        Directory.CreateDirectory(modsDir);
        var filesToDownload = parser.GetLibraries(gameData.Name, modsDir); 

        if (filesToDownload.Any())
        {
            await downloadTool.DownloadListAsync(null, filesToDownload, Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads, token); //
        }

        string overridesSourceDir = Path.Combine(tempWorkDir, "overrides");
        if (Directory.Exists(overridesSourceDir))
        {
            await Tools.CopyDirectoryAsync(overridesSourceDir, gameData.InstancePath, token); //
        }
    }

    public void Dispose()
    {
        if (!string.IsNullOrEmpty(tempWorkDir) && Directory.Exists(tempWorkDir))
        {
            try
            {
                Directory.Delete(tempWorkDir, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理整合包临时目录 '{tempWorkDir}' 失败: {ex.Message}");
            }
        }
        downloadTool?.Dispose();
    }
}