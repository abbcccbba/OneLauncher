using OneLauncher.Core.Downloader;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Core.Net.msa;
using System;
using System.Diagnostics;
using System.Text.Json;

using var down = new Download();
Stopwatch stopwatch = new Stopwatch();

Console.WriteLine($"开始 DownloadFileBig: {DateTime.Now}");
stopwatch.Start();
await down.DownloadFileBig(
    "https://piston-data.mojang.com/v1/objects/b88808bbb3da8d9f453694b5d8f74a3396f1a533/client.jar",
    "F:\\1.jar",
    28984409,
    12,
    CancellationToken.None
);
stopwatch.Stop();
Console.WriteLine($"完成 DownloadFileBig: {DateTime.Now}");
Console.WriteLine($"耗时: {stopwatch.ElapsedMilliseconds} 毫秒\n");
stopwatch.Reset();
Console.WriteLine($"开始 DownloadFile: {DateTime.Now}");
stopwatch.Start();
await down.DownloadFile(
    "https://piston-data.mojang.com/v1/objects/b88808bbb3da8d9f453694b5d8f74a3396f1a533/client.jar",
    "F:\\2.jar"
);
stopwatch.Stop();
Console.WriteLine($"完成 DownloadFile: {DateTime.Now}");
Console.WriteLine($"耗时: {stopwatch.ElapsedMilliseconds} 毫秒");