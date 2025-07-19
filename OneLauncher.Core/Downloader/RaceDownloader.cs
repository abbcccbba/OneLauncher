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
public enum RaceStrategy
{
    /// <summary>
    /// 对每个文件都进行全源竞速。
    /// </summary>
    RaceEveryTime,

    /// <summary>
    /// 仅在第一次下载时竞速，缓存胜出的源供后续同批次任务使用。
    /// </summary>
    RaceOnceAndCacheWinner
}
public class RaceDownloader
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, IDownloadSourceUrlProvider> _winnerCache = new();

    public RaceDownloader(HttpClient client) => _httpClient = client;

    internal Task RaceManyFilesAsync(
        IEnumerable<NdDowItem> basicItems,
        IDownloadSourceUrlProvider[] urlProviders,
        RaceStrategy strategy,
        int maxParallelism,
        IProgress<string> progress,
        CancellationToken token)
    {
        string cacheKey = strategy == RaceStrategy.RaceOnceAndCacheWinner ? Guid.NewGuid().ToString() : "default";
        return Parallel.ForEachAsync(basicItems,
            new ParallelOptions { MaxDegreeOfParallelism = maxParallelism, CancellationToken = token },
            async (item, ct) =>
            {
                await RaceSingleFileInternalAsync(item, urlProviders, strategy, cacheKey, ct);
                progress?.Report(Path.GetFileName(item.path));
            });
    }

    private async Task RaceSingleFileInternalAsync(
        NdDowItem basicItem,
        IDownloadSourceUrlProvider[] urlProviders,
        RaceStrategy strategy,
        string cacheKey,
        CancellationToken token)
    {
        if (strategy == RaceStrategy.RaceOnceAndCacheWinner && _winnerCache.TryGetValue(cacheKey, out var winnerProvider))
        {
            await DownloadWithRetryAsync(GetItemFromProvider(basicItem, winnerProvider), token);
            return;
        }

        var itemsToRace = GetItemsFromProviders(basicItem, urlProviders);
        (var successfulItem, var successfulProvider) = await RaceAndGetWinnerAsync(itemsToRace, urlProviders, token);

        if (strategy == RaceStrategy.RaceOnceAndCacheWinner)
        {
            _winnerCache.TryAdd(cacheKey, successfulProvider);
        }
        await DownloadWithRetryAsync(successfulItem, token);
    }

    private NdDowItem GetItemFromProvider(NdDowItem basicItem, IDownloadSourceUrlProvider provider)
    {
        if (basicItem.path.EndsWith(".jar") && !basicItem.path.Contains("libraries"))
            return provider.GetClientMainFile(basicItem);
        return provider.GetLibrariesFiles(new[] { basicItem }).FirstOrDefault();
    }

    private IEnumerable<NdDowItem> GetItemsFromProviders(NdDowItem basicItem, IDownloadSourceUrlProvider[] providers)
    {
        if (basicItem.path.EndsWith(".jar") && !basicItem.path.Contains("libraries"))
            return providers.Select(p => p.GetClientMainFile(basicItem));
        return providers.Select(p => p.GetLibrariesFiles(new[] { basicItem }).FirstOrDefault());
    }

    private async Task<(NdDowItem, IDownloadSourceUrlProvider)> RaceAndGetWinnerAsync(IEnumerable<NdDowItem> items, IDownloadSourceUrlProvider[] providers, CancellationToken token)
    {
        var validItems = items.Zip(providers, (item, provider) => (item, provider))
                              .Where(x => !string.IsNullOrEmpty(x.item.url)).ToList();
        if (!validItems.Any()) throw new OlanException("无有效下载链接", "所有源均未提供有效的URL。");

        var tasks = validItems.Select(async x => (await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, x.item.url), token), x)).ToList();

        while (tasks.Any())
        {
            var completedTask = await Task.WhenAny(tasks);
            var (response, pair) = await completedTask;
            if (response.IsSuccessStatusCode)
            {
                response.Dispose();
                return pair;
            }
            tasks.Remove(completedTask);
        }
        return validItems.First();
    }

    private async Task DownloadWithRetryAsync(NdDowItem item, CancellationToken token)
    {
        const int maxRetries = 3;
        var delay = TimeSpan.FromSeconds(1);
        for (int i = 0; i < maxRetries; i++)
        {
            if (token.IsCancellationRequested) throw new OperationCanceledException(token);
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
                using var response = await _httpClient.GetAsync(item.url, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                response.EnsureSuccessStatusCode();
                var dir = Path.GetDirectoryName(item.path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using var fs = new FileStream(item.path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                await response.Content.CopyToAsync(fs, linkedCts.Token);
                return;
            }
            catch (Exception)
            {
                if (i == maxRetries - 1) throw;
                await Task.Delay(delay, token);
                delay *= 2;
            }
        }
    }
}