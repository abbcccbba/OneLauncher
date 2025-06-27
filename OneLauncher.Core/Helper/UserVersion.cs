using OneLauncher.Core.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper;
public class UserVersion
{
    public string VersionID { get; set; }
    [JsonIgnore]
    public string VersionPath { get => Path.Combine(Init.GameRootPath,"versions",VersionID);}
    public ModType modType { get; set; }
    public DateTime AddTime { get; set; }
    public override string ToString()
    {
        return VersionID;
    }
}