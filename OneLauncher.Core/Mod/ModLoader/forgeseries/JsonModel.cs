using System.Text.Json.Serialization;
using static OneLauncher.Core.Mod.ModLoader.forgeseries.ForgeVersionListGetter;

[JsonSerializable(typeof(ForgeSeriesVersionJson))]
[JsonSerializable(typeof(ForgeSeriesArguments))]
[JsonSerializable(typeof(ForgeSeriesLibrary))]
[JsonSerializable(typeof(ForgeSeriesDownloads))]
[JsonSerializable(typeof(ForgeSeriesArtifact))]
[JsonSerializable(typeof(ForgeSeriesProcessor))]
[JsonSerializable(typeof(ForgeSeriesInstallProfile))] 
[JsonSerializable(typeof(ForgeSeriesData))]
[JsonSerializable(typeof(ForgePromotionData))]
[JsonSerializable(typeof(ForgeVersionInfo))]
[JsonSerializable(typeof(NeoForgeVersionInfo))]
public partial class ForgeSeriesJsonContext : JsonSerializerContext { }

public class ForgePromotionData
{
    [JsonPropertyName("promos")]
    public Dictionary<string, string> Promos { get; set; }
}

public class ForgeVersionInfo : IComparable<ForgeVersionInfo>
{
    public string FullVersionString { get; }
    public string MinecraftVersion { get; }
    public Version ForgeVersion { get; }

    public ForgeVersionInfo(string versionStr)
    {
        FullVersionString = versionStr ?? throw new ArgumentNullException(nameof(versionStr));
        int separatorIndex = versionStr.IndexOf('-');
        if (separatorIndex <= 0) throw new ArgumentException($"Invalid Forge version format: {versionStr}");

        MinecraftVersion = versionStr.Substring(0, separatorIndex);
        string forgePart = versionStr.Substring(separatorIndex + 1);

        if (!Version.TryParse(forgePart.Split('-')[0], out var parsedVersion))
            throw new ArgumentException($"Cannot parse Forge version part: {forgePart}");
        ForgeVersion = parsedVersion;
    }

    public int CompareTo(ForgeVersionInfo other) => other == null ? 1 : ForgeVersion.CompareTo(other.ForgeVersion);
    public override string ToString() => FullVersionString;
}
public class NeoForgeVersionInfo : IComparable<NeoForgeVersionInfo>
{
    public string FullVersionString { get; }
    public Version ParsedNumericVersion { get; }
    public bool IsBeta { get; }

    public NeoForgeVersionInfo(string versionStr)
    {
        FullVersionString = versionStr ?? throw new ArgumentNullException(nameof(versionStr));
        string numericPart = versionStr;
        if (versionStr.Contains("-"))
        {
            numericPart = versionStr.Split('-')[0];
            IsBeta = versionStr.ToLowerInvariant().Contains("beta");
        }
        if (!Version.TryParse(numericPart, out var parsedVersion))
            throw new ArgumentException($"Cannot parse NeoForge version part: {numericPart}");
        ParsedNumericVersion = parsedVersion;
    }

    public int CompareTo(NeoForgeVersionInfo other)
    {
        if (other == null) return 1;
        int numericCompare = ParsedNumericVersion.CompareTo(other.ParsedNumericVersion);
        if (numericCompare != 0) return numericCompare;
        return IsBeta == other.IsBeta ? 0 : (IsBeta ? -1 : 1); // 稳定版 (非Beta) 优先
    }
    public override string ToString() => FullVersionString;
}
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