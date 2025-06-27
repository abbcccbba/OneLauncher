using OneLauncher.Core.Downloader.DownloadMinecraftProviders.Sources;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Mod.ModLoader.forgeseries;
using OneLauncher.Core.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders;
public enum DownProgress
{
    Meta,
    DownMain,
    DownLibs,
    DownAndInstModFiles,
    DownAssets,
    DownLog4j2,
    Verify,
    Done
}
public partial class DownloadMinecraft
{
    private int retryTimes; // 已经重试次数
    private const int MAX_DOWNLOAD_RETRIES = 3; // 每个文件最多重试3次
    private const int DOWNLOAD_TIMEOUT_SECONDS = 10; // 每次尝试的超时时间为10秒
    private async Task DownloadClientTasker(NdDowItem main, bool UseBMLCAPI)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);

        if (!UseBMLCAPI)
        {
            progress?.Report((DownProgress.DownMain, alls, dones, Path.GetFileName(main.path)));
            await info.DownloadTool.DownloadFile(main.url, main.path, cancelToken);
        }
        else
        {
            await Task.Run(async () =>
            {
                progress?.Report((DownProgress.DownMain, alls, dones, Path.GetFileName(main.path)));
                string mirrorUrl = $"https://bmclapi2.bangbang93.com/version/{info.ID}/client";
                using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);

                try
                {
                    var originalTask = info.DownloadTool.unityClient.GetAsync(main.url, HttpCompletionOption.ResponseHeadersRead, raceCts.Token);
                    var mirrorTask = info.DownloadTool.unityClient.GetAsync(mirrorUrl, HttpCompletionOption.ResponseHeadersRead, raceCts.Token);

                    var winnerTask = await Task.WhenAny(originalTask, mirrorTask);

                    await raceCts.CancelAsync();

                    HttpResponseMessage winnerResponse = await winnerTask;
                    winnerResponse.EnsureSuccessStatusCode();

                    using var contentStream = await winnerResponse.Content.ReadAsStreamAsync();
                    Directory.CreateDirectory(Path.GetDirectoryName(main.path));
                    using (var fileStream = new FileStream(main.path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await contentStream.CopyToAsync(fileStream, cancelToken);
                    }

                    winnerResponse?.Dispose();
                }
                catch (OperationCanceledException)
                {

                }
                catch (HttpRequestException)
                {
                    // 自动回退
                    await info.DownloadTool.DownloadFile(main.url, main.path, cts.Token);
                }
            }, cts.Token);
        }
    }
    private async Task DownloadAssetsSupportTasker(List<NdDowItem> assets, bool UseBMLCAPI)
    {
        List<NdDowItem> assetsToDownload = assets;
        if (UseBMLCAPI)
        {
            // 替换 URL 使用 BMLCAPI
            assetsToDownload = assets.Select(x =>
                new NdDowItem(
                    x.url.Replace("https://resources.download.minecraft.net/", "https://bmclapi2.bangbang93.com/assets/"),
                    x.path, x.size, x.sha1)).ToList();

            using CancellationTokenSource ctsd = new CancellationTokenSource();
            Task o = info.DownloadTool.unityClient.GetAsync(assets[0].url, HttpCompletionOption.ResponseHeadersRead, ctsd.Token);
            Task b = info.DownloadTool.unityClient.GetAsync(assetsToDownload[0].url, HttpCompletionOption.ResponseHeadersRead, ctsd.Token);

            await Task.WhenAny(o, b);
            if (b.IsCompleted && !b.IsFaulted)
            {
                ctsd.Cancel();
                Debug.WriteLine("使用 BMLC 源");
            }
            else
            {
                ctsd.Cancel();
                Debug.WriteLine("使用官方源");
                assetsToDownload = assets; // 回退到官方源
            }
        }
        else
        {
            Debug.WriteLine("使用官方源");
        }

        // 执行下载
        await               info.DownloadTool.DownloadListAsync(
            new Progress<(int completedFiles, string FilesName)>(p =>
            {
                Interlocked.Increment(ref dones);
                progress?.Report((DownProgress.DownAssets, alls, dones, p.FilesName));
            }),
            assetsToDownload, maxDownloadThreads, cancelToken);
    }
    private async Task DownloadLibrariesSupportTasker(List<NdDowItem> libraries, bool UseBMLCAPI)
    {
        if (UseBMLCAPI)
        {
            await Parallel.ForEachAsync(libraries,
                new ParallelOptions { MaxDegreeOfParallelism = maxDownloadThreads, CancellationToken = cancelToken },
                async (library, cancellationToken) =>
                {
                    string mirrorUrl = library.url.Replace("https://libraries.minecraft.net/", "https://bmclapi2.bangbang93.com/maven/");

                    for (int attempt = 1; attempt <= MAX_DOWNLOAD_RETRIES; attempt++)
                    {
                        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        attemptCts.CancelAfter(TimeSpan.FromSeconds(DOWNLOAD_TIMEOUT_SECONDS));

                        try
                        {
                            // --- 3. 竞速：同时启动两个下载任务 ---
                            Task<HttpResponseMessage> originalTask = info.DownloadTool.unityClient.GetAsync(library.url, HttpCompletionOption.ResponseHeadersRead, attemptCts.Token);
                            Task<HttpResponseMessage> mirrorTask = info.DownloadTool.unityClient.GetAsync(mirrorUrl, HttpCompletionOption.ResponseHeadersRead, attemptCts.Token);

                            var completedTask = await Task.WhenAny(originalTask, mirrorTask);

                            HttpResponseMessage successfulResponse = null;

                            // --- 4. 检查胜出者，如果失败则准备回退 ---
                            try
                            {
                                var winnerResponse = await completedTask;
                                if (winnerResponse.IsSuccessStatusCode)
                                {
                                    Debug.WriteLine($"{(completedTask == mirrorTask ? "镜像源" : "官方源")} 胜出并成功: {library.url}");
                                    successfulResponse = winnerResponse;
                                    await attemptCts.CancelAsync(); // 取消另一个请求
                                }
                            }
                            catch (HttpRequestException ex)
                            {
                                Debug.WriteLine($"胜出方任务失败: {ex.Message}");
                                // 忽略异常，继续尝试回退
                            }

                            // --- 5. 回退：如果胜出者失败，则等待另一个源 ---
                            if (successfulResponse == null)
                            {
                                var loserTask = completedTask == originalTask ? mirrorTask : originalTask;
                                try
                                {
                                    Debug.WriteLine($"胜出方失败，回退到另一源... {library.url}");
                                    var loserResponse = await loserTask;
                                    if (loserResponse.IsSuccessStatusCode)
                                    {
                                        Debug.WriteLine($"回退源成功: {library.url}");
                                        successfulResponse = loserResponse;
                                    }
                                }
                                catch (HttpRequestException ex)
                                {
                                    Debug.WriteLine($"回退源也失败: {ex.Message}");
                                }
                            }

                            // --- 6. 处理成功下载 ---
                            if (successfulResponse != null)
                            {
                                using (successfulResponse)
                                using (var contentStream = await successfulResponse.Content.ReadAsStreamAsync())
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(library.path));
                                    using (var fileStream = new FileStream(library.path, FileMode.Create, FileAccess.Write, FileShare.None))
                                    {
                                        await contentStream.CopyToAsync(fileStream, cancellationToken);
                                    }
                                }

                                Interlocked.Increment(ref dones);
                                progress?.Report((DownProgress.DownLibs, alls, dones, Path.GetFileName(library.path)));
                                return;
                            }

                            // 如果代码执行到这里，说明两个源都失败了
                            throw new OlanException("下载失败", "经过多次包括更换下载源尝试均以失败告终");
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            // 用户主动取消，终止所有下载
                            Debug.WriteLine("用户取消下载。");
                            throw;
                        }
                        catch (HttpRequestException ex)
                        {
                            // --- 7. 处理尝试失败，准备重试 ---
                            Debug.WriteLine($"文件 {Path.GetFileName(library.path)} 下载第 {attempt} 次尝试失败: {ex.Message}");
                            if (attempt == MAX_DOWNLOAD_RETRIES)
                            {
                                Debug.WriteLine($"文件 {Path.GetFileName(library.path)} 已达最大重试次数，下载失败。");
                                // 注意：这里用 return 来放弃该文件，而用 throw 会使整个 Parallel.ForEachAsync 失败
                                return;
                            }
                            await Task.Delay(1000, cancellationToken); // 等待1秒后重试
                        }
                    }
                });
        }
        else
        {
            // 不使用镜像的原始逻辑
            await info.DownloadTool.DownloadListAsync(
                new Progress<(int a, string b)>(p =>
                {
                    Interlocked.Increment(ref dones);
                    progress?.Report((DownProgress.DownLibs, alls, dones, p.b));
                }), libraries, maxDownloadThreads, cancelToken);
        }

        // 解压原生库文件
        await Task.Run(() =>
        {
            foreach (var i in info.VersionMojangInfo.NativesLibs)
            {
                Download.ExtractFile(Path.Combine(info.GameRootPath, "libraries", i), Path.Combine(info.VersionInstallInfo.VersionPath, "natives"));
            }
        }, cancelToken);
    }
    private async Task UnityModsInstallTasker(List<NdDowItem> modNds, IModLoaderConcreteProviders[] modProviders,Task javaInstallTask)
    {
        // 先把依赖库下载了
        await info.DownloadTool.DownloadListAsync(
                new Progress<(int a, string b)>(p =>
                {
                    Interlocked.Increment(ref dones);
                    progress?.Report((DownProgress.DownAndInstModFiles, alls, dones, p.b));
                }),
                modNds, maxDownloadThreads, cancelToken);
        // 处理器需要Java
        await javaInstallTask;
        // 依次执行每个加载器的处理器
        foreach (var provider in modProviders)
            await provider.RunInstaller(
                new Progress<string>(p =>
                {
                    progress?.Report((DownProgress.DownAndInstModFiles, alls, dones, p));
                }), cancelToken);
        
    }
    private Task LogginInstallTasker(NdDowItem log4j2_xml)
    {
        Interlocked.Increment(ref dones);
        return info.DownloadTool.DownloadFile(log4j2_xml.url, log4j2_xml.path, cancelToken);
    }
    private Task JavaInstallTasker() => Task.Run(async () =>
    {
        if (!Init.ConfigManger.config.AvailableJavaList.Contains(info.VersionMojangInfo.GetJavaVersion()))
        {
            await AdoptiumAPI.JavaReleaser(
              info.VersionMojangInfo.GetJavaVersion().ToString(),
              Path.Combine(Path.GetDirectoryName(info.GameRootPath), "runtimes"), Init.SystemType);
            Init.ConfigManger.config.AvailableJavaList.Add(info.VersionMojangInfo.GetJavaVersion());
            await Init.ConfigManger.Save();
        }
    });
}
