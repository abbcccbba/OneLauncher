using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.ModLoader.fabric;
using OneLauncher.Core.ModLoader.neoforge;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OneLauncher.Core.Minecraft;

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
    private readonly ModEnum modType;
    private FabricVJParser fabricParser;
    private NeoForgeUsing neoForgeParser;
    private readonly string separator;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="basePath">游戏基本路径（含.minecraft）</param>
    /// <param name="version">启动的游戏版本</param>
    /// <param name="userModel">启动游戏的用户模型</param>
    /// <param name="modType">模组类型</param>
    /// <param name="system">系统类型</param>
    /// <param name="VersionInsulation">此游戏是否启用了版本隔离</param>
    public LaunchCommandBuilder
        (
            string basePath,
            string version,
            UserModel userModel,
            ModEnum modType,
            SystemType system,
            bool VersionInsulation = false
        )
    {
        this.basePath = basePath;
        this.version = version;
        this.userModel = userModel;
        systemType = system;
        IsVersionInsulation = VersionInsulation;
        this.modType = modType;
        separator = systemType == SystemType.windows ? ";" : ":";
    }
    public string GetJavaPath() =>
        Tools.IsUseOlansJreOrOssJdk(versionInfo.GetJavaVersion(), Path.GetDirectoryName(basePath));
    public async Task<string> BuildCommand(string OtherArgs = "")
    {
        string VersionPath = Path.Combine(basePath, "versions", version);
        string MainClass;
        versionInfo = new VersionInfomations(
            await File.ReadAllTextAsync(Path.Combine(VersionPath, $"{version}.json")),
            basePath, systemType, IsVersionInsulation
        );
        if (modType == ModEnum.fabric)
        {
            fabricParser = new FabricVJParser(
              Path.Combine(VersionPath, $"{version}-fabric.json"), basePath);
            MainClass = fabricParser.GetMainClass();
        }
        else if (modType == ModEnum.neoforge)
        {
            neoForgeParser = new NeoForgeUsing();
            await neoForgeParser.Init(basePath, version);
            MainClass = neoForgeParser.info.MainClass;
        }
        else MainClass = versionInfo.GetMainClass();
        string Args = $"{OtherArgs} {BuildJvmArgs()} {MainClass} {BuildGameArgs()}";
        Debug.WriteLine(Args);
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
            { "natives_directory", Path.Combine(basePath,"versions",version,"natives") },
            { "launcher_name", "OneLauncher" },
            { "launcher_version", Init.OneLauncherVersoin },
            { "classpath",BuildClassPath() },
            // 一些仅限NeoForge的
            { "version_name" , version},
            { "library_directory" ,"\""+Path.Combine(basePath, "libraries")+"\""},
            { "classpath_separator" , separator}
        };
        // 处理1.13以前版本没有Arguments的情况
        if (versionInfo.info.Arguments == null)
        {
            return
                // 针对 1.6.x 版本不存在log4j2的情况
                (versionInfo.GetLoggingConfigPath() != null ?
                $"-Dlog4j.configurationFile=\"{versionInfo.GetLoggingConfigPath()}\" " : " ") +
                // 处理特定平台要求的参数
                (systemType == SystemType.windows ? "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump "
                : systemType == SystemType.osx ? "-XstartOnFirstThread " : "") +
                // 标准JVM参数
                $"-Djava.library.path={placeholders["natives_directory"]} " +
                $"-Djna.tmpdir={placeholders["natives_directory"]} " +
                $"-Dorg.lwjgl.system.SharedLibraryExtractPath={placeholders["natives_directory"]} " +
                $"-Dio.netty.native.workdir={placeholders["natives_directory"]} " +
                $"-Dminecraft.launcher.brand={placeholders["launcher_name"]} " +
                $"-Dminecraft.launcher.version={placeholders["launcher_version"]} " +
                $"-cp {placeholders["classpath"]} ";
        }
        else
        {
            var jvmArgs = new List<string>();
            if (modType == ModEnum.neoforge)
            {
                foreach (var item in neoForgeParser.info.Arguments.Jvm)
                {
                    string replaced = ReplacePlaceholders(item, placeholders);
                    jvmArgs.Add(replaced);
                }
            }
            foreach (var item in versionInfo.info.Arguments.Jvm)
            {
                // 判断是规则套字符串还是简单字符串
                if (item is string str)
                {
                    string replaced = ReplacePlaceholders(str, placeholders);
                    jvmArgs.Add(replaced);
                }
                else if (item is MinecraftArgument arg)
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
              // 打上空格和双引号
              string.Join(" ", jvmArgs);
        }
    }
    /// <summary>
    /// 拼接类路径，不包含-p参数
    /// </summary>
    private string BuildClassPath()
    {
        // 使用 List<string> 来收集所有库路径
        var allLibPaths = new List<string>();

        // 1. 添加所有原版库 (来自 versionInfo)
        allLibPaths.AddRange(versionInfo.GetLibrarys().Select(x => x.path));

        // 2. 添加所有 NeoForge 库 (来自 neoForgeParser)
        if (modType == ModEnum.neoforge)
        {
            allLibPaths.AddRange(neoForgeParser.GetLibrariesForLaunch(basePath));
        }
        // (如果需要支持 Fabric, 在这里添加 fabricParser 的库)
        else if (modType == ModEnum.fabric)
        {
            allLibPaths.AddRange(fabricParser.GetLibraries().Select(x => x.path));
        }

        // 3. 添加主游戏 JAR 文件
        allLibPaths.Add(versionInfo.GetMainFile().path);

        // 4. 使用 Distinct() 去除可能存在的重复项，并过滤掉空路径，然后用分隔符连接
        string AllClassArgs = string.Join(separator,
                                          allLibPaths.Where(p => !string.IsNullOrEmpty(p))
                                                     .Distinct());

        // 5. 确保你的 -cp 参数正确地使用了这个字符串。
        //    你需要确保你的 BuildJvmArgs() 方法最终会生成类似 "-cp <AllClassArgs>" 的参数。
        //    如果原版 JSON 已经包含了 -cp ${classpath}，你需要确保 ${classpath} 被正确替换。
        //    从你的输出看，你似乎是手动构建了 -cp ;... 这意味着你需要在 BuildJvmArgs 中确保
        //    最终的 JVM 参数里包含 "-cp" 和这个构建好的路径字符串。
        //    如果你的 BuildJvmArgs 依赖于 ${classpath}，那么你需要确保你的 placeholders 字典里
        //    的 "classpath" 键值是这个 AllClassArgs。
        //    看你的原始命令，似乎 -cp 后面直接跟了分号和路径，所以你可能需要返回 $";{AllClassArgs}"
        //    或者在 BuildJvmArgs 里拼接时加上分号。
        //    最稳妥的方式是直接返回路径，在 BuildJvmArgs 拼接时处理 -cp 和分号。
        return AllClassArgs;
    }
    private string BuildGameArgs()
    {
        string GameArgs =
            $"--username \"{userModel.Name}\" " +
            $"--version \"{version}\" " +
            $"--gameDir \"{(IsVersionInsulation ? Path.Combine(basePath, "versions", version) : basePath)}\" " +
            $"--assetsDir \"{Path.Combine(basePath, "assets")}\" " +
            // 1.7版本及以上启用新用户验证机制
            (new Version(version) > new Version("1.7") ?
            $"--assetIndex \"{versionInfo.GetAssetIndexVersion()}\" " +
            $"--uuid \"{userModel.uuid}\" " +
            $"--accessToken \"{userModel.accessToken.ToString()}\" " +
            $"--userType \"{(userModel.IsMsaUser ? "msa" : "legacy")}\" " +
            $"--versionType \"OneLauncher\" " +
            "--userProperties {} "
            // 针对旧版用户验证机制
            : $"--session \"{userModel.accessToken}\" ");
        if (modType == ModEnum.neoforge)
            GameArgs +=
                string.Join(" ", neoForgeParser.info.Arguments.Game);
        return GameArgs;
    }
    private bool EvaluateRules(List<MinecraftRule> rules, string osName, string arch)
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
