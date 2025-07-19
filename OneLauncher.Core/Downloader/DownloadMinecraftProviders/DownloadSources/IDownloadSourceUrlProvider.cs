using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders.DownloadSources;
internal interface IDownloadSourceUrlProvider
{
    NdDowItem GetClientMainFile(NdDowItem basic);
    IEnumerable<NdDowItem> GetAssetsFiles(IEnumerable<NdDowItem> basic);
    IEnumerable<NdDowItem> GetLibrariesFiles(IEnumerable<NdDowItem> basic);
}
