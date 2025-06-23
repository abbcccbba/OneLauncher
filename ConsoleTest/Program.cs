using OneLauncher.Core.Compatible.ImportPCL2Version;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.ImportPCL2Version;
using OneLauncher.Core.Minecraft;

await Init.Initialize();
var par = new PCL2SetupFucker(@"E:\mc\.minecraft\versions\1666\PCL\Setup.ini");
Console.WriteLine(par.GetMinecraftVersion());
Console.WriteLine(par.GetModLoader());