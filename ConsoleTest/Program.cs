using OneLauncher.Core; 
using OneLauncher.Core.Net.ConnectToolPower; 
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
await Init.Initialize();
var mainPower = await MainPower.InitializationAsync();
mainPower.CoreLog += (logMessage) =>
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"[Core Log] -> {logMessage}");
};
Console.WriteLine("输入1作为房主，输入2作为加入者");
string r = Console.ReadLine();
if(r == "1")
{
    await mainPower.LaunchCore("-node consoletest666 -token 17073157824633806511");
}
else
{
    Console.WriteLine("输入一个你认为没有被占用过的端口号");
    string p = Console.ReadLine();
    await mainPower.LaunchCore($"-node consoletest888 -peernode consoletest666 -srcport {p} -dstport 55555 -dstip 127.0.0.1 -appname Minecraft23456 -token 17073157824633806511");
}
mainPower.Dispose();
Console.ReadKey();

































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
