using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders;

public struct DownloadInfo
{
    public string VersionID;
    public string GameRootPath;
    public UserVersion VersionInstallInfo;
    public GameData UserInfo;
    public Download DownloadTool;
    public VersionBasicInfo VersionDownloadInfo;
    public VersionInfomations VersionMojangInfo;
}
