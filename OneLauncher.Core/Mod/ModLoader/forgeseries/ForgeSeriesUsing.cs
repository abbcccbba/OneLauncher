using System.Text;
using System.Text.Json;
using OneLauncher.Core.Global;
namespace OneLauncher.Core.Mod.ModLoader.forgeseries;
public class ForgeSeriesUsing
{
    public ForgeSeriesVersionJson info;
    public async Task Init(string basePath, string version,bool isForge)
    {
        string jsonPath = Path.Combine(basePath, "versions", version, $"version.{(isForge ? "forge" : "neoforge")}.json");
        using var fs = new FileStream(jsonPath,FileMode.Open,FileAccess.Read,FileShare.None,0,true);
        info = await JsonSerializer.DeserializeAsync(fs, ForgeSeriesJsonContext.Default.ForgeSeriesVersionJson)
            ?? throw new OlanException("无法解析","在解析Neoforge文本时出现错误");
    }
    /// <summary>
    /// 获取当前Forge/NeoForge的依赖库列表。
    /// </summary>
    /// <param name="LibBasePath">.minecraft/libraries 路径</param>
    /// <returns>一个以 "groupId:artifactId" 为键，库文件完整路径为值的字典。</returns>
    public Dictionary<string, string> GetLibrariesForLaunch(string LibBasePath)
    {
        return info.Libraries
            .Where(item => item?.Downloads?.Artifact != null)
            .ToDictionary(
                // 关键点：Key selector lambda函数，明确指定如何从item.Name生成Key
                item => {
                    var parts = item.Name.Split(':');
                    return $"{parts[0]}:{parts[1]}"; // groupId:artifactId
                },
                item => Path.Combine(LibBasePath, "libraries", Path.Combine(item.Downloads.Artifact.Path.Split('/'))),
                StringComparer.OrdinalIgnoreCase // Key冲突时，保留后者
            );
    }
}
