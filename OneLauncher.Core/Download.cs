using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core;
public enum DownProgress
{
    DownMeta,
    DownLibs,
    DownAssets,
    DownMain,
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
    private readonly HttpClient UnityClient;
    private VersionInfomations versionInfomations;
    /// <summary>
    /// 开始异步下载
    /// </summary>
    /// <param name="DownloadVersion">下载哪个版本？</param>
    /// <param name="GameRootPath">游戏根目录（比如 F:\.minecraft ）</param>
    /// <param name="OsType">系统类型</param>
    /// <param name="progress">进度回调</param>
    /// <param name="IsVersionIsolation">是否启用版本隔离</param>
    /// <param name="maxConcurrentDownloads">最大下载线程</param>
    /// <param name="maxConcurrentSha1">最大sha1校验线程</param>
    /// <param name="IsSha1">是否校验sha1</param>
    /// <param name="IsCheckFileExists">是否检查文件是否已经存在</param>
    public async Task StartAsync(
        VersionBasicInfo DownloadVersion,
        string GameRootPath,
        SystemType OsType,
        IProgress<(DownProgress Title,int AllFiles,int DownedFiles,string DowingFileName)> progress,
        bool IsVersionIsolation = false,
        int maxConcurrentDownloads = 16,
        int maxConcurrentSha1 = 16,
        bool IsSha1 = true,
        bool IsCheckFileExists = true)
    {
        // 下载 version.json
        var versionjsonpath = (IsVersionIsolation)
            ? Path.Combine(GameRootPath, $"v{DownloadVersion.ID.ToString()}", $"{DownloadVersion.ID.ToString()}.json")
            : Path.Combine(GameRootPath, "versions", DownloadVersion.ID.ToString(), $"{DownloadVersion.ToString()}.json");
        if(!File.Exists(versionjsonpath)) 
            await DownloadFile(new NdDowItem(DownloadVersion.Url,versionjsonpath,0));
        // 实例化版本信息解析器
        versionInfomations = new VersionInfomations
            (await File.ReadAllTextAsync(versionjsonpath), GameRootPath, OsType,IsVersionIsolation);
        // 下载资源文件索引
        var assetsIndex = versionInfomations.GetAssets();   
        if (!File.Exists(assetsIndex.path))
            await DownloadFile(assetsIndex);

        var AllNd = new List<NdDowItem>();
        var NdLibs = versionInfomations.GetLibrarys();
        var NdAssets = VersionAssetIndex.ParseAssetsIndex(await File.ReadAllTextAsync(versionInfomations.GetAssets().path), GameRootPath);
        var mainFile = versionInfomations.GetMainFile();
        NdDowItem log4j2;
        AllNd.AddRange(NdLibs);
        AllNd.AddRange(NdAssets);
        AllNd.Add(mainFile);

        int FileCount = AllNd.Count;
        int Filed = 0;
        // 下载资源文件和库文件
        progress.Report((DownProgress.DownLibs,FileCount,Filed,""));
        await DownloadListAsync(
            new Progress<(int donecount, string filename)>(p =>
            {
                Interlocked.Increment(ref Filed);
                progress.Report((DownProgress.DownLibs, FileCount, p.donecount, p.filename)); 
            }), 
            IsCheckFileExists ? CheckFilesExists(NdLibs) : NdLibs,
            GameRootPath,
            maxConcurrentDownloads);
        // 释放本地库文件
        foreach (var i in versionInfomations.NativesLibs)
            ExtractFile(Path.Combine(GameRootPath, "libraries", i), 
            (IsVersionIsolation)
            ? Path.Combine(GameRootPath, $"v{DownloadVersion.ID.ToString()}", "natives")
            : Path.Combine(GameRootPath, "versions", DownloadVersion.ID.ToString(), "natives"));
        progress.Report((DownProgress.DownAssets, FileCount, Filed, ""));
        await DownloadListAsync(
            new Progress<(int donecount, string filename)>(p =>
            {
                Interlocked.Increment(ref Filed);
                progress.Report((DownProgress.DownAssets, FileCount, p.donecount, p.filename));
            }),
            IsCheckFileExists ? CheckFilesExists(NdAssets) : NdAssets,
            GameRootPath,
            maxConcurrentDownloads
        );

        // 下载主文件
        if(!File.Exists(mainFile.path))
            await DownloadFile(mainFile);
        Interlocked.Increment(ref Filed);
        progress.Report((DownProgress.DownMain, FileCount, Filed, mainFile.path));
        // 下载日志配置文件
        if (DownloadVersion.ID > new Version("1.7"))
        {
            log4j2 = versionInfomations.GetLoggingConfig();
            if (log4j2 != null)
            {
                AllNd.Add(log4j2);
                if(!File.Exists(log4j2.path))
                    await DownloadFile(log4j2);
                Interlocked.Increment(ref Filed);
                progress.Report((DownProgress.DownLog4j2,FileCount,Filed,log4j2.path));
            }
        }
        // 校验所有文件
        if (IsSha1)
        {
            await CheckAllSha1(AllNd, maxConcurrentSha1);
            progress.Report((DownProgress.Verify, FileCount, Filed, "All Files"));
        }
        progress.Report((DownProgress.Done,FileCount,Filed,"OK"));
    }
    private async Task DownloadListAsync(IProgress<(int completedFiles,string FilesName)> progress, List<NdDowItem> downloadNds,string GameRootPath,int maxConcurrentDownloads)
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
                    // 执行下载操作
                    await DownloadFile(item);
                    // 原子递增已完成文件数
                    Interlocked.Increment(ref completedFiles);
                    // 报告进度
                    progress?.Report((completedFiles, item.path));
                }
                catch (Exception ex)
                {
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                        try
                        {
                            await DownloadFile(item);
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
    private List<NdDowItem> CheckFilesExists(List<NdDowItem> FDI)
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
    private async Task DownloadFile(NdDowItem item)
    {
        try
        {
            using (var response = await UnityClient.GetAsync(item.url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (var httpStream = await response.Content.ReadAsStreamAsync())
                {
                    var directory = Path.GetDirectoryName(item.path);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);
                    using (var fileStream = new FileStream(item.path, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                    {
                        await httpStream.CopyToAsync(fileStream, 8192);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"下载失败: {ex.Message}, URL: {item.url}");
            throw;
        }
    }
    private async Task CheckAllSha1(List<NdDowItem> FDI, int maxConcurrentSha1)
    {
        var semaphore = new SemaphoreSlim(maxConcurrentSha1);
        var sha1Tasks = new List<Task>(FDI.Count);
        foreach (var item in FDI)
        {
            await semaphore.WaitAsync();
            sha1Tasks.Add(Task.Run(async () =>
            {
                using (var stream = new FileStream(item.path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true))
                using (var sha1Hash = SHA1.Create())
                {
                    //Debug.WriteLine($"正在校验文件: {item.path}");
                    byte[] hash = await sha1Hash.ComputeHashAsync(stream);
                    string calculatedSha1 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(calculatedSha1, item.sha1, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"SHA1 校验失败。文件: {item.path}, 预期: {item.sha1}, 实际: {calculatedSha1}");
                    }
                }
                
                semaphore.Release();
            }));
            await Task.WhenAll(sha1Tasks);
        }
    }
    public void Dispose()
    {
        UnityClient.Dispose();
    }
}

