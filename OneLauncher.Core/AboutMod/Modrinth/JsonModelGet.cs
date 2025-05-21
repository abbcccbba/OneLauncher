using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Modrinth.JsonModelGet;

public class ModrinthProjects
{

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("version_number")]
    public string VersionNumber { get; set; }

    [JsonPropertyName("date_published")]
    public DateTime DatePublished { get; set; }

    [JsonPropertyName("version_type")]
    public string VersionType { get; set; }

    [JsonPropertyName("files")]
    public List<JarDownload> Files { get; set; }
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