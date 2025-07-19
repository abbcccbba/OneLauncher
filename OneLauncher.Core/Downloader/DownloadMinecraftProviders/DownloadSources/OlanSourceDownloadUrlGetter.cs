using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders.DownloadSources;
// 还没写完，得等搞镜像那哥么把调用方法给我
internal class OlanSourceDownloadUrlGetter : IDownloadSourceUrlProvider
{
    public string? versionId;
    public NdDowItem GetClientMainFile(NdDowItem basic)
    {
        if (string.IsNullOrEmpty(versionId))
        {
            throw new OlanException("内部错误", "下载源处理器未被提供游戏版本");
        }
        //string mirrorUrl = $"https://bmclapi2.bangbang93.com/version/{versionId}/client";
        //return new NdDowItem(mirrorUrl, basic.path, basic.size, basic.sha1);
        throw new NotImplementedException();
    }

    public IEnumerable<NdDowItem> GetAssetsFiles(IEnumerable<NdDowItem> basic)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<NdDowItem> GetLibrariesFiles(IEnumerable<NdDowItem> basic)
    {
        throw new NotImplementedException();
    }
}
