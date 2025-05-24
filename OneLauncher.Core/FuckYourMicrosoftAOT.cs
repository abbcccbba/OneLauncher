using OneLauncher.Core.fabric.JsonModel;
using OneLauncher.Core.Models;
using OneLauncher.Core.Modrinth.JsonModelGet;
using OneLauncher.Core.Modrinth.JsonModelSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core;

// --- 核心配置 ---
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(List<aVersion>))]
[JsonSerializable(typeof(aVersion))]
[JsonSerializable(typeof(List<UserModel>))]
[JsonSerializable(typeof(UserModel))]

// --- ModrinthSearch 相关的类型 ---
[JsonSerializable(typeof(ModrinthSearch))]
[JsonSerializable(typeof(List<ModrinthProjectHit>))]
[JsonSerializable(typeof(ModrinthProjectHit))]

// --- ModrinthProjects 相关的类型 ---
[JsonSerializable(typeof(ModrinthProjects))]
[JsonSerializable(typeof(List<ModrinthProjects>))] 
[JsonSerializable(typeof(Dependency))]
[JsonSerializable(typeof(List<Dependency>))]
[JsonSerializable(typeof(JarDownload))]
[JsonSerializable(typeof(List<JarDownload>))]
[JsonSerializable(typeof(Hashes))]

// --- RootFabric 相关的类型 ---
[JsonSerializable(typeof(RootFabric))]
[JsonSerializable(typeof(Loader))]
[JsonSerializable(typeof(Intermediary))]
[JsonSerializable(typeof(LauncherMeta))]
[JsonSerializable(typeof(Libraries))]
[JsonSerializable(typeof(List<fabric.JsonModel.FabricLibrary>))] 
[JsonSerializable(typeof(fabric.JsonModel.FabricLibrary))]
[JsonSerializable(typeof(List<RootFabric>))]
[JsonSerializable(typeof(MainClass))]

// --- VersionInformation 相关的类型 ---
[JsonSerializable(typeof(VersionInformation))]
[JsonSerializable(typeof(JavaVersion))]
[JsonSerializable(typeof(AssetIndex))]
[JsonSerializable(typeof(Downloads))]
[JsonSerializable(typeof(DownloadUrl))]
[JsonSerializable(typeof(LibraryDownloads))]
[JsonSerializable(typeof(LibraryArtifact))]
[JsonSerializable(typeof(Dictionary<string, LibraryArtifact>))] // LibraryDownloads.Classifiers 是 Dictionary<string, LibraryArtifact>
[JsonSerializable(typeof(Arguments))]
[JsonSerializable(typeof(Logging))]
[JsonSerializable(typeof(LoggingClient))]
[JsonSerializable(typeof(LoggingFile))]
[JsonSerializable(typeof(Argument))]
[JsonSerializable(typeof(Natives))]
[JsonSerializable(typeof(VersionInformation))]
[JsonSerializable(typeof(List<OneLauncher.Core.Models.Library>))] // <--- 关键！
[JsonSerializable(typeof(OneLauncher.Core.Models.Library))]       // <--- 确保 FabricLibrary 本身也被标记
[JsonSerializable(typeof(List<Models.Rule>))] // 确保这里的 Rule 也是 Models.Rule
[JsonSerializable(typeof(Models.Rule))]
[JsonSerializable(typeof(Models.Os))]
[JsonSerializable(typeof(Models.Natives))] // 仍然建议保留，以防万一
// 针对 Argument.Value 的特殊处理：它是一个 object，可能代表 string 或 List<string>。
// 对于 AOT，你需要告诉序列化器这些可能的具体类型。
[JsonSerializable(typeof(string))] 
[JsonSerializable(typeof(List<string>))] 

// --- VersionJsonInfo 相关的类型 ---
[JsonSerializable(typeof(VersionJsonInfo))]
[JsonSerializable(typeof(LatestList))]
[JsonSerializable(typeof(List<AllVersionInfomations>))]
[JsonSerializable(typeof(AllVersionInfomations))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
