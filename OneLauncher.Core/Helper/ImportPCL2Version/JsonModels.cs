using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.Compatible.ImportPCL2Version;
[JsonSerializable(typeof(PCL2VersionJsonModels))]
public partial class PCL2VersionJsonContent : JsonSerializerContext { }
public class PCL2VersionJsonModels
{
    [JsonPropertyName("clientVersion")]
    public string ClientVersionID { get; set; }
    [JsonPropertyName("mainClass")]
    public string MainClass {  get; set; }
    [JsonPropertyName("id")]
    public string UserCustomName { get; set; }
}
