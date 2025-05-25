using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core;

public class VersionsList
{
    VersionJsonInfo a;
    private static readonly JsonSerializerOptions VersionsListJsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true, // 如果需要，可以添加
        TypeInfoResolver = AppJsonSerializerContext.Default // 关键：指定 TypeInfoResolver 为源生成器
    };
    public VersionsList(string Json)
    {
        try
        {
            // 使用带有选项的源生成器反序列化
            a = JsonSerializer.Deserialize<VersionJsonInfo>(Json, VersionsListJsonOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"解析版本列表Json时出错: {ex.Message}", ex); // 记录原始异常
        }
    }
    public List<VersionBasicInfo> GetReleaseVersionList()
    {
        List<VersionBasicInfo> a = new List<VersionBasicInfo>();
        foreach (var i in this.a.AllVersions)
        {
            if (i.Type == "release")
                a.Add(new VersionBasicInfo(i.Id, i.Type, i.Time, i.Url));
        }
        return a;
    }
}
public class VersionJsonInfo
{
    [JsonPropertyName("latest")]
    public LatestList latest { get; set; }
    [JsonPropertyName("versions")]
    public List<AllVersionInfomations> AllVersions { get; set; }
}
public class LatestList 
{
    [JsonPropertyName("release")]
    public string release { get; set; }
    [JsonPropertyName("snapshot")]
    public string snapshot { get; set; }
}
public class AllVersionInfomations 
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("releaseTime")]
    public DateTime Time { get; set; }

}
