using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Mod.ModPack.JsonModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OneLauncher.Core.Mod.ModPack;

/// <summary>
/// 解析 Modrinth 清单文件 (modrinth.index.json) 并提取关键信息。
/// </summary>
public class MrpackParser
{
    private readonly ModrinthManifest _manifest;

    public MrpackParser(Stream manifestStream)
    {
        try
        {
            _manifest = JsonSerializer.Deserialize(manifestStream, MrpackJsonContext.Default.ModrinthManifest)
                       ?? throw new JsonException("反序列化 modrinth.index.json 返回 null。");
        }
        catch (JsonException ex)
        {
            throw new OlanException("解析失败", "无法解析 modrinth.index.json。文件可能已损坏或格式不正确。", OlanExceptionAction.Error, ex);
        }
    }

    public string GetName() => _manifest.Name;

    public string GetMinecraftVersion()
    {
        if (_manifest.Dependencies.TryGetValue("minecraft", out var version))
            return version;

        throw new OlanException("清单格式错误", "在 dependencies 中找不到指定的 'minecraft' 版本。", OlanExceptionAction.Error);
    }

    public (ModEnum Type, string Version) GetLoaderInfo()
    {
        if (_manifest.Dependencies.TryGetValue("fabric-loader", out var fabricVersion))
            return (ModEnum.fabric, fabricVersion);

        if (_manifest.Dependencies.TryGetValue("neoforge", out var neoForgeVersion))
            return (ModEnum.neoforge, neoForgeVersion);

        if (_manifest.Dependencies.TryGetValue("forge", out var forgeVersion))
            return (ModEnum.forge, forgeVersion);

        if (_manifest.Dependencies.TryGetValue("quilt-loader", out var quiltVersion))
            return (ModEnum.quilt, quiltVersion);

        var unsupportedLoader = _manifest.Dependencies.Keys.FirstOrDefault(k => k.Contains("loader"));
        if (unsupportedLoader != null)
            throw new OlanException("加载器不兼容", $"该整合包需要 '{unsupportedLoader}' 加载器，当前启动器暂不支持。", OlanExceptionAction.Error);

        throw new OlanException("加载器不兼容", "未在整合包中找到受支持的加载器。", OlanExceptionAction.Error);
    }

    /// <summary>
    /// 从清单中获取所有需要下载的 Mod 文件信息。
    /// </summary>
    /// <param name="modsPath">要将Mod下载到的目标目录 (例如 .../instance/xxxx/mods)。</param>
    /// <returns>一个包含所有Mod下载信息的列表。</returns>
    public List<NdDowItem> GetModFiles(string modsPath)
    {
        var items = new List<NdDowItem>(_manifest.Files.Count);

        foreach (var file in _manifest.Files)
        {
            // 有些整合包文件路径可能包含 "overrides" 文件夹，我们需要过滤掉这些，只处理mods
            if (file.Path.StartsWith("overrides"))
            {
                continue;
            }

            // 使用 Path.Combine 来处理不同操作系统的路径分隔符
            // file.Path 已经是相对路径，如 "mods/fabric-api-0.91.0.jar"
            string finalPath = Path.Combine(modsPath, Path.GetFileName(file.Path));

            items.Add(new NdDowItem(
                Url: file.Downloads.FirstOrDefault(),
                Path: finalPath,
                Size: (int)file.FileSize,
                Sha1: file.Hashes.GetValueOrDefault("sha1")
            ));
        }

        return items;
    }
}