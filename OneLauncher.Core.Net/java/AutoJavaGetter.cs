using OneLauncher.Core.Downloader;
using OneLauncher.Core.Helper;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneLauncher.Core.Net.java;

public class AutoJavaGetter
{
    // https://api.adoptium.net/v3/assets/feature_releases/21/ga?architecture=x64&os=mac&image_type=jre
    public static async Task JavaReleaser(string javaVersion, string savePath, SystemType OsType) 
    {
        var javaDownloadPath = Path.Combine(savePath, javaVersion, $"{javaVersion}.zip");
        var os = OsType switch
        {
            SystemType.windows => "windows",
            SystemType.linux => "linux",
            SystemType.osx => "mac"
        };
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "aarch64"
        };
        using (var a = new Download())
        {
            await a.DownloadFileBig(
                url:await GetBinaryPackageLinkAsync(
                    $"https://api.adoptium.net/v3/assets/feature_releases/{javaVersion}/ga?architecture={arch}&os={os}&image_type=jre"
                    , a.unityClient),
                size:null,
                savepath:javaDownloadPath,
                maxSegments: 6,
                token: CancellationToken.None); 
        }
        // 对于windows，api返回的是zip，对于mac或者linux，api返回的是tag.gz
        if(OsType == SystemType.windows)
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
        try
        {
            using (Stream responseStream = await client.GetStreamAsync(apiUrl))
            {

                JsonNode rootNode = await JsonNode.ParseAsync(responseStream);

                string? link = rootNode?             // 根节点
                                .AsArray()?         // 尝试将其视为数组
                                [0]?  // 获取数组的第一个元素
                                ["binaries"]?       // 访问名为 "binaries" 的属性
                                .AsArray()?         // 尝试将其视为数组
                                [0]?  // 获取 "binaries" 数组的第一个元素
                                ["package"]?        // 访问名为 "package" 的属性
                                ["link"]?           // 访问名为 "link" 的属性
                                .GetValue<string>(); // 获取其字符串值

                // 我造密码的编译器报错了就是返回null
                return link;
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"网络请求错误: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"JSON 解析错误: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"发生未知错误: {ex.Message}");
            return null;
        }
    }
}