using OneLauncher.Core.Global;
using OneLauncher.Core.Mod.ModLoader.forgeseries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher.Strategys;
internal class NeoForgeStrategy : IModStrategy
{
    private readonly ForgeSeriesUsing _parser;
    private readonly string _basePath;
    private readonly string _versionID;
    private NeoForgeStrategy(string basePath, string versionID)
    {
        _parser = new ForgeSeriesUsing();
        _basePath = basePath;
        _versionID = versionID;
    }

    public static async Task<NeoForgeStrategy> CreateAsync(string versionPath, string basePath, string versionID)
    {
        var strategy = new NeoForgeStrategy(basePath, versionID);
        string jsonPath = Path.Combine(versionPath, "version.neoforge.json"); // 关键区别文件名
        // isForge 参数为 false
        await strategy._parser.Init(basePath, Path.GetFileName(versionPath), false);
        return strategy;
    }

    public string? GetMainClassOverride() => _parser.info.MainClass;

    public IDictionary<string, string> GetModLibraries()
        => _parser.GetLibrariesForLaunch(Init.GameRootPath);

    public IEnumerable<string> GetAdditionalJvmArgs()
    {
        var placeholders = new Dictionary<string, string>
            {
                { "library_directory", Path.Combine(_basePath, "libraries") },
                { "version_name", _versionID },
#if WINDOWS
                { "classpath_separator" , ";" }
#else
                { "classpath_separator" , ":" }
#endif
            };
        return _parser.info.Arguments.Jvm.Select(arg => LaunchCommandBuilder.ReplacePlaceholders(arg, placeholders));
    }

    public IEnumerable<string> GetAdditionalGameArgs() => _parser.info.Arguments.Game;
}
