using OneLauncher.Core.fabric.JsonModel;
using OneLauncher.Core.Modrinth.JsonModelGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core.Modrinth;

public class GetModrinth
{
    public readonly ModrinthProjects info;
    private readonly string modPath;

    public GetModrinth(string jsonPath, string modPath)
    {
        this.modPath = modPath;

        using (FileStream stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
            using (JsonDocument document = JsonDocument.Parse(stream))
            {
                JsonElement root = document.RootElement;
                JsonElement firstObjectElement = root[0];
                info = JsonSerializer.Deserialize<ModrinthProjects>(firstObjectElement.GetRawText());
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
