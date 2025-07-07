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
        if (string.IsNullOrEmpty(input) || placeholders == null || placeholders.Count == 0)
        {
            return input;
        }

        var sb = new StringBuilder(input);
        foreach (var kvp in placeholders)
        {
            sb.Replace("${" + kvp.Key + "}", kvp.Value);
        }
        return sb.ToString();
    }
}
