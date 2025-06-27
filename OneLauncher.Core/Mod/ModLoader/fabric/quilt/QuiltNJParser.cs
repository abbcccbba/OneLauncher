using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.ModLoader.fabric.JsonModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core.Mod.ModLoader.fabric.quilt;
public class QuiltNJParser
{
    public readonly FabricRoot info;
    private readonly string basePath;

    private QuiltNJParser(FabricRoot json, string BasePath)
    {
        basePath = BasePath;
        info = json;
    }
    public static QuiltNJParser ParserAuto(Stream json, string basePath)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        JsonElement firstElement = document.RootElement[0];

        var info = JsonSerializer.Deserialize(firstElement.GetRawText(), FabricJsonContext.Default.FabricRoot)
            ?? throw new OlanException("内部错误", "无法解析Quilt文本");
        return new QuiltNJParser(info, basePath);

    }
    //public static FabricVJParser ParserUseVersion(Stream json, string basePath, string version)
    //{
    //    using JsonDocument document = JsonDocument.Parse(json);
    //    var element = document.RootElement;
    //    for (int i = 0; i < element.GetArrayLength(); i++)
    //    {
    //        if (element[i].GetProperty("loader").GetProperty("version").GetString() == version)
    //            return new FabricVJParser(
    //                JsonSerializer.Deserialize(element[i].GetRawText(), FabricJsonContext.Default.FabricRoot),
    //                basePath
    //                );
    //    }
    //    throw new OlanException("内部错误", "无法找到对应的Fabric版本");
    //}
    public string GetMainClass()
    {
        return info.LauncherMeta.MainClass.Client;
    }
    public int GetJavaVersion()
    {
        return info.LauncherMeta.MinJavaVersion;
    }
    public List<NdDowItem> GetLibraries()
    {
        const string defaultBaseUrl = "https://maven.quiltmc.org/repository/release/";
        List<NdDowItem> dowItems = new List<NdDowItem>(info.LauncherMeta.Libraries.Common.Count + 3);
        foreach (var item in info.LauncherMeta.Libraries.Common)
        {
            string[] parts = item.Name.Split(':');
            // 包
            string groupId = parts[0];
            // 名
            string artifactId = parts[1];
            // 版本
            string version = parts[2];
            // 后缀
            string? suffix = parts.Length > 3 ? parts[3] : null;

            // 构造 Url
            // org.ow2.asm:asm:9.8 -> org/ow2/asm/asm/9.8/asm-9.8.jar
            string urlPathSegments = Path.Combine(groupId.Replace('.', Path.DirectorySeparatorChar),
                                                  artifactId,
                                                  version,
                                                  $"{artifactId}-{version}.jar");
            string url = $"{defaultBaseUrl}{urlPathSegments.Replace('\\', '/')}"; // 确保是正斜杠

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
        // 额外添加三个特殊的
        string[] _parts;
        _parts = info.Loader.DownName.Split(':');
        string _groupId = _parts[0], _artifactId = _parts[1], _version = _parts[2];
        string _urlPathSegments = Path.Combine(_groupId.Replace('.', Path.DirectorySeparatorChar),
                                                  _artifactId,
                                                  _version,
                                                  $"{_artifactId}-{_version}.jar");
        string _url = $"{defaultBaseUrl}{_urlPathSegments.Replace('\\', '/')}"; // 确保是正斜杠
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
        _url = $"{defaultBaseUrl}{_urlPathSegments.Replace('\\', '/')}"; // 确保是正斜杠
        _fullPath = Path.Combine(basePath,
                                            "libraries",
                                            _groupId.Replace('.', Path.DirectorySeparatorChar),
                                            _artifactId,
                                            _version,
                                            $"{_artifactId}-{_version}.jar");
        dowItems.Add(new NdDowItem(_url, _fullPath, 0));

        _parts = info.QuiltHashed.DownName.Split(':');
        _groupId = _parts[0]; _artifactId = _parts[1]; _version = _parts[2];
        _urlPathSegments = Path.Combine(_groupId.Replace('.', Path.DirectorySeparatorChar),
                                                  _artifactId,
                                                  _version,
                                                  $"{_artifactId}-{_version}.jar");
        _url = $"{defaultBaseUrl}{_urlPathSegments.Replace('\\', '/')}"; // 确保是正斜杠
        _fullPath = Path.Combine(basePath,
                                            "libraries",
                                            _groupId.Replace('.', Path.DirectorySeparatorChar),
                                            _artifactId,
                                            _version,
                                            $"{_artifactId}-{_version}.jar");
        dowItems.Add(new NdDowItem(_url, _fullPath, 0));
        return dowItems;
    }
    public List<(string name, string path)> GetLibrariesForUsing()
    {
        // 初始化库列表，预分配容量以优化性能
        var libraries = new List<(string name, string path)>(info.LauncherMeta.Libraries.Common.Count + 2);

        // 处理 Common 库
        foreach (var item in info.LauncherMeta.Libraries.Common)
        {
            // 解析 Maven 坐标（格式如 org.ow2.asm:asm:9.8）
            string[] parts = item.Name.Split(':');
            if (parts.Length < 3) continue; // 跳过无效格式

            string groupId = parts[0]; // 例如 org.ow2.asm
            string artifactId = parts[1]; // 例如 asm
            string version = parts[2]; // 例如 9.8

            // 构造本地路径
            string fileName = $"{artifactId}-{version}.jar";
            string fullPath = Path.Combine(
                basePath,
                "libraries",
                groupId.Replace('.', Path.DirectorySeparatorChar),
                artifactId,
                version,
                fileName);

            libraries.Add((item.Name, fullPath));
        }

        // 处理 Loader 库
        {
            string[] parts = info.Loader.DownName.Split(':');
            if (parts.Length >= 3)
            {
                string groupId = parts[0];
                string artifactId = parts[1];
                string version = parts[2];

                string fileName = $"{artifactId}-{version}.jar";
                string fullPath = Path.Combine(
                    basePath,
                    "libraries",
                    groupId.Replace('.', Path.DirectorySeparatorChar),
                    artifactId,
                    version,
                    fileName);

                libraries.Add((info.Loader.DownName, fullPath));
            }
        }

        // 处理 Intermediary 库
        {
            string[] parts = info.Intermediary.DownName.Split(':');
            if (parts.Length >= 3)
            {
                string groupId = parts[0];
                string artifactId = parts[1];
                string version = parts[2];

                string fileName = $"{artifactId}-{version}.jar";
                string fullPath = Path.Combine(
                    basePath,
                    "libraries",
                    groupId.Replace('.', Path.DirectorySeparatorChar),
                    artifactId,
                    version,
                    fileName);

                libraries.Add((info.Intermediary.DownName, fullPath));
            }
        }
        // 处理 hasd 库
        {
            string[] parts = info.QuiltHashed.DownName.Split(':');
            if (parts.Length >= 3)
            {
                string groupId = parts[0];
                string artifactId = parts[1];
                string version = parts[2];

                string fileName = $"{artifactId}-{version}.jar";
                string fullPath = Path.Combine(
                    basePath,
                    "libraries",
                    groupId.Replace('.', Path.DirectorySeparatorChar),
                    artifactId,
                    version,
                    fileName);

                libraries.Add((info.QuiltHashed.DownName, fullPath));
            }
        }
        return libraries;
    }
}
