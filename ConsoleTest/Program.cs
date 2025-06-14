using OneLauncher.Core.Downloader;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Core.Net.msa;
using System;
using System.Diagnostics;
using System.Text.Json;

using var down = new Download();
Console.WriteLine(DateTime.Now.ToString());
await down.DownloadFileBig
    (
    "https://piston-data.mojang.com/v1/objects/b88808bbb3da8d9f453694b5d8f74a3396f1a533/client.jar", 
    "F:\\1.jar", 
    28984409,
    4,
    CancellationToken.None
    );
Console.WriteLine(DateTime.Now.ToString());
await down.DownloadFile("https://piston-data.mojang.com/v1/objects/b88808bbb3da8d9f453694b5d8f74a3396f1a533/client.jar", "F:\\2.jar");
Console.WriteLine(DateTime.Now.ToString());