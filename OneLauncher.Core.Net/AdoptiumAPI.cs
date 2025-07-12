using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneLauncher.Core.Net;

public class AdoptiumAPI
{
    // https://api.adoptium.net/v3/assets/feature_releases/21/ga?architecture=x64&os=mac&image_type=jre
    public static async Task JavaReleaser(string javaVersion, string savePath, SystemType OsType) 
    {
        var javaDownloadPath = Path.Combine(savePath, javaVersion, $"{javaVersion}.zip");
        var os = OsType switch
        {
            SystemType.windows => "windows",
            SystemType.linux => "linux",
            SystemType.osx => "mac",
            _ => throw new OlanException("不支持的操作系统", "当前操作系统不受支持，请使用Windows、Linux或macOS。")
        };
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "aarch64",
            _ => throw new OlanException("不支持的架构", "当前操作系统架构不受支持，请使用x64或aarch64架构的操作系统。")
        };

        await Init.Download.DownloadFileBig(
            url: await GetBinaryPackageLinkAsync(
                $"https://api.adoptium.net/v3/assets/feature_releases/{javaVersion}/ga?architecture={arch}&os={os}&image_type=jre"
                , Init.Download.unityClient),
            savePath: javaDownloadPath,
            knownSize:null,
            maxSegments: 6);
        
        // 对于windows，api返回的是zip，对于mac或者linux，api返回的是tag.gz
        if (OsType == SystemType.windows)
            Download.ExtractFile(javaDownloadPath, Path.Combine(savePath, javaVersion));
        else if (OsType == SystemType.osx || OsType == SystemType.linux)
        {
            var tempTarPath = Path.Combine(savePath, javaVersion, $"{javaVersion}.tar");

            using (FileStream sourceFileStream = new FileStream(javaDownloadPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (GZipStream gzipStream = new GZipStream(sourceFileStream, CompressionMode.Decompress))
                {
                    using (FileStream tempTarFileStream = new FileStream(tempTarPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await gzipStream.CopyToAsync(tempTarFileStream);
                    }
                }
            }

            // 步骤 2: 异步解压临时 .tar 文件到目标目录
            using (FileStream tarFileStream = new FileStream(tempTarPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // TarFile.ExtractToDirectoryAsync 接受一个 Stream 参数
                await TarFile.ExtractToDirectoryAsync(tarFileStream, Path.Combine(savePath, javaVersion), overwriteFiles: true);
            }
            File.Delete(tempTarPath);
        }
        File.Delete(javaDownloadPath);
    }

    public static async Task<string> GetBinaryPackageLinkAsync(string apiUrl, HttpClient client)
    {
        using (Stream responseStream = await client.GetStreamAsync(apiUrl))
        {

            JsonNode rootNode = await JsonNode.ParseAsync(responseStream) 
                ?? throw new OlanException("无法解析Java","从服务器解析数据时出错，无法解析到值");

            string link = rootNode?             // 根节点
                            .AsArray()?         // 尝试将其视为数组
                            [0]?  // 获取数组的第一个元素
                            ["binaries"]?       // 访问名为 "binaries" 的属性
                            .AsArray()?         // 尝试将其视为数组
                            [0]?  // 获取 "binaries" 数组的第一个元素
                            ["package"]?        // 访问名为 "package" 的属性
                            ["link"]?           // 访问名为 "link" 的属性
                            .GetValue<string>() // 获取其字符串值
                    ?? throw new OlanException("无法解析Java","最终无法从服务器返回数据取到下载地址"); 

            // 我造密码的编译器报错了就是返回null
            return link;
        }
    }
}