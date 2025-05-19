using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core.Models;
// 表示version.json的顶层结构
public class VersionInformation
{
    [JsonPropertyName("assetIndex")]
    public AssetIndex? AssetIndex { get; set; }

    [JsonPropertyName("downloads")]
    public Downloads? Downloads { get; set; }

    [JsonPropertyName("libraries")]
    public List<Library>? Libraries { get; set; }

    [JsonPropertyName("arguments")]
    public Arguments? Arguments { get; set; }
    /*
     * 旧版本用
     * 未来可优化
    [JsonPropertyName("minecraftArguments")]
    public string? MinecraftArguments { get; set; } 
    */
    [JsonPropertyName("mainClass")]
    public string? MainClass { get; set; }

    [JsonPropertyName("logging")]
    public Logging? Logging { get; set; }

    [JsonPropertyName("javaVersion")]
    public JavaVersion? JavaVersion { get; set; }
    [JsonPropertyName("id")]
    public string? ID { get; set; }
}

// 表示Java版本信息
public class JavaVersion
{
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

    [JsonPropertyName("size")]
    public int? Size { get; set; }
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
    public string? Sha1 { get; set; } 

    [JsonPropertyName("url")]
    public string? Url { get; set; }
    [JsonPropertyName("size")]
    public int? Size { get; set; }
}

// 表示单个库信息（顶层）
public class Library
{
    [JsonPropertyName("downloads")]
    public LibraryDownloads? Downloads { get; set; } // 可以为空

    [JsonPropertyName("rules")]
    public List<Rule>? Rules { get; set; } // 可以为空
}

// 表示库的下载信息
public class LibraryDownloads
{
    // 普通库文件
    [JsonPropertyName("artifact")]
    public LibraryArtifact? Artifact { get; set; } 

    [JsonPropertyName("classifiers")]
    public Dictionary<string, LibraryArtifact>? Classifiers { get; set; } 
}

// 表示普通库文件的具体下载信息
public class LibraryArtifact
{
    [JsonPropertyName("path")]
    public string? Path { get; set; } 

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; set; } 

    [JsonPropertyName("url")]
    public string? Url { get; set; }
    [JsonPropertyName("size")]
    public int? Size { get; set; } 
}
// 表示启动参数（jvm和game）
public class Arguments
{
    [JsonPropertyName("jvm")]
    [JsonConverter(typeof(JvmArgumentConverter))] // 使用自定义转换器处理混合类型
    public List<object>? Jvm { get; set; } // object 可以是 string 或 Argument
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
// 表示单个启动参数，可能包含 rules
public class Argument
{
    [JsonPropertyName("rules")]
    public List<Rule>? Rules { get; set; } // 可以为空

    [JsonPropertyName("value")]
    [JsonConverter(typeof(ArgumentValueConverter))] // 使用自定义转换器处理 string 或 List<string>
    public object? Value { get; set; } // 值可以是 string 或 List<string>，可以为空
}
// 表示规则
public class Rule
{
    [JsonPropertyName("action")]
    public string? Action { get; set; } // action 可以是 "allow" 或 "disallow"，可以为空

    [JsonPropertyName("os")]
    public Os? Os { get; set; } // 操作系统要求，可以为空
}

// 表示操作系统要求
public class Os
{
    [JsonPropertyName("name")]
    public string? Name { get; set; } // 操作系统名称，可以为空
}

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
    [JsonPropertyName("size")]
    public int? Size { get; set; } // 文件大小，可以为空
}
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
