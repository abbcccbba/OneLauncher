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

public class ModManager
{
    public readonly ModInfo NullModInfo = new ModInfo
    {
        Id = "未知标识符",
        Version = "未知版本",
        Name = "未知名称",
        Description = "未知描述",
        Icon = null,
        IsEnabled = false
    };
    public static async Task<ModInfo> GetFabricModInfo(string filePath)
    {
        if (!File.Exists(filePath))
            throw new OlanException("模组文件不存在", $"无法找到模组文件'{filePath}'", OlanExceptionAction.Warning);
        using FileStream modFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4192, true);
        using ZipArchive modOpener = new ZipArchive(modFileStream, ZipArchiveMode.Read, true);
        FabricModJson info = await JsonSerializer.DeserializeAsync<FabricModJson>(
            modOpener.GetEntry("fabric.mod.json")?.Open()
            ?? throw new OlanException("无法读取Fabric模组信息", $"无法找到模组文件'{filePath}'的自述文件", OlanExceptionAction.Warning),
            FabricModJsonContext.Default.FabricModJson
        ) ?? throw new OlanException("无法读取Fabric模组信息", $"反序列化模组描述文件'{filePath}'出错", OlanExceptionAction.Warning);
        // 处理图标
        byte[]? iconBytes = null;
        if (info.Icon != null)
        {
            using (MemoryStream iconStream = new MemoryStream())
            {
                ZipArchiveEntry? iconEntry = modOpener.GetEntry(info.Icon);
                if (iconEntry != null)
                {
                    using (Stream iconFileStream = iconEntry.Open())
                    {
                        await iconFileStream.CopyToAsync(iconStream);
                    }
                    iconBytes = iconStream.ToArray();
                }
            }
        }
        return new ModInfo
        {
            Id = info.Id ?? "未知标识符",
            Version = info.Version ?? "未知标识符",
            Name = info.Name ?? "未知标识符",
            Description = info.Description ?? "未知标识符",
            Icon = iconBytes,
            IsEnabled = Path.GetFileName(filePath).Split('.')[^1] == "disabled" ? false : true
        };
    }
}