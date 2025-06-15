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

var t  =SystemInfoHelper.GetMemoryMetrics();
Console.WriteLine($"{t.TotalMB},{t.FreeMB},{t.UsedMB}");