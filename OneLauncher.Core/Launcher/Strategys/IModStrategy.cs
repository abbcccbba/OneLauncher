using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher.Strategys;

internal interface IModStrategy
{
    string GetMainClassOverride();
    IDictionary<string,string> GetModLibraries();
    IEnumerable<string> GetAdditionalJvmArgs();
    IEnumerable<string> GetAdditionalGameArgs();
}
