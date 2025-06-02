using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.ModLoader.neoforge.JsonModels;
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
// 下面是安装文件部分
public class SideEntry
{
    [JsonPropertyName("client")]
    public string Client { get; set; }
}
public class Data
{
    [JsonPropertyName("MC_SLIM")]
    public SideEntry MCSlim { get; set; }

    [JsonPropertyName("MC_UNPACKED")]
    public SideEntry MCUnpacked { get; set; }

    [JsonPropertyName("MERGED_MAPPINGS")]
    public SideEntry MergedMappings { get; set; }

    [JsonPropertyName("BINPATCH")]
    public SideEntry Binpatch { get; set; }

    [JsonPropertyName("MCP_VERSION")]
    public SideEntry MCPVersion { get; set; }

    [JsonPropertyName("MAPPINGS")]
    public SideEntry Mappings { get; set; }

    [JsonPropertyName("MC_EXTRA")]
    public SideEntry MCExtra { get; set; }

    [JsonPropertyName("MOJMAPS")]
    public SideEntry Mojmaps { get; set; }

    [JsonPropertyName("PATCHED")]
    public SideEntry Patched { get; set; }

    [JsonPropertyName("MC_SRG")]
    public SideEntry MCSRG { get; set; }
}
public class Processor
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
public class Root
{
    [JsonPropertyName("data")]
    public Data Data { get; set; }

    [JsonPropertyName("processors")]
    public List<Processor> Processors { get; set; }

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; }
}