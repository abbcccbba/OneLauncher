using System.Text.Json.Serialization;

namespace OneLauncher.Core.Net.ModService.Modrinth.JsonModelGet;
[JsonSerializable(typeof(ModrinthProjects))]
[JsonSerializable(typeof(ModrinthDependency))]
[JsonSerializable(typeof(ModJarDownload))]
[JsonSerializable(typeof(ModJarHashes))]
public partial class ModrinthGetJsonContext : JsonSerializerContext { }
public class ModrinthProjects
{

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("version_type")]
    public string VersionType { get; set; }

    [JsonPropertyName("files")]
    public List<ModJarDownload> Files { get; set; }
    [JsonPropertyName("dependencies")]
    public List<ModrinthDependency> Dependencies { get; set; }
}
public class ModrinthDependency
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; }
}
public class ModJarDownload
{
    [JsonPropertyName("hashes")]
    public ModJarHashes Hashes { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}

public class ModJarHashes
{
    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; }
}