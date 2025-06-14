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
// 单个Url分段
Console.WriteLine(@"
【开始测试】
你会看到两个结果
第一个是52分段的多分段下载
第二个传统的单线程下载
保存路径是C盘

");
Console.WriteLine($"开始 {DateTime.Now}");
stopwatch.Start();
await down.DownloadFileBig(
    "https://piston-data.mojang.com/v1/objects/b88808bbb3da8d9f453694b5d8f74a3396f1a533/client.jar",
    "C:\\test1.jar",
    28984409,
    52
);
stopwatch.Stop();

Console.WriteLine($"完成: {DateTime.Now}");
Console.WriteLine($"耗时: {stopwatch.ElapsedMilliseconds} 毫秒\n");

stopwatch.Reset();
Console.WriteLine($"开始: {DateTime.Now}");
stopwatch.Start();
await down.DownloadFile(
    "https://piston-data.mojang.com/v1/objects/b88808bbb3da8d9f453694b5d8f74a3396f1a533/client.jar",
    "F:\\one.tar.gz"
);
stopwatch.Stop();
Console.WriteLine($"完成: {DateTime.Now}");
Console.WriteLine($"耗时: {stopwatch.ElapsedMilliseconds} 毫秒");