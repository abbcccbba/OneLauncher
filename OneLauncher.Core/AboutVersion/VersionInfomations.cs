using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using System.Linq;
using OneLauncher.Core.Models;
using System.Text;
using System.Reflection;

namespace OneLauncher.Core
{
    /// <summary>
    /// 表示Minecraft版本信息的解析器，用于解析version.json文件并提取关键信息。
    /// 支持动态解析依赖库、资源索引等。
    /// </summary>
    public class VersionInfomations
    {
        public readonly OneLauncher.Core.Models.VersionInformation info;
        public readonly string basePath;

        /// <summary>
        /// 初始化VersionInfomations实例，解析version.json字符串。
        /// </summary>
        /// <param name="json">version.json文件的字符串内容。</param>
        /// <param name="basePath">游戏存放目录路径（例如"C:/minecraft/"）。</param>
        /// <exception cref="InvalidOperationException">如果JSON解析失败或内容无效，抛出此异常。</exception>
        public VersionInfomations(string json, string basePath)
        {
            try
            {
                info = JsonSerializer.Deserialize<Models.VersionInformation>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("解析版本JSON失败");
                this.basePath = basePath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("解析版本JSON时出错", ex);
            }
        }

        public List<NdDowItem> GetLibrarys()
        {
            var libraries = new List<NdDowItem>(info.Libraries.Count);
            // 获取操作系统名称
            string osName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" :
                            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "";
            string arch = RuntimeInformation.OSArchitecture == Architecture.X64 || RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "64" : "32";

            foreach (var lib in info.Libraries)
            {
                // 检查rules
                bool allowed = false;
                if (lib.Rules != null)
                {
                    foreach (var rule in lib.Rules)
                    {
                        bool osMatch = true;
                        if (rule.Os != null)
                        {
                            if (rule.Os.Name != null)
                                osMatch = osName.Contains(rule.Os.Name, StringComparison.OrdinalIgnoreCase);
                            if (rule.Os.Arch != null)
                                osMatch = osMatch && rule.Os.Arch == arch;
                        }
                        if (osMatch)
                            allowed = rule.Action == "allow";
                    }
                }
                else
                {
                    allowed = true;
                }

                if (!allowed)
                    continue;

                // 普通库文件
                if (lib.Downloads?.Artifact != null)
                {
                    libraries.Add(new NdDowItem(
                        lib.Downloads.Artifact.Url,
                        lib.Downloads.Artifact.Sha1,
                        $"{basePath}.minecraft/libraries/{lib.Downloads.Artifact.Path}"
                    ));
                }
                // natives库文件
                if (lib.Natives != null && lib.Downloads?.Classifiers != null)
                {
                    string nativeKey = osName == "windows" && lib.Natives.Windows != null ? lib.Natives.Windows :
                                        osName == "linux" && lib.Natives.Linux != null ? lib.Natives.Linux :
                                        osName == "osx" && lib.Natives.Osx != null ? lib.Natives.Osx : null;
                    if (nativeKey != null && lib.Downloads.Classifiers.TryGetValue(nativeKey, out var classifier))
                    {
                        libraries.Add(new NdDowItem(
                            classifier.Url,
                            classifier.Sha1,
                            $"{basePath}.minecraft/libraries/{classifier.Path}"
                        ));
                    }
                }
            }

            return libraries;
        }

        /// <summary>
        /// 获取版本主文件下载地址。
        /// </summary>
        /// <param name="version">Minecraft版本号。</param>
        public NdDowItem GetMainFile(string version)
        {
            return new NdDowItem(
                info.Downloads.Client.Url,
                info.Downloads.Client.Sha1,
                $"{basePath}.minecraft/versions/{version}/{version}.jar"
            );
        }

        /// <summary>
        /// 获取版本资源索引文件下载地址。
        /// </summary>
        public NdDowItem GetAssets()
        {
            return new NdDowItem(
                info.AssetIndex.Url,
                info.AssetIndex.Sha1,
                $"{basePath}.minecraft/assets/indexes/{info.AssetIndex.Id}.json"
            );
        }

        /// <summary>
        /// 获取资源索引的版本ID。
        /// </summary>
        public string GetAssetIndexVersion()
        {
            return info.AssetIndex.Id;
        }

        /// <summary>
        /// 获取版本的主类名。
        /// </summary>
        /// <returns>主类名（例如"net.minecraft.client.main.Main"）。</returns>
        public string GetMainClass()
        {
            return info.MainClass;
        }

        /// <summary>
        /// 获取日志配置文件信息。
        /// </summary>
        public NdDowItem? GetLoggingConfig()
        {
            if (info.Logging?.Client?.File == null)
                return null;
            return new NdDowItem(
                info.Logging.Client.File.Url,
                info.Logging.Client.File.Sha1,
                $"{basePath}logConfig/{info.Logging.Client.File.Id}"
            );
        }

