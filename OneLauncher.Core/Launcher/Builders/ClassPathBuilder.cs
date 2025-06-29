using OneLauncher.Core.Helper.Models;
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
        else if (modType == ModEnum.quilt) // 假设你的 ModEnum 枚举中已添加 quilt
        {
            foreach (var lib in quiltParser.GetLibrariesForUsing())
            {
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
}
