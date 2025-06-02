using System.Text.Json.Serialization;

namespace OneLauncher.Core.ModLoader.fabric.JsonModels;

public class RootFabric
{
    [JsonPropertyName("loader")]
    public Loader Loader { get; set; }

    [JsonPropertyName("intermediary")]
    public Intermediary Intermediary { get; set; }
    [JsonPropertyName("launcherMeta")]
    public LauncherMeta LauncherMeta { get; set; }
}
public class Loader
{
    [JsonPropertyName("maven")]
    public string DownName { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }
}

public class Intermediary
{
    [JsonPropertyName("maven")]
    public string DownName { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }
}
public class LauncherMeta
{
    [JsonPropertyName("min_java_version")]
    public int MinJavaVersion { get; set; }

    [JsonPropertyName("libraries")]
    public Libraries Libraries { get; set; }

    [JsonPropertyName("mainClass")]
    public MainClass MainClass { get; set; }
}

public class Libraries
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

public class MainClass
{
    [JsonPropertyName("client")]
    public string Client { get; set; }
}
