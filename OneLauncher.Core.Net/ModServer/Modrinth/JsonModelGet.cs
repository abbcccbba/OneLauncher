using System.Text.Json.Serialization;

namespace OneLauncher.Core.Modrinth.JsonModelGet;

public class ModrinthProjects
{

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("version_type")]
    public string VersionType { get; set; }

    [JsonPropertyName("files")]
    public List<JarDownload> Files { get; set; }
    [JsonPropertyName("dependencies")]
    public List<Dependency> Dependencies { get; set; }
}
public class Dependency
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; }
}
public class JarDownload
{
    [JsonPropertyName("hashes")]
    public Hashes Hashes { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}

public class Hashes
{
    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; }
}