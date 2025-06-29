using OneLauncher.Core.Global;
using OneLauncher.Core.Mod.ModLoader.fabric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher.Strategys;
internal class FabricStrategy : IModStrategy
{
    private FabricVJParser fabricParser;
    public FabricStrategy(string versionPath)
    {
        using var fs = File.OpenRead(Path.Combine(versionPath, "version.fabric.json")); // 同步方法就这么写
        this.fabricParser = FabricVJParser.ParserAuto(fs,Init.GameRootPath);
    }
    public IEnumerable<string> GetClassPathBeforeVanilla()
    {
        throw new Exception("还没写完，消除编译器报错用的");
    }

    public IEnumerable<string> GetGameArgsAfterVanilla()
        => Array.Empty<string>(); // Fabric没这个东西

    public IEnumerable<string> GetJvmArgsAfrerVanilla()
        => Array.Empty<string>(); // Fabric没这个东西
    public string GetMainClass()
    {
        return fabricParser.GetMainClass();
    }
}
