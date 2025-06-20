using System.Text.Json.Serialization;

[JsonSerializable(typeof(ForgeSeriesVersionJson))]
[JsonSerializable(typeof(ForgeSeriesArguments))]
[JsonSerializable(typeof(ForgeSeriesLibrary))]
[JsonSerializable(typeof(ForgeSeriesDownloads))]
[JsonSerializable(typeof(ForgeSeriesArtifact))]
[JsonSerializable(typeof(ForgeSeriesProcessor))]
[JsonSerializable(typeof(ForgeSeriesInstallProfile))] 
[JsonSerializable(typeof(ForgeSeriesData))]       
public partial class ForgeSeriesJsonContext : JsonSerializerContext { }

// --- version.json 的模型 ---
public class ForgeSeriesVersionJson
{
    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; }
    [JsonPropertyName("arguments")]
    public ForgeSeriesArguments Arguments { get; set; }
    [JsonPropertyName("libraries")]
    public List<ForgeSeriesLibrary> Libraries { get; set; }
}

public class ForgeSeriesArguments
{
    [JsonPropertyName("game")]
    public List<string> Game { get; set; }
    [JsonPropertyName("jvm")]
    public List<string> Jvm { get; set; }
}

// --- install_profile.json 的模型 ---

public class ForgeSeriesInstallProfile
{
    [JsonPropertyName("data")]
    public ForgeSeriesData Data { get; set; } 
    [JsonPropertyName("processors")]
    public List<ForgeSeriesProcessor> Processors { get; set; }
    [JsonPropertyName("libraries")]
    public List<ForgeSeriesLibrary> Libraries { get; set; }
}

// ✨ 最关键的部分：使用 JsonExtensionData 来处理所有占位符 ✨
public class ForgeSeriesData
{
    [JsonExtensionData]
    public Dictionary<string, object> Placeholders { get; set; }
}

// --- 通用子模型 ---
public class ForgeSeriesProcessor
{
    [JsonPropertyName("sides")]
    public List<string> Sides { get; set; }
    [JsonPropertyName("jar")]
    public string Jar { get; set; }
    [JsonPropertyName("classpath")]
    public List<string> Classpath { get; set; }
    [JsonPropertyName("args")]
    public List<string> Args { get; set; }
}

public class ForgeSeriesLibrary
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("downloads")]
    public ForgeSeriesDownloads Downloads { get; set; }
}

public class ForgeSeriesDownloads
{
    [JsonPropertyName("artifact")]
    public ForgeSeriesArtifact Artifact { get; set; }
}

public class ForgeSeriesArtifact
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