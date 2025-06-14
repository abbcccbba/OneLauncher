using Duende.IdentityModel.OidcClient;
using OneLauncher.Core;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Core.Net.msa;
using OneLauncher.Core.Net.QuStellar;
using System;
using System.Diagnostics;
using System.Text.Json;


var msa = await MsalMicrosoftAuthenticator.CreateAsync(Init.AzureApplicationID);
var r = await msa.LoginAsync();
Console.WriteLine(r.Value.accessToken,r.Value.Name,r.Value.uuid);





//Console.WriteLine("正在尝试登录...");
//const int port = 52726;
//const string redirectUri = $"http://127.0.0.1:52726/";

//// 配置 OidcClient
//var options = new OidcClientOptions
//{
//    // !! 重要：换成你服务端的真实地址 !!
//    Authority = "https://your-auth-server.com",

//    // !! 重要：换成你自己的 ClientId !!
//    ClientId = "10001",

//    Scope = "openid profile", 
//    RedirectUri = redirectUri,
//    Browser = new QOauth()
//};

//var client = new OidcClient(options);
//LoginResult result = await client.LoginAsync(new LoginRequest());

//Console.WriteLine("------------------------------------------");

//if (result.IsError)
//{
//    Console.WriteLine($"登录失败: {result.Error}");
//}
//else
//{
//    Console.WriteLine("登录成功!");
//    Console.WriteLine($"Access Token: {result.AccessToken}");
//    Console.WriteLine();
//    Console.WriteLine("用户信息 (Claims):");
//    foreach (var claim in result.User.Claims)
//    {
//        Console.WriteLine($"{claim.Type,-20}: {claim.Value}");
//    }
//}

//Console.WriteLine("按任意键退出...");
//Console.ReadKey();