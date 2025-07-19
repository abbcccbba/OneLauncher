using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders.DownloadSources;
internal class MojangSourceDownloadUrlGetter : IDownloadSourceUrlProvider
{
    // 官方源无需变动
    public NdDowItem GetClientMainFile(NdDowItem basic)
        => basic;
    public IEnumerable<NdDowItem> GetAssetsFiles(IEnumerable<NdDowItem> basic)
        => basic;
    public IEnumerable<NdDowItem> GetLibrariesFiles(IEnumerable<NdDowItem> basic)   
        => basic;
}
