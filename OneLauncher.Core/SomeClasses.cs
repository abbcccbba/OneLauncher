using System.Text.RegularExpressions;

namespace OneLauncher.Core;
public struct ModType
{
    public bool IsFabric { get; set; }
    public bool IsNeoForge { get; set; }
}
public enum ModEnum
{
    none,
    fabric,
    neoforge
}
public class PreferencesLaunchMode
{
    public ModEnum LaunchModType { get; set; }
    public bool IsUseDebugModeLaunch { get; set; }
}
public static class Tools
{
    public static string IsUseOlansJreOrOssJdk(int javaVersion, string basePath)
    {
        var t = Path.Combine(basePath, "JavaRuntimes", javaVersion.ToString());
        if (Init.ConfigManger.config.AvailableJavaList.Contains(javaVersion))
            return Path.Combine(t, Directory.GetDirectories(t)[0], "bin", "java");
        return "java"; // 否则默认使用系统Java 
    }
    /// <summary>
    /// 把各种奇奇怪怪的仓库坐标转换为标准路径
    /// </summary>
    public static string MavenToPath(string librariesPath, string item)
    {
        if (item.StartsWith("[") && item.EndsWith("]"))
        {
            item = item.Substring(1, item.Length - 2);
        }
        string[] parts = item.Split(':');
        // 包
        string groupId = parts[0];
        // 名
        string artifactId = parts[1];
        // 版本
        string version = parts[2];
        // 可选信息 (classifier)
        string? classifier = null;
        // 后缀名 (extension)
        string suffix = "jar"; // 默认后缀名

        if (parts.Length > 3)
        {
            string more = parts[3];
            if (more.Contains("@"))
            {
                string[] s = more.Split('@');
                classifier = s[0];
                suffix = s[1];
            }
            else
            {
                classifier = more; // 如果没有@，则整个more就是classifier
                // 此时 suffix 保持默认值 "jar"
            }
        }
        else if (version.Contains("@")) // 如果可选信息为空，但版本中包含@，例如 "org.ow2.asm:asm:9.3@jar"
        {
            string[] s = version.Split('@');
            version = s[0];
            suffix = s[1];
        }

        // 构建文件名
        string filename = artifactId + "-" + version;
        if (!string.IsNullOrEmpty(classifier))
        {
            filename += "-" + classifier;
        }
        filename += "." + suffix;

        // 构建完整路径
        string fullPath = Path.Combine(librariesPath,
                                       Path.Combine(groupId.Split('.')),
                                       artifactId,
                                       version,
                                       filename);

        return fullPath;
    }

    /// <summary>
    /// 过滤出 Minecraft 的纯粹正式版版本号（如 1.20, 1.20.6）。
    /// </summary>
    /// <param name="versions">包含所有 Minecraft 版本名称的列表。</param>
    /// <returns>只包含纯粹正式版版本号的列表。</returns>
    public static List<string> McVsFilter(List<string> versions)
    {
        // 静态编译正则表达式，匹配 1.x 或 1.x.x 格式的正式版
        Regex OfficialVersionRegex = new Regex(
            @"^1\.[0-9]{1,2}(?:\.[0-9]{1,2})?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        return versions
            .Where(version => OfficialVersionRegex.IsMatch(version))
            .ToList();
    }
}

/// <summary>
/// 描述单个下载项
/// </summary>
/// 
public struct NdDowItem
{
    /// <param ID="Url">下载地址</param>
    /// <param ID="Sha1">SHA1校验码</param>
    /// <param ID="Path">保存地址（含文件名）</param>
    /// <param name="Size">文件大小（单位字节）</param>
    public NdDowItem(string Url, string Path, int Size, string? Sha1 = null)
    {
        this.url = Url;
        this.path = Path;
        if (Sha1 != null)
            this.sha1 = Sha1;
    }
    public string url;
    public string path;
    public int size;
    public string? sha1;
}
public class UserVersion
{
    public string VersionID { get; set; }
    public ModType modType { get; set; }
    public bool IsVersionIsolation { get; set; }
    public DateTime AddTime { get; set; }
    public PreferencesLaunchMode preferencesLaunchMode { get; set; }
    public override string ToString()
    {
        return VersionID;
    }
}
public class VersionBasicInfo
{
    /// <param ID="name">版本标识符</param>
    /// <param ID="type">版本类型</param>
    /// <param ID="url">版本文件下载地址</param>
    /// <param ID="time">版本发布时间</param>
    public VersionBasicInfo(string ID, string type, DateTime time, string url)
    {
        this.ID = ID;
        this.type = type;
        this.time = time;
        this.Url = url;
    }
    // 如果不重写该方法 AutoCompleteBox 会报错
    public override string ToString()
    {
        return ID.ToString();
    }
    public string ID { get; set; }
    public string type { get; set; }
    public DateTime time { get; set; }
    public string Url { get; set; }
}
public struct UserModel
{
    /// <param name="accessToken">访问令牌</param>
    /// <param name="refreshToken">刷新令牌</param>
    public UserModel(string Name, Guid uuid, string? accessToken = null, string? refreshToken = null)
    {
        if (accessToken == null)
        {
            this.accessToken = "0000-0000-0000-0000";
            this.refreshToken = "0000-0000-0000-0000";
            this.IsMsaUser = false;
        }
        else
        {
            this.IsMsaUser = true;
            this.accessToken = accessToken;
            this.refreshToken = refreshToken ?? string.Empty;
        }
        this.AuthTime = DateTime.UtcNow;
        this.Name = Name;
        this.uuid = uuid;
    }
    public override string ToString()
    {
        return (IsMsaUser ? "正版登入" : "离线登入");
    }

    public string Name { get; set; }
    public Guid uuid { get; set; }
    public string? accessToken { get; set; }
    public bool IsMsaUser { get; set; }


    public string? refreshToken { get; set; }
    public DateTime? AuthTime { get; set; }
}
public enum SystemType
{
    windows,
    osx,
    linux
}
