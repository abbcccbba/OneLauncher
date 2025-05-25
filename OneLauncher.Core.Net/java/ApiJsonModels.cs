using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Net.java;

public class Release
{
    [JsonPropertyName("binaries")]
    public List<Binary> Binaries { get; set; }
}

public class Binary
{
    [JsonPropertyName("package")]
    public Package Package { get; set; }
}

public class Package
{
    [JsonPropertyName("link")]
    public string Link { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
