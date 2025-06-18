using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.ModLoader.neoforge;
using OneLauncher.Core.Net.java;
using OneLauncher.Core.Net.ModService.Modrinth;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader;
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
public class DownloadMinecraft
{
    private readonly Download downloadTool;
    private readonly VersionBasicInfo basic;
    private readonly UserVersion userInfo;
    private readonly string GameRootPath;
    private readonly string versionPath;
    private readonly string ID;
    private VersionInfomations mations;

    public readonly CancellationToken cancelToken;
    public readonly IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress;
    public int maxDownloadThreads = 24;
    public int maxSha1Threads = 24;
    public int alls = 0;
    public int dones = 0;

    /// <param Name="downloadTool">Download实例，用于下载</param>
    /// <param Name="cancelToken">取消令牌</param>
    /// <param Name="versionInfo">VersionInfomations实例，用于得到Minecraft信息</param>
    /// <param Name="versionUserInfo">由用户主持的下载信息</param>
    /// <param Name="GameRootPath">游戏基本路径，不含.minecraft</param>
    public DownloadMinecraft(
        Download downloadTool,
        UserVersion versionUserInfo,
        VersionBasicInfo basic,
        string GameRootPath,
        IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress,
        CancellationToken? cancelToken = null
        )
    {
        this.progress = progress;
        this.cancelToken = cancelToken ?? CancellationToken.None;
        this.downloadTool = downloadTool;
        this.basic = basic;
        this.userInfo = versionUserInfo;
        this.GameRootPath = GameRootPath;
        this.versionPath = Path.Combine(
            GameRootPath,"versions",versionUserInfo.VersionID);
        this.ID = versionUserInfo.VersionID;
    }
    public async Task MinecraftBasic
        (
        int maxDownloadThreads = 24,
        int maxSha1Threads = 24,
        bool IsSha1 = true,
        bool IsDownloadFabricWithAPI = true,
        bool IsAllowDownloadBetaNeoforge = false,
        bool AndJava = false,
        bool UseBMLCAPI = false
        )
    {
        this.maxDownloadThreads = maxDownloadThreads;
        this.maxSha1Threads = maxSha1Threads;
        #region 非正式下载阶段
        Interlocked.Increment(ref dones);
        progress.Report((DownProgress.Meta, alls, dones, "版本文件"));
        string versionJsonFileName = Path.Combine(versionPath, "version.json");
        if (!File.Exists(versionJsonFileName))
            await downloadTool.DownloadFile(basic.Url, versionJsonFileName, cancelToken);
        mations = new VersionInfomations(
            await File.ReadAllTextAsync(
                Path.Combine(versionJsonFileName)),GameRootPath,Init.systemType,userInfo.IsVersionIsolation);
        Task javaInstallTask = Task.CompletedTask;
        if (AndJava)
            javaInstallTask = JavaInstallTasker();
        Interlocked.Increment(ref dones);
        progress.Report((DownProgress.Meta, alls, dones, "资源文件索引"));
        var assets = mations.GetAssets();
        if (!File.Exists(assets.path))
            await downloadTool.DownloadFile(assets.url, assets.path, cancelToken);
        
        #region 统计所有需要下载的文件列表
        List<NdDowItem> AllNdListSha1 = new ();
        List<NdDowItem> LibNds = downloadTool.CheckFilesExists(mations.GetLibrarys(),cancelToken);
        List<NdDowItem> AssetsNds = downloadTool.CheckFilesExists(VersionAssetIndex.ParseAssetsIndex(await File.ReadAllTextAsync(assets.path), GameRootPath),cancelToken);
        List<NdDowItem> ModNds = null;
        NdDowItem main = new(),log4j2 = new();
        string loggingFile = mations.GetLoggingConfigPath();
        if (LibNds.Count == 0 && AssetsNds.Count == 0)
        if (!userInfo.modType.IsNeoForge && !userInfo.modType.IsFabric)
            try
            {
                Debug.WriteLine("已安装");
                progress.Report((DownProgress.Meta,0,0,"请稍后"));
                await downloadTool.CheckAllSha1(AllNdListSha1,maxSha1Threads,cancelToken);
                if(!Init.ConfigManger.config.VersionList.Contains(userInfo))
                {
                    Init.ConfigManger.config.VersionList.Add(userInfo);
                    await Init.ConfigManger.Save();
                }    
                throw new OlanException("不建议下载","您当前已经完成下载，可以转到版本列表启动游戏");
            }
            catch
            {

            }
        
        if (!File.Exists(Path.Combine(versionPath, $"{userInfo.VersionID}.jar")))
        {
            main = mations.GetMainFile();
            Interlocked.Increment(ref dones);
            AllNdListSha1.Add((NdDowItem)main);
        }
        {
            if (new Version(userInfo.VersionID) > new Version("1.7") && loggingFile != null)
            {
                log4j2 = (NdDowItem)mations.GetLoggingConfig();
                Interlocked.Increment(ref dones);
            } 
        }
        if(userInfo.modType.IsFabric)
        {
            Interlocked.Increment(ref dones);
            progress.Report((DownProgress.Meta, alls, dones, "查找Fabric相关链接"));
            const string FabricVersionJsonName = "version.fabric.json";
            string modVersionPath = Path.Combine(versionPath, FabricVersionJsonName);
            if (!File.Exists(modVersionPath))
                await downloadTool.DownloadFile(
                  $"https://meta.fabricmc.net/v2/versions/loader/{ID}/", modVersionPath,cancelToken);
            // 获取 Fabric 加载器下载信息
            ModNds = downloadTool.CheckFilesExists(new ModLoader.fabric.FabricVJParser(
                Path.Combine(modVersionPath), GameRootPath
                ).GetLibraries(),cancelToken);
            // 获取Fabric API下载信息
            if (IsDownloadFabricWithAPI)
            {
                var a = new GetModrinth(
                 "fabric-api", ID,
                  !userInfo.IsVersionIsolation
                  ? Path.Combine(GameRootPath, "mods")
                  : Path.Combine(versionPath, "mods"));
                await a.Init();
                var modApi = (NdDowItem)a!.GetDownloadInfos();
                if(!File.Exists( modApi.path))
                    ModNds.Add(modApi);
            }
        }
        string neoforgeTempDBFilePath = null;
        NeoForgeInstallTasker neofogreITExp = null;
        if(userInfo.modType.IsNeoForge)
        {
              NeoForgeInstallTasker installTasker = new NeoForgeInstallTasker
              (
                  downloadTool,
                  Path.Combine(GameRootPath, "libraries"),
                  Path.Combine(GameRootPath, "versions", ID),
                  ID
              );
            neofogreITExp = installTasker;
            Interlocked.Increment(ref dones);
            progress.Report((DownProgress.Meta, alls, dones, "查找Neoforge相关链接"));
            // 下载依赖库和工具依赖库文件
            string neoForgeActualVersion = await new NeoForgeVersionListGetter(downloadTool.unityClient)
            // 调用Gemini写的名字贼长的方法来获取NeoForge安装程序的url
                .GetLatestSuitableNeoForgeVersionStringAsync(ID, IsAllowDownloadBetaNeoforge);
            string installerUrl = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{neoForgeActualVersion}/neoforge-{neoForgeActualVersion}-installer.jar";
            (List<NdDowItem> NdModLibs, List<NdDowItem> NdModToolsLibs, string BDFilePath) = await installTasker.StartReady(installerUrl);
            ModNds = NdModLibs;
            ModNds.AddRange(NdModToolsLibs);
            neoforgeTempDBFilePath = BDFilePath;
        }

        AllNdListSha1.AddRange(LibNds);
        AllNdListSha1.AddRange(AssetsNds);
        if (ModNds != null)
        {
            AllNdListSha1.AddRange(ModNds);
            Interlocked.Add(ref dones,ModNds.Count);
        }
        // 我也忘了当初干嘛要写这个东西，反正会导致进度报告混乱
        //Interlocked.Add(ref dones, );//LibNds.Count+AssetsNds.Count);
        Interlocked.Add(ref alls, dones+AllNdListSha1.Count);
        #endregion
        #endregion
        if(main.url != null && main.path != null)
            await DownloadClientTasker((NdDowItem)main,UseBMLCAPI);
        Task neoforgeToRunTask = Task.CompletedTask;
        if(ModNds != null)
        {
            await UnityModsInstallTasker(ModNds);
            if (userInfo.modType.IsNeoForge)
            {
                Directory.CreateDirectory(Path.Combine(GameRootPath, "libraries"));
                await javaInstallTask;
                if (neoforgeTempDBFilePath != null && neofogreITExp != null)
                    neoforgeToRunTask = NeoforgeInstallTasker(neoforgeTempDBFilePath, neofogreITExp);
                else
                    throw new OlanException("内部错误", "Neoforge安装器传入的值为Null。请尝试联系开发者，或打开https://github.com/abbcccbba/OneLauncher/issues寻求解决方案",OlanExceptionAction.Error);
            }    
        }
        if(new Version(userInfo.VersionID) > new Version("1.7") && loggingFile != null)
            await LogginInstallTasker((NdDowItem)log4j2);
        await DownloadLibrariesSupportTasker(LibNds,UseBMLCAPI);
        await DownloadAssetsSupportTasker(AssetsNds,UseBMLCAPI);
        await neoforgeToRunTask;
        await javaInstallTask;
        if (IsSha1)
        {
            progress.Report((DownProgress.Verify, alls, dones, "校验中！"));
            await downloadTool.CheckAllSha1(AllNdListSha1, maxSha1Threads, cancelToken);
        }
        progress.Report((DownProgress.Done,alls,dones,"下载完毕！"));
    }
    private int retryTimes; // 已经重试次数
    private const int MAX_DOWNLOAD_RETRIES = 3; // 每个文件最多重试3次
    private const int DOWNLOAD_TIMEOUT_SECONDS = 10; // 每次尝试的超时时间为10秒
    private async Task DownloadClientTasker(NdDowItem main, bool UseBMLCAPI)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        // 启动超时挽救线程
        _ = Task.Run(async () =>
        {
            await Task.Delay(DOWNLOAD_TIMEOUT_SECONDS, cancelToken); 
            if (cts.IsCancellationRequested) return; 

            Interlocked.Increment(ref retryTimes);
            if (retryTimes >= 3)
                return;

            await cts.CancelAsync();
            await DownloadClientTasker(main, UseBMLCAPI); 
        }, cancelToken);

