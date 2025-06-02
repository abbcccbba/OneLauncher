using System.Text.Json;

namespace OneLauncher.Core.Minecraft;

public class VersionAssetIndex
{
    /// <summary>
    /// 版本资源文件解析器
    /// </summary>
    /// <param name="jsonString">资源文件索引文件内容</param>
    /// <param name="path">基本路径（不含.minecraft）</param>
    /// <returns>该版本所需的资源文件列表下载信息</returns>
    public static List<NdDowItem> ParseAssetsIndex(string jsonString, string path)
    {
        var assets = new List<NdDowItem>();
        var jsonDocument = JsonDocument.Parse(jsonString);
        var objects = jsonDocument.RootElement.GetProperty("objects");
        foreach (var property in objects.EnumerateObject())
        {
            string fileName = property.Name;
            string hash = property.Value.GetProperty("hash").GetString();
            int size = property.Value.GetProperty("size").GetInt32();

            string hashPrefix = hash.Substring(0, 2);

            assets.Add(
                new NdDowItem
                (
                    $"https://resources.download.minecraft.net/{hashPrefix}/{hash}",
                    Path.Combine(path, "assets", "objects", hashPrefix, hash)
                    , size
                    , hash
                ));
        }

        return assets;
    }
}
