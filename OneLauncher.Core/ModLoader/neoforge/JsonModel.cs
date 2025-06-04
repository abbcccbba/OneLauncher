using System.Text.Json.Serialization;

namespace OneLauncher.Core.ModLoader.neoforge.JsonModels;
public class NeoForgeVersionJson
{
    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; }

    [JsonPropertyName("arguments")]
    public NeoforgeArguments Arguments { get; set; }

    [JsonPropertyName("libraries")]
    public List<NeoforgeLibrary> Libraries { get; set; }
}
public class NeoforgeArguments
{
    [JsonPropertyName("game")]
    public List<string> Game { get; set; }

    [JsonPropertyName("jvm")]
    public List<string> Jvm { get; set; }
}
public class NeoforgeLibrary
{
    [JsonPropertyName("downloads")]
    public NeoforgeDownloads Downloads { get; set; }
}
public class NeoforgeDownloads
{
    [JsonPropertyName("artifact")]
    public NeoforgeArtifact Artifact { get; set; }
}
public class NeoforgeArtifact
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
// 下面是安装文件部分
public class NeoforgeSideEntry
{
    [JsonPropertyName("client")]
    public string Client { get; set; }
}
public class NeoforgeData
{
    [JsonPropertyName("MC_SLIM")]
    public NeoforgeSideEntry MCSlim { get; set; }

    [JsonPropertyName("MC_UNPACKED")]
    public NeoforgeSideEntry MCUnpacked { get; set; }

    [JsonPropertyName("MERGED_MAPPINGS")]
    public NeoforgeSideEntry MergedMappings { get; set; }

    [JsonPropertyName("BINPATCH")]
    public NeoforgeSideEntry Binpatch { get; set; }

    [JsonPropertyName("MCP_VERSION")]
    public NeoforgeSideEntry MCPVersion { get; set; }

    [JsonPropertyName("MAPPINGS")]
    public NeoforgeSideEntry Mappings { get; set; }

    [JsonPropertyName("MC_EXTRA")]
    public NeoforgeSideEntry MCExtra { get; set; }

    [JsonPropertyName("MOJMAPS")]
    public NeoforgeSideEntry Mojmaps { get; set; }

    [JsonPropertyName("PATCHED")]
    public NeoforgeSideEntry Patched { get; set; }

    [JsonPropertyName("MC_SRG")]
    public NeoforgeSideEntry MCSRG { get; set; }
}
public class NeoforgeProcessor
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
// 定义整个 JSON 结构的根类
public class NeoforgeRoot
{
    [JsonPropertyName("data")]
    public NeoforgeData Data { get; set; }

    [JsonPropertyName("processors")]
    public List<NeoforgeProcessor> Processors { get; set; }

    [JsonPropertyName("libraries")]
    public List<NeoforgeLibrary> Libraries { get; set; }
}