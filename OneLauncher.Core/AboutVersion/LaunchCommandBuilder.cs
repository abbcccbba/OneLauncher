using System;
using System.Collections.Generic;
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

    public LaunchCommandBuilder(string basePath, string version, UserModel userModel)
    {
        this.basePath = basePath;
        this.version = version;
        this.userModel = userModel;
        versionInfo = new VersionInfomations(
            File.ReadAllText($"{basePath}.minecraft/versions/{version}/{version}.json"),
            basePath
        );
    }

    public string BuildCommand(string OtherJvmArgs)
    {
        return $"{BuildJvmArgs(OtherJvmArgs)} {versionInfo.GetMainClass()} {BuildGameArgs()}";
    }

    private string BuildJvmArgs(string OtherJvmArgs)
    {
        if (versionInfo.info.Arguments == null || versionInfo.info.Arguments.Jvm == null)
        {
            throw new InvalidOperationException("JVM arguments not found in version.json");
        }

        string osName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" :
                        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "";
        string arch = RuntimeInformation.OSArchitecture.ToString().ToLower();

        string nativesDir = $"{basePath}.minecraft/versions/{version}/{version}-natives";
        string classpath = BuildClassPath();
        var placeholders = new Dictionary<string, string>
        {
            { "natives_directory", nativesDir },
            { "launcher_name", "OneLauncher" },
            { "launcher_version", "1.0.0" },
            { "classpath", classpath }
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

        return OtherJvmArgs + string.Join(" ", jvmArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
    }

    private string BuildClassPath()
    {
        string separator = OperatingSystem.IsWindows() ? ";" : ":";
        var jarLibraries = versionInfo.GetLibrarys()
            .Where(lib => lib.path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
            .Select(lib => lib.path);
        string mainJar = versionInfo.GetMainFile(version).path;
        return string.Join(separator, new[] { mainJar }.Concat(jarLibraries));
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
                if (rule.Os.Arch != null && rule.Os.Arch != arch) matches = false;
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
            $"--userType \"{userModel.UserType}\" " +
            $"--versionType OneLauncher";//);
    }
}
