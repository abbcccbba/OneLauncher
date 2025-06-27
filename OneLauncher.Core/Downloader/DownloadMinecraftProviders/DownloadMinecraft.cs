using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.Mod.ModLoader.fabric;
using OneLauncher.Core.Mod.ModLoader.forgeseries;
using OneLauncher.Core.Net.ModService.Modrinth;


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
public partial class DownloadMinecraft
{
    public readonly Download downloadTool;
    private readonly VersionBasicInfo basic;
    public readonly UserVersion userInfo;
    private readonly GameData gameData;
    public readonly string GameRootPath;
    private readonly string versionPath;
    private readonly string ID;
    private VersionInfomations mations;

    public readonly CancellationToken cancelToken;
    public readonly IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)>? progress;
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
        GameData gameData,
        string GameRootPath,
        IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress,
        CancellationToken? cancelToken = null
        )
    {
        this.gameData = gameData;
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
        bool IsUseRecommendedToInstallForge = true,
        bool AndJava = false,
        bool UseBMLCAPI = false
        )
    {
        this.maxDownloadThreads = maxDownloadThreads;
        this.maxSha1Threads = maxSha1Threads;
        #region 非正式下载阶段
        Interlocked.Increment(ref dones);
        progress?.Report((DownProgress.Meta, alls, dones, "版本文件"));
        string versionJsonFileName = Path.Combine(versionPath, "version.json");
        if (!File.Exists(versionJsonFileName))
            await downloadTool.DownloadFile(basic.Url, versionJsonFileName, cancelToken);
        mations = new VersionInfomations(
            await File.ReadAllTextAsync(
                Path.Combine(versionJsonFileName)),GameRootPath);
        Task javaInstallTask = Task.CompletedTask;
        if (AndJava)
            javaInstallTask = JavaInstallTasker();
        Interlocked.Increment(ref dones);
        progress?.Report((DownProgress.Meta, alls, dones, "资源文件索引"));
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
        //if (LibNds.Count == 0 && AssetsNds.Count == 0)
        //这行代码干什么用的不知道，反正会导致bug
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
            progress?.Report((DownProgress.Meta, alls, dones, "查找Fabric相关链接"));
            const string FabricVersionJsonName = "version.fabric.json";
            string modVersionPath = Path.Combine(versionPath, FabricVersionJsonName);
            if (!File.Exists(modVersionPath))
                await downloadTool.DownloadFile(
                  $"https://meta.fabricmc.net/v2/versions/loader/{ID}/", modVersionPath,cancelToken);
            // 获取 Fabric 加载器下载信息
            ModNds = downloadTool.CheckFilesExists(FabricVJParser.ParserAuto(
                File.OpenRead(FabricVersionJsonName), GameRootPath
                ).GetLibraries(),cancelToken);
            // 获取Fabric API下载信息
            if (IsDownloadFabricWithAPI)
            {
                var a = new GetModrinth(
                 "fabric-api", ID,
                  Path.Combine(gameData.InstancePath,"mods"));
                await a.Init();
                var modApi = (NdDowItem)a!.GetDownloadInfos();
                if(!File.Exists( modApi.path))
                    ModNds.Add(modApi);
            }
        }
        string neoforgeTempDBFilePath = null;
        ForgeSeriesInstallTasker neofogreITExp = null;
        if(userInfo.modType.IsNeoForge || userInfo.modType.IsForge)
        {
              ForgeSeriesInstallTasker installTasker = new ForgeSeriesInstallTasker
              (
                  downloadTool,
                  Path.Combine(GameRootPath, "libraries"),
                  GameRootPath
              );
            neofogreITExp = installTasker;
            Interlocked.Increment(ref dones);
            progress?.Report((DownProgress.Meta, alls, dones, "查找Neoforge相关链接"));
            // 下载依赖库和工具依赖库文件
            
            (List<NdDowItem> NdModLibs, List<NdDowItem> NdModToolsLibs, string BDFilePath) = 
                await installTasker.StartReadyAsync(
                    // 获取安装器url
                    await new ForgeVersionListGetter(downloadTool.unityClient)
                    .GetInstallerUrlAsync(
                        userInfo.modType == ModEnum.forge ? true : false,
                        ID,IsAllowDownloadBetaNeoforge, IsUseRecommendedToInstallForge
                        )
                    ,(
                    userInfo.modType == ModEnum.forge ? "forge" : "neoforge"
                    ),ID);
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
        //我也忘了当初干嘛要写这个东西，反正会导致进度报告混乱
        //Interlocked.Add(ref dones, );//LibNds.Count+AssetsNds.Count);
        Interlocked.Add(ref alls, dones+AllNdListSha1.Count);
        #endregion
        #endregion
        if(main.url != null || main.path != null)
            await DownloadClientTasker((NdDowItem)main,UseBMLCAPI);
        Task neoforgeToRunTask = Task.CompletedTask;
        if(ModNds != null)
        {
            await UnityModsInstallTasker(downloadTool.CheckFilesExists(ModNds,cancelToken));
            if (userInfo.modType.IsNeoForge || userInfo.modType.IsForge)
            {
                Directory.CreateDirectory(Path.Combine(GameRootPath, "libraries"));
                await javaInstallTask;
                if (neoforgeTempDBFilePath != null && neofogreITExp != null)
                    neoforgeToRunTask = ForgeSeriesInstallTasker(neoforgeTempDBFilePath, neofogreITExp);
                else
                    throw new OlanException("内部错误", "安装器传入的值为Null。请尝试联系开发者，或打开https://github.com/abbcccbba/OneLauncher/issues寻求解决方案",OlanExceptionAction.Error);
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
            progress?.Report((DownProgress.Verify, alls, dones, "校验中！"));
            await downloadTool.CheckAllSha1(AllNdListSha1, maxSha1Threads, cancelToken);
        }
        progress?.Report((DownProgress.Done, alls, dones, "下载完毕！"));
    }
    
}