using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Net.ConnectToolPower;
internal static class ConnectToolHelpers
{
    private const string CoreExecutableName = "main.exe";
    private static readonly string CoreDirectory = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp");
    private static readonly string CoreFilePath = Path.Combine(CoreDirectory, CoreExecutableName);

    /// <summary>
    /// 确保核心可执行文件存在且版本正确。如果需要，会自动下载。
    /// </summary>
    /// <param name="logCallback">用于报告进度的日志回调函数。</param>
    public static async Task EnsureCoreFileAsync(Action<string> logCallback)
    {
        Directory.CreateDirectory(CoreDirectory);

        string url_64 = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew.exe";
        string md5_64 = "08160296509deac13e7d12c8754de9ef";
        string url_32 = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/main32.exe";
        string md5_32 = "e8f1007a43eb520eecf9c0fade0300b0";

        string targetUrl = Environment.Is64BitOperatingSystem ? url_64 : url_32;
        string targetMd5 = Environment.Is64BitOperatingSystem ? md5_64 : md5_32;

        bool needsDownload = true;
        if (File.Exists(CoreFilePath))
        {
            string currentMd5 = await CalculateFileMD5Async(CoreFilePath);
            // 允许已存在任意一个有效版本，避免不必要的下载
            if (currentMd5.Equals(md5_64, StringComparison.OrdinalIgnoreCase) ||
                currentMd5.Equals(md5_32, StringComparison.OrdinalIgnoreCase))
            {
                needsDownload = false;
                logCallback("P2P核心已存在且校验通过。");
            }
        }

        if (needsDownload)
        {
            logCallback($"开始下载P2P核心文件从: {targetUrl}");
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(targetUrl);
                response.EnsureSuccessStatusCode();
                using (var fs = new FileStream(CoreFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
            logCallback("P2P核心下载完成。");
        }
    }

    /// <summary>
    /// 启动核心进程并附加日志事件处理器。
    /// </summary>
    /// <param name="arguments">要传递给进程的命令行参数。</param>
    /// <param name="outputHandler">处理标准输出的回调。</param>
    /// <param name="errorHandler">处理标准错误的回调。</param>
    /// <returns>启动的 Process 对象。</returns>
    public static Process StartCoreProcess(string arguments, Action<string> outputHandler, Action<string> errorHandler)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = CoreFilePath,
                Arguments = arguments,
                WorkingDirectory = CoreDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data)) outputHandler(args.Data);
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data)) errorHandler($"[ERROR] {args.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        outputHandler("P2P核心进程已启动。");
        return process;
    }

    /// <summary>
    /// 异步计算文件的MD5哈希值。
    /// </summary>
    /// <param name="filePath">文件路径。</param>
    /// <returns>小写的MD5字符串。</returns>
    private static async Task<string> CalculateFileMD5Async(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                byte[] hash = await md5.ComputeHashAsync(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
