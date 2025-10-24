using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Minecraft.JsonModels;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Minecraft;

public class VersionsList
{
    internal const string VersionListUrl = "https://piston-meta.mojang.com/mc/game/version_manifest.json";

    // 构造函数可以保持私有或删除，因为我们将使用静态方法
    private VersionsList() { }

    public static async Task<IEnumerable<VersionBasicInfo>> GetOrRefreshVersionListAsync()
    {
        var filePath = Path.Combine(Init.BasePath, "version_manifest.json");
        // 检查文件是否存在，以及缓存是否超过24小时
        bool cacheExists = File.Exists(filePath);
        if (!cacheExists)
            await Init.Download.DownloadFile(VersionListUrl, filePath);
        if ((DateTimeOffset.UtcNow - Init.ConfigManger.GetConfig().OlanSettings.LastVersionManifestRefreshTime).TotalHours > 24)
        {
            // 为了避免网络请求导致版本列表加载缓慢，先丢后台，下次打开就可以看到新的了
            _=Task.Run(async() =>
            {
                // 下载最新的、完整的版本清单文件
                await Init.Download.DownloadFile(VersionListUrl, filePath);

                // 成功下载后，才更新刷新时间并保存
                var newConfig = Init.ConfigManger.GetConfig().OlanSettings;
                newConfig.LastVersionManifestRefreshTime = DateTimeOffset.UtcNow;
                await Init.ConfigManger.EditSettings(newConfig);
            });
        }

        await using var fs = File.OpenRead(filePath);
        var fullManifest = await JsonSerializer.DeserializeAsync<MinecraftVersionList>(fs, MinecraftJsonContext.Default.MinecraftVersionList)
            ?? throw new OlanException("无法解析版本列表", $"反序列化文件'{filePath}'时出错，文件可能已损坏。");

        // 调用筛选方法，并更新全局的静态列表
        var releaseVersions = GetReleaseVersionList(fullManifest);
        Init.MojangVersionList = releaseVersions.ToList();

        return releaseVersions;
    }

    // 如果未来功能复杂就不能static了
    internal static IEnumerable<VersionBasicInfo> GetReleaseVersionList(MinecraftVersionList data)
    {
        return data.AllVersions
                   .Where(v => v.Type == "release")
                   .Select(v => new VersionBasicInfo(v.Id, v.Type, v.Time, v.Url));
    }
}
