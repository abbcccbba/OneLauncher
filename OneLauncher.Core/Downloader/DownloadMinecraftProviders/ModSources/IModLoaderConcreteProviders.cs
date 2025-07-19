using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders.ModSources;

internal interface IModLoaderConcreteProviders
{
    Task<List<NdDowItem>> GetDependencies();
    Task RunInstaller(IProgress<string> Put,CancellationToken token)
    {
        /* 某些资源可能没有，提供一个默认啥都不干的实现 */
        return Task.CompletedTask;
    }
}
