using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders.DownloadSources;
internal class BmlcSourceDownloadUrlGetter(string version) : IDownloadSourceUrlProvider
{
    readonly string versionId = version;
    public NdDowItem GetClientMainFile(NdDowItem basic)
    {
        string mirrorUrl = $"https://bmclapi2.bangbang93.com/version/{versionId}/client";
        return new NdDowItem(mirrorUrl, basic.path, basic.size, basic.sha1);
    }
    
    public IEnumerable<NdDowItem> GetAssetsFiles(IEnumerable<NdDowItem> basic)
    {
        return basic.Select(x => new NdDowItem(
            x.url.Replace("https://resources.download.minecraft.net/", "https://bmclapi2.bangbang93.com/assets/"),
            x.path, x.size, x.sha1
        ));
    }

    public IEnumerable<NdDowItem> GetLibrariesFiles(IEnumerable<NdDowItem> basic)
    {
        return basic.Select(x => new NdDowItem(
            x.url.Replace("https://libraries.minecraft.net/", "https://bmclapi2.bangbang93.com/maven/"),
            x.path, x.size, x.sha1
        ));
    }
}