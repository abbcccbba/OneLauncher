using OneLauncher.Core.Global;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Console;
public class boot
{
    public static async Task RunBoot(string[] args)
    {
        try
        {
            await Init.Initialize();
            if (args.Length != 2)
                return;
            switch (args[0])
            {
                case "--quicklyPlay":
                    await QuicklyPlay.GameLauncher.Launch(args[1]);
                    break;
            }
        }
        catch (Exception e) { 
            Environment.FailFast(e.ToString());
        }
    }
}
