using System.Text.Json.Serialization;

namespace OneLauncher.Core.ModLoader.fabric.JsonModels;
[JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricRoot))]
[JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricLoader))]
[JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricIntermediary))]
[JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricLauncherMeta))]
[JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricLibraries))]
[JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricLibrary))]
[JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricMainClass))]
[JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.QuiltHashed))]
public partial class FabricJsonContext : JsonSerializerContext { }
public class FabricRoot
{
    [JsonPropertyName("loader")]
    public FabricLoader Loader { get; set; }

    [JsonPropertyName("intermediary")]
    public FabricIntermediary Intermediary { get; set; }
    [JsonPropertyName("launcherMeta")]
    public FabricLauncherMeta LauncherMeta { get; set; }
    [JsonPropertyName("hashed")]
    public QuiltHashed QuiltHashed { get; set; }
}
public class FabricLoader
{
    [JsonPropertyName("maven")]
    public string DownName { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }
}
public class FabricIntermediary
{
    [JsonPropertyName("maven")]
    public string DownName { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }
}
/* 这个东西是Quilt专属的，但因为结构跟Fabric几乎没区别就丢这里了 */
public class QuiltHashed
{
    [JsonPropertyName("maven")]
    public string DownName { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }
}
public class FabricLauncherMeta
{
    [JsonPropertyName("min_java_version")]
    public int MinJavaVersion { get; set; }

    [JsonPropertyName("libraries")]
    public FabricLibraries Libraries { get; set; }

    [JsonPropertyName("mainClass")]
    public FabricMainClass MainClass { get; set; }
}
public class FabricLibraries
{
    [JsonPropertyName("common")]
    public List<FabricLibrary> Common { get; set; }
}
public class FabricLibrary
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}
public class FabricMainClass
{
    [JsonPropertyName("client")]
    public string Client { get; set; }
}
