using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Minecraft.JsonModels;
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
            a = JsonSerializer.Deserialize<MinecraftVersionList>(Json,MinecraftJsonContext.Default.MinecraftVersionList);
        }
        catch (Exception ex)
        {
            throw new OlanException("意外错误","解析版本列表时遇到意外错误导致无法解析",OlanExceptionAction.Error);
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

