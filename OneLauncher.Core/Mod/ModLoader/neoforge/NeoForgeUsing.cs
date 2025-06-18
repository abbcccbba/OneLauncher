using OneLauncher.Core.ModLoader.neoforge.JsonModels;
using System.Text;
using System.Text.Json;
namespace OneLauncher.Core.Mod.ModLoader.neoforge;
public class NeoForgeUsing
{
    public NeoForgeVersionJson info;
    public NeoForgeUsing()
    {

    }
    public async Task Init(string basePath, string version)
    {
        string jsonPath = Path.Combine(basePath, "versions", version, $"version.neoforge.json");
        string jsonString = await File.ReadAllTextAsync(jsonPath, Encoding.UTF8);
        info = JsonSerializer.Deserialize(jsonString,NeoforgeJsonContext.Default.NeoForgeVersionJson);
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
