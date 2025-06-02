using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Minecraft;

public class VersionsList
{
    VersionJsonInfo a;
    public VersionsList(string Json)
    {
        try
        {
            // 使用带有选项的源生成器反序列化
            a = JsonSerializer.Deserialize<VersionJsonInfo>(Json);
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
