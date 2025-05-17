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
            File.ReadAllText(Path.Combine(basePath,".minecraft","versions",version,$"{version}.json")),
            basePath,system
        );
    }

    public string BuildCommand(string OtherArgs = "")
    {
        string Args = $"{OtherArgs} {BuildJvmArgs()} {versionInfo.GetMainClass()} {BuildGameArgs()}";
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
            { "natives_directory", "\""+Path.Combine(basePath,".minecraft","versions",version,"natives")+"\"" },
            { "launcher_name", "\"OneLauncher\"" },
            { "launcher_version", "\"1.0.0\"" },
            { "classpath",$"\"{BuildClassPath()}\"" }// """ "E:\mc\.minecraft\libraries\com\mojang\patchy\1.3.9\patchy-1.3.9.jar;E:\mc\.minecraft\libraries\oshi-project\oshi-core\1.1\oshi-core-1.1.jar;E:\mc\.minecraft\libraries\net\java\dev\jna\jna\4.4.0\jna-4.4.0.jar;E:\mc\.minecraft\libraries\net\java\dev\jna\platform\3.4.0\platform-3.4.0.jar;E:\mc\.minecraft\libraries\com\ibm\icu\icu4j-core-mojang\51.2\icu4j-core-mojang-51.2.jar;E:\mc\.minecraft\libraries\net\sf\jopt-simple\jopt-simple\5.0.3\jopt-simple-5.0.3.jar;E:\mc\.minecraft\libraries\com\paulscode\codecjorbis\20101023\codecjorbis-20101023.jar;E:\mc\.minecraft\libraries\com\paulscode\codecwav\20101023\codecwav-20101023.jar;E:\mc\.minecraft\libraries\com\paulscode\libraryjavasound\20101123\libraryjavasound-20101123.jar;E:\mc\.minecraft\libraries\com\paulscode\librarylwjglopenal\20100824\librarylwjglopenal-20100824.jar;E:\mc\.minecraft\libraries\com\paulscode\soundsystem\20120107\soundsystem-20120107.jar;E:\mc\.minecraft\libraries\io\netty\netty-all\4.1.9.Final\netty-all-4.1.9.Final.jar;E:\mc\.minecraft\libraries\com\google\guava\guava\21.0\guava-21.0.jar;E:\mc\.minecraft\libraries\org\apache\commons\commons-lang3\3.5\commons-lang3-3.5.jar;E:\mc\.minecraft\libraries\commons-io\commons-io\2.5\commons-io-2.5.jar;E:\mc\.minecraft\libraries\commons-codec\commons-codec\1.10\commons-codec-1.10.jar;E:\mc\.minecraft\libraries\net\java\jinput\jinput\2.0.5\jinput-2.0.5.jar;E:\mc\.minecraft\libraries\net\java\jutils\jutils\1.0.0\jutils-1.0.0.jar;E:\mc\.minecraft\libraries\com\google\code\gson\gson\2.8.0\gson-2.8.0.jar;E:\mc\.minecraft\libraries\com\mojang\authlib\1.5.25\authlib-1.5.25.jar;E:\mc\.minecraft\libraries\com\mojang\realms\1.10.22\realms-1.10.22.jar;E:\mc\.minecraft\libraries\org\apache\commons\commons-compress\1.8.1\commons-compress-1.8.1.jar;E:\mc\.minecraft\libraries\org\apache\httpcomponents\httpclient\4.3.3\httpclient-4.3.3.jar;E:\mc\.minecraft\libraries\commons-logging\commons-logging\1.1.3\commons-logging-1.1.3.jar;E:\mc\.minecraft\libraries\org\apache\httpcomponents\httpcore\4.3.2\httpcore-4.3.2.jar;E:\mc\.minecraft\libraries\it\unimi\dsi\fastutil\7.1.0\fastutil-7.1.0.jar;E:\mc\.minecraft\libraries\org\apache\logging\log4j\log4j-api\2.8.1\log4j-api-2.8.1.jar;E:\mc\.minecraft\libraries\org\apache\logging\log4j\log4j-core\2.8.1\log4j-core-2.8.1.jar;E:\mc\.minecraft\libraries\org\lwjgl\lwjgl\lwjgl\2.9.4-nightly-20150209\lwjgl-2.9.4-nightly-20150209.jar;E:\mc\.minecraft\libraries\org\lwjgl\lwjgl\lwjgl_util\2.9.4-nightly-20150209\lwjgl_util-2.9.4-nightly-20150209.jar;E:\mc\.minecraft\libraries\com\mojang\text2speech\1.10.3\text2speech-1.10.3.jar;E:\mc\.minecraft\versions\1.12.2\1.12.2.jar" """}//$"\"{BuildClassPath()}\"" }
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
        // 缓存机制
        string cacheFilePath = Path.Combine(basePath, ".minecraft", "versions", version, "classpath.cache");
        string separator = systemType == SystemType.windows ? ";" : ":";
        //string AllClassArgs = $"\"\"{separator}{Libs}{separator}\"{versionInfo.GetMainFile(version).path}\"{separator}\"\"";
        var sb = new StringBuilder(); sb.Append($"\"\"{separator}");
        foreach (var path in versionInfo.GetLibrarys().Select(x => x.path))
        sb.Append($"\"{path}\"{separator}");
        sb.Append($"\"{versionInfo.GetMainFile().path}\"{separator}\"\"");
        return sb.ToString();
    }
    private string BuildGameArgs()
    {
        return 
            $"--username \"{userModel.Name}\" " +
            $"--version \"{version}\" " +
            $"--gameDir \"{Path.Combine(basePath, ".minecraft")}\" " +
            $"--assetsDir \"{Path.Combine(basePath, ".minecraft", "assets")}\" " +
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
