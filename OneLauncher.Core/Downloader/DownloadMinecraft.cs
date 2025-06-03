using OneLauncher.Core.Minecraft;
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
    public readonly string versoinPath;
    public readonly bool IsDownloadFabricWithAPI;
    public readonly bool IsDownloadNeoforgeAllowBetaVersoin = false;
    public readonly bool IsSha1 = true;

    public readonly CancellationToken cancelToken;
    public readonly IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> progress;

    public List<string> AllSha1;
    public List<NdDowItem> NdMCLibl;
    public List<NdDowItem> NdMCAssets;

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
        bool IsDownloadFabricWithAPI = true,
        bool IsDownloadNeoforgeAllowBetaVersoin = false,
        bool IsSha1 = true,
        CancellationToken? cancelToken = null
        )
    {
        this.cancelToken = cancelToken ?? CancellationToken.None;
        
    }
    public async Task MinecraftBasic()
    {
        var assets = mations.GetAssets();
        if (File.Exists(assets.path))
            await downloadTool.DownloadFile(assets.url, assets.path);
        #region 统计所有需要下载的文件列表
        List<string> AllNdListSha1 = new List<string>();
        List<NdDowItem> LibNds = downloadTool.CheckFilesExists(mations.GetLibrarys());
        List<NdDowItem> AssetsNds = downloadTool.CheckFilesExists(VersionAssetIndex.ParseAssetsIndex(await File.ReadAllTextAsync(assets.path),GameRootPath));
        #endregion
    }
}
