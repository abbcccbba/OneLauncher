using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.Bundle;
using OneLauncher.Core;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.Mod.ModPack;
using OneLauncher.Core.Net.ConnectToolPower; 
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
await Init.Initialize();
async Task VersionManifestReader()
{
    VersionsList vl;
    /*
     此方法中代码可能经过三个路径
    1、不存在清单文件，下载成功，读取
    2、不存在清单文件，下载失败，写入失败信息
    3、存在清单文件，读取
     */
    if (!File.Exists(Path.Combine(Init.BasePath, "version_manifest.json")))
    {
            // 路径（1）
            using (Download download = new Download())
                await download.DownloadFile(
                    "https://piston-meta.mojang.com/mc/game/version_manifest.json",
                    Path.Combine(Init.BasePath, "version_manifest.json")
                );
            vl = new VersionsList(await File.ReadAllTextAsync(Path.Combine(Init.BasePath, "version_manifest.json")));

    }
    // 路径（3）
    vl = new VersionsList(await File.ReadAllTextAsync(Path.Combine(Init.BasePath, "version_manifest.json")));
    // 提前缓存避免UI线程循环卡顿
    List<VersionBasicInfo> releaseVersions = Init.MojangVersionList = vl.GetReleaseVersionList();
}
await VersionManifestReader();
await ModpackImporter.ImportFromMrpackAsync(@"F:\User.WWWIN\dos\Better MC [NEOFORGE] BMC5 v31.mrpack",Init.GameRootPath);































//Console.WriteLine("OAuth 客户端测试程序 - 手动交换令牌模式");
//Console.WriteLine("------------------------------------------");

//// 你哥们提供的信息
//var authority = "http://110.42.59.70:5100";
//var clientId = "10001";
//var clientSecret = "618470616107912bf450b448b5530282";
//var scope = "profile email";
//var port = 51762; // 他要求的端口
//var redirectUri = $"http://127.0.0.1:{port}/";

//// --- 步骤 1: 使用你升级后的 QOauth 类获取 Code ---
//var browser = new QOauth(port); // 把端口号传进去
//var startUrl = $"http://110.42.59.70:5100/oauth/authorize?response_type=code&client_id=10001&scope=profile&redirect_uri=http://127.0.0.1:51762";
//var browserOptions = new Duende.IdentityModel.OidcClient.Browser.BrowserOptions(startUrl, redirectUri);

//Console.WriteLine("准备打开浏览器获取 Code...");
//var browserResult = await browser.InvokeAsync(browserOptions);

//if (browserResult.IsError)
//{
//    Console.WriteLine($"获取 Code 失败: {browserResult.Error}");
//    return;
//}

//// 从回调 URL 中解析出 Code
//var code = new Uri(browserResult.Response).Query
//    .Split(new[] { '&', '?' }, StringSplitOptions.RemoveEmptyEntries)
//    .FirstOrDefault(s => s.StartsWith("code="))?
//    .Substring(5);

//if (string.IsNullOrEmpty(code))
//{
//    Console.WriteLine("未能从回调中获取到 Code。");
//    return;
//}

//Console.ForegroundColor = ConsoleColor.Green;
//Console.WriteLine($"成功获取到 Code: {code}");
//Console.ResetColor();
//Console.WriteLine("------------------------------------------");

//// --- 步骤 2: 手动携带 Secret 交换 Token ---
//Console.WriteLine("正在使用 Code 和 Secret 交换 Access Token...");
//using var httpClient = new HttpClient();

//var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
//{
//    { "grant_type", "authorization_code" },
//    { "code", code },
//    { "redirect_uri", redirectUri },
//    { "client_id", clientId },
//    { "client_secret", clientSecret }
//});

//try
//{
//    var tokenEndpoint = $"{authority}/oauth/token"; // 令牌端点的地址
//    Console.WriteLine($"正在向 {tokenEndpoint} 发送 POST 请求...");
//    var tokenResponse = await httpClient.PostAsync(tokenEndpoint, tokenRequestContent);
//    var responseString = await tokenResponse.Content.ReadAsStringAsync();

//    if (!tokenResponse.IsSuccessStatusCode)
//    {
//        Console.ForegroundColor = ConsoleColor.Red;
//        Console.WriteLine($"交换 Token 失败: {(int)tokenResponse.StatusCode} {tokenResponse.ReasonPhrase}");
//        Console.WriteLine($"服务器响应: {responseString}");
//        Console.ResetColor();
//        return;
//    }

//    Console.ForegroundColor = ConsoleColor.Green;
//    Console.WriteLine("成功获取到 Access Token!");
//    Console.ResetColor();

//    using var jsonDoc = JsonDocument.Parse(responseString);
//    var formattedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
//    Console.WriteLine(formattedJson);
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"请求时发生异常: {ex.Message}");
//}

//Console.WriteLine("\n测试结束，按任意键退出...");
//Console.ReadKey();
