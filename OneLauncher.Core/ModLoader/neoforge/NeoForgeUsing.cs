using OneLauncher.Core.ModLoader.neoforge.JsonModels;
using OneLauncher.Core.Serialization;
using System.Text;
using System.Text.Json;
namespace OneLauncher.Core.ModLoader.neoforge;
public class NeoForgeUsing
{
    public NeoForgeVersionJson info;
    public NeoForgeUsing()
    {

    }
    public async Task Init(string basePath, string version)
    {
        string jsonPath = Path.Combine(basePath, "versions", version, $"{version}-neoforge.json");
        string jsonString = await File.ReadAllTextAsync(jsonPath, Encoding.UTF8);
        info = JsonSerializer.Deserialize<NeoForgeVersionJson>(jsonString,OneLauncherJsonContext.Default.NeoForgeVersionJson);
    }
    /// <summary>
    /// 获取当前NeoForge的依赖库下载列表
    /// </summary>
    public List<string> GetLibrariesForLaunch(string LibBasePath)
    {
        return info.Libraries.Select
            (item => Path.Combine(LibBasePath, "libraries",
                     Path.Combine(item.Downloads.Artifact.Path.Split('/')))
            ).ToList();
    }
}
