using OneLauncher.Core.Helper;
using OneLauncher.Core.Mod.ModLoader.forgeseries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders.Sources;

internal class ForgeSeriesProvider : IConcreteProviders
{
    private readonly DownloadMinecraft downloadInfo;
    private readonly ForgeSeriesInstallTasker installTasker;
    public ForgeSeriesProvider(DownloadMinecraft downloadInfo)
    {
        this.downloadInfo = downloadInfo;                                   
        installTasker = new ForgeSeriesInstallTasker(
            downloadInfo.downloadTool,
            Path.Combine(downloadInfo.GameRootPath, "libraries"),
            downloadInfo.GameRootPath
            );
    }
    public async Task<List<NdDowItem>> GetDownloadInfo()
    {
        (List<NdDowItem> NdModLibs, List<NdDowItem> NdModToolsLibs, string BDFilePath) =
                await installTasker.StartReadyAsync(
                    // 获取安装器url
                    await new ForgeVersionListGetter(downloadInfo.downloadTool.unityClient)
                    .GetInstallerUrlAsync(
                        downloadInfo.userInfo.modType == ModEnum.forge ? true : false,
                        downloadInfo.userInfo.VersionID, IsAllowDownloadBetaNeoforge, IsUseRecommendedToInstallForge
                        )
                    , (
                    userInfo.modType == ModEnum.forge ? "forge" : "neoforge"
                    ), ID);
        List<NdDowItem> result = new List<NdDowItem>();
    }
}
