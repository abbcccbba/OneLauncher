using OneLauncher.Core.Modrinth.JsonModelGet;
using OneLauncher.Core.Serialization;
using System.Diagnostics;
using System.Text.Json;

namespace OneLauncher.Core.Modrinth;

public class GetModrinth
{
    public ModrinthProjects info;
    public List<ModrinthProjects> dependencies;
    private readonly string modPath;
    private readonly string ModID;
    private readonly string version;
    public GetModrinth(string ModID, string version, string modPath)
    {
        this.modPath = modPath;
        this.ModID = ModID;
        this.version = version;
    }
    public async Task Init()
    {
        using (HttpClient client = new HttpClient())
        {
            var Url = $"https://api.modrinth.com/v2/project/{ModID}/version?game_versions=[\"{version}\"]&loaders=[\"fabric\"]";
            Debug.WriteLine(Url);
            HttpResponseMessage response = await client.GetAsync(Url);
            response.EnsureSuccessStatusCode();

            try
            {
                using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
                {
                    JsonElement firstElement = document.RootElement[0];
                    info = JsonSerializer.Deserialize<ModrinthProjects>(firstElement.GetRawText(),OneLauncherJsonContext.Default.ModrinthProjects);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载或解析 Modrinth 主模组版本时出错: {ex.Message}");
                info = null;
                return;
            }

            if (info == null || info.Dependencies == null || info.Dependencies.Count == 0)
                return;

            this.dependencies = new List<ModrinthProjects>();
            foreach (var item in info.Dependencies)
            {
                var DUrl = $"https://api.modrinth.com/v2/project/{item.ProjectId}/version?game_versions=[\"{version}\"]&loaders=[\"fabric\"]";
                Debug.WriteLine(DUrl);
                HttpResponseMessage Dresponse = await client.GetAsync(DUrl);
                Dresponse.EnsureSuccessStatusCode();

                try
                {
                    // **依赖模组版本处理：只获取第一个**
                    using (JsonDocument dDocument = JsonDocument.Parse(await Dresponse.Content.ReadAsStringAsync()))
                    {
                        JsonElement firstDependencyElement = dDocument.RootElement[0];
                        ModrinthProjects dependencyProject = JsonSerializer.Deserialize<ModrinthProjects>(firstDependencyElement.GetRawText())
                            ?? throw new InvalidOperationException("解析 Modrinth 依赖模组第一个版本失败。");
                        this.dependencies.Add(dependencyProject);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"加载或解析 Modrinth 依赖模组版本时出错 (ID: {item.ProjectId}): {ex.Message}");
                }
            }
        }
    }
    public NdDowItem? GetDownloadInfos()
    {
        if (info == null)
            return null;
        return new NdDowItem(
            Url: info.Files[0].Url,
            Path: Path.Combine(modPath, info.Files[0].Filename),
            Size: info.Files[0].Size,
            Sha1: info.Files[0].Hashes.Sha1);
    }
    public List<NdDowItem> GetDependenciesInfos()
    {
        List<NdDowItem> items = new List<NdDowItem>();
        foreach (var item in dependencies)
        {
            items.Add(new NdDowItem(
                Url: item.Files[0].Url,
                Path: Path.Combine(modPath, item.Files[0].Filename),
                Size: item.Files[0].Size,
                Sha1: item.Files[0].Hashes.Sha1));
        }
        return items;
    }
}
