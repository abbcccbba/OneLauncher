using OneLauncher.Core.fabric.JsonModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace OneLauncher.Core.fabric;

public class FabricVJParser
{
    public readonly RootFabric info;
    private readonly string basePath;
    private static readonly JsonSerializerOptions FabricJsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true, // 保持不区分大小写
        TypeInfoResolver = AppJsonSerializerContext.Default // 关键：指定 TypeInfoResolver 为源生成器
    };

    public FabricVJParser(string jsonPath, string BasePath)
    {
        this.basePath = BasePath;

        using (FileStream stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
        using (JsonDocument document = JsonDocument.Parse(stream))
        {
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                // 只序列化第一个对象，不要他妈的乱改不然他妈的会报错
                if (document.RootElement.GetArrayLength() > 0)
                {
                    JsonElement firstElement = document.RootElement[0];
                    info = JsonSerializer.Deserialize<RootFabric>(firstElement.GetRawText(), FabricJsonOptions)
                        ?? throw new InvalidOperationException("解析 Fabric JSON 的第一个对象失败。");
                }
                else
                {
                    throw new InvalidOperationException("Fabric JSON 文件是空数组。");
                }
            }
        }
    }
    public string GetMainClass()
    {
        return info.LauncherMeta.MainClass.Client;
    }
    public int GetJavaVersion()
    {
        return info.LauncherMeta.MinJavaVersion;
    }
    public List<NdDowItem> GetLibraries(bool iv = false)
    {
        List<NdDowItem> dowItems = new List<NdDowItem>(info.LauncherMeta.Libraries.Common.Count);
        foreach (var item in info.LauncherMeta.Libraries.Common)
        {
            string[] parts = item.Name.Split(':');
            // 包
            string groupId = parts[0];
            // 名
            string artifactId = parts[1];
            // 版本
            string version = parts[2];
            // 构造 Url
            // org.ow2.asm:asm:9.8 -> org/ow2/asm/asm/9.8/asm-9.8.jar
            string urlPathSegments = Path.Combine(groupId.Replace('.', Path.DirectorySeparatorChar),
                                                  artifactId,
                                                  version,
                                                  $"{artifactId}-{version}.jar");
            string url = $"https://maven.fabricmc.net/{urlPathSegments.Replace('\\', '/')}"; // 确保是正斜杠

            // 构造 Path
            // Path.Combine(basePath,"libraries", "org","ow2","asm","asm","9.8","asm-9.8.jar");
            string fullPath = Path.Combine(basePath,
                                                "libraries",
                                                groupId.Replace('.', Path.DirectorySeparatorChar),
                                                artifactId,
                                                version,
                                                $"{artifactId}-{version}.jar");
            dowItems.Add(new NdDowItem(url, fullPath, item.Size, item.Sha1));
        }
        // 额外添加两个特殊的
        string[] _parts;
        _parts = info.Loader.DownName.Split(':');
        string _groupId = _parts[0], _artifactId = _parts[1], _version = _parts[2];
        string _urlPathSegments = Path.Combine(_groupId.Replace('.', Path.DirectorySeparatorChar),
                                                  _artifactId,
                                                  _version,
                                                  $"{_artifactId}-{_version}.jar");
        string _url = $"https://maven.fabricmc.net/{_urlPathSegments.Replace('\\', '/')}"; // 确保是正斜杠
        string _fullPath = Path.Combine(basePath,
                                            "libraries",
                                            _groupId.Replace('.', Path.DirectorySeparatorChar),
                                            _artifactId,
                                            _version,
                                            $"{_artifactId}-{_version}.jar");
        dowItems.Add(new NdDowItem(_url, _fullPath, 0));

        _parts = info.Intermediary.DownName.Split(':');
        _groupId = _parts[0]; _artifactId = _parts[1]; _version = _parts[2];
        _urlPathSegments = Path.Combine(_groupId.Replace('.', Path.DirectorySeparatorChar),
                                                  _artifactId,
                                                  _version,
                                                  $"{_artifactId}-{_version}.jar");
        _url = $"https://maven.fabricmc.net/{_urlPathSegments.Replace('\\', '/')}"; // 确保是正斜杠
        _fullPath = Path.Combine(basePath,
                                            "libraries",
                                            _groupId.Replace('.', Path.DirectorySeparatorChar),
                                            _artifactId,
                                            _version,
                                            $"{_artifactId}-{_version}.jar");
        dowItems.Add(new NdDowItem(_url, _fullPath, 0));
        return dowItems;
    }
}