        /// <summary>
        /// 获取Java版本信息。
        /// </summary>
        /// <returns>Java版本信息，包含组件名和主要版本号；如果无javaVersion字段，返回null。</returns>
        public OneLauncher.Core.Models.JavaVersion? GetJavaVersion()
        {
            return info.JavaVersion;
        }
    }
}
namespace OneLauncher.Core.Models
{
    // 表示version.json的顶层结构
    public class VersionInformation
    {
        [JsonPropertyName("assetIndex")]
        public AssetIndex? AssetIndex { get; set; } // 可以为空

        [JsonPropertyName("downloads")]
        public Downloads? Downloads { get; set; } // 可以为空

        [JsonPropertyName("libraries")]
        public List<Library>? Libraries { get; set; } // 可以为空

        [JsonPropertyName("arguments")]
        public Arguments? Arguments { get; set; }

        [JsonPropertyName("minecraftArguments")]
        public string? MinecraftArguments { get; set; } // 可以为空，旧版本使用

        [JsonPropertyName("mainClass")]
        public string? MainClass { get; set; } // 可以为空

        [JsonPropertyName("logging")]
        public Logging? Logging { get; set; } // 可以为空

        [JsonPropertyName("javaVersion")]
        public JavaVersion? JavaVersion { get; set; } // 可以为空
    }

    // 表示Java版本信息
    public class JavaVersion
    {
        [JsonPropertyName("component")]
        public string? Component { get; set; } // 可以为空

        [JsonPropertyName("majorVersion")]
        public int MajorVersion { get; set; }
    }

