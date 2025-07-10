using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;
public struct LaunchCommand(IEnumerable<string> fileArgs, IEnumerable<string> commandArgs) : IDisposable
{
    private IEnumerable<string> commandArgs = commandArgs; // 必须在命令行的参数
    private IEnumerable<string> fileArgs = fileArgs; // 可以在命令行也可以在文件中的参数
    private string? tempFilePath = null;
    public void Dispose()
    {
        if (tempFilePath != null)
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
    }

    public async Task<string> GetArguments()
    {
        string launchArg = string.Join(" ", fileArgs);
        string launchCommand = string.Join(" ", commandArgs);
        if (launchArg.Length > 8000) // 标准是8191的命令行长度上限，这里考虑到Java本身的路径
        {
            tempFilePath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFilePath, launchArg
#if WINDOWS
                .Replace("\\", @"\\") // Windows需要转义
#endif
                );
            return $"{launchCommand} @\"{tempFilePath}\"";
        }
        else
            return $"{launchCommand} {launchArg}";
    }
}