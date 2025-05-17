using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using System.Linq;
using OneLauncher.Core.Models;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace OneLauncher.Core;

/// <summary>
/// 表示Minecraft版本信息的解析器，用于解析version.json文件并提取关键信息。
/// 支持动态解析依赖库、资源索引等。
/// </summary>
public class VersionInfomations
{
    public readonly OneLauncher.Core.Models.VersionInformation info;
    public readonly string basePath;
    public readonly SystemType OsType;
    public List<string> NativesLibs = new List<string>();
    /// <summary>
    /// 初始化VersionInfomations实例，解析version.json字符串。
    /// </summary>
    /// <param name="json">version.json文件的字符串内容。</param>
    /// <param name="basePath">游戏存放目录路径（例如"C:/minecraft/"）。</param>
    /// <param name="OsType">运行时操作系统类型</param>
    /// <exception cref="InvalidOperationException">如果JSON解析失败或内容无效，抛出此异常。</exception>
    public VersionInfomations(string json, string basePath,SystemType OsType)
    {
        this.basePath = basePath;
        this.OsType = OsType;
        try
        {
            info = JsonSerializer.Deserialize<Models.VersionInformation>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("解析版本JSON失败"); // 消除警告     
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("解析版本JSON时出错", ex);
        }
    }

    public List<NdDowItem> GetLibrarys()
    {
        var libraries = new List<NdDowItem>(info.Libraries.Count); // 提前初始化相应长度内存，避免频繁扩容影响性能
            
        foreach (var lib in info.Libraries)
        {
            // 检查规则
            bool allowed = false;
            // 如果包含规则
            if (lib.Rules != null)
            {
                // 判断是双规则还是单规则
                // 先处理双规则
                if(lib.Rules.Count == 2)
                {
                    // 与当前系统尝试匹配
                    // 第二条规则的 action 是 disallow
                    if (lib.Rules[1].Os.Name != OsType.ToString())
                        allowed = true;
                    else allowed = false;
                }
                if(lib.Rules.Count == 1)
                {
                    // 与当前系统尝试匹配
                    if (lib.Rules[0].Os.Name == OsType.ToString())
                        allowed = true;
                    else
                        allowed = false;     
                }
            }
            // 没有规则直接下载
            else
            {
                allowed = true;
            }

            // 不合当前操作系统跳过
            if (!allowed)
                continue;

            // 普通库文件 
            if (lib.Downloads.Artifact != null)
                libraries.Add(new NdDowItem(
                    lib.Downloads.Artifact.Url,
                    lib.Downloads.Artifact.Sha1,
                    Path.Combine(basePath,".minecraft","libraries",lib.Downloads.Artifact.Path)
                ));
            
            // natives库文件
            if (lib.Downloads?.Classifiers != null)
            {
                LibraryArtifact ta;
                try
                {
                    ta = lib.Downloads.Classifiers
                        [OsType == SystemType.windows ? "natives-windows" : OsType == SystemType.osx ? "natives-osx" : "natives-linux"];
                }
                // 某些古早版本可能会针对架构
                catch (System.Collections.Generic.KeyNotFoundException)
                {
                    ta = lib.Downloads.Classifiers
                    [OsType == SystemType.windows ? "natives-windows-64" : OsType == SystemType.osx ? "natives-osx-64" : "natives-linux-64"];
                }
                NativesLibs.Add(ta.Path);
                libraries.Add(new NdDowItem(
                    ta.Url,
                    ta.Sha1,
                    Path.Combine(basePath,".minecraft","libraries",ta.Path)
                ));
            }    
        }

        return libraries;
    }

    /// <summary>
    /// 获取版本主文件下载地址。
    /// </summary>
    /// <param name="version">Minecraft版本号。</param>
    public NdDowItem GetMainFile()
    {
        return new NdDowItem(
            info.Downloads.Client.Url,
            info.Downloads.Client.Sha1,
            Path.Combine(basePath, ".minecraft","versions",info.ID,$"{info.ID}.jar")
        );
    }

    /// <summary>
    /// 获取版本资源索引文件下载地址。
    /// </summary>
    public NdDowItem GetAssets()
    {
        return new NdDowItem(
            info.AssetIndex.Url,
            info.AssetIndex.Sha1,
            Path.Combine(basePath,".minecraft","assets","indexes",$"{info.AssetIndex.Id}.json")  
        );
    }

    /// <summary>
    /// 获取资源索引的版本ID。
    /// </summary>
    public string GetAssetIndexVersion()
    {
        return info.AssetIndex.Id;
    }

    /// <summary>
    /// 获取版本的主类名。
    /// </summary>
    /// <returns>主类名（例如"net.minecraft.client.main.Main"）。</returns>
    public string GetMainClass()
    {
        return info.MainClass;
    }

    /// <summary>
    /// 获取日志配置文件信息。
    /// </summary>
    public NdDowItem? GetLoggingConfig()
    {
        if (info.Logging?.Client?.File == null)
            return null;
        return new NdDowItem(
            info.Logging.Client.File.Url,
            info.Logging.Client.File.Sha1,
            Path.Combine(basePath,".minecraft","versions",info.ID,info.Logging.Client.File.Id)
        );
    }
    public string? GetLoggingConfigPath()
    {
        if (info.Logging?.Client?.File == null)
            return null;
        return Path.Combine(basePath, ".minecraft", "versions", info.ID, info.Logging.Client.File.Id);
    }

    /// <summary>
    /// 获取Java版本信息。
    /// </summary>
    /// <returns>Java版本信息，包含组件名和主要版本号；如果无javaVersion字段，返回null。</returns>
    public int? GetJavaVersion()
    {
        return info.JavaVersion.MajorVersion;
    }
}

