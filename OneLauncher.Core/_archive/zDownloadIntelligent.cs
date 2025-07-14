using Microsoft.Win32.SafeHandles;
using OneLauncher.Core.Helper;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core._archive;
// 此代码已暂时弃用
/*
    public async Task DownloadFileBig(
    string url,
    string savepath,
    long? size,
    int maxSegments,
    CancelToken token)
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
            CancelToken = token
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
    CancelToken token)
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
            CancelToken = token
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
/*
public partial class Download
{
    /// <summary>
    /// 定义下载器的工作模式
    /// </summary>
    public enum DownloadStrategy
    {
        /// <summary>
        /// 自动模式：探测网络，在单线程和并行故障转移模式间自动选择最优策略 (推荐，默认)
        /// </summary>
        Auto,
        /// <summary>
        /// 单线程模式：最简单的顺序下载，在网络极好时速度最快。
        /// </summary>
        SingleThread,
        /// <summary>
        /// 并行故障转移模式：第一个URL为主力，其余为备用。适合主源稳定，备用源不稳定的情况。
        /// </summary>
        ParallelFailover,
        /// <summary>
        /// 并行协同模式：所有URL一视同仁，共同下载。适合所有源都很稳定的情况。
        /// </summary>
        ParallelConcurrent
    }

    /// <summary>
    /// 下载选项
    /// </summary>
    public struct DownloadOptions
    {
        public DownloadOptions() { }
        /// <summary>
        /// 下载策略，默认为自动选择。
        /// </summary>
        public DownloadStrategy Strategy { get; set; } = DownloadStrategy.Auto;

        /// <summary>
        /// 预先知道的文件大小（字节），可避免一次HEAD请求。
        /// </summary>
        public long? FileSize { get; set; } = null;

        /// <summary>
        /// 并行下载时的最大工作线程数。
        /// </summary>
        public int MaxWorkers { get; set; } = 36;

        /// <summary>
        /// (仅用于 ParallelFailover 模式) 分配为备用下载的线程比例。
        /// </summary>
        public double FailoverWorkerRatio { get; set; } = 0.2;

        /// <summary>
        /// (仅用于 Auto 模式) 判断为“快速连接”的阈值（毫秒），用于下载1MB探测数据。
        /// </summary>
        public int FastConnectionThresholdMs { get; set; } = 500;
    }
    /// <summary>
    /// 通用文件下载方法，支持多种下载策略。
    /// </summary>
    /// <param Name="urls">下载URL列表。在Failover模式下，第一个URL是主URL。</param>
    /// <param Name="savepath">文件保存路径。</param>
    /// <param Name="options">下载选项，不提供则使用默认值。</param>
    /// <param Name="token">取消令牌。</param>
    public async Task DownloadFileIntelligent(
        string[] urls,
        string savepath,
        DownloadOptions options = default,
        CancelToken? token = null)
    {
        var cancelToken = token ?? CancelToken.None;
        long filesize;
        if(options.MaxWorkers == 0)
            options = new DownloadOptions();
        if (options.FileSize.HasValue)
        {
            filesize = options.FileSize.Value;
        }
        else
        {
            using var requestForSize = new HttpRequestMessage(HttpMethod.Head, urls[0]);
            using var responseForSize = await unityClient.SendAsync(requestForSize, cancelToken);
            responseForSize.EnsureSuccessStatusCode();

            filesize = responseForSize.Content.Headers.ContentLength
                       ?? throw new OlanException("下载失败", "无法获知文件大小。");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(savepath));

        switch (options.Strategy)
        {
            case DownloadStrategy.Auto:
                await AutoStrategy(urls, savepath, filesize, options, cancelToken);
                break;
            case DownloadStrategy.SingleThread:
                await this.DownloadFile(urls[0],savepath,cancelToken);
                break;
            case DownloadStrategy.ParallelFailover:
                await FailoverStrategy(urls, savepath, filesize, options, cancelToken);
                break;
            case DownloadStrategy.ParallelConcurrent:
                await ConcurrentStrategy(urls, savepath, filesize, options, cancelToken);
                break;
        }
    }

    #region 内部逻辑

    private async Task AutoStrategy(string[] urls, string savepath, long filesize, DownloadOptions options, CancelToken token)
    {
        // 文件过小直接进度单线程
        var probeSize = 1 * 1024 * 1024;
        if (filesize < probeSize)
        {
            await this.DownloadFile(urls[0], savepath, token);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, urls[0]);
        using var response = await unityClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        using var httpStream = await response.Content.ReadAsStreamAsync(token);

        byte[] buffer = new byte[probeSize];
        Stopwatch stopwatch = Stopwatch.StartNew();
        var bytesRead = await httpStream.ReadAtLeastAsync(buffer, probeSize, false, token);
        stopwatch.StopCore();

        if (stopwatch.ElapsedMilliseconds < options.FastConnectionThresholdMs)
        {
            using var fileStream = new FileStream(savepath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
            await httpStream.CopyToAsync(fileStream, token);
        }
        else
            await FailoverStrategy(urls, savepath, filesize, options, token);
        
    }
    private async Task ConcurrentStrategy(string[] urls, string savepath, long filesize, DownloadOptions options, CancelToken token)
    {
        using var fs = new FileStream(savepath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
        fs.SetLength(filesize);
        var fileHandle = fs.SafeFileHandle;
        var segments = Tools.GetSegments(filesize, options.MaxWorkers);
        var segmentPool = new System.Collections.Concurrent.ConcurrentBag<(long start, long end)>(segments);
        var workerTasks = new List<Task>();

        for (int i = 0; i < options.MaxWorkers; i++)
        {
            string assignedUrl = urls[i % urls.Length]; // 使用 .Length 替代 .Count
            workerTasks.Add(Task.Run(async () => {
                while (segmentPool.TryTake(out var segment))
                {
                    if (!await TryDownloadSegment(fileHandle, assignedUrl, segment, 3, token))
                    {
                        segmentPool.Add(segment);
                    }
                }
            }, token));
        }
        await Task.WhenAll(workerTasks);
    }
    private async Task FailoverStrategy(string[] urls, string savepath, long filesize, DownloadOptions options, CancelToken token)
    {
        // 实现 "FinalBoss" (主备分离) 逻辑
        var primaryUrl = urls[0];
        var failoverUrls = urls.Skip(1).ToArray();

        if (failoverUrls.Length == 0)
        { 
            await ConcurrentStrategy(urls, savepath, filesize, options, token);
            return;
        }

        using var fs = new FileStream(savepath, FileMode.Create, FileAccess.Write, FileShare.None, 8129,useAsync: true);
        fs.SetLength(filesize);
        var fileHandle = fs.SafeFileHandle;

        var segments = Tools.GetSegments(filesize, options.MaxWorkers);
        var primaryPool = new System.Collections.Concurrent.ConcurrentBag<(long start, long end)>(segments);
        var failoverPool = new System.Collections.Concurrent.ConcurrentBag<(long start, long end)>();

        var failoverWorkerCount = (int)Math.Max(1, Math.Round(options.MaxWorkers * options.FailoverWorkerRatio));
        var primaryWorkerCount = options.MaxWorkers - failoverWorkerCount;

        var allWorkers = new List<Task>();

        // --- 补完的主力线程逻辑 ---
        for (int i = 0; i < primaryWorkerCount; i++)
        {
            allWorkers.Add(Task.Run(async () =>
            {
                while (primaryPool.TryTake(out var segment))
                {
                    // 尝试用主URL下载，如果多次重试后仍失败
                    if (!await TryDownloadSegment(fileHandle, primaryUrl, segment, 3, token))
                    {
                        // 将这个“硬骨头”分片扔到备用池
                        failoverPool.Add(segment);
                    }
                }
            }, token));
        }

        // --- 补完的备用线程逻辑 ---
        for (int i = 0; i < failoverWorkerCount; i++)
        {
            allWorkers.Add(Task.Run(async () =>
            {
                // 只要主池或备用池里还有任务，就继续工作
                while (!primaryPool.IsEmpty || !failoverPool.IsEmpty)
                {
                    if (failoverPool.TryTake(out var segment))
                    {
                        bool success = false;
                        // 轮流尝试所有备用URL
                        foreach (var url in failoverUrls)
                        {
                            if (await TryDownloadSegment(fileHandle, url, segment, 2, token))
                            {
                                success = true;
                                break; // 这个备用URL成功了，不用再试别的
                            }
                        }
                        if (!success)
                        {
                            // 如果所有备用源都搞不定，可以选择抛出异常或记录日志
                            // 为了健壮性，我们暂时只记录，也可以选择重新放回主池做最后一搏
                            Console.WriteLine($"CRITICAL: Segment {segment.start} failed on all failover URLs.");
                        }
                    }
                    else
                    {
                        // 备用池暂时没任务，稍等一下，避免CPU空转
                        await Task.Delay(200, token);
                    }
                }
            }, token));
        }

        await Task.WhenAll(allWorkers);
    }

    #endregion
    #region 实现逻辑
    /// <summary>
    /// 尝试下载单个分片，内部包含重试逻辑。
    /// </summary>
    /// <returns>成功返回 true，失败返回 false。</returns>
    private async Task<bool> TryDownloadSegment(SafeFileHandle fileHandle, string url, (long start, long end) segment, int maxRetries, CancelToken token)
    {
        int attempts = 0;
        while (attempts < maxRetries)
        {
            try
            {
                await DownloadSegmentAsync(fileHandle, url, segment, token);
                return true; // 下载成功
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                attempts++;
                if (attempts < maxRetries)
                {
                    // 简单的延迟后重试
                    await Task.Delay(500 * attempts, token);
                }
            }
        }
        return false; // 所有重试均失败
    }

    /// <summary>
    /// 执行单个分片的下载和写入操作。
    /// </summary>
    private async Task DownloadSegmentAsync(SafeFileHandle fileHandle, string url, (long startBit, long endBit) segment, CancelToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(segment.startBit, segment.endBit);

        using var response = await unityClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var httpStream = await response.Content.ReadAsStreamAsync(ct);
        // 使用 ArrayPool 减少内存分配
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
    }
    #endregion
}
*/