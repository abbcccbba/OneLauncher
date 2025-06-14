using Microsoft.Win32.SafeHandles;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.ModLoader.neoforge;
using OneLauncher.Core.ModLoader.neoforge.JsonModels;
using OneLauncher.Core.Modrinth;
using OneLauncher.Core.Net.java;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

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
        unityClient = tc ?? new HttpClient(new HttpClientHandler
        {
            MaxConnectionsPerServer = 32 
        })
        {
            Timeout = TimeSpan.FromSeconds(60) 
        };
    }
    public readonly HttpClient unityClient;
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
    public async Task DownloadFileBig(
        string url,
        string savePath,
        long? knownSize,
        int maxSegments,
        IProgress<(long Start, long End)>? segmentProgress = null,
        CancellationToken token = default)
    {
        long fileSize;
        if (knownSize.HasValue)
            fileSize = knownSize.Value;
        
        else
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            headRequest.Headers.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };

            using var headResponse = await unityClient.SendAsync(headRequest, token);
            headResponse.EnsureSuccessStatusCode();
            fileSize = headResponse.Content.Headers.ContentLength
                       ?? throw new OlanException("下载失败","无法确定文件大小。");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

        long interval = (fileSize + maxSegments - 1) / maxSegments;

        var segments = new List<(long Start, long End)>();
        for (long current = 0; current < fileSize; current += interval)
        {
            long end = Math.Min(current + interval - 1, fileSize - 1);
            segments.Add((current, end));
        }

        // 禁用FileStream内部缓冲，并使用WriteThrough模式
        // WriteThrough可以绕过操作系统缓存，对于随机写入可能提升性能
        await using var fileStream = new FileStream(
            savePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 0, // 禁用缓冲
            FileOptions.Asynchronous
        );
        fileStream.SetLength(fileSize);
        var fileHandle = fileStream.SafeFileHandle;

        // 下载
        await Parallel.ForEachAsync(
            segments,
            new ParallelOptions { MaxDegreeOfParallelism = maxSegments, CancellationToken = token },
            async (segment, ct) =>
            {
                // 报告当前处理的分片范围
                segmentProgress?.Report((segment.Start, segment.End));

                // 租用内存池的缓冲区，顶级性能优化
                using var bufferOwner = MemoryPool<byte>.Shared.Rent(128 * 1024); // 128KB
                var buffer = bufferOwner.Memory;

                // 简单的重试逻辑
                const int maxRetries = 3;
                for (int attempt = 1; ; attempt++)
                {
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.Range = new RangeHeaderValue(segment.Start, segment.End);

                        using var response = await unityClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                        response.EnsureSuccessStatusCode();

                        await using var httpStream = await response.Content.ReadAsStreamAsync(ct);

                        long position = segment.Start;
                        int bytesRead;
                        while ((bytesRead = await httpStream.ReadAsync(buffer, ct)) > 0)
                        {
                            // 使用 SafeFileHandle 和 RandomAccess 进行线程安全的并行写入
                            await RandomAccess.WriteAsync(fileHandle, buffer.Slice(0, bytesRead), position, ct);
                            position += bytesRead;
                        }

                        return; // 分片下载成功，退出重试循环
                    }
                    catch (HttpRequestException) when (attempt < maxRetries)
                    {
                        // 发生异常都简单等待后重试
                        await Task.Delay(200 * attempt, ct);
                    }
                }
            }
        );
    }

    public Task DownloadListAsync(
        IProgress<(int completedFiles,string FilesName)> progress,
        List<NdDowItem> downloadNds,
        int maxConcurrentDownloads,
        CancellationToken token)
    {
        int completedFiles = 0;
        var semaphore = new SemaphoreSlim(maxConcurrentDownloads);
        return Task.WhenAll(downloadNds.Select(async item => 
        {
            await semaphore.WaitAsync(token);
            try
            {
                // 原子递增已完成文件数
                Interlocked.Increment(ref completedFiles);
                // 报告进度
                progress?.Report((completedFiles, item.url));
                // 执行下载操作
                await DownloadFile(item.url, item.path, token);
            }
            catch (HttpRequestException ex)
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), token);
                    try
                    {
                        await DownloadFile(item.url, item.path, token);
                        break;
                    }
                    catch (HttpRequestException ex2)
                    {
                        Debug.WriteLine($"重试下载失败: {ex2.Message}, URL: {item.url}");
                        continue;
                    }
                }
                throw new OlanException("下载失败", "重试到达阈值抛出", OlanExceptionAction.Error, ex);
            }
            finally
            {
                // 释放信号量
                semaphore.Release();
            }
        }));
    }
    /*
    public async Task DownloadFileBig(
    string url,
    string savepath,
    long? size,
    int maxSegments,
    CancellationToken token)
    {
        long filesize;

        if (size != null)
            filesize = size.Value;
        else
        {
            using var requestForSize = new HttpRequestMessage(HttpMethod.Head, url);
            using var responseForSize = await unityClient.SendAsync(requestForSize, token);
            responseForSize.EnsureSuccessStatusCode();

            filesize = responseForSize.Content.Headers.ContentLength
                       ?? throw new OlanException("下载失败", "无法知道文件大小");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(savepath));

        using var fs = new FileStream(savepath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);
        fs.SetLength(filesize);
        var fileHandle = fs.SafeFileHandle;

        var segments = Tools.GetSegments(filesize, filesize / maxSegments);
        var semaphore = new SemaphoreSlim(maxSegments);

        await Parallel.ForEachAsync(segments,
        new ParallelOptions
        {
            MaxDegreeOfParallelism = maxSegments,
            CancellationToken = token
        },
        async (segment, ct) =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(segment.startBit, segment.endBit);

            using var response = await unityClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var httpStream = await response.Content.ReadAsStreamAsync(ct);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(81920); 
            try
            {
                int bytesRead;
                long viewWritePosition = segment.startBit;

                while ((bytesRead = await httpStream.ReadAsync(buffer, ct)) > 0)
                {
                    await RandomAccess.WriteAsync(fileHandle, buffer.AsMemory(0, bytesRead), viewWritePosition, ct);
                    viewWritePosition += bytesRead;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        });
        
    }
    public async Task DownloadFileBigMoreUrl(
    string[] urls,
    string savepath,
    long? size,
    int maxSegments,
    CancellationToken token)
    {
        long filesize;

        if (size != null)
        {
            filesize = size.Value;
        }
        else
        {
            using var requestForSize = new HttpRequestMessage(HttpMethod.Head, urls[0]);
            using var responseForSize = await unityClient.SendAsync(requestForSize, token);
            responseForSize.EnsureSuccessStatusCode();

            filesize = responseForSize.Content.Headers.ContentLength
                       ?? throw new OlanException("下载失败", "无法知道文件大小");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(savepath));

        using var fs = new FileStream(savepath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);
        fs.SetLength(filesize);
        var fileHandle = fs.SafeFileHandle;

        var segments = Tools.GetSegments(filesize, filesize / maxSegments);

        // 3. 用于线程安全地轮询URL的计数器
        int urlIndexCounter = -1;

        await Parallel.ForEachAsync(segments,
        new ParallelOptions
        {
            MaxDegreeOfParallelism = maxSegments,
            CancellationToken = token
        },
        async (segment, ct) =>
        {
            int urlIndex = Interlocked.Increment(ref urlIndexCounter) % urls.Length;
            string urlToUse = urls[urlIndex];

            using var request = new HttpRequestMessage(HttpMethod.Get, urlToUse);
            request.Headers.Range = new RangeHeaderValue(segment.startBit, segment.endBit);

            using var response = await unityClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var httpStream = await response.Content.ReadAsStreamAsync(ct);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(81920);
            try
            {
                int bytesRead;
                long viewWritePosition = segment.startBit;

                while ((bytesRead = await httpStream.ReadAsync(buffer, ct)) > 0)
                {
                    await RandomAccess.WriteAsync(fileHandle, buffer.AsMemory(0, bytesRead), viewWritePosition, ct);
                    viewWritePosition += bytesRead;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        });
        
    }
    */
    public async Task DownloadFile(string url,string savepath, CancellationToken? token = null)
    {
        CancellationToken cancellationToken = token ?? CancellationToken.None;
        using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead,cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            using (var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savepath));
                using (var fileStream = new FileStream(savepath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 8192, useAsync: true))
                {
                    await httpStream.CopyToAsync(fileStream, 8192,cancellationToken);
                }
            }
        }
    }
    public async Task DownloadFileAndSha1(string url, string savepath,string sha1, CancellationToken token)
    {
        using (var response = await unityClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
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
    public async Task CheckAllSha1(List<NdDowItem> FDI, int maxConcurrentSha1,CancellationToken token)
    {
        var semaphore = new SemaphoreSlim(maxConcurrentSha1);
        var sha1Tasks = new List<Task>(FDI.Count);
        foreach (var item in FDI)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(item.sha1))
                continue;
            
            sha1Tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
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
    public void Dispose() => unityClient.Dispose();
}