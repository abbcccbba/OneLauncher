//using OneLauncher.Core.Compatible.ImportPCL2Version;
//using OneLauncher.Core.Downloader;
//using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
//using OneLauncher.Core.Global;
//using OneLauncher.Core.Helper;
//using OneLauncher.Core.Minecraft;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace OneLauncher.Core.Helper.ImportPCL2Version;

///// <summary>
///// 负责处理从 PCL2 实例文件夹导入游戏到 OneLauncher 的全部逻辑。
///// </summary>
//public class PCL2Importer
//{
//    public CancellationToken token;
//    public IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> process;
//    public PCL2Importer(IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> process, CancellationToken? token = null)
//    {
//        this.token = token ?? CancellationToken.None;
//        this.process = process;
//    }

//    //public async Task ImportAsync(string pclVersionPath)
//    //{
//    //    string versionJsonPath = FindAuthoritativeJsonFile(pclVersionPath);
//    //    var info = await JsonSerializer.DeserializeAsync(File.OpenRead(versionJsonPath),PCL2VersionJsonContent.Default.PCL2VersionJsonModels);
//    //    ModEnum modLoaderType = Tools.MainClassToModEnum(info.MainClass);
//    //    GameData gameData = new GameData(info.UserCustomName,info.ClientVersionID,modLoaderType,null);
//    //    await Init.GameDataManger.AddGameDataAsync(gameData);
//    //    await InstallMinecraft(gameData,info.ClientVersionID,modLoaderType);
//    //    await MigrateUserDataAsync(pclVersionPath,gameData.InstancePath);
//    //}
//    public async Task ImportAsync(string pclVersionPath)
//    {
//        // 1. 识别路径和基本信息
//        string pclMinecraftRoot = Directory.GetParent(pclVersionPath)?.Parent?.FullName
//                                  ?? throw new OlanException("导入失败","无法确定 PCL2 的 .minecraft 根目录。");

        
//        string instanceName = Path.GetFileName(pclVersionPath);

//        // 2. 创建 OneLauncher 实例
//        var gameData = new GameData(instanceName, mcVersion, modLoader, null);

//        // 3. 调用重构后的 InstallMinecraft 方法，执行核心的文件同步和安装流程
//        await InstallMinecraft(gameData, pclMinecraftRoot);

//        // 4. 迁移用户数据
//        ProcessReporter?.Report((DownProgress.Done, _totalFilesToProcess, _totalFilesToProcess, "正在迁移用户数据..."));
//        await MigrateUserDataAsync(pclVersionPath, gameData.InstancePath);

//        await Init.GameDataManger.AddGameDataAsync(gameData);

//        ProcessReporter?.Report((DownProgress.Done, _totalFilesToProcess, _totalFilesToProcess, "导入完成！"));
//    }
//    #region 辅助方法
//    private async Task InstallMinecraft(GameData gameData, string pclMinecraftRoot)
//    {
//        ProcessReporter?.Report((DownProgress.Meta, 0, 0, "正在生成标准文件清单..."));

//        // 创建一个临时的 DownloadInfo 来生成标准文件清单
//        var downloadInfo = await DownloadInfo.Create(
//            gameData.VersionId,
//            new ModType { IsFabric = gameData.ModLoader == ModEnum.fabric, IsNeoForge = gameData.ModLoader == ModEnum.neoforge, IsForge = gameData.ModLoader == ModEnum.forge },
//            _downloader,
//            gameDataD: gameData
//        );

//        var downloadPlanner = new DownloadMinecraft(downloadInfo, null, Token);

//        // 手动模拟 CreateDownloadPlan 的逻辑来获取完整文件列表
//        var versionInfo = downloadInfo.VersionMojangInfo;
//        var libraries = versionInfo.GetLibraries();
//        var assetsIndexItem = versionInfo.GetAssets();

//        string assetsIndexPathInOlan = assetsIndexItem.path;
//        if (!File.Exists(assetsIndexPathInOlan))
//        {
//            string assetsIndexPathInPcl = Path.Combine(pclMinecraftRoot, "assets", "indexes", $"{assetsIndexItem.Id}.json");
//            if (File.Exists(assetsIndexPathInPcl))
//            {
//                Directory.CreateDirectory(Path.GetDirectoryName(assetsIndexPathInOlan)!);
//                File.Copy(assetsIndexPathInPcl, assetsIndexPathInOlan, true);
//            }
//            else
//            {
//                await _downloader.DownloadFile(assetsIndexItem.Url, assetsIndexPathInOlan, Token);
//            }
//        }
//        var assets = VersionAssetIndex.ParseAssetsIndex(await File.ReadAllTextAsync(assetsIndexPathInOlan, Token), Init.GameRootPath);

//        var allRequiredFiles = new List<NdDowItem>();
//        allRequiredFiles.AddRange(libraries);
//        allRequiredFiles.AddRange(assets);
//        allRequiredFiles.Add(versionInfo.GetMainFile());
//        if (versionInfo.GetLoggingConfig() is NdDowItem loggingConfig) allRequiredFiles.Add(loggingConfig);

//        _totalFilesToProcess = allRequiredFiles.Count;
//        var filesToDownload = new ConcurrentBag<NdDowItem>();

//        // 并行处理文件：优先从 PCL2 复制
//        ProcessReporter?.Report((DownProgress.Meta, _totalFilesToProcess, 0, "正在比对 PCL2 本地文件..."));
//        await Parallel.ForEachAsync(allRequiredFiles, new ParallelOptions { MaxDegreeOfParallelism = 16, CancellationToken = Token }, async (requiredFile, cancellationToken) =>
//        {
//            string relativePath = Path.GetRelativePath(Init.GameRootPath, requiredFile.path);
//            string pclSourcePath = Path.Combine(pclMinecraftRoot, relativePath);

//            if (!File.Exists(pclSourcePath) || (requiredFile.sha1 != null && !await IsFileValidAsync(pclSourcePath, requiredFile.sha1)))
//            {
//                filesToDownload.Add(requiredFile);
//            }
//            else
//            {
//                Directory.CreateDirectory(Path.GetDirectoryName(requiredFile.path)!);
//                File.Copy(pclSourcePath, requiredFile.path, true);
//            }

//            int currentCount = Interlocked.Increment(ref _processedFilesCount);
//            ProcessReporter?.Report((DownProgress.Meta, _totalFilesToProcess, currentCount, Path.GetFileName(requiredFile.path)));
//        });

//        // 下载所有缺失或损坏的文件
//        if (!filesToDownload.IsEmpty)
//        {
//            var downloadList = filesToDownload.ToList();
//            ProcessReporter?.Report((DownProgress.DownLibs, downloadList.Count, 0, "开始下载缺失的文件..."));
//            await _downloader.DownloadListAsync(new Progress<(int completed, string fileName)>(p =>
//            {
//                ProcessReporter?.Report((DownProgress.DownLibs, downloadList.Count, p.completed, p.fileName));
//            }), downloadList, Init.ConfigManager.config.OlanSettings.MaximumDownloadThreads, Token);
//        }

//        // 最后，完整地执行一次安装流程（主要为了运行处理器和生成最终的json）
//        ProcessReporter?.Report((DownProgress.DownAndInstModFiles, 0, 0, "正在配置 Mod 加载器..."));
//        await downloadPlanner.MinecraftBasic(
//            Init.ConfigManager.config.OlanSettings.MaximumDownloadThreads,
//            Init.ConfigManager.config.OlanSettings.MaximumSha1Threads,
//            Init.ConfigManager.config.OlanSettings.IsSha1Enabled,
//            Init.ConfigManager.config.OlanSettings.IsAllowToDownloadUseBMLCAPI
//        );
//    }
//    private string FindAuthoritativeJsonFile(string versionPath)
//    {
//        var jsonFiles = Directory.EnumerateFiles(versionPath, "*.json").ToList();
//        if (!jsonFiles.Any())
//            throw new OlanException("无法导入","无法在PCL的版本文件内");

//        // 优先寻找和文件夹同名的json
//        string expectedJsonName = $"{Path.GetDirectoryName(versionPath)}.json";
//        string? authoritativeJson = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals(expectedJsonName, StringComparison.OrdinalIgnoreCase));

//        // 如果找不到，就用第一个作为备选
//        return authoritativeJson ?? jsonFiles.First();
//    }
//    //private async Task InstallMinecraft(GameData gameData,string versionID,ModEnum modLoaderType)
//    //{
//    //    //using Download downTool = new Download();
//    //    //// 调用自己的方法把版本下了
//    //    //// 这里离开using会释放，所以要在这里等待
//    //    //await new DownloadMinecraft(downTool, new UserVersion()
//    //    //{
//    //    //    VersionID = versionID,
//    //    //    modType = new ModType()
//    //    //    {
//    //    //        IsFabric = modLoaderType == ModEnum.fabric,
//    //    //        IsNeoForge = modLoaderType == ModEnum.neoforge,
//    //    //    }
//    //    //}, Init.MojangVersionList.FirstOrDefault(x => x.ID == versionID),
//    //    //gameData, Init.GameRootPath,
//    //    //process, 
//    //    //token).MinecraftBasic(
//    //    //    Init.ConfigManager.config.OlanSettings.MaximumDownloadThreads,
//    //    //    Init.ConfigManager.config.OlanSettings.MaximumSha1Threads,
//    //    //    Init.ConfigManager.config.OlanSettings.IsSha1Enabled,
//    //    //    Init.ConfigManager.config.OlanSettings.IsAllowToDownloadUseBMLCAPI);
//    //}
//    private async Task MigrateUserDataAsync(string sourcePath, string destinationPath)
//    {
//        // 定义需要迁移的文件夹和文件列表
//        var foldersToMove = new[] { "saves", "mods", "resourcepacks", "shaderpacks", "config", "logs" };
//        var filesToMove = new[] { "options.txt" };

//        // --- 这是补全的部分 ---
//        foreach (var folderName in foldersToMove)
//        {
//            token.ThrowIfCancellationRequested(); // 在每个循环开始时检查是否已请求取消
//            string sourceFolder = Path.Combine(sourcePath, folderName);
//            if (Directory.Exists(sourceFolder))
//            {
//                string destFolder = Path.Combine(destinationPath, folderName);
//                // 使用你已有的 Tools.CopyDirectoryAsync 方法来递归复制文件夹
//                await Tools.CopyDirectoryAsync(sourceFolder, destFolder, token);
//            }
//        }
//        // --- 补全结束 ---

//        foreach (var fileName in filesToMove)
//        {
//            token.ThrowIfCancellationRequested();
//            string sourceFile = Path.Combine(sourcePath, fileName);
//            if (File.Exists(sourceFile))
//            {
//                string destFile = Path.Combine(destinationPath, fileName);
//                File.Copy(sourceFile, destFile, true);
//            }
//        }
//    }
//    #endregion
//}