//using OneLauncher.Core.Helper;
//using OneLauncher.Core.Mod.ModLoader.forgeseries;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace OneLauncher.Core.Downloader;

////public partial class DownloadMinecraft
//{
//    /// <summary>
//    /// 一个封装了所有 Forge/NeoForge 下载和安装步骤的入口方法。
//    /// </summary>
//    /// <param name="isForge">为 true 时安装 Forge，为 false 时安装 NeoForge。</param>
//    /// <param name="allowBeta">对于 NeoForge，是否允许安装 Beta 版本。</param>
//    /// <param name="useRecommended">对于 Forge，是否优先安装官方推荐版。</param>
//    private async Task InstallForgeSeriesAsync(bool isForge, bool allowBeta, bool useRecommended)
//    {
//        var modType = isForge ? ForgeSeriesType.Forge : ForgeSeriesType.NeoForge;
//        string modTypeName = isForge ? "Forge" : "NeoForge";

//        // --- 1. 获取最新版本和安装器 URL ---
//        progress?.Report((DownProgress.Meta, alls, dones, $"正在获取最新的 {modTypeName} 版本..."));
//        var versionGetter = new ForgeVersionListGetter(downloadTool.unityClient);
//        string fullVersion = await versionGetter.GetLatestVersionStringAsync(modType, ID, allowBeta, useRecommended);

//        string installerUrl;
//        if (isForge)
//        {
//            installerUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{fullVersion}/forge-{fullVersion}-installer.jar";
//        }
//        else // NeoForge
//        {
//            installerUrl = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{fullVersion}/neoforge-{fullVersion}-installer.jar";
//        }

//        // --- 2. 准备安装任务器 ---
//        var installTasker = new ForgeSeriesInstallTasker(
//            downloadTool,
//            Path.Combine(GameRootPath, "libraries"),
//            GameRootPath // 确保这里是 .minecraft 根目录
//        );

//        // 监听处理器输出事件
//        installTasker.ProcessorsOutEvent += (a, b, c) =>
//        {
//            if (a == -1 && b == -1) // 这是一个我们定义的错误信号
//                throw new OlanException($"{modTypeName} 安装失败", $"执行处理器时报错: {c}", OlanExceptionAction.Error);

//            // 将处理器日志转发到主进度报告器
//            progress?.Report((DownProgress.DownAndInstModFiles, alls, dones, $"[处理器({b}/{a})] {c}"));
//        };

//        // --- 3. 调用准备阶段，获取需要下载的文件列表 ---
//        progress?.Report((DownProgress.Meta, alls, dones, $"正在解析 {modTypeName} 安装器..."));
//        var (versionLibs, installerLibs, lzmaPath) = await installTasker.StartReadyAsync(installerUrl, modTypeName, ID);

//        // --- 4. 将库文件加入总下载列表 ---
//        // 注意：你可能需要调整 AllNdListSha1 和 dones/alls 的计算逻辑
//        var modLibsToDownload = downloadTool.CheckFilesExists(versionLibs.Concat(installerLibs).ToList(), cancelToken);
//        if (modLibsToDownload.Any())
//        {
//            AllNdListSha1.AddRange(modLibsToDownload);
//            Interlocked.Add(ref alls, modLibsToDownload.Count);
//        }

//        // --- 5. 执行下载 ---
//        // 假设你有一个统一的下载方法来处理这些库文件
//        if (modLibsToDownload.Any())
//        {
//            await UnityModsInstallTasker(modLibsToDownload);
//        }

//        // --- 6. 运行处理器 ---
//        // 确保原版 Jar 已经下载完成
//        if (main.url != null)
//            await DownloadClientTasker((NdDowItem)main, UseBMLCAPI);

//        // 确保 Java 也准备好了
//        await javaInstallTask;

//        // 运行安装处理器
//        await installTasker.RunProcessorsAsync(
//            mations.GetMainJarPath(), // 获取主jar的路径
//            Tools.IsUseOlansJreOrOssJdk(mations.GetJavaVersion()), // 获取Java路径
//            lzmaPath,
//            cancelToken
//        );

//        // --- 7. 清理临时文件 ---
//        try
//        {
//            if (File.Exists(lzmaPath))
//            {
//                File.Delete(lzmaPath);
//            }
//        }
//        catch (Exception ex)
//        {
//            // 忽略清理失败，只记录一个调试信息
//            System.Diagnostics.Debug.WriteLine($"[InstallForgeSeries] 清理临时文件 {lzmaPath} 失败: {ex.Message}");
//        }
//    }
//}
