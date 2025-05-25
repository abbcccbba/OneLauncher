using OneLauncher.Core.fabric;
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
    private VersionInfomations versionInfo;
    private readonly string version;
    private readonly UserModel userModel;
    private readonly string basePath;
    private readonly SystemType systemType;
    private readonly bool IsVersionInsulation;
    private readonly bool IsMod;
    private  FabricVJParser fabricParser;
    /// <param ID="basePath">游戏基本路径（含.minecraft）</param>
    /// <param ID="version">游戏版本</param>
    /// <param ID="userModel">以哪个用户模型来拼接启动参数？</param>
    /// <param ID="system">运行时系统类型</param>
    public LaunchCommandBuilder
        (
            string basePath, 
            string version, 
            UserModel userModel,
            SystemType system,
            bool VersionInsulation = false,
            bool IsMod = false
        )
    {
        this.basePath = basePath;
        this.version = version;
        this.userModel = userModel;
        this.systemType = system;
        this.IsVersionInsulation = VersionInsulation;
        this.IsMod = IsMod;   
    }
    public string GetJavaPath()
    {
        var vjava = versionInfo.GetJavaVersion();
        var t = Path.Combine(Path.GetDirectoryName(basePath), "JavaRuntimes",vjava.ToString());
        if (Init.ConfigManger.config.JavaList.Contains(vjava))
            return Path.Combine(t, Directory.GetDirectories(t)[0], "bin", "java");
        return "java"; // 否则默认使用系统Java  
    }
    public async Task<string> BuildCommand(string OtherArgs = "")
    {
        string VersionPath = (IsVersionInsulation)
                ? Path.Combine(basePath, $"v{version}")
                : Path.Combine(basePath, "versions", version);
        versionInfo = new VersionInfomations(
            await File.ReadAllTextAsync(Path.Combine(VersionPath,$"{version}.json")),
            basePath, systemType, IsVersionInsulation
        );
        if (IsMod)
            fabricParser = new fabric.FabricVJParser(
              Path.Combine(VersionPath, $"{version}-fabric.json"), basePath);
        string Args = $"{OtherArgs} {BuildJvmArgs()} {((IsMod) ? fabricParser.GetMainClass() : versionInfo.GetMainClass())} {BuildGameArgs()}";
        return Args;
    }
    private string BuildJvmArgs()
    {
        string osName = systemType == SystemType.windows ? "windows" :
                        systemType == SystemType.linux ? "linux" :
                        systemType == SystemType.osx ? "osx" : "";
        // AI 写的，干什么用的我也不知道
        string arch = RuntimeInformation.OSArchitecture.ToString().ToLower();

        var placeholders = new Dictionary<string, string>
        {
            // 创建占位符映射表 
            // 参考1.21.5.json
            // 手动加上引号
            { "natives_directory", "\""+
            ((IsVersionInsulation)
            ? Path.Combine(basePath,$"v{version}","natives")
            : Path.Combine(basePath,".minecraft","versions",version,"natives"))+"\"" 
            },
            { "launcher_name", "\"OneLauncher\"" },
            { "launcher_version", "\"1.0.0\"" },
            { "classpath",$"\"{BuildClassPath()}\"" }
        };
        // 处理1.13以前版本没有Arguments的情况
        if (versionInfo.info.Arguments == null)
        {
            return
                // 针对 1.6.x 版本不存在log4j2的情况
                (versionInfo.GetLoggingConfigPath() != null ? 
                $"-Dlog4j.configurationFile=\"{versionInfo.GetLoggingConfigPath()}\" " : " ")+
                // 处理特定平台要求的参数
                (systemType == SystemType.windows ? "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump "
                : systemType == SystemType.osx ? "-XstartOnFirstThread " : "")+
                // 标准JVM参数
                $"-Djava.library.path={placeholders["natives_directory"]} "+
                $"-Djna.tmpdir={placeholders["natives_directory"]} "+
                $"-Dorg.lwjgl.system.SharedLibraryExtractPath={placeholders["natives_directory"]} " + 
                $"-Dio.netty.native.workdir={placeholders["natives_directory"]} " +
                $"-Dminecraft.launcher.brand={placeholders["launcher_name"]} "+
                $"-Dminecraft.launcher.version={placeholders["launcher_version"]} "+
                $"-cp {placeholders["classpath"]} ";
        }
        else
        {
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

            return $"-Dlog4j.configurationFile=\"{versionInfo.GetLoggingConfigPath()}\" " +
              string.Join(" ", jvmArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
        }
    }
    private string BuildClassPath()
    {
        string modLibs = string.Empty;
        string separator = systemType == SystemType.windows ? ";" : ":";
        if(this.IsMod)
        {
            modLibs = string.Join(separator, fabricParser.GetLibraries().Select(x => x.path).ToList());
        }
        string Libs = string.Join(separator, versionInfo.GetLibrarys().Select(x => x.path));
        // 使用三引号可能导致模组加载器故障
        string AllClassArgs = $"{Libs}{separator}{versionInfo.GetMainFile().path}{separator}{modLibs}";
        return AllClassArgs;
    }
    private string BuildGameArgs()
    {
        return 
            $"--username \"{userModel.Name}\" " +
            $"--version \"{version}\" " +
            $"--gameDir \"{((IsVersionInsulation) ? Path.Combine(basePath,$"v{version}") : basePath)}\" " +
            $"--assetsDir \"{Path.Combine(basePath, "assets")}\" " +
            // 1.7版本及以上启用新用户验证机制
            (new Version(version) > new Version("1.7") ?
            $"--assetIndex \"{versionInfo.GetAssetIndexVersion()}\" " +
            $"--uuid \"{userModel.uuid}\" " +
            $"--accessToken \"{userModel.accessToken.ToString()}\" " +
            $"--userType \"{userModel.userType}\" " +
            $"--versionType \"OneLauncher\" " +
            "--userProperties {} " 
            // 针对旧版用户验证机制
            : $"--session \"{userModel.accessToken}\"");
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
