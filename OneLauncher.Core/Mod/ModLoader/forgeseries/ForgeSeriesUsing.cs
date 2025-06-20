using System.Text;
using System.Text.Json;
namespace OneLauncher.Core.Mod.ModLoader.forgeseries;
public class ForgeSeriesUsing
{
    public ForgeSeriesVersionJson info;
    public async Task Init(string basePath, string version,bool isForge)
    {
        string jsonPath = Path.Combine(basePath, "versions", version, $"version.{(isForge ? "forge" : "neoforge")}.json");
        info = await JsonSerializer.DeserializeAsync(File.OpenRead(jsonPath), ForgeSeriesJsonContext.Default.ForgeSeriesVersionJson);
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
