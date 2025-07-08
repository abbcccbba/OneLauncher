using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.Models;
public class ModInfo
{
    public string Id { get; set; }
    public string Version { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public byte[]? Icon { get; set; } // 可能为null
    public bool IsEnabled { get; set; }
    public string fileName;
}
