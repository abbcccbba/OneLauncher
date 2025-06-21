using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Mod.ModPack.JsonModels;
using System.Text.Json;

namespace OneLauncher.Core.Mod.ModPack;
public class MrpackParser
{
    private readonly ModrinthManifest manifest;

    public MrpackParser(Stream manifest)
    {
        try
        {
            this.manifest = JsonSerializer.Deserialize(manifest, MrpackJsonContext.Default.ModrinthManifest)
                       ?? throw new JsonException("反序列化返回 null。");
        }
        catch (JsonException ex)
        {
            throw new OlanException("解析失败", "无法解析 modrinth.index.json。文件可能已损坏或格式不正确。", OlanExceptionAction.Error,ex);
        }
    }

    public string GetName() => manifest.Name;
    public string GetMinecraftVersion()
    {
        if (manifest.Dependencies.TryGetValue("minecraft", out var version))
            return version;

        throw new OlanException("清单格式错误", "在 dependencies 中找不到指定的 'minecraft' 版本。", OlanExceptionAction.Error);
    }
    public (ModEnum Type, string Version) GetLoaderInfo()
    {
        if (manifest.Dependencies.TryGetValue("fabric-loader", out var fabricVersion))
            return (ModEnum.fabric, fabricVersion);

        if (manifest.Dependencies.TryGetValue("neoforge", out var neoForgeVersion))
            return (ModEnum.neoforge, neoForgeVersion);

        var unsupportedLoader = manifest.Dependencies.Keys.FirstOrDefault(k => k.Contains("forge") || k.Contains("quilt"));
        if (unsupportedLoader != null)
            throw new OlanException("加载器不兼容", $"该整合包需要 '{unsupportedLoader}' 加载器，当前启动器不支持。", OlanExceptionAction.Error);

        throw new OlanException("加载器不兼容", "未在整合包中找到受支持的加载器 (Fabric 或 NeoForge)。", OlanExceptionAction.Error);
    }
    public List<NdDowItem> GetLibraries(string instanceName, string modPath)
    {
        var items = new List<NdDowItem>(manifest.Files.Count);

        foreach (var file in manifest.Files)
        {
            items.Add(new NdDowItem(
                Url: file.Downloads.FirstOrDefault(),
                Path:Path.Combine(modPath,Path.Combine(file.Path.Split('/'))),
                Size:file.FileSize,
                Sha1:file.Hashes["sha1"]));
        }

        return items;
    }
}