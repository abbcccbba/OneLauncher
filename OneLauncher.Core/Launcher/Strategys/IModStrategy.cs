using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher.Strategys;

internal interface IModStrategy
{
    IEnumerable<string> GetJvmArgsAfrerVanilla();
    IEnumerable<string> GetClassPathBeforeVanilla();
    IEnumerable<string> GetGameArgsAfterVanilla();
    string GetMainClass();
}
