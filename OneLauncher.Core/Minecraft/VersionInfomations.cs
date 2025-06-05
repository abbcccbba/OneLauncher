using OneLauncher.Core.Minecraft.JsonModels;
using System.Diagnostics;
using System.Text.Json;

namespace OneLauncher.Core.Minecraft;

/// <summary>
/// 表示Minecraft版本信息的解析器，用于解析version.json文件并提取关键信息。
/// 支持动态解析依赖库、资源索引等。
/// </summary>
public class VersionInfomations
{
    public readonly MinecraftVersionInfo info;
    public readonly string basePath;
    public readonly bool? IsVersionInsulation;
    public readonly SystemType OsType;
    public List<string> NativesLibs = new List<string>();

    /// <summary>
    /// version.json 文件解析器构造函数。
    /// </summary>
    /// <param name="json">json文件内容</param>
    /// <param name="basePath">游戏资本路径（含.minecraft）</param>
    /// <param name="OsType">系统类型</param>
    /// <param name="IsVersionInsulation">是否启用了版本隔离</param>
    /// <exception cref="InvalidOperationException">当json解析出错时</exception>
    public VersionInfomations(string json, string basePath, SystemType OsType, bool? IsVersionInsulation = false)
    {
        this.basePath = basePath;
        this.OsType = OsType;

        info = JsonSerializer.Deserialize<MinecraftVersionInfo>(json, MinecraftJsonContext.Default.MinecraftVersionInfo);


        this.IsVersionInsulation = IsVersionInsulation;
    }

    public List<NdDowItem> GetLibrarys()
    {
        var libraries = new List<NdDowItem>(info.Libraries.Count); // 提前初始化相应长度内存，避免频繁扩容影响性能

        foreach (var lib in info.Libraries)
        {
            // 检查规则
            bool allowed = false;
            // 如果包含规则
            if (lib.Rules != null)
            {
                // 判断是双规则还是单规则
                // 先处理双规则
                if (lib.Rules.Count == 2)
                {
                    // 与当前系统尝试匹配
                    // 第二条规则的 action 是 disallow
                    if (lib.Rules[1].Os.Name != OsType.ToString())
                        allowed = true;
                    else allowed = false;
                }
                if (lib.Rules.Count == 1)
                {
                    // 与当前系统尝试匹配
                    if (lib.Rules[0].Os.Name == OsType.ToString())
                        allowed = true;
                    else
                        allowed = false;
                }
            }
            // 没有规则直接下载
            else
            {
                allowed = true;
            }

            // 不合当前操作系统跳过
            if (!allowed)
                continue;

            // 普通库文件 
            if (lib.Downloads.Artifact != null)
                libraries.Add(new NdDowItem(
                    Url: lib.Downloads.Artifact.Url,
                    Path: Path.Combine(basePath, "libraries",
                        // 手动把左斜杠转换为当前系统的路径分隔符
                        Path.Combine(lib.Downloads.Artifact.Path.Split('/'))),
                    Size: (int)lib.Downloads.Artifact.Size,
                    Sha1: lib.Downloads.Artifact.Sha1

                ));

            // natives库文件
            if (lib.Downloads?.Classifiers != null)
            {
                MInecraftLibraryArtifact ta;
                try
                {
                    ta = lib.Downloads.Classifiers
                        [OsType == SystemType.windows ? "natives-windows" : OsType == SystemType.osx ? "natives-osx" : "natives-linux"];
                }
                // 某些古早版本可能会针对架构
                catch (KeyNotFoundException)
                {
                    ta = lib.Downloads.Classifiers
                    [OsType == SystemType.windows ? "natives-windows-64" : OsType == SystemType.osx ? "natives-osx-64" : "natives-linux-64"];
                }
                NativesLibs.Add(ta.Path);
                libraries.Add(new NdDowItem(
                    Url: ta.Url,
                    Sha1: ta.Sha1,
                    Size: (int)ta.Size,
                    Path: Path.Combine(basePath, "libraries", ta.Path)
                ));
            }
        }

        return libraries;
    }

    /// <summary>
    /// 获取版本主文件下载地址。
    /// </summary>
    /// <param ID="version">Minecraft版本号。</param>
    public NdDowItem GetMainFile()
    {
        return new NdDowItem(
            Url: info.Downloads.Client.Url,
            Sha1: info.Downloads.Client.Sha1,
            Size: (int)info.Downloads.Client.Size,
            Path: Path.Combine(basePath, "versions", info.ID, $"{info.ID}.jar")
        );
    }

    /// <summary>
    /// 获取版本资源索引文件下载地址。
    /// </summary>
    public NdDowItem GetAssets()
    {
        return new NdDowItem(
            Url: info.AssetIndex.Url,
            Path: Path.Combine(basePath, "Assets", "Indexes", $"{info.AssetIndex.Id}.json"),
            Size: (int)info.AssetIndex.Size,
            Sha1: info.AssetIndex.Sha1
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
            Url: info.Logging.Client.File.Url,
            Sha1: info.Logging.Client.File.Sha1,
            Size: (int)info.Logging.Client.File.Size,
            Path: Path.Combine(basePath, "versions", info.ID, info.Logging.Client.File.Id)
        );
    }
    public string? GetLoggingConfigPath()
    {
        if (info.Logging?.Client?.File == null)
            return null;
        return Path.Combine(basePath, "versions", info.ID, info.Logging.Client.File.Id);
    }

    public int GetJavaVersion()
    {
        return info.JavaVersion.MajorVersion;
    }
}

