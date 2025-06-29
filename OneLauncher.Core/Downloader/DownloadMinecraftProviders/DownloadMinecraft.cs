using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.Mod.ModLoader.fabric;
using OneLauncher.Core.Mod.ModLoader.forgeseries;
using OneLauncher.Core.Net.ModService.Modrinth;


namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders;
public partial class DownloadMinecraft
{
    private readonly DBManager _configManager;
    internal DownloadInfo info;

    public readonly CancellationToken cancelToken;
    public readonly IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)>? progress;
    public int maxDownloadThreads = 24;
    public int maxSha1Threads = 24;
    public int alls = 0;
    public int dones = 0;

    public DownloadMinecraft(
        DBManager configManager,
        DownloadInfo info,
        
        IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress,
        CancellationToken? cancelToken = null
        )
    {
        this._configManager = configManager;
        this.info = info;
        this.progress = progress;
        this.cancelToken = cancelToken ?? CancellationToken.None;
    }

    public async Task MinecraftBasic(
        int maxDownloadThreads = 24,
        int maxSha1Threads = 24,
        bool IsSha1 = true,
        bool useBMLCAPI = false)
    {
        this.maxDownloadThreads = Math.Clamp(maxDownloadThreads,1,256);
        this.maxSha1Threads = Math.Clamp(maxSha1Threads,1,256);

        // 1. 启动后台任务：如果需要，则开始下载Java
        Task javaInstallTask = info.AndJava ? JavaInstallTasker() : Task.CompletedTask;

        // 2. 生成下载计划
        progress?.Report((DownProgress.Meta, 0, 0, "正在生成下载计划..."));
        var plan = await CreateDownloadPlan();

        // 3. 初始化进度报告
        alls = plan.AllFilesGoVerify.Count;
        progress?.Report((DownProgress.Meta, alls, dones, "下载计划生成完毕，开始下载..."));

        // 4. 执行核心下载任务

        await DownloadClientTasker(plan.ClientMainFile, useBMLCAPI);
        // 模组加载器的下载和安装是后台任务
        Task modInstallTasker = 
            plan.ModProviders != null && info.UserInfo.ModLoader != ModEnum.none 
            ? UnityModsInstallTasker(plan.ModLoaderFiles, plan.ModProviders,javaInstallTask)
            : Task.CompletedTask;
        
        await DownloadLibrariesSupportTasker(plan.LibraryFiles, useBMLCAPI);
        await DownloadAssetsSupportTasker(plan.AssetFiles, useBMLCAPI);


        if (plan.LoggingFile.HasValue)
            await LogginInstallTasker(plan.LoggingFile.Value);

        // 5. 等待后台任务完成
        await modInstallTasker;
        await javaInstallTask;

        // 6. (可选)校验所有文件
        if (IsSha1)
        {
            progress?.Report((DownProgress.Verify, alls, alls, "校验中...")); 
            await info.DownloadTool.CheckAllSha1(plan.AllFilesGoVerify, maxSha1Threads, cancelToken);
        }

        // 7. 报告最终完成
        progress?.Report((DownProgress.Done, alls, alls, "下载完毕！"));
    }
}