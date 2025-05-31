using OneLauncher.Core.Modrinth;
using OneLauncher.Core.neoforge;
using OneLauncher.Core.Net.java;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace OneLauncher.Core;
public enum DownProgress
{
    DownMain,  
    DownLibs,
    DownAndInstModFiles,
    DownAssets,
    DownLog4j2,
    Verify,
    Done
}
public class Download : IDisposable
{
    /// <summary>
    /// 解压 ZIP 结构文件到指定目录
    /// </summary>
    /// <param ID="filePath">待解压的文件路径（例如 .docx 或其他 ZIP 结构文件）</param>
    /// <param ID="extractPath">解压到的目标目录</param>
    /// <exception cref="IOException">文件访问或解压失败</exception>
    /// <exception cref="InvalidDataException">文件不是有效的 ZIP 格式</exception>
    public static void ExtractFile(string filePath, string extractPath)
    {
        try
        {
            // 确保输出目录存在
            Directory.CreateDirectory(extractPath);

            // 打开 ZIP 文件
            using (ZipArchive archive = ZipFile.OpenRead(filePath))
            {
                // 遍历 ZIP 条目
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // 确定解压路径
                    string destinationPath = Path.Combine(extractPath, entry.FullName);

                    // 确保目录存在
                    string destinationDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    // 仅处理文件（跳过目录）
                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        // 提取文件
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    public Download()
    {
        UnityClient = new HttpClient(new HttpClientHandler
        {
            MaxConnectionsPerServer = 32 
        })
        {
            Timeout = TimeSpan.FromSeconds(60) 
        };
    }
    public readonly HttpClient UnityClient;
    public VersionInfomations versionInfomations;
    /// <summary>
    /// 开始异步下载
    /// </summary>
    /// <param name="DownloadVersion">下载哪个版本？</param>
    /// <param name="GameRootPath">游戏根目录（比如 F:\.minecraft ）</param>
    /// <param name="OsType">系统类型</param>
    /// <param name="progress">进度回调</param>
    /// <param name="IsVersionIsolation">是否启用版本隔离</param>
    /// <param name="IsMod">是否下载Mod（Fabric）版本</param>
    /// <param name="maxConcurrentDownloads">最大下载线程</param>
    /// <param name="maxConcurrentSha1">最大sha1校验线程</param>
    /// <param name="IsSha1">是否校验sha1</param>
    /// <param name="AndJava">同时下载为其下载合适的Java版本</param>
    public async Task StartAsync(
        VersionBasicInfo DownloadVersion,
        string GameRootPath,
        SystemType OsType,
        IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress,
        ModType modType,
        bool IsVersionIsolation = false,
        int maxConcurrentDownloads = 16,
        int maxConcurrentSha1 = 16,
        bool IsSha1 = true,
        bool AndJava = false)
    {
        string VersionPath = Path.Combine(GameRootPath, "versions", DownloadVersion.ID.ToString());
        #region 下载原信息
        {
            // 下载 version.json
            var versionjsonpath =
                Path.Combine(VersionPath, $"{DownloadVersion.ID.ToString()}.json");
            if (!System.IO.File.Exists(versionjsonpath))
                await DownloadFile(DownloadVersion.Url, versionjsonpath);

            // 实例化版本信息解析器
            versionInfomations = new VersionInfomations
                (await System.IO.File.ReadAllTextAsync(versionjsonpath), GameRootPath, OsType, IsVersionIsolation);

            // 下载资源文件索引
            var assetsIndex = versionInfomations.GetAssets();
            if (!System.IO.File.Exists(assetsIndex.path))
                await DownloadFile(assetsIndex.url, assetsIndex.path);
        }
        #endregion
        #region 下载可选信息
        // 下载Java运行时环境
        if (AndJava && !Init.ConfigManger.config.JavaList.Contains(versionInfomations.GetJavaVersion())) //_=Task.Run(async () =>
        {
            await AutoJavaGetter.JavaReleaser(
                versionInfomations.GetJavaVersion().ToString(), 
                Path.Combine(Path.GetDirectoryName(GameRootPath), "JavaRuntimes"), OsType);
            Init.ConfigManger.config.JavaList.Add(versionInfomations.GetJavaVersion());
            Init.ConfigManger.Save();
        }//);

        // 下载Mod相关资源索引
        string modpath = Path.Combine(VersionPath, $"{DownloadVersion.ID.ToString()}-fabric.json");
        if (modType.IsFabric)
        {
            // 加载器
            if (!File.Exists(modpath))
                await DownloadFile(
                    $"https://meta.fabricmc.net/v2/versions/loader/{DownloadVersion.ID.ToString()}/", modpath);
        }
        #endregion
        #region 声明一些信息和下载主文件
        List<NdDowItem> AllNd = new List<NdDowItem>();
        List<NdDowItem> NdLibs = CheckFilesExists(versionInfomations.GetLibrarys());
        List<NdDowItem> NdAssets = CheckFilesExists(VersionAssetIndex.ParseAssetsIndex(await File.ReadAllTextAsync(versionInfomations.GetAssets().path), GameRootPath));
        NdDowItem mainFile = versionInfomations.GetMainFile();
        NdDowItem? log4j2;
        NdDowItem modApi;
        List<NdDowItem> modLibs;
        
        AllNd.AddRange(NdLibs);
        AllNd.AddRange(NdAssets);
        AllNd.Add(mainFile);
        
        int FileCount = AllNd.Count;
        int Filed = 0;
        
        Interlocked.Increment(ref Filed);
        progress.Report((DownProgress.DownMain, FileCount, Filed, mainFile.path));
        if (!File.Exists(mainFile.path))
            await DownloadFile(mainFile.url, mainFile.path);
        #endregion
        
        await DownloadListAsync(
            new Progress<(int donecount, string filename)>(p =>
            {
                Interlocked.Increment(ref Filed);
                progress.Report((DownProgress.DownLibs, FileCount, p.donecount, p.filename)); 
            }), 
            CheckFilesExists(NdLibs),
            maxConcurrentDownloads);
        // 释放本地库文件
        await Task.Run(() =>
        {
            foreach (var i in versionInfomations.NativesLibs)
                ExtractFile(Path.Combine(GameRootPath, "libraries", i), Path.Combine(VersionPath, "natives"));
        });
        #region 下载mod相关依赖
        if (modType.IsFabric)
        {
            // 获取 Fabric 加载器下载信息
            modLibs = CheckFilesExists(new fabric.FabricVJParser(modpath, GameRootPath).GetLibraries());
            // 获取Fabric API下载信息
            var a = new Modrinth.GetModrinth(
               "fabric-api", DownloadVersion.ID.ToString(),
                ((!IsVersionIsolation)
                ? Path.Combine(GameRootPath, "mods")
                : Path.Combine(VersionPath, "mods")));
            await a.Init();
            modApi = (NdDowItem)a!.GetDownloadInfos();

            AllNd.AddRange(modLibs);
            AllNd.Add(modApi);
            FileCount = AllNd.Count;

            await DownloadListAsync(
            new Progress<(int donecount, string filename)>(p =>
            {
                Interlocked.Increment(ref Filed);
                progress.Report((DownProgress.DownAndInstModFiles, FileCount, p.donecount, p.filename));
            }),
            modLibs,
            maxConcurrentDownloads);

            Interlocked.Increment(ref Filed);
            progress.Report((DownProgress.DownAndInstModFiles, FileCount, Filed, ((NdDowItem)modApi).path));
            await DownloadFile(((NdDowItem)modApi).url, ((NdDowItem)modApi).path);
        }
        else if (modType.IsNeoForge)
        {
            NeoForgeInstallTasker installTasker = new NeoForgeInstallTasker
                (
                    this,
                    Path.Combine(GameRootPath, "libraries"),
                    Path.Combine(GameRootPath, "versions", DownloadVersion.ID),
                    DownloadVersion.ID
                );
            // 下载依赖库和工具依赖库文件
            string neoForgeActualVersion = await new NeoForgeVersionListGetter(UnityClient)
                // 调用Gemini写的名字贼长的方法来获取NeoForge安装程序的url
                .GetLatestSuitableNeoForgeVersionStringAsync(DownloadVersion.ID,true);
            string installerUrl = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{neoForgeActualVersion}/neoforge-{neoForgeActualVersion}-installer.jar";
            (List<NdDowItem> NdModLibs,List<NdDowItem> NdModToolsLibs,string BDFilePath) = await installTasker.StartReady(installerUrl);
            await DownloadListAsync(
            new Progress<(int donecount, string filename)>(p =>
            {
                Interlocked.Increment(ref Filed);
                progress.Report((DownProgress.DownAndInstModFiles, FileCount, p.donecount, p.filename));
            }),
            CheckFilesExists(NdModLibs.Concat(NdModToolsLibs).ToList()),
            maxConcurrentDownloads
            );

            FileCount = AllNd.Count;
            // 执行NeoForge处理器
            installTasker.ProcessorsOutEvent += (int a, int b, string c) =>
            {
                if (a == -1 && b == -1)
                    throw new OlanException("NeoForge安装失败",$"执行处理器时报错。信息：{c}",OlanExceptionAction.Error);
                progress.Report((DownProgress.DownAndInstModFiles,FileCount,Filed,$"[执行处理器({b}/{a})]{Environment.NewLine}{c}"));
            };
            await installTasker.ToRunProcessors
                (
                    Path.Combine(GameRootPath, "versions", DownloadVersion.ID, $"{DownloadVersion.ID}.jar"),
                    Tools.IsUseOlansJreOrOssJdk(versionInfomations.GetJavaVersion(), Path.GetDirectoryName(GameRootPath)),
                    BDFilePath,OsType
                );
        }
        #endregion
        // 下载资源文件
        await DownloadListAsync(
            new Progress<(int donecount, string filename)>(p =>
            {
                Interlocked.Increment(ref Filed);
                progress.Report((DownProgress.DownAssets, FileCount, p.donecount, p.filename));
            }),
            CheckFilesExists(NdAssets),
            maxConcurrentDownloads
        );
        // 下载日志配置文件
        if (new Version(DownloadVersion.ID) > new Version("1.7"))
        {
            log4j2 = (NdDowItem)versionInfomations.GetLoggingConfig();
            FileCount = AllNd.Count;
            if (log4j2.HasValue)
            {
                AllNd.Add((NdDowItem)log4j2);
                Interlocked.Increment(ref Filed);
                progress.Report((DownProgress.DownLog4j2, FileCount, Filed, ((NdDowItem)log4j2).path));
                if (!File.Exists(((NdDowItem)log4j2).path))
                    await DownloadFile(((NdDowItem)log4j2).url, ((NdDowItem)log4j2).path);
            }
        }
        // 校验所有文件
        if (IsSha1)
        {
            progress.Report((DownProgress.Verify, FileCount, Filed, "All Files"));
            await CheckAllSha1(AllNd, maxConcurrentSha1);
        }
        progress.Report((DownProgress.Done,FileCount,Filed,"OK"));
    }
    /// <summary>
    /// 开始异步下载Mod（可选是否下载依赖项）
    /// </summary>
    /// <param name="progress">进度回调：总字节数，已经下载的字节数，当前正在操作的文件名</param>
    /// <param name="ModID">Mod ID （Modrinth）</param>
    /// <param name="ModPath">Mods文件夹路径</param>
    /// <param name="version">需要安装Mod的版本的版本号</param>
    /// <param name="IsIncludeDependencies">是否下载依赖</param>
    /// <param name="maxConcurrentDownloads">最大下载线程</param>
    /// <param name="maxConcurrentSha1">最大Sha1校验线程</param>
    /// <param name="IsSha1">是否校验Sha1</param>
    /// <returns></returns>
    public async Task StartDownloadMod(
        IProgress<(long AllSizes, long DownedSizes, string DowingFileName)> progress,
        string ModID,
        string ModPath,
        string version,
        bool IsIncludeDependencies = true,
        int maxConcurrentDownloads = 8,
        int maxConcurrentSha1 = 4,
        bool IsSha1 = true
        )
    {
        var GetTask = new GetModrinth(ModID, version, ModPath);
        await GetTask.Init();

        // 获取主 Mod 文件信息
        NdDowItem? mainMod = GetTask.GetDownloadInfos();
        if (!mainMod.HasValue)
            return;

        List<NdDowItem> filesToProcess = new List<NdDowItem> { (NdDowItem)mainMod };

        // 如果需要下载依赖项，则获取依赖项信息并添加到下载列表
        if (IsIncludeDependencies)
        {
            List<NdDowItem> dependencies = GetTask.GetDependenciesInfos();
            filesToProcess.AddRange(dependencies);
        }

        // 过滤掉已经存在的文件
        filesToProcess = CheckFilesExists(filesToProcess);

        // 计算总下载文件大小
        long totalBytesToDownload = filesToProcess.Sum(item => (long)item.size);
        // 用于累积已下载字节数，将在 DownloadListAsync 报告文件完成时更新
        long accumulatedDownloadedBytes = 0;

        // 创建一个内部进度报告器，用于适配 DownloadListAsync 的进度到 StartDownloadMod 的进度
        var fileCompletionProgress = new Progress<(int completedFiles, string FilesName)>(p =>
        {
            // 当 DownloadListAsync 报告一个文件完成时，我们会在这里接收到通知
            // p.FilesName 是刚刚完成下载的文件的完整路径
            NdDowItem? completedItem = filesToProcess.FirstOrDefault(item => item.path == p.FilesName);
            if (completedItem.HasValue)
            {
                Interlocked.Add(ref accumulatedDownloadedBytes, ((NdDowItem)completedItem).size);
                progress?.Report(((int)totalBytesToDownload, (int)accumulatedDownloadedBytes, Path.GetFileName(p.FilesName)));
            }
        });

        progress?.Report(((int)totalBytesToDownload, 0, "开始下载Mod文件..."));
        await DownloadListAsync(fileCompletionProgress, filesToProcess, maxConcurrentDownloads);

        if (IsSha1)
        {
            progress?.Report(((int)totalBytesToDownload, (int)totalBytesToDownload, "正在校验文件..."));
            await CheckAllSha1(filesToProcess, maxConcurrentSha1);
        }

        progress?.Report(((int)totalBytesToDownload, (int)totalBytesToDownload, "下载完成！"));
    }

    public async Task DownloadListAsync(IProgress<(int completedFiles,string FilesName)> progress, List<NdDowItem> downloadNds,int maxConcurrentDownloads)
    {
        // 初始化已完成文件数
        int completedFiles = 0;

        // 使用信号量控制并发数
        var semaphore = new SemaphoreSlim(maxConcurrentDownloads);
        var downloadTasks = new List<Task>(downloadNds.Count);

        // 遍历下载列表，创建并发任务
        foreach (var item in downloadNds)
        {
            await semaphore.WaitAsync();
            downloadTasks.Add(Task.Run(async () =>
            {
                try
                {
                    // 原子递增已完成文件数
                    Interlocked.Increment(ref completedFiles);
                    // 报告进度
                    progress?.Report((completedFiles, item.path));
                    // 执行下载操作
                    await DownloadFile(item.url,item.path); 
                }
                catch (Exception ex)
                {
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                        try
                        {
                            await DownloadFile(item.url, item.path);
                            break;
                        }
                        catch (Exception ex2)
                        {
                            Debug.WriteLine($"重试下载失败: {ex2.Message}, URL: {item.url}");
                            continue;
                        }
                    }
                    throw;
                }
                finally
                {
                    // 释放信号量
                    semaphore.Release();
                }
            }));
        }

        // 等待所有任务完成
        await Task.WhenAll(downloadTasks);
    }
    public List<NdDowItem> CheckFilesExists(List<NdDowItem> FDI)
    {
        List<NdDowItem> filesToDownload = new List<NdDowItem>(FDI.Count);
        foreach (var item in FDI)
        {
            if (File.Exists(item.path))
                continue;
            filesToDownload.Add(item);
        }
        return filesToDownload;
    }
    public async Task DownloadFile(string url,string savepath)
    {
        try
        {
            using (var response = await UnityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (var httpStream = await response.Content.ReadAsStreamAsync())
                {
                    var directory = Path.GetDirectoryName(savepath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);
                    using (var fileStream = new FileStream(savepath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                    {
                        await httpStream.CopyToAsync(fileStream, 8192);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"下载失败: {ex.Message}, URL: {url}");
            throw;
        }
    }
    public async Task CheckAllSha1(List<NdDowItem> FDI, int maxConcurrentSha1)
    {
        var semaphore = new SemaphoreSlim(maxConcurrentSha1);
        var sha1Tasks = new List<Task>(FDI.Count);
        foreach (var item in FDI)
        {
            if (item.sha1 == null)
                continue;
            await semaphore.WaitAsync();
            sha1Tasks.Add(Task.Run(async () =>
            {
                using (var stream = new FileStream(item.path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true))
                using (var sha1Hash = SHA1.Create())
                {
                    byte[] hash = await sha1Hash.ComputeHashAsync(stream);
                    string calculatedSha1 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(calculatedSha1, item.sha1, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"SHA1 校验失败。文件: {item.path}, 预期: {item.sha1}, 实际: {calculatedSha1}");
                    }
                }
                
                semaphore.Release();
            }));
        }
        await Task.WhenAll(sha1Tasks);
    }
    public void Dispose() => UnityClient.Dispose();}

