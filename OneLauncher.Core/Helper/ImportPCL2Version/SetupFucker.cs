using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.ImportPCL2Version;

/// <summary>
/// 用于读取 PCL2 的 Setup.ini 文件来知道MC版本号、模组加载器。
/// </summary>
public class PCL2SetupFucker
{
    private readonly Dictionary<string, string> values;

    public PCL2SetupFucker(string filePath)
    {
        values = File.ReadAllLines(filePath)
            .Select(line => line.Split(new[] { ':' }, 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase); // 忽略键的大小写
    }
    public string GetValue(string key) => values.TryGetValue(key, out var value) ? value : string.Empty;
    public string GetMinecraftVersion() => GetValue("VersionOriginal");
    public ModEnum GetModLoader()
    {
        if (!string.IsNullOrEmpty(GetValue("VersionNeoForge"))) return ModEnum.neoforge;
        if (!string.IsNullOrEmpty(GetValue("VersionFabric"))) return ModEnum.fabric;
        if (!string.IsNullOrEmpty(GetValue("VersionForge"))) return ModEnum.forge;
        return ModEnum.none;
    }
}