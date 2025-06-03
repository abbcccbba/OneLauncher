using OneLauncher.Core.Downloader;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneLauncher.Core.Net.java;

public class AutoJavaGetter
{
    // https://api.adoptium.net/v3/assets/feature_releases/21/ga?architecture=x64&os=mac&image_type=jre
    public static async Task JavaReleaser(string javaVersion, string savePath, SystemType OsType) //=> Task.Run( async () =>
    {
        var javaZipPath = Path.Combine(savePath, javaVersion, $"{javaVersion}.zip");
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
            await a.DownloadFile(
                await GetBinaryPackageLinkAsync(
                    $"https://api.adoptium.net/v3/assets/feature_releases/{javaVersion}/ga?architecture={arch}&os={os}&image_type=jre"
                    , a.UnityClient), javaZipPath);
        }

        Download.ExtractFile(javaZipPath, Path.Combine(savePath, javaVersion));
        File.Delete(javaZipPath);

    }//);

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
                return link ?? null;
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
