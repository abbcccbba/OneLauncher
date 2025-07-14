using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace OneLauncher.Core.Net.JavaProviders;
internal class AzulZuluAPI : BaseJavaProvider, IJavaProvider
{
    //https://api.azul.com/metadata/v1/zulu/packages/?java_version=8&os=windows&arch=x64&java_package_type=jre&availability_types=CA&release_status=ga&latest=true
    public AzulZuluAPI(int javaVersion)
        : base(javaVersion,null)
    {}
    public Task GetAutoAsync()
    {
        string apiUrl = 
            $"https://api.azul.com/metadata/v1/zulu/packages/?java_version={javaVersion}&os={systemTypeName}&arch={systemArchName}&java_package_type=jre&availability_types=CA&release_status=ga&latest=true";
        return GetAndDownloadAsync(() => GetDownloadUrl(apiUrl,CancelToken ?? CancellationToken.None));
    }
    private async Task<string?> GetDownloadUrl(string apiUrl,CancellationToken token)
    {
        using var responseStream = httpClient.GetStreamAsync(apiUrl,token);
        JsonNode? rootNode = await JsonNode.ParseAsync(await responseStream,cancellationToken: token);
        return rootNode
            ?.AsArray() // 视为数组
            ?.FirstOrDefault() // 获取第一个元素
            ?["download_url"] // 获取 download_url 字段
            ?.GetValue<string>(); // 转换为字符串
    }
}
