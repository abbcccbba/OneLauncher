using OneLauncher.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders.Sources;

internal interface IConcreteProviders
{
    Task<List<NdDowItem>> GetDownloadInfo();
    async Task RunInstaller()
    {
        /* 某些资源可能没有，提供一个默认啥都不干的实现 */
    }
}
