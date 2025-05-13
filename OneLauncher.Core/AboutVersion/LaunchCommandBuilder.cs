using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core;

/// <summary>
/// Minecraft 启动命令构造器，提供一个简单的方法来生成启动命令。
/// </summary>
public class LaunchCommandBuilder
{
    private readonly VersionInfomations versionInfo;
    private readonly string version;
    private readonly UserModel userModel;
    private readonly string basePath;
    private readonly SystemType systemType;
    /// <param name="basePath">游戏基本路径（不含.minecraft，末尾加'/'）</param>
    /// <param name="version">游戏版本</param>
    /// <param name="userModel">以哪个用户模型来拼接启动参数？</param>
    /// <param name="system">运行时系统类型</param>
    public LaunchCommandBuilder(string basePath, string version, UserModel userModel,SystemType system)
    {
        this.basePath = basePath;
        this.version = version;
        this.userModel = userModel;
        this.systemType = system;
        versionInfo = new VersionInfomations(
            File.ReadAllText($"{basePath}.minecraft/versions/{version}/{version}.json"),
            basePath,system
        );
    }

    public string BuildCommand(string OtherArgs = "")
    {
        return $"{BuildJvmArgs()} {OtherArgs} {versionInfo.GetMainClass()} {BuildGameArgs()}";
    }
    private string BuildJvmArgs()
    {
        if (versionInfo.info.Arguments == null || versionInfo.info.Arguments.Jvm == null)
        {
            throw new InvalidOperationException("JVM arguments not found in version.json");
        }

        string osName = systemType == SystemType.windows ? "windows" :
                        systemType == SystemType.linux ? "linux" :
                        systemType == SystemType.osx ? "osx" : "";
        string arch = RuntimeInformation.OSArchitecture.ToString().ToLower();

        var placeholders = new Dictionary<string, string>
        {
            // 创建占位符映射表 
            // 参考1.21.5.json
            // 手动加上引号
            { "natives_directory", $"\"{basePath}.minecraft/versions/{version}/{version}-natives\"" },
            { "launcher_name", "\"OneLauncher\"" },
            { "launcher_version", "\"1.0.0\"" },
            { "classpath", $"\"{BuildClassPath()}\"" }
        };

        var jvmArgs = new List<string>();
        foreach (var item in versionInfo.info.Arguments.Jvm)
        {
            if (item is string str)
            {
                string replaced = ReplacePlaceholders(str, placeholders);
                jvmArgs.Add(replaced);
            }
            else if (item is Models.Argument arg)
            {
                if (EvaluateRules(arg.Rules, osName, arch))
                {
                    if (arg.Value is string valStr)
                    {
                        string replaced = ReplacePlaceholders(valStr, placeholders);
                        jvmArgs.Add(replaced);
                    }
                    else if (arg.Value is List<string> valList)
                    {
                        foreach (var val in valList)
                        {
                            string replaced = ReplacePlaceholders(val, placeholders);
                            jvmArgs.Add(replaced);
                        }
                    }
                }
            }
        }

        return string.Join(" ", jvmArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
    }
    private string BuildClassPath()
    {
        string separator = systemType == SystemType.windows ? ";" : ":";
        string Libs = string.Join(separator, versionInfo
            .GetLibrarys()
            .Select(x => x.path) // 提取path属性
            .Select(s => $"\"{s}\"")
            .ToList());
        // 三引号内前后加系统分隔符，我也不知道为什么反正不加会报错
        string cpLibs = $"\"\"{separator}{Libs}{separator}\"{versionInfo.GetMainFile(version).path}\"{separator}\"\"";
        return cpLibs;
    }
    private string BuildGameArgs()
    {
        return //string.Join(" ",
            $"--username \"{userModel.Name}\" " +
            $"--version \"{version}\" " +
            $"--gameDir \"{Path.Combine(basePath, ".minecraft")}\" " +
            $"--assetsDir \"{Path.Combine(basePath, ".minecraft", "assets")}\" " +
            $"--assetIndex \"{versionInfo.GetAssetIndexVersion()}\" " +
            $"--uuid \"{userModel.uuid}\" " +
            $"--accessToken \"{userModel.accessToken}\" " +
            $"--userType \"{userModel.userType}\" " +
            $"--versionType OneLauncher";//);
    }
    private bool EvaluateRules(List<Models.Rule> rules, string osName, string arch)
    {
        if (rules == null || rules.Count == 0) return true;
        bool allowed = false;
        foreach (var rule in rules)
        {
            bool matches = true;
            if (rule.Os != null)
            {
                if (rule.Os.Name != null && rule.Os.Name != osName) matches = false;
            }
            if (matches)
            {
                allowed = rule.Action == "allow";
            }
        }
        return allowed;
    }
    private string ReplacePlaceholders(string input, Dictionary<string, string> placeholders)
    {
        foreach (var kvp in placeholders)
        {
            input = input.Replace("${" + kvp.Key + "}", kvp.Value);
        }
        return input;
    }
}