    // 表示资源索引文件下载
    public class AssetIndex
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; } // 可以为空

        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; } // 可以为空

        [JsonPropertyName("url")]
        public string? Url { get; set; } // 可以为空
    }

    // 表示主文件下载信息
    public class Downloads
    {
        [JsonPropertyName("client")]
        public DownloadUrl? Client { get; set; } // 可以为空
    }

    // 表示主文件的下载信息和地址
    public class DownloadUrl
    {
        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; } // 可以为空

        [JsonPropertyName("url")]
        public string? Url { get; set; } // 可以为空
    }

    // 表示库信息
    public class Library
    {
        [JsonPropertyName("downloads")]
        public LibraryDownloads? Downloads { get; set; } // 可以为空

        [JsonPropertyName("name")]
        public string? Name { get; set; } // 可以为空

        [JsonPropertyName("rules")]
        public List<Rule>? Rules { get; set; } // 可以为空

        [JsonPropertyName("natives")]
        public Natives? Natives { get; set; } // 可以为空
    }

    // 表示库的下载信息
    public class LibraryDownloads
    {
        [JsonPropertyName("artifact")]
        public LibraryArtifact? Artifact { get; set; } // 可以为空

        [JsonPropertyName("classifiers")]
        public Dictionary<string, LibraryArtifact>? Classifiers { get; set; } // 可以为空
    }

    // 表示库文件的具体下载信息
    public class LibraryArtifact
    {
        [JsonPropertyName("path")]
        public string? Path { get; set; } // 可以为空

        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; } // 可以为空

        [JsonPropertyName("url")]
        public string? Url { get; set; } // 可以为空
    }
    ///*
    // 表示启动参数（jvm和game）
    public class Arguments
    {
        [JsonPropertyName("jvm")]
        [JsonConverter(typeof(JvmArgumentConverter))] // 使用自定义转换器处理混合类型
        public List<object>? Jvm { get; set; } // object 可以是 string 或 Argument

        [JsonPropertyName("game")]
        [JsonConverter(typeof(GameArgumentConverter))] // 为游戏参数列表添加的自定义转换器
        public List<object>? Game { get; set; } // object 可以是 string 或 Argument
    }

    // JVM 参数列表的自定义 JSON 转换器，处理 string 和 Argument 对象
    public class JvmArgumentConverter : JsonConverter<List<object>>
    {
        public override List<object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<object>();
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("JVM 参数应为数组");

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    list.Add(reader.GetString() ?? ""); // 处理可能的 null 字符串
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    // 反序列化为 Argument 对象
                    var arg = JsonSerializer.Deserialize<Argument>(ref reader, options);
                    if (arg != null) list.Add(arg);
                }
                else
                {
                    throw new JsonException($"JVM 参数中遇到未知 Token 类型: {reader.TokenType}");
                }
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<object> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                if (item is string str)
                    writer.WriteStringValue(str);
                else if (item is Argument arg)
                    JsonSerializer.Serialize(writer, arg, options); // 序列化为 Argument
                else
                    // 处理其他类型或抛出异常
                    throw new JsonException($"JVM 参数中遇到未知对象类型: {item?.GetType().Name}");
            }
            writer.WriteEndArray();
        }
    }
    // 游戏参数列表的自定义 JSON 转换器，处理 string 和 Argument 对象
    public class GameArgumentConverter : JsonConverter<List<object>>
    {
        public override List<object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<object>();
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("游戏参数应为数组");

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    list.Add(reader.GetString() ?? ""); // 处理可能的 null 字符串
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    // 反序列化为 Argument 对象
                    var arg = JsonSerializer.Deserialize<Argument>(ref reader, options);
                    if (arg != null) list.Add(arg);
                }
                else
                {
                    throw new JsonException($"游戏参数中遇到未知 Token 类型: {reader.TokenType}");
                }
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<object> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                if (item is string str)
                    writer.WriteStringValue(str);
                else if (item is Argument arg)
                    JsonSerializer.Serialize(writer, arg, options); // 序列化为 Argument
                else
                    // 处理其他类型或抛出异常
                    throw new JsonException($"游戏参数中遇到未知对象类型: {item?.GetType().Name}");
            }
            writer.WriteEndArray();
        }
    }
    // 表示单个启动参数，可能包含 rules
    public class Argument
    {
        [JsonPropertyName("rules")]
        public List<Rule>? Rules { get; set; } // 可以为空

        [JsonPropertyName("value")]
        [JsonConverter(typeof(ArgumentValueConverter))] // 使用自定义转换器处理 string 或 List<string>
        public object? Value { get; set; } // 值可以是 string 或 List<string>，可以为空
    }

    // 表示适用规则
    public class Rule
    {
        [JsonPropertyName("action")]
        public string? Action { get; set; } // action 可以是 "allow" 或 "disallow"，可以为空

        [JsonPropertyName("os")]
        public Os? Os { get; set; } // 操作系统要求，可以为空

        [JsonPropertyName("features")] // 特性要求
        public Features? Features { get; set; } // 特性要求，可以为空
    }

    // 表示操作系统要求
    public class Os
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; } // 操作系统名称，可以为空

        [JsonPropertyName("arch")]
        public string? Arch { get; set; } // 操作系统架构，可以为空
    }

    // 表示规则中的特性要求
    public class Features
    {
        [JsonPropertyName("is_demo_user")]
        public bool? IsDemoUser { get; set; } // 是否为 Demo 用户，可空布尔值

        [JsonPropertyName("has_custom_resolution")]
        public bool? HasCustomResolution { get; set; } // 是否使用自定义分辨率，可空布尔值

        [JsonPropertyName("has_quick_plays_support")]
        public bool? HasQuickPlaysSupport { get; set; } // 是否支持快速游戏，可空布尔值

        [JsonPropertyName("is_quick_play_singleplayer")]
        public bool? IsQuickPlaySingleplayer { get; set; } // 是否为快速游戏单人模式，可空布尔值

        [JsonPropertyName("is_quick_play_multiplayer")]
        public bool? IsQuickPlayMultiplayer { get; set; } // 是否为快速游戏多人模式，可空布尔值

        [JsonPropertyName("is_quick_play_realms")]
        public bool? IsQuickPlayRealms { get; set; } // 是否为快速游戏 Realms 模式，可空布尔值

        // 添加 version.json 中可能出现的其他特性
    } //*/


    // 表示natives库的操作系统映射
    public class Natives
    {
        [JsonPropertyName("linux")]
        public string? Linux { get; set; } // Linux 原生库分类器键，可以为空

        [JsonPropertyName("osx")]
        public string? Osx { get; set; } // macOS 原生库分类器键，可以为空

        [JsonPropertyName("windows")]
        public string? Windows { get; set; } // Windows 原生库分类器键，可以为空
    }

    // 表示日志配置信息
    public class Logging
    {
        [JsonPropertyName("client")]
        public LoggingClient? Client { get; set; } // 客户端日志配置，可以为空
    }

    // 表示客户端日志配置
    public class LoggingClient
    {
        [JsonPropertyName("argument")]
        public string? Argument { get; set; } // 日志参数字符串，可以为空

        [JsonPropertyName("file")]
        public LoggingFile? File { get; set; } // 日志配置文件信息，可以为空
    }

    // 表示日志配置文件下载地址
    public class LoggingFile
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; } // 文件 ID，可以为空

        [JsonPropertyName("sha1")]
        public string? Sha1 { get; set; } // 文件 SHA1 哈希，可以为空

        [JsonPropertyName("url")]
        public string? Url { get; set; } // 文件下载 URL，可以为空
        [JsonPropertyName("path")]
        public string? Path { get; set; } // 文件在游戏目录内的相对路径，可以为空
    }
    ///*
    // 自定义 JSON 转换器，用于处理 argument.value（string 或 List<string>）
    public class ArgumentValueConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
                return reader.GetString() ?? ""; // 处理可能的 null 字符串
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var list = new List<string>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        list.Add(reader.GetString() ?? ""); // 处理可能的 null 字符串
                    }
                    else
                    {
                        // 如果数组中包含非字符串项，根据需要处理或抛出异常
                        // 根据 Minecraft version.json 规范，value 数组中的项通常是字符串。
                        throw new JsonException($"参数值数组中遇到未知 Token 类型: {reader.TokenType}");
                    }
                }
                return list;
            }
            // 根据规范，value 可能还支持数字、布尔值等其他类型，如果遇到需要根据实际情况添加处理。
            throw new JsonException($"无效的参数值 Token 类型: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is string str)
                writer.WriteStringValue(str);
            else if (value is List<string> list)
            {
                writer.WriteStartArray();
                foreach (var item in list)
                    writer.WriteStringValue(item);
                writer.WriteEndArray();
            }
            else
            {
                // 处理其他类型或抛出异常
                throw new JsonException($"无效的参数值对象类型: {value?.GetType().Name}");
            }
        }
    } //*/
}