using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Launcher.Strategys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;

public partial class LaunchCommandBuilder
{
    /// <summary>
    /// 拼接类路径，不包含-p参数
    /// </summary>
    private string BuildClassPath(IModStrategy? strategy) 
    {
        var libraryMap = new Dictionary<string, string>();

        // 如果是Mod，优先添加Mod的库
        if (strategy != null)
            strategy.GetModLibraries()
                .ToList()
                .ForEach(lib => libraryMap[lib.key] = lib.path);

        // 添加原版库（如果不存在于map中）
        foreach (var lib in versionInfo.GetLibraryiesForUsing())
        {
            var parts = lib.name.Split(':');
            if (parts.Length >= 2)
            {
                var libKey = $"{parts[0]}:{parts[1]}";
                libraryMap.TryAdd(libKey, lib.path); // TryAdd 简洁高效
            }
        }

        var finalClassPathLibs = libraryMap.Values.ToList();
        finalClassPathLibs.Add(versionInfo.GetMainFile().path);

        return string.Join(separator, finalClassPathLibs.Where(p => !string.IsNullOrEmpty(p)).Distinct());
    }
}
