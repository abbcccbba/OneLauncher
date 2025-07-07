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
    /// 获取当前NeoForge的依赖库列表
    /// </summary>
    public List<(string name, string path)> GetLibrariesForLaunch(string LibBasePath)
    {
        return info.Libraries.Select(
            item => (
                item.Name,
                Path.Combine(LibBasePath, "libraries",
                             Path.Combine(item.Downloads.Artifact.Path.Split('/')))
            )
        ).ToList();
    }
}
