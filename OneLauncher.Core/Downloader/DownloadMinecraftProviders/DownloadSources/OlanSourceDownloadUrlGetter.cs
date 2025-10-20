using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders.DownloadSources;
internal class OlanSourceDownloadUrlGetter : IDownloadSourceUrlProvider
{
    const string OLON_SOURCE_BASE_URL = "http://110.42.59.70:8840/";
    public NdDowItem GetClientMainFile(NdDowItem basic)
    {
        return new NdDowItem(
            basic.url.Replace("https://piston-data.mojang.com/",OLON_SOURCE_BASE_URL),
            basic.path, 
            basic.size, 
            basic.sha1);
    }

    public IEnumerable<NdDowItem> GetAssetsFiles(IEnumerable<NdDowItem> basic)
    {
#if true
        return basic;
#endif
        return basic.Select(x => new NdDowItem(
            x.url.Replace("https://resources.download.minecraft.net/", OLON_SOURCE_BASE_URL),
            x.path, x.size, x.sha1
        ));
    }

    public IEnumerable<NdDowItem> GetLibrariesFiles(IEnumerable<NdDowItem> basic)
    {
        return basic.Select(x => new NdDowItem(
            x.url.Replace("https://resources.download.minecraft.net/", OLON_SOURCE_BASE_URL),
            x.path, x.size, x.sha1
        ));
    }
}
