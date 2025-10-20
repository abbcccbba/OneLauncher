using OneLauncher.Core.Downloader.DownloadMinecraftProviders.DownloadSources;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader;
public class RaceDownloader
{
    private readonly HttpClient _httpClient;
    public RaceDownloader(HttpClient client) => _httpClient = client;

    internal Task RaceManyFilesAsync(
        IEnumerable<IEnumerable<NdDowItem>> basicItems, // 外部是单个文件，内部是单个文件的不同下载源
        int maxParallelism,
        IProgress<string> progress,
        CancellationToken token)
    {
        return Parallel.ForEachAsync(basicItems,
            new ParallelOptions { MaxDegreeOfParallelism = maxParallelism, CancellationToken = token },
            async (item, ct) =>
            {
                await RaceSingleFileAsync(item, ct);
                progress?.Report(Path.GetFileName(item.FirstOrDefault().path));
            });
    }

    public async Task RaceSingleFileAsync(
        IEnumerable<NdDowItem> item,
        CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        HttpResponseMessage? successfulResponse = null;

        // 同时启动所有源的下载任务
        Task<HttpResponseMessage>[] sourceTasks =
            item.Select(
                x =>
                _httpClient.GetAsync(x.url, HttpCompletionOption.ResponseHeadersRead, cts.Token))
            .ToArray();

        // 等待最先完成的任务并取消其他任务
        var winnerTask = await Task.WhenAny(sourceTasks);
        #region 正常情况
        try
        {
            HttpResponseMessage winnerResponse = await winnerTask;
            // 先不着急取消任务，先检查是否成功
            if (winnerResponse.IsSuccessStatusCode)
            {
                successfulResponse = winnerResponse;
                await cts.CancelAsync();
            }
            else winnerResponse.Dispose();
        }
        catch (HttpRequestException)
        {
            // 如果失败了，等待回退
        }
        #endregion
        #region 回退机制
        if(successfulResponse == null)
        {
            // 找到其他非第一个的响应
            var remainingTasks = sourceTasks.Where(t => t != winnerTask);
            foreach (var task in remainingTasks)
            {
                try
                {
                    HttpResponseMessage response = await task;
                    if (response.IsSuccessStatusCode)
                    {
                        successfulResponse = response;
                        await cts.CancelAsync();
                        break; // 找到一个成功的响应就停止
                    }
                    else response.Dispose(); 
                }
                catch (HttpRequestException)
                {
                    // 忽略失败的请求
                }
            }
        }
        if(successfulResponse == null)
        {
            // 如果依旧没有成功的响应，抛出异常
            throw new OlanException("下载失败","所有下载源均无法访问头");
        }
        #endregion

        await ReadResponseAndWriteToFileAsync(
            successfulResponse, 
            // 这里不关心下载地址是什么，只关心保存路径
            item.FirstOrDefault(), 
            token);
        successfulResponse.Dispose();
    }
    private async Task ReadResponseAndWriteToFileAsync(
        HttpResponseMessage response,
        NdDowItem main,
        CancellationToken cancelToken)
    {
        // 不包含SHA1校验
        using var contentStream = await response.Content.ReadAsStreamAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(main.path)!);
        using (var fileStream = new FileStream(main.path, FileMode.Create, FileAccess.Write, FileShare.None,8192,true))
            await contentStream.CopyToAsync(fileStream, cancelToken);
    }
}