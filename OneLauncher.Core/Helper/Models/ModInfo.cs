using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.Models;
public class ModInfo
{
    public string Id;
    public string Version;
    public string Name;
    public string Description;
    public byte[]? Icon; // 可能为null
    public bool IsEnabled;
}
