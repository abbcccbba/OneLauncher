using OneLauncher.Core.Minecraft;
using OneLauncher.Core.ModLoader.neoforge;
using OneLauncher.Core.Modrinth;
using System;
using System.Collections.Generic;
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
internal class DownloadMinecraft
{
    public readonly Download downloadTool;
    public readonly VersionInfomations mations;
    public readonly UserVersion userInfo;
    public readonly string GameRootPath;
    public readonly string versionPath;
    public readonly string ID;

    public readonly CancellationToken cancelToken;
    public readonly IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress;

    /// <param name="downloadTool">Download实例，用于下载</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="versionInfo">VersionInfomations实例，用于得到Minecraft信息</param>
    /// <param name="versionUserInfo">由用户主持的下载信息</param>
    /// <param name="GameRootPath">游戏基本路径，不含.minecraft</param>
    public DownloadMinecraft(
        Download downloadTool,
        VersionInfomations versionInfo,
        UserVersion versionUserInfo,
        string GameRootPath,
        CancellationToken? cancelToken = null
        )
    {
        this.cancelToken = cancelToken ?? CancellationToken.None;
        this.downloadTool = downloadTool;
        this.mations = versionInfo;
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
        bool IsAllowDownloadBetaNeoforge = false
        )
    {
        var assets = mations.GetAssets();
        if (File.Exists(assets.path))
            await downloadTool.DownloadFile(assets.url, assets.path);
        #region 统计所有需要下载的文件列表
        List<string> AllNdListSha1 = new List<string>();
        List<NdDowItem> LibNds = downloadTool.CheckFilesExists(mations.GetLibrarys());
        List<NdDowItem> AssetsNds = downloadTool.CheckFilesExists(VersionAssetIndex.ParseAssetsIndex(await File.ReadAllTextAsync(assets.path), GameRootPath));
        List<NdDowItem> ModNds = null;

        if(userInfo.modType.IsFabric)
        {
            const string FabricVersionJsonName = "version.fabric.json";
            string modVersionPath = Path.Combine(versionPath, FabricVersionJsonName);
            if (!File.Exists(modVersionPath))
                await downloadTool.DownloadFile(
                  $"https://meta.fabricmc.net/v2/versions/loader/{ID}/", modVersionPath);
            // 获取 Fabric 加载器下载信息
            ModNds = downloadTool.CheckFilesExists(new ModLoader.fabric.FabricVJParser
                (Path.Combine(), GameRootPath
                ).GetLibraries());
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
                if(File.Exists( modApi.path))
                    ModNds.Add(modApi);
            }
        }
        string NeoforgeTempDBFilePath;
        if(userInfo.modType.IsNeoForge)
        {
              NeoForgeInstallTasker installTasker = new NeoForgeInstallTasker
        (
              downloadTool,
              Path.Combine(GameRootPath, "libraries"),
              Path.Combine(GameRootPath, "versions", ID),
              ID
        );
            // 下载依赖库和工具依赖库文件
            string neoForgeActualVersion = await new NeoForgeVersionListGetter(downloadTool.UnityClient)
            // 调用Gemini写的名字贼长的方法来获取NeoForge安装程序的url
                .GetLatestSuitableNeoForgeVersionStringAsync(ID, IsAllowDownloadBetaNeoforge);
            string installerUrl = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{neoForgeActualVersion}/neoforge-{neoForgeActualVersion}-installer.jar";
            (List<NdDowItem> NdModLibs, List<NdDowItem> NdModToolsLibs, string BDFilePath) = await installTasker.StartReady(installerUrl);
            ModNds = NdModLibs;
            ModNds.AddRange(NdModToolsLibs);
        }
        #endregion
    }
}