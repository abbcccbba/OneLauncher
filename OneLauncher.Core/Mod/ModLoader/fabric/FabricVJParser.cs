using OneLauncher.Core.Helper;
using OneLauncher.Core.ModLoader.fabric.JsonModels;
using System.Text.Json;
namespace OneLauncher.Core.Mod.ModLoader.fabric;

public class FabricVJParser
{
    public readonly FabricRoot info;
    private readonly string basePath;

    public FabricVJParser(string jsonPath, string BasePath)
    {
        basePath = BasePath;

        using (FileStream stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
        using (JsonDocument document = JsonDocument.Parse(stream))
        {
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                // 只序列化第一个对象，不要他妈的乱改不然他妈的会报错
                if (document.RootElement.GetArrayLength() > 0)
                {
                    JsonElement firstElement = document.RootElement[0];
                    info = JsonSerializer.Deserialize(firstElement.GetRawText(),FabricJsonContext.Default.FabricRoot)
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
    public List<NdDowItem> GetLibraries()
    {
        List<NdDowItem> dowItems = new List<NdDowItem>(info.LauncherMeta.Libraries.Common.Count+2);
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

        return libraries;
    }
}
