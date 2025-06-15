using OneLauncher.Core.Helper;
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
    public VersionInfomations versionInfo;
    private readonly string version;
    private readonly UserModel userModel;
    private readonly string basePath;
    private readonly SystemType systemType;
    private readonly bool IsVersionInsulation;
    private readonly ModEnum modType;
    private FabricVJParser fabricParser;
    private NeoForgeUsing neoForgeParser;
    private readonly string separator;
    private readonly string VersionPath;
    /// <summary>
    /// 
    /// </summary>
    /// <param Name="basePath">游戏基本路径（含.minecraft）</param>
    /// <param Name="version">启动的游戏版本</param>
    /// <param Name="userModel">启动游戏的用户模型</param>
    /// <param Name="modType">模组类型</param>
    /// <param Name="system">系统类型</param>
    /// <param Name="VersionInsulation">此游戏是否启用了版本隔离</param>
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
        this.VersionPath = Path.Combine(basePath, "versions", version);
        systemType = system;
        IsVersionInsulation = VersionInsulation;
        this.modType = modType;
        separator = systemType == SystemType.windows ? ";" : ":";
        versionInfo = new VersionInfomations(
            File.ReadAllText(Path.Combine(VersionPath, $"version.json")),
            basePath, systemType, IsVersionInsulation
        );
    }
    public string GetJavaPath() =>
        Tools.IsUseOlansJreOrOssJdk(versionInfo.GetJavaVersion(), Path.GetDirectoryName(basePath));
    public async Task<string> BuildCommand(string OtherArgs = "",bool UseTempFileToPaserClassPathToJvm = false)
    {
        string MainClass;
        if (modType == ModEnum.fabric)
        {
            fabricParser = new FabricVJParser(
              Path.Combine(VersionPath, $"version.fabric.json"), basePath);
            MainClass = fabricParser.GetMainClass();
        }
        else if (modType == ModEnum.neoforge)
        {
            neoForgeParser = new NeoForgeUsing();
            await neoForgeParser.Init(basePath, version);
            MainClass = neoForgeParser.info.MainClass;
        }
        else MainClass = versionInfo.GetMainClass();
        string Args = $"{OtherArgs} {BuildJvmArgs(UseTempFileToPaserClassPathToJvm)} {MainClass} {BuildGameArgs()}";
        Debug.WriteLine(Args);
        return Args;
    }
    private string BuildJvmArgs(bool IsUTFTPCPTJ)
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
        var allLibPaths = new List<(string name, string path)>();
        allLibPaths.AddRange(versionInfo.GetLibrarysForUsing());

        // 根据模组类型添加模组库
        if (modType == ModEnum.neoforge)
        {
            allLibPaths.AddRange(neoForgeParser.GetLibrariesForLaunch(basePath));
        }
        else if (modType == ModEnum.fabric)
        {
            allLibPaths.AddRange(fabricParser.GetLibrariesForUsing());
        }
        allLibPaths.Add(("", versionInfo.GetMainFile().path));

        var libraryVersions = new Dictionary<string, (string path, Version version)>();

        // 用于存储非 Maven 库或不符合过滤条件的库的最终路径
        var nonMavenOrUniquePaths = new HashSet<string>();

        foreach (var (name, path) in allLibPaths)
        {
            if (string.IsNullOrEmpty(path)) continue;

            string groupIdArtifactId = "";
            Version currentVersion = null;
            bool isMavenLib = false;

            string[] parts = name.Split(':');
            // 一个基本的 Maven 坐标至少包含 groupId:artifactId:version (3 部分)
            if (parts.Length >= 3)
            {
                groupIdArtifactId = string.Join(":", parts.Take(parts.Length - 1)); 
                if (Version.TryParse(parts.Last(), out currentVersion))
                {
                    isMavenLib = true;
                }
            }

            if (isMavenLib)
            {
                if (libraryVersions.TryGetValue(groupIdArtifactId, out var existing))
                    libraryVersions[groupIdArtifactId] = (path, currentVersion);
                else
                    libraryVersions[groupIdArtifactId] = (path, currentVersion);
            }
            else
                nonMavenOrUniquePaths.Add(path);
            
        }

        // 构建最终的类路径列表
        var finalClassPaths = new HashSet<string>(); // 使用 HashSet 确保最终路径的唯一性

        // 首先添加所有筛选出的最佳 Maven 库版本
        foreach (var entry in libraryVersions.Values)
        {
            finalClassPaths.Add(entry.path);
        }

        // 然后添加所有非 Maven 库或之前未参与版本过滤的独特路径
        foreach (var path in nonMavenOrUniquePaths)
        {
            finalClassPaths.Add(path);
        }
        finalClassPaths.Add(versionInfo.GetMainFile().path);
        string allClassArgs = string.Join(separator,
                                          finalClassPaths.Where(p => !string.IsNullOrEmpty(p)));

        return allClassArgs;
    }
    private string BuildGameArgs()
    {
        string GameArgs =
            $"--username \"{userModel.Name}\" " +
            $"--version \"{version}\" " +
            $"--gameDir \"{(IsVersionInsulation ? Path.Combine(basePath, "versions", version) : basePath)}\" " +
            $"--assetsDir \"{((systemType == SystemType.windows) ? Path.Combine(basePath, "assets").Replace("\\",@"\\") : Path.Combine(basePath, "assets"))}\" " +
            // 1.7版本及以上启用新用户验证机制
            (new Version(version) > new Version("1.7") ?
            $"--assetIndex \"{versionInfo.GetAssetIndexVersion()}\" " +
            $"--uuid \"{userModel.uuid}\" " +
            $"--accessToken \"{userModel.AccessToken.ToString()}\" " +
            $"--userType \"{(userModel.IsMsaUser ? "msa" : "legacy")}\" " +
            $"--versionType \"OneLauncher\" " +
            "--userProperties {} "
            // 针对旧版用户验证机制
            : $"--session \"{userModel.AccessToken}\" ");
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
