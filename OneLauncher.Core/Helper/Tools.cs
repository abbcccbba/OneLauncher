using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper;
public static class Tools
{
    public static async Task<IAccount?> UseAccountIDToFind(string accountID)
    {
        return (await Init.MMA.GetCachedAccounts())
            .FirstOrDefault(a => a.HomeAccountId.Identifier == accountID);
    }
    public static void OpenFolder(string folderPath)
    {
        var processOpenInfo = new ProcessStartInfo()
        {
            Arguments = $"\"{folderPath}\"",
            UseShellExecute = true
        };
        Directory.CreateDirectory(folderPath);
        try
        {
            switch (Init.systemType)
            {
                case SystemType.windows:
                    processOpenInfo.FileName = "explorer.exe";
                    break;
                case SystemType.osx:
                    processOpenInfo.FileName = "open";
                    break;
                case SystemType.linux:
                    processOpenInfo.FileName = "xdg-open";
                    break;
            }
            Process.Start(processOpenInfo);
        }
        catch (Exception ex)
        {
            throw new OlanException(
                "无法打开文件夹",
                "无法执行启动操作",
                OlanExceptionAction.Error);
        }
    }
    public static string IsUseOlansJreOrOssJdk(int javaVersion, string basePath)
    {
        var t = Path.Combine(basePath, "JavaRuntimes", javaVersion.ToString());
        if (Init.ConfigManger.config.AvailableJavaList.Contains(javaVersion))
            return Init.systemType == SystemType.osx
                ? Path.Combine(t, Directory.GetDirectories(t)[0], "Contents", "Home", "bin", "java")
                : Path.Combine(t, Directory.GetDirectories(t)[0], "bin", "java");
        return "java"; // 否则默认使用系统Java 
    }
    public static int ForNullJavaVersion(string version)
    {
        return // 1.16.5及以下都是Java8
                new Version(version) > new Version("1.16.5") ? 8 :
                // 1.17是Java6
                new Version(version) == new Version("1.17") ? 16 :
                // 1.18 1.19 Java 17 1.20往上Java20
                new Version(version) > new Version("1.18") ? 17 : 20;
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
    /// <param Name="versions">包含所有 Minecraft 版本名称的列表。</param>
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
