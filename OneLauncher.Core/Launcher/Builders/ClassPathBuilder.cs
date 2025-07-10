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
    /// 高效地拼接类路径，自动处理原版库和Mod库的优先级与去重。
    /// </summary>
    private string BuildClassPath(IModStrategy? strategy)
    {
        // 1. 获取原版库字典。
        // 这是基础，包含所有当前版本需要的原版库。
        var libraryMap = versionInfo.GetLibraryiesForUsing();

        // 2. 如果有Mod策略，用Mod库的字典来更新（合并/覆盖）原版库字典。
        if (strategy != null)
        {
            var modLibraries = strategy.GetModLibraries();
            foreach (var lib in modLibraries)
            {
                // 核心逻辑：直接使用字典的索引器。
                // 如果key已存在（例如，Mod提供了与原版同名的库），则Mod库的路径会覆盖原版库的路径。
                // 如果key不存在，则直接添加新的Mod库。
                // 这完美地实现了“Mod库优先”的原则。
                libraryMap[lib.Key] = lib.Value;
            }
        }

        // 3. 将合并后的所有库路径和游戏主文件路径提取出来。
        var finalClassPathLibs = libraryMap.Values.ToList();
        finalClassPathLibs.Add(versionInfo.GetMainFile().path);

        // 4. 使用指定的分隔符拼接成最终的类路径字符串。
        return string.Join(separator, finalClassPathLibs.Where(p => !string.IsNullOrEmpty(p)));
    }
}
