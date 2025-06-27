using OneLauncher.Core.Global;
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
    public string VersionPath { get => Path.Combine(Init.GameRootPath,"versions",VersionID);}
    public ModType modType { get; set; }
    public DateTime AddTime { get; set; }
    public PreferencesLaunchMode preferencesLaunchMode { get; set; }
    public override string ToString()
    {
        return VersionID;
    }
}