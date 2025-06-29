using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.Mod.ModLoader.fabric;
using OneLauncher.Core.Mod.ModLoader.fabric.quilt;
using OneLauncher.Core.Mod.ModLoader.forgeseries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;
/// <summary>
/// Minecraft 启动命令构造器，提供一个简单的方法来生成启动命令。
/// </summary>
public partial class LaunchCommandBuilder
{
    public VersionInfomations versionInfo;
    private readonly GameData gameData;
    private readonly string version;
    private readonly string basePath;
    private readonly ModEnum modType;
    private FabricVJParser fabricParser;
    private ForgeSeriesUsing neoForgeParser;
    private QuiltNJParser quiltParser;
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
    public async Task<string> BuildCommand(string OtherArgs = "", bool useRootLaunch = false)
    {
        string MainClass;
        if (modType == ModEnum.fabric)
        {
            fabricParser = FabricVJParser.ParserAuto(
              File.OpenRead(Path.Combine(VersionPath, $"version.fabric.json")), basePath);
            MainClass = fabricParser.GetMainClass();
        }
        else if (modType == ModEnum.quilt)
        {
            quiltParser = QuiltNJParser.ParserAuto(
                File.OpenRead(Path.Combine(VersionPath, $"version.quilt.json")), basePath);
            MainClass = quiltParser.GetMainClass();
        }
        else if (modType == ModEnum.neoforge || modType == ModEnum.forge)
        {
            neoForgeParser = new ForgeSeriesUsing();
            await neoForgeParser.Init(basePath, version, (modType == ModEnum.forge ? true : false));
            MainClass = neoForgeParser.info.MainClass;
        }
        else MainClass = versionInfo.GetMainClass();
        string Args = $"{OtherArgs} {BuildJvmArgs()} {MainClass} {BuildGameArgs(useRootLaunch)}";
        Debug.WriteLine(Args);
        return Args;
    }
}
