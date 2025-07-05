using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Launcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Console;
public class Boot
{
    public static async Task RunBoot(string[] args)
    {
        try
        {
            await Init.Initialize();
            switch (args[0])
            {
                case "--quicklyPlay":
                    await new GameLauncher().Play(args[1]);
                    break;
                case "--releaseMemory":
                    await ReleaseMemory.OptimizeAsync();
                    break;
            }
        }
        catch (Exception e) { 
            Environment.FailFast(e.ToString());
        }
    }
}
