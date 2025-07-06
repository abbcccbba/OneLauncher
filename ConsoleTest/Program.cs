using OneLauncher.Core.Helper;

string mdp = @"C:\Users\wwwin\OneLauncher\installed\.minecraft\instance\122b255e\mods";
var files = Directory.GetFiles(mdp);
foreach (var file in files)
{
    try
    {
        var modInfo = await OneLauncher.Core.Mod.ModManager.GetFabricModInfo(file);
        Console.WriteLine($"模组名称: {modInfo.Name}, 版本: {modInfo.Version}, 描述: {modInfo.Description}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"处理模组文件 '{file}' 时出错: {ex.Message}");
    }
}