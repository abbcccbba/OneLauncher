using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Minecraft;

public class VersionsList
{
    MinecraftVersionList a;
    public VersionsList(string Json)
    {
        try
        {
            // 使用带有选项的源生成器反序列化
            a = JsonSerializer.Deserialize<MinecraftVersionList>(Json,OneLauncherJsonContext.Default.MinecraftVersionList);
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

