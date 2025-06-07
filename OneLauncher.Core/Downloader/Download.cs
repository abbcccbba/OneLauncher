using OneLauncher.Core.Minecraft;
using OneLauncher.Core.ModLoader.neoforge;
using OneLauncher.Core.ModLoader.neoforge.JsonModels;
using OneLauncher.Core.Modrinth;
using OneLauncher.Core.Net.java;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OneLauncher.Core.Downloader;

public partial class Download : IDisposable
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
    public Download(HttpClient? tc = null)
    {
        UnityClient = tc ?? new HttpClient(new HttpClientHandler
        {
            MaxConnectionsPerServer = 32 
        })
        {
            Timeout = TimeSpan.FromSeconds(60) 
        };
    }
    public readonly HttpClient UnityClient;
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
        bool IsSha1 = true,
        CancellationToken? token = null
        )
    {
        CancellationToken cancellationToken = token ?? CancellationToken.None;

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
            if(dependencies != null)
                filesToProcess.AddRange(dependencies);
        }

        // 过滤掉已经存在的文件
        filesToProcess = CheckFilesExists(filesToProcess,cancellationToken);

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
        await DownloadListAsync(fileCompletionProgress, filesToProcess, maxConcurrentDownloads,cancellationToken);

        if (IsSha1)
        {
            progress?.Report(((int)totalBytesToDownload, (int)totalBytesToDownload, "正在校验文件..."));
            await CheckAllSha1(filesToProcess, maxConcurrentSha1,cancellationToken);
        }

        progress?.Report(((int)totalBytesToDownload, (int)totalBytesToDownload, "下载完成！"));
    }

    public async Task DownloadListAsync(
        IProgress<(int completedFiles,string FilesName)> progress,
        List<NdDowItem> downloadNds,
        int maxConcurrentDownloads,
        CancellationToken token)
    {
        // 初始化已完成文件数
        int completedFiles = 0;

        // 使用信号量控制并发数
        var semaphore = new SemaphoreSlim(maxConcurrentDownloads);
        var downloadTasks = new List<Task>(downloadNds.Count);

        // 遍历下载列表，创建并发任务
        foreach (var item in downloadNds)
        {
            await semaphore.WaitAsync(token);
            downloadTasks.Add(Task.Run(async () =>
            {
                try
                {
                    // 原子递增已完成文件数
                    Interlocked.Increment(ref completedFiles);
                    // 报告进度
                    progress?.Report((completedFiles, item.url));
                    // 执行下载操作
                    await DownloadFile(item.url,item.path,token); 
                }
                catch (HttpRequestException ex)
                {
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)),token);
                        try
                        {
                            await DownloadFile(item.url, item.path,token);
                            break;
                        }
                        catch (HttpRequestException ex2)
                        {
                            Debug.WriteLine($"重试下载失败: {ex2.Message}, URL: {item.url}");
                            continue;
                        }
                    }
                    throw new OlanException("下载失败","重试到达阈值抛出",OlanExceptionAction.Error,ex);
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
    public List<NdDowItem> CheckFilesExists(List<NdDowItem> FDI, CancellationToken token)
    {
        List<NdDowItem> filesToDownload = new List<NdDowItem>(FDI.Count);
        foreach (var item in FDI)
        {
            token.ThrowIfCancellationRequested();
            if (File.Exists(item.path))
                continue;
            filesToDownload.Add(item);
        }
        return filesToDownload;
    }
    public async Task DownloadFile(string url,string savepath, CancellationToken? token = null)
    {
        CancellationToken cancellationToken = token ?? CancellationToken.None;
        using (var response = await UnityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead,cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            using (var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                var directory = Path.GetDirectoryName(savepath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                using (var fileStream = new FileStream(savepath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                {
                    await httpStream.CopyToAsync(fileStream, 8192,cancellationToken);
                }
            }
        }
    }
    public async Task DownloadFileAndSha1(string url, string savepath,string sha1, CancellationToken token)
    {
        using (var response = await UnityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
        {
            response.EnsureSuccessStatusCode();
            using (var httpStream = await response.Content.ReadAsStreamAsync(token))
            {
                var directory = Path.GetDirectoryName(savepath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                using (var fileStream = new FileStream(savepath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: 8192, useAsync: true))
                {
                    await httpStream.CopyToAsync(fileStream, 8192, token);
                    fileStream.Position = 0;
                    using (var sha1Hash = SHA1.Create())
                    {
                        byte[] hash = await sha1Hash.ComputeHashAsync(fileStream,token);
                        string calculatedSha1 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        if (!string.Equals(calculatedSha1, sha1, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new OlanException(
                                "下载失败",
                                $"无法校验文件({savepath})Sha1，实际：{calculatedSha1}预期：{sha1}",
                                OlanExceptionAction.Warning);
                        }
                    }
                }
            }
        }
    }
    public async Task CheckAllSha1(List<NdDowItem> FDI, int maxConcurrentSha1,CancellationToken token)
    {
        var semaphore = new SemaphoreSlim(maxConcurrentSha1);
        var sha1Tasks = new List<Task>(FDI.Count);
        foreach (var item in FDI)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(item.sha1))
                continue;
            await semaphore.WaitAsync();
            sha1Tasks.Add(Task.Run(async () =>
            {
                using (var stream = new FileStream(item.path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true))
                using (var sha1Hash = SHA1.Create())
                {
                    byte[] hash = await sha1Hash.ComputeHashAsync(stream,token);
                    string calculatedSha1 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(calculatedSha1, item.sha1, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new OlanException(
                            "下载失败",
                            $"无法校验文件({item.path})Sha1，实际：{calculatedSha1}预期：{item.sha1}",
                            OlanExceptionAction.Warning);
                    }
                }
                
                semaphore.Release();
            },token));
        }
        await Task.WhenAll(sha1Tasks);
    }
    public void Dispose() => UnityClient.Dispose();
}