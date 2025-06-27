using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders;

public class DownloadInfo
{
    private DownloadInfo() { }
    public static async Task<DownloadInfo> Create(
        string versionId,
        ModType modType,
        Download download,
        // 下面是一些下载选项
        bool isAllowToUseBetaNeoforge = false,
        bool isUseRecommendedToInstallForge = false,
        bool isDownloadFabricWithAPI = true,
        bool isDownloadWithJavaRuntime = true,
        // 下面是一些可传递可不传递的参数，不传递会自动获取
        VersionBasicInfo? versionBasic = null
        )
    {
        string gameRootPath = Init.GameRootPath;
        #region 查找一些资源
        // 创建默认实例
        ModEnum modEnum = modType.ToModEnum();
        string defaultInstanceModLoaderDisplayName = 
            modEnum == ModEnum.fabric 
            ? "fabric" 
            : modEnum == ModEnum.neoforge 
            ? "neoforge" 
            : modEnum == ModEnum.forge 
            ? "forge" 
            : "原版";
        string defaultInstanceName = $"{versionId} - {defaultInstanceModLoaderDisplayName}";
        GameData gameData = new GameData(defaultInstanceName,versionId,modEnum,null); 

        await Init.GameDataManger.AddGameDataAsync(gameData);
        _=Init.GameDataManger.SetDefaultInstanceAsync(gameData);

        // 确定下载信息
        VersionBasicInfo versionDownloadInfo = 
            versionBasic ??
            Init.MojangVersionList.FirstOrDefault(x => x.ID == versionId) 
            ?? throw new OlanException("内部错误","无法搜索到你需要下载版本的下载信息");

        // 确定版本信息
        UserVersion userVersion = new()
        { 
            VersionID = versionId,
            modType = modType,
            AddTime = DateTime.Now,
        };
        var mayInstalledVersion = Init.ConfigManger.config.VersionList.FirstOrDefault(x => x.VersionID == versionId);
        if (mayInstalledVersion == null)
            Init.ConfigManger.config.VersionList.Add(userVersion);
        else
        {
            var updatedModType = mayInstalledVersion.modType;
            if (userVersion.modType.IsFabric) updatedModType.IsFabric = true;
            if (userVersion.modType.IsNeoForge) updatedModType.IsNeoForge = true;
            if (userVersion.modType.IsForge) updatedModType.IsForge = true;
            mayInstalledVersion.modType = updatedModType; // 将修改后的整个副本赋值回去
        }
        _ = Init.ConfigManger.Save();
        #endregion
        #region 补全可能没有的资源
        VersionInfomations mations;

        var versionJsonSavePath = Path.Combine(userVersion.VersionPath, "version.json");
        
        if (!File.Exists(versionJsonSavePath))
            await download.DownloadFile(versionDownloadInfo.Url,versionJsonSavePath);
        mations = new VersionInfomations(
            await File.ReadAllTextAsync(versionJsonSavePath),
            gameRootPath
            );
        var assetsIndexFile = mations.GetAssets();
        if (!File.Exists(assetsIndexFile.path))
            await download.DownloadFile(assetsIndexFile.url,assetsIndexFile.path);
        #endregion

        return new DownloadInfo()
        {
            DownloadTool = download,
            VersionMojangInfo = mations,

            VersionInstallInfo = userVersion,
            UserInfo = gameData,
            VersionDownloadInfo = versionDownloadInfo,

            IsAllowToUseBetaNeoforge = isAllowToUseBetaNeoforge,
            IsDownloadFabricWithAPI = isDownloadFabricWithAPI,
            IsUseRecommendedToInstallForge = isUseRecommendedToInstallForge,
            AndJava = isDownloadWithJavaRuntime,
        };
    }
    #region 属性定义
    // 基本信息
    public string ID => VersionDownloadInfo.ID ?? VersionInstallInfo.VersionID ?? UserInfo.VersionId;
    public string GameRootPath => Init.GameRootPath;
    
    // 核心下载组件与配置
    public Download DownloadTool { get; init; }
    public VersionInfomations VersionMojangInfo { get; init; }

    // 用户意图与版本选择
    public UserVersion VersionInstallInfo { get; init; } 
    public GameData UserInfo { get; init; } 
    public VersionBasicInfo VersionDownloadInfo { get; init; }

    // 可选参数
    // 下面三个是预留的参数
    public string SpecifiedFabricVersion { get; init; }
    public string SpecifiedForgeVersion { get; init; }
    public string SpecifiedNeoForgeVersion { get; init; }

    public bool IsAllowToUseBetaNeoforge { get; init; }
    public bool IsUseRecommendedToInstallForge { get; init; }
    public bool IsDownloadFabricWithAPI { get; init; }
    public bool AndJava { get; init; }
    #endregion
}