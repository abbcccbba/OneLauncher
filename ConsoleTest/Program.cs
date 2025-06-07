using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Core.Net.msa;
using System.Text.Json;

SystemEC a = new();
var ID = a.SetRefreshToken("afsufgafgasufghasufhuafhuashfashfuashf");
Console.WriteLine(ID);
var token = a.GetRefreshToken(ID);
Console.WriteLine(token);