using OneLauncher.Core.Global;
using OneLauncher.Core.Launcher;
using OneLauncher.Core.Helper.Models;
using System.Diagnostics;
using System.Text;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Net.Account.Yggdrasil.ServiceProviders; // 为了使用 TextHelper

class Program
{
    static async Task Main(string[] args)
    {
        await Init.Initialize();
        
    }
}