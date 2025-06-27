//using OneLauncher.Core.ModLoader.forgeseries.JsonModels;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.Json.Serialization;
//using System.Threading.Tasks;

//namespace OneLauncher.Core.Mod.ModLoader.forgeseries;

//[JsonSerializable(typeof(ForgeInstallProfile))]
//[JsonSerializable(typeof(ForgeData))]
//[JsonSerializable(typeof(NeoforgeSideEntry))]
//// 下面是Neoforge JsonModels的
//[JsonSerializable(typeof(NeoForgeVersionJson))]
//[JsonSerializable(typeof(NeoforgeArguments))]
//[JsonSerializable(typeof(ForgeSeriesLibrary))]
//[JsonSerializable(typeof(ForgeSeriesDownloads))]
//[JsonSerializable(typeof(ForgeSeriesArtifact))]
//[JsonSerializable(typeof(NeoforgeSideEntry))]
//[JsonSerializable(typeof(NeoforgeData))]
//[JsonSerializable(typeof(NeoforgeProcessor))]
//[JsonSerializable(typeof(ForgeSeriesInstallProfileRoot))]
//public partial class ForgeJsonContext : JsonSerializerContext { }
//public class ForgeInstallProfile
//{
//    [JsonPropertyName("data")]
//    public ForgeData Data { get; set; }

//    [JsonPropertyName("processors")]
//    public List<NeoforgeProcessor> Processors { get; set; } // 复用你的模型

//    [JsonPropertyName("libraries")]
//    public List<ForgeSeriesLibrary> Libraries { get; set; } // 复用你的模型
//}

//public class ForgeData
//{
//    [JsonExtensionData]
//    public Dictionary<string, object> AdditionalData { get; set; }
//}
