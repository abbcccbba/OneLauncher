using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher.Strategys;

internal interface IModStrategy
{
    string GetMainClassOverride();
    IEnumerable<(string key,string path)> GetModLibraries();
    IEnumerable<string> GetAdditionalJvmArgs();
    IEnumerable<string> GetAdditionalGameArgs();
}
