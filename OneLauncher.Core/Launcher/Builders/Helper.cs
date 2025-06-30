using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;
public partial class  LaunchCommandBuilder
{
    internal static string ReplacePlaceholders(string input, Dictionary<string, string> placeholders)
    {
        foreach (var kvp in placeholders)
        {
            input = input.Replace("${" + kvp.Key + "}", kvp.Value);
        }
        return input;
    }
}
