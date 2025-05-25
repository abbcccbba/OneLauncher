using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneLauncher.Core.Net.java;

public class AutoJavaGetter
{
    // https://api.adoptium.net/v3/assets/feature_releases/21/ga?architecture=x64&os=mac&image_type=jre
    public static async Task JavaReleaser(string javaVersion,string savePath,SystemType OsType) //=> Task.Run( async () =>
    {
        var javaZipPath = Path.Combine(savePath,javaVersion, $"{javaVersion}.zip");
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
        
        Download.ExtractFile(javaZipPath, Path.Combine(savePath,javaVersion));
        File.Delete(javaZipPath);
        
    }//);

    public static async Task<string> GetBinaryPackageLinkAsync(string apiUrl, HttpClient client)
    {
        try
        {
            using (Stream responseStream = await client.GetStreamAsync(apiUrl))
            {

                JsonNode rootNode = await JsonNode.ParseAsync(responseStream);

                string link = rootNode? // 根节点
                              .AsArray()? // 尝试作为数组
                              .FirstOrDefault()? // 取第一个元素
                              .AsObject()? // 尝试作为对象
                              .TryGetPropertyValue("binaries", out JsonNode binariesNode) == true
                                ? binariesNode.AsArray()? // 尝试作为binaries数组
                                .FirstOrDefault()? // 取第一个元素
                                .AsObject()? // 尝试作为对象
                                .TryGetPropertyValue("package", out JsonNode packageNode) == true
                                    ? packageNode.AsObject()? // 尝试作为package对象
                                    .TryGetPropertyValue("link", out JsonNode linkNode) == true
                                        ? linkNode.GetValue<string>()
                                     : null // link属性未找到或不是值
                                 : null
                              : null;

                return link;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"网络请求错误: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON 解析错误: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生未知错误: {ex.Message}");
            return null;
        }
    }
}
