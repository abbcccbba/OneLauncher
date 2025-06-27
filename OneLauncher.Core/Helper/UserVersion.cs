using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper;
public struct PreferencesLaunchMode
{
    public ModEnum LaunchModType { get; set; }
    public bool IsUseDebugModeLaunch { get; set; }
}
public class UserVersion
{
    public string VersionID { get; set; }
    public ModType modType { get; set; }
    public DateTime AddTime { get; set; }
    public PreferencesLaunchMode preferencesLaunchMode { get; set; }
    public override string ToString()
    {
        return VersionID;
    }
}