using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Mod.ModPack.JsonModels;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices.ObjectiveC;

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
        // 创建下载工具的新实例
        using var downloader = new Download();
        using var importer = new ModpackImporter(downloader, gameRoot);
        await importer.RunImportAsync(packPath,token);
    }

    /// <summary>
    /// 执行导入的主流程。
    /// </summary>
    private async Task RunImportAsync(
        string packPath,
        CancellationToken token)
    {
        #region 解压
        tempWorkDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempWorkDir);
        Download.ExtractFile(packPath, tempWorkDir);
        token.ThrowIfCancellationRequested();
        string manifestPath = Path.Combine(tempWorkDir, "modrinth.index.json");
        if (!File.Exists(manifestPath))
            throw new OlanException("整合包无效", "压缩包内未找到 modrinth.index.json 文件。", OlanExceptionAction.Error);

        await using var manifestStream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read,FileShare.None,bufferSize:8129,useAsync:true);
        var parser = new MrpackParser(manifestStream);
        #endregion
        // 安装游戏本体
        string mcVersion = parser.GetMinecraftVersion();
        (ModEnum loaderType, string loaderVersion) = parser.GetLoaderInfo();
        await InstallBaseGameAsync(mcVersion, loaderType, token);

        token.ThrowIfCancellationRequested();

        // 安装Mod
        await InstallModpackFilesAsync(parser,token);
        token.ThrowIfCancellationRequested();
    }

    private async Task InstallBaseGameAsync(
        string mcVersion,
        ModEnum loaderType,
        CancellationToken token)
    {
        // 先判断要安装的版本是否已经安装
        ModType omc = new ModType()
        { 
            IsFabric = loaderType == ModEnum.fabric,
            IsNeoForge = loaderType == ModEnum.neoforge
        };
        bool IsNotAddNewConfig = false;
        for (int i = 0; i < Init.ConfigManger.config.VersionList.Count; i++)
        {
            var temp = Init.ConfigManger.config.VersionList[i];
            // 寻找版本相同的
            if (temp.VersionID != mcVersion)
                continue;
            // 寻找模组加载器相同的
            if (temp.modType == loaderType)
                return;
            // 下面的代码版本相同但加载器不同执行
            omc = temp.modType;
            omc = loaderType switch
            {
                ModEnum.fabric => new ModType
                {
                    IsFabric = true,
                    IsNeoForge = omc.IsNeoForge
                },
                ModEnum.neoforge => new ModType 
                { 
                    IsFabric = omc.IsFabric,
                    IsNeoForge = true
                }
            };
            IsNotAddNewConfig = true;
            Init.ConfigManger.config.VersionList[i].modType = omc;
        }
        
        // 找到需要下载的版本下载信息
        var versionBasicInfo = Init.MojangVersionList.FirstOrDefault(x => x.ID == mcVersion);
        var userVersionForDownloader = new UserVersion
        {
            VersionID = mcVersion, 
            modType = omc,
            AddTime = DateTime.Now
        };

        // 执行下载
        var mcDownloader = new DownloadMinecraft(
            downloadTool,
            userVersionForDownloader,
            versionBasicInfo,
            new GameData("整合包名字",mcVersion,loaderType,Init.ConfigManger.config.DefaultUserModel),
            gameRootPath,
            null,
            token
        );
        await mcDownloader.MinecraftBasic(AndJava: true);
        if (IsNotAddNewConfig)
            return;
        Init.ConfigManger.config.VersionList.Add(userVersionForDownloader);
        await Init.ConfigManger.Save();
    }

  
    private async Task InstallModpackFilesAsync(
        MrpackParser parser,                      
        CancellationToken token)
    {
        string overridesSourceDir = Path.Combine(tempWorkDir, "overrides"); 
        string gameVersion = parser.GetMinecraftVersion();
        string destinationGameDir = Path.Combine(gameRootPath, "versions", gameVersion);

        // 确保 overrides 目录存在才进行复制
        if (Directory.Exists(overridesSourceDir))
        {
            // 将 overrides 目录下的所有内容复制到游戏版本目录
            await Tools.CopyDirectoryAsync(
                overridesSourceDir,
                destinationGameDir, 
                token);
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
                // 记录日志或发出警告，但不要让清理失败抛出异常
                Debug.WriteLine($"清理临时目录失败: {ex.Message}");
            }
        }
        downloadTool?.Dispose(); 
    }
}