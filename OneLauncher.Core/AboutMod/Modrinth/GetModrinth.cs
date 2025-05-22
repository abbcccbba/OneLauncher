using OneLauncher.Core.fabric.JsonModel;
using OneLauncher.Core.Modrinth.JsonModelGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core.Modrinth;

public class GetModrinth
{
    public ModrinthProjects info;
    private readonly string modPath;
    private readonly string ModID;
    private readonly string version;
    public GetModrinth(string ModID, string version,string modPath)
    {
        this.modPath = modPath;
        this.ModID = this.ModID;
        this.version = version;
    }
    public async Task Init()
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync
                ($"https://api.modrinth.com/v2/project/{ModID}/version?game_versions=[\"{version}\"]&loaders=[\"fabric\"]");
            response.EnsureSuccessStatusCode();
            using (JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            {
                JsonElement root = document.RootElement;
                JsonElement firstObjectElement = root[0];
                info = JsonSerializer.Deserialize<ModrinthProjects>(firstObjectElement.GetRawText());
            }
        }
    }
    public NdDowItem GetDownloadInfos()
    { 
        return new NdDowItem(
            Url: info.Files[0].Url,
            Path :Path.Combine(modPath, info.Files[0].Filename),
            Size :info.Files[0].Size,
            Sha1 :info.Files[0].Hashes.Sha1);
    }
}
