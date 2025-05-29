using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.neoforge;
public class NeoForgeVersionJson
{
    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; }

    [JsonPropertyName("arguments")]
    public Arguments Arguments { get; set; }

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; }
}

public class Arguments
{
    [JsonPropertyName("game")]
    public List<string> Game { get; set; }

    [JsonPropertyName("jvm")]
    public List<string> Jvm { get; set; }
}

public class Library
{

    [JsonPropertyName("downloads")]
    public Downloads Downloads { get; set; }
}

public class Downloads
{
    [JsonPropertyName("artifact")]
    public Artifact Artifact { get; set; }
}

public class Artifact
{
    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }
}
