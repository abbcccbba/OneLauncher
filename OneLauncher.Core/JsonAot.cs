using System; 
using System.Collections.Generic; 
using System.Text.Json.Serialization;


namespace OneLauncher.Core.Serialization
{
    // 微软验证
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.DeviceCodeResponse))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.TokenResponse))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.ErrorResponse))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.XboxLiveAuthResponse))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.XUIDisplayClaims))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.XUI))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.XSTSAuthResponse))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.XSTSErrorResponse))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.MinecraftLoginResponse))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.EntitlementsResponse))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.EntitlementItem))]
    [JsonSerializable(typeof(OneLauncher.Core.Net.msa.JsonModels.MinecraftProfileResponse))]
    // 模组获取
    [JsonSerializable(typeof(OneLauncher.Core.Modrinth.JsonModelGet.ModrinthProjects))]
    [JsonSerializable(typeof(OneLauncher.Core.Modrinth.JsonModelGet.ModrinthDependency))]
    [JsonSerializable(typeof(OneLauncher.Core.Modrinth.JsonModelGet.ModJarDownload))]
    [JsonSerializable(typeof(OneLauncher.Core.Modrinth.JsonModelGet.ModJarHashes))]
    // 模组搜索
    [JsonSerializable(typeof(OneLauncher.Core.Modrinth.JsonModelSearch.ModrinthSearch))]
    [JsonSerializable(typeof(OneLauncher.Core.Modrinth.JsonModelSearch.ModrinthProjectHit))]
    // 应用配置
    [JsonSerializable(typeof(OneLauncher.Core.AppSettings))]
    [JsonSerializable(typeof(OneLauncher.Core.AppConfig))]
    [JsonSerializable(typeof(OneLauncher.Core.UserModel))]
    [JsonSerializable(typeof(OneLauncher.Core.UserVersion))]
    [JsonSerializable(typeof(OneLauncher.Core.ModType))] 
    [JsonSerializable(typeof(OneLauncher.Core.PreferencesLaunchMode))]
    [JsonSerializable(typeof(OneLauncher.Core.ModEnum))] 
    // Neoforge
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.neoforge.JsonModels.NeoForgeVersionJson))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.neoforge.JsonModels.NeoforgeArguments))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.neoforge.JsonModels.NeoforgeLibrary))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.neoforge.JsonModels.NeoforgeDownloads))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.neoforge.JsonModels.NeoforgeArtifact))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.neoforge.JsonModels.NeoforgeSideEntry))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.neoforge.JsonModels.NeoforgeData))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.neoforge.JsonModels.NeoforgeProcessor))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.neoforge.JsonModels.NeoforgeRoot))]
    // fabric
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricRoot))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricLoader))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricIntermediary))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricLauncherMeta))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricLibraries))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricLibrary))]
    [JsonSerializable(typeof(OneLauncher.Core.ModLoader.fabric.JsonModels.FabricMainClass))]
    // 版本信息
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftVersionInfo))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftAssetIndex))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftDownloads))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftDownloadUrl))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftLibrary))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftLibraryDownloads))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MInecraftLibraryArtifact))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftArguments))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftArgument))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftRule))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.Os))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftNatives))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftLogging))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftLoggingClient))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftLoggingFile))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.JavaVersion))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftVersionList))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftLatestList))]
    [JsonSerializable(typeof(OneLauncher.Core.Minecraft.JsonModels.MinecraftAllVersionInfomations))]
    [JsonSerializable(typeof(System.Collections.Generic.List<OneLauncher.Core.Minecraft.JsonModels.MinecraftLibrary>))] 
    [JsonSerializable(typeof(System.Collections.Generic.List<OneLauncher.Core.Minecraft.JsonModels.MinecraftRule>))] 
    [JsonSerializable(typeof(System.Collections.Generic.List<object>))] 
    [JsonSerializable(typeof(System.Collections.Generic.List<string>))]
    [JsonSerializable(typeof(System.Collections.Generic.Dictionary<string, OneLauncher.Core.Minecraft.JsonModels.MInecraftLibraryArtifact>))] 
    public partial class OneLauncherJsonContext : JsonSerializerContext
    {
        
    }
}