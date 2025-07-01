using OneLauncher.Core.Global;
using OneLauncher.Core.Mod.ModLoader.fabric;
using OneLauncher.Core.ModLoader.fabric.JsonModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher.Strategys;

internal class FabricStrategy : IModStrategy
{
    private readonly FabricVJParser _fabricParser;

    public FabricStrategy(string versionPath, string basePath)
    {
        string fabricJsonPath = Path.Combine(versionPath, "version.fabric.json");
        using var fs = File.OpenRead(fabricJsonPath);
        _fabricParser = new FabricVJParser(
            JsonSerializer.Deserialize<FabricRoot>(fs,FabricJsonContext.Default.FabricRoot),basePath);
    }

    public string? GetMainClassOverride() => _fabricParser.GetMainClass();

    // Fabric需要优先加载自己的库
    //public IEnumerable<(string key, string path)> GetModLibraries()=> _fabricParser.GetLibrariesForUsing().Select(x => (string.Join(string.Empty, x.name.Split(':')[..^1]), x.path));
    public IEnumerable<(string key, string path)> GetModLibraries()
       => _fabricParser.GetLibrariesForUsing().Select(lib =>
          {
              var parts = lib.name.Split(':');
              var key = string.Join(":", parts[..^1]);
              return (key, lib.path);
          });
    

    // Fabric本身不添加额外的JVM和游戏参数
    public IEnumerable<string> GetAdditionalJvmArgs() => Enumerable.Empty<string>();
    public IEnumerable<string> GetAdditionalGameArgs() => Enumerable.Empty<string>();
}

