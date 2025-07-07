using OneLauncher.Core.Global;
using OneLauncher.Core.Mod.ModLoader.fabric;
using OneLauncher.Core.Mod.ModLoader.fabric.quilt;
using OneLauncher.Core.ModLoader.fabric.JsonModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher.Strategys;
internal class QuiltStrategy : IModStrategy
{
    private readonly QuiltNJParser _quiltParser;

    private QuiltStrategy(QuiltNJParser quiltNJParser)
    {
        _quiltParser = quiltNJParser;
    }
    public static async Task<QuiltStrategy> CreateAsync(string versionPath, string basePath)
    {
        string quiltJsonPath = Path.Combine(versionPath, "version.quilt.json");
        await using var fs = File.OpenRead(quiltJsonPath);
        return new QuiltStrategy(new QuiltNJParser(
            await JsonSerializer.DeserializeAsync<FabricRoot>(fs, FabricJsonContext.Default.FabricRoot)
            ?? throw new OlanException("启动失败",$"在解析文件'{quiltJsonPath}'时出错"), basePath));
    }
    public string GetMainClassOverride() => _quiltParser.GetMainClass();

    public IEnumerable<(string key, string path)> GetModLibraries()
        => _quiltParser.GetLibrariesForUsing().Select(lib =>
        {
            var parts = lib.name.Split(':');
            var key = string.Join(":", parts[..^1]); // 不要考虑兼容性，考虑性能
            return (key, lib.path);
        });
    

    public IEnumerable<string> GetAdditionalJvmArgs() => Enumerable.Empty<string>();
    public IEnumerable<string> GetAdditionalGameArgs() => Enumerable.Empty<string>();
}
