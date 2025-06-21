using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Mod.ModLoader.fabric;
using OneLauncher.Core.Mod.ModLoader.forgeseries;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace OneLauncher.Core.Minecraft;

/// <summary>
/// Minecraft 启动命令构造器，提供一个简单的方法来生成启动命令。
/// </summary>
public class LaunchCommandBuilder
{
    public VersionInfomations versionInfo;
    private readonly GameData gameData;
    private readonly string version;
    private readonly string basePath;
    private readonly ModEnum modType;
    private FabricVJParser fabricParser;
    private ForgeSeriesUsing neoForgeParser;
    private readonly string separator;
    private readonly string VersionPath;
    private readonly ServerInfo? serverInfo;
    public LaunchCommandBuilder(
        string basePath,
        GameData gameData,
        ServerInfo? serverInfo = null)
    {
        this.basePath = basePath;
        this.version = gameData.VersionId;
        this.modType = gameData.ModLoader;
        this.serverInfo = serverInfo;
        this.gameData = gameData;
        this.VersionPath = Path.Combine(this.basePath, "versions", this.version);
#if WINDOWS
        separator = ";";
#else
        separator = ":";
#endif
        versionInfo = new VersionInfomations(
            File.ReadAllText(Path.Combine(VersionPath, "version.json")),
            this.basePath
        );
    }
    public string GetJavaPath() =>
        Tools.IsUseOlansJreOrOssJdk(versionInfo.GetJavaVersion());
    public async Task<string> BuildCommand(string OtherArgs = "")
    {
        string MainClass;
        if (modType == ModEnum.fabric)
        {
            fabricParser = new FabricVJParser(
              Path.Combine(VersionPath, $"version.fabric.json"), basePath);
            MainClass = fabricParser.GetMainClass();
        }
        else if (modType == ModEnum.neoforge || modType == ModEnum.forge)
        {
            neoForgeParser = new ForgeSeriesUsing();
            await neoForgeParser.Init(basePath, version,(modType == ModEnum.forge ? true : false));
            MainClass = neoForgeParser.info.MainClass;
        }
        else MainClass = versionInfo.GetMainClass();
        string Args = $"{OtherArgs} {BuildJvmArgs()} {MainClass} {BuildGameArgs()}";
        Debug.WriteLine(Args);
        return Args;
    }
    private string BuildJvmArgs()
    {
        string osName;
#if WINDOWS
        osName = "windows";
#elif MACOS
        osName = "osx";
#else
        osName = "linux"; 
#endif  
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
            { "classpath","\""+BuildClassPath()+"\"" },
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
                (osName == "windows" ? "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump "
                : osName == "osx" ? "-XstartOnFirstThread " : "") +
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
            if (modType == ModEnum.neoforge || modType == ModEnum.forge)
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
                                jvmArgs.Add($"\"{replaced}\"");
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
        var libraryMap = new Dictionary<string, string>();
        if (modType == ModEnum.fabric)
        {
            foreach (var lib in fabricParser.GetLibrariesForUsing())
            {
                // 从 "org.ow2.asm:asm:9.5" 中提取 "org.ow2.asm:asm" 作为key
                var parts = lib.name.Split(':');
                if (parts.Length >= 2)
                {
                    var libKey = $"{parts[0]}:{parts[1]}";
                    libraryMap[libKey] = lib.path;
                }
            }
        }
        else if (modType == ModEnum.neoforge || modType == ModEnum.forge)
        {
            // NeoForge 同理
            foreach (var lib in neoForgeParser.GetLibrariesForLaunch(basePath))
            {
                var parts = lib.name.Split(':');
                if (parts.Length >= 2)
                {
                    var libKey = $"{parts[0]}:{parts[1]}";
                    libraryMap[libKey] = lib.path;
                }
            }
        }
        foreach (var lib in versionInfo.GetLibraryiesForUsing())
        {
            var parts = lib.name.Split(':');
            if (parts.Length >= 2)
            {
                var libKey = $"{parts[0]}:{parts[1]}";
                if (!libraryMap.ContainsKey(libKey)) // 如果不存在，才添加
                {
                    libraryMap[libKey] = lib.path;
                }
            }
        }
        var finalClassPathLibs = libraryMap.Values.ToList();
        finalClassPathLibs.Add(versionInfo.GetMainFile().path);

        return string.Join(
            separator,
            finalClassPathLibs
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct() 
        );
    }
    private string BuildGameArgs()
    {
        string serverArgs = string.Empty;
        if(serverInfo != null)
        {
            serverArgs = $"--server \"{((ServerInfo)serverInfo).Ip}\" --port\"{((ServerInfo)serverInfo).Port}\" ";
            if(new Version(version) > new Version("1.20"))
            {
                serverArgs += $"--quickPlayMultiplayer \"{serverInfo.Value.Ip}:{serverInfo.Value.Port}\" ";
            }
        }
        string GameArgs =
            $"--username \"{gameData.DefaultUserModel.Name}\" " +
            $"--version \"{version}\" " +
            $"--gameDir \"{gameData.InstancePath}\" " +
            $"--assetsDir \"{(Path.Combine(basePath, "assets"))}\" " +
            // 1.7版本及以上启用新用户验证机制
            (new Version(version) > new Version("1.7") ?
            $"--assetIndex \"{versionInfo.GetAssetIndexVersion()}\" " +
            $"--uuid \"{gameData.DefaultUserModel.uuid}\" " +
            $"--accessToken \"{gameData.DefaultUserModel.AccessToken.ToString()}\" " +
            $"--userType \"{(gameData.DefaultUserModel.IsMsaUser ? "msa" : "legacy")}\" " +
            $"--versionType \"OneLauncher\" " +
            serverArgs+
            "--userProperties {} "
            // 针对旧版用户验证机制
            : $"--session \"{gameData.DefaultUserModel.AccessToken}\" ");
        if (modType == ModEnum.neoforge || modType == ModEnum.forge)
            GameArgs +=
                string.Join(" ", neoForgeParser.info.Arguments.Game);
        Debug.WriteLine(GameArgs);
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