        if (!UseBMLCAPI)
        {
            progress.Report((DownProgress.DownMain, alls, dones, Path.GetFileName(main.path)));
            await downloadTool.DownloadFile(main.url, main.path, cancelToken);
        }
        else
        {
            await Task.Run(async () =>
            {
                progress.Report((DownProgress.DownMain, alls, dones, Path.GetFileName(main.path)));
                string mirrorUrl = $"https://bmclapi2.bangbang93.com/version/{ID}/client";
                using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);

                try
                {
                    var originalTask = downloadTool.unityClient.GetAsync(main.url, HttpCompletionOption.ResponseHeadersRead, raceCts.Token);
                    var mirrorTask = downloadTool.unityClient.GetAsync(mirrorUrl, HttpCompletionOption.ResponseHeadersRead, raceCts.Token);

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
                    await downloadTool.DownloadFile(main.url, main.path, cts.Token);
                }
            }, cts.Token); 
        }
    }
    private async Task DownloadAssetsSupportTasker(List<NdDowItem> assets,bool UseBMLCAPI)
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
            Task o = downloadTool.unityClient.GetAsync(assets[0].url, HttpCompletionOption.ResponseHeadersRead, ctsd.Token);
            Task b = downloadTool.unityClient.GetAsync(assetsToDownload[0].url, HttpCompletionOption.ResponseHeadersRead, ctsd.Token);

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
        await downloadTool.DownloadListAsync(
            new Progress<(int completedFiles, string FilesName)>(p =>
            {
                Interlocked.Increment(ref dones);
                progress.Report((DownProgress.DownAssets, alls, dones, p.FilesName));
            }),
            assetsToDownload, maxDownloadThreads, cancelToken);
    }
    private async Task DownloadLibrariesSupportTasker(List<NdDowItem> libraries,bool UseBMLCAPI)
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
                            Task<HttpResponseMessage> originalTask = downloadTool.unityClient.GetAsync(library.url, HttpCompletionOption.ResponseHeadersRead, attemptCts.Token);
                            Task<HttpResponseMessage> mirrorTask = downloadTool.unityClient.GetAsync(mirrorUrl, HttpCompletionOption.ResponseHeadersRead, attemptCts.Token);

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
                                progress.Report((DownProgress.DownLibs, alls, dones, Path.GetFileName(library.path)));
                                return; 
                            }

                            // 如果代码执行到这里，说明两个源都失败了
                            throw new OlanException("下载失败","经过多次包括更换下载源尝试均以失败告终");
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
            await downloadTool.DownloadListAsync(
                new Progress<(int a, string b)>(p =>
                {
                    Interlocked.Increment(ref dones);
                    progress.Report((DownProgress.DownLibs, alls, dones, p.b));
                }), libraries, maxDownloadThreads, cancelToken);
        }

        // 解压原生库文件
        await Task.Run(() =>
        {
            foreach (var i in mations.NativesLibs)
            {
                Download.ExtractFile(Path.Combine(GameRootPath, "libraries", i), Path.Combine(versionPath, "natives"));
            }
        }, cancelToken);
    }
    private Task UnityModsInstallTasker(List<NdDowItem> modNds) =>
        downloadTool.DownloadListAsync(
            new Progress<(int a, string b)>(p =>
            {
                Interlocked.Increment(ref dones);
                progress.Report((DownProgress.DownAndInstModFiles, alls, dones, p.b));
            }),
            modNds, maxDownloadThreads, cancelToken); 
    private Task NeoforgeInstallTasker(string tbfm,NeoForgeInstallTasker exp)
    {
        exp.ProcessorsOutEvent += (a, b, c) =>
        {
            if (a == -1 && b == -1)
                throw new OlanException("NeoForge安装失败", $"执行处理器时报错。信息：{c}", OlanExceptionAction.Error);
            progress.Report((DownProgress.DownAndInstModFiles, alls, dones, $"[执行处理器({b}/{a})]{Environment.NewLine}{c}"));
        };
        return Task.Run(() =>
            exp.ToRunProcessors
            (
              Path.Combine(GameRootPath, "versions", ID, $"{ID}.jar"),
              Tools.IsUseOlansJreOrOssJdk(mations.GetJavaVersion()),
              tbfm,Init.systemType,cancelToken
            ));
    }
    private Task LogginInstallTasker(NdDowItem log4j2_xml)
    {
        Interlocked.Increment(ref dones);
        return downloadTool.DownloadFile(log4j2_xml.url, log4j2_xml.path, cancelToken);
    }
    private Task JavaInstallTasker() => Task.Run(async () =>
    {
        if (!Init.ConfigManger.config.AvailableJavaList.Contains(mations.GetJavaVersion()))
        {
            await AutoJavaGetter.JavaReleaser(
              mations.GetJavaVersion().ToString(),
              Path.Combine(Path.GetDirectoryName(GameRootPath), "runtimes"), Init.systemType);
            Init.ConfigManger.config.AvailableJavaList.Add(mations.GetJavaVersion());
            await Init.ConfigManger.Save();
        }
    });
}