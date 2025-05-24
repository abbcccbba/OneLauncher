﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Modrinth.JsonModelSearch;
public class ModrinthSearch
{
    [JsonPropertyName("hits")]
    // [JsonProperty("hits")] // Newtonsoft.Json
    public List<ModrinthProjectHit> Hits { get; set; }
}
public class ModrinthProjectHit
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; }

    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    [JsonPropertyName("date_created")]
    public DateTime DateCreated { get; set; }
}
