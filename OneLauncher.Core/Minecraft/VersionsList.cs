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

    public static async Task<List<VersionBasicInfo>> GetOrRefreshVersionListAsync()
    {
        var filePath = Path.Combine(Init.BasePath, "version_manifest.json");
        string versionJsonContent;

        // 1. 修正刷新逻辑：检查文件是否存在，以及缓存是否超过24小时
        bool cacheExists = File.Exists(filePath);
        bool isCacheStale = !cacheExists || (DateTimeOffset.UtcNow - Init.ConfigManger.Data.LastVersionManifestRefreshTime).TotalHours > 24;

        if (isCacheStale)
        {
            try
            {
                // 下载最新的、完整的版本清单文件
                await Init.Download.DownloadFile(VersionListUrl, filePath);

                // 成功下载后，才更新刷新时间并保存
                Init.ConfigManger.Data.LastVersionManifestRefreshTime = DateTimeOffset.UtcNow;
                await Init.ConfigManger.Save();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                if(!cacheExists)
                // 如果连旧缓存都没有，那只能抛出致命错误了
                throw new OlanException("无法获取版本列表", "网络请求失败且本地无可用缓存。", OlanExceptionAction.Error, ex);
                
            }
        }
        else
        {
            Debug.WriteLine("从有效的本地缓存加载版本列表。");
        }

        // 2. 读取原始的、完整的JSON文件内容
        await using var fs = File.OpenRead(filePath);

        // 3. 在内存中反序列化和处理数据
        var fullManifest = await JsonSerializer.DeserializeAsync<MinecraftVersionList>(fs, MinecraftJsonContext.Default.MinecraftVersionList)
            ?? throw new OlanException("无法解析版本列表", $"反序列化文件'{filePath}'时出错，文件可能已损坏。");

        // 4. 调用筛选方法，并更新全局的静态列表
        var releaseVersions = GetReleaseVersionList(fullManifest);
        Init.MojangVersionList = releaseVersions;

        return releaseVersions;
    }

    // 如果未来功能复杂就不能static了
    internal static List<VersionBasicInfo> GetReleaseVersionList(MinecraftVersionList data)
    {
        return data.AllVersions
                   .Where(v => v.Type == "release")
                   .Select(v => new VersionBasicInfo(v.Id, v.Type, v.Time, v.Url))
                   .ToList();
    }
}
