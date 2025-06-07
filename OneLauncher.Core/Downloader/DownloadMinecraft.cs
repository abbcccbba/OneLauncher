using OneLauncher.Core.Minecraft;
using OneLauncher.Core.ModLoader.neoforge;
using OneLauncher.Core.Modrinth;
using OneLauncher.Core.Net.java;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    /// <param name="downloadTool">Download实例，用于下载</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="versionInfo">VersionInfomations实例，用于得到Minecraft信息</param>
    /// <param name="versionUserInfo">由用户主持的下载信息</param>
    /// <param name="GameRootPath">游戏基本路径，不含.minecraft</param>
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
        bool AndJava = false
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
        if (!File.Exists(Path.Combine(versionPath, "client.jar")))
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
            string neoForgeActualVersion = await new NeoForgeVersionListGetter(downloadTool.UnityClient)
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
            Interlocked.And(ref dones,ModNds.Count);
        }
        Interlocked.And(ref dones, LibNds.Count+AssetsNds.Count);
        Interlocked.Add(ref alls, dones+AllNdListSha1.Count);
        #endregion
        #endregion
        if(main.url != null && main.path != null)
            await DownloadClientTasker((NdDowItem)main);
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
        await await DownloadLibrariesSupportTakser(LibNds);
        await DownloadAssetsSupportTakser(AssetsNds);
        await neoforgeToRunTask;
        await javaInstallTask;
        if (IsSha1)
        {
            progress.Report((DownProgress.Verify, alls, dones, "校验中！"));
            await downloadTool.CheckAllSha1(AllNdListSha1, maxSha1Threads, cancelToken);
        }
        progress.Report((DownProgress.Done,alls,dones,"下载完毕！"));
    }
    private Task DownloadClientTasker(NdDowItem main)
    {
        Interlocked.Increment(ref dones);
        return downloadTool.DownloadFile(main.url, main.path, cancelToken);
    }
    private Task DownloadAssetsSupportTakser(List<NdDowItem> assets)
    {
        return downloadTool.DownloadListAsync(
            new Progress<(int completedFiles, string FilesName)>(p =>
            {
                Interlocked.Increment(ref dones);
                progress.Report((DownProgress.DownAssets, alls, dones, p.FilesName));
            }),
            assets, maxDownloadThreads, cancelToken);
    }
    private async Task<Task> DownloadLibrariesSupportTakser(List<NdDowItem> libraries)
    {
        await downloadTool.DownloadListAsync(
            new Progress<(int completedFiles, string FilesName)>(p =>
            {
                Interlocked.Increment(ref dones);
                progress.Report((DownProgress.DownLibs, alls, dones, p.FilesName));
            }),
            libraries, maxDownloadThreads, cancelToken);

        // 释放本地原生库文件
        return Task.Run(() =>
        {
            foreach (var i in mations.NativesLibs)
            {
                cancelToken.ThrowIfCancellationRequested(); // 检查取消请求
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
              Path.Combine(GameRootPath, "versions", ID, $"client.jar"),
              Tools.IsUseOlansJreOrOssJdk(mations.GetJavaVersion(), 
              Path.GetDirectoryName(GameRootPath)),
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
              Path.Combine(Path.GetDirectoryName(GameRootPath), "JavaRuntimes"), Init.systemType);
            Init.ConfigManger.config.AvailableJavaList.Add(mations.GetJavaVersion());
            Init.ConfigManger.Save();
        }
    });
}