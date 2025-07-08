using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Mod.FabricModJsonModels;
using OneLauncher.Core.Global;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core.Mod;

public class InstanceModService
{
    private readonly GameData _instance;
    private readonly string _modsPath;

    public InstanceModService(GameData instance)
    {
        _instance = instance;
        _modsPath = Path.Combine(_instance.InstancePath, "mods");
        Directory.CreateDirectory(_modsPath); // 确保mods文件夹存在
    }

    /// <summary>
    /// 获取当前实例的所有Mod列表
    /// </summary>
    public async Task<List<ModInfo>> GetModsAsync()
    {
        var modFiles = Directory.GetFiles(_modsPath, "*.jar", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(_modsPath, "*.jar.disabled", SearchOption.TopDirectoryOnly));

        var modInfos = new List<ModInfo>();
        foreach (var file in modFiles)
        {
            try
            {
                // 你现有的ModManager已经很棒了，直接用
                // 注意：GetFabricModInfo 需要扩展以支持 Forge/Quilt
                var modInfo = await ModManager.GetFabricModInfo(file);
                modInfos.Add(modInfo);
            }
            catch
            {
                // 如果某个mod解析失败，可以跳过或记录日志
            }
        }
        return modInfos;
    }

    /// <summary>
    /// 启用一个Mod
    /// </summary>
    public Task EnableModAsync(string modFileName)
    {
        string disabledPath = Path.Combine(_modsPath, $"{modFileName}.disabled");
        string enabledPath = Path.Combine(_modsPath, modFileName);

        if (File.Exists(disabledPath))
        {
            File.Move(disabledPath, enabledPath);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 禁用一个Mod
    /// </summary>
    public Task DisableModAsync(string modFileName)
    {
        string enabledPath = Path.Combine(_modsPath, modFileName);
        string disabledPath = Path.Combine(_modsPath, $"{modFileName}.disabled");

        if (File.Exists(enabledPath))
        {
            File.Move(enabledPath, disabledPath);
        }
        return Task.CompletedTask;
    }
}
