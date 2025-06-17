//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;

//namespace OneLauncher.Core.Net.ConnectToolPower;

//// 负责与 main.exe 直接交互的类
//internal class MainPower : IDisposable
//{
//    private const string CoreExecutableName = "main.exe";
//    private const string CoreUrl = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew.exe";
//    private const string CoreMd5 = "08160296509deac13e7d12c8754de9ef";

//    private readonly string _coreDirectory;
//    private readonly string _coreFilePath;
//    private Process? _process;

//    public event Action<string>? OnLogReceived;

//    public MainPower()
//    {
//        // 使用你项目定义的路径来存储核心文件
//        _coreDirectory = Path.Combine(Init.BasePath, "bin", "ConnectTool");
//        _coreFilePath = Path.Combine(_coreDirectory, CoreExecutableName);
//    }

//    /// <summary>
//    /// 检查并下载核心文件。
//    /// </summary>
//    private async Task EnsureCoreFileAsync()
//    {
//        Directory.CreateDirectory(_coreDirectory);

//        string? currentMd5 = await ConnectHelpers.CalculateFileMD5Async(_coreFilePath);

//        if (CoreMd5.Equals(currentMd5, StringComparison.OrdinalIgnoreCase))
//        {
//            Log("P2P核心已存在且校验通过。");
//            return;
//        }

//        Log(currentMd5 == null ? "P2P核心不存在，开始下载..." : "P2P核心校验失败，重新下载...");

//        using (var client = new HttpClient())
//        {
//            var response = await client.GetAsync(CoreUrl);
//            response.EnsureSuccessStatusCode();
//            using (var fs = new FileStream(_coreFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
//            {
//                await response.Content.CopyToAsync(fs);
//            }
//        }
//        Log("P2P核心下载完成。");
//    }

//    /// <summary>
//    /// 异步启动核心进程。
//    /// </summary>
//    /// <param name="arguments">启动参数。</param>
//    public async Task StartAsync(string arguments)
//    {
//        if (_process != null && !_process.HasExited)
//        {
//            throw new InvalidOperationException("P2P核心已经在运行中。");
//        }

//        await EnsureCoreFileAsync();

//        _process = new Process
//        {
//            StartInfo = new ProcessStartInfo
//            {
//                FileName = _coreFilePath,
//                Arguments = arguments,
//                WorkingDirectory = _coreDirectory,
//                UseShellExecute = false,
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                CreateNoWindow = true,
//                StandardOutputEncoding = Encoding.UTF8,
//                StandardErrorEncoding = Encoding.UTF8
//            },
//            EnableRaisingEvents = true
//        };

//        _process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log(e.Data); };
//        _process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log($"[ERROR] {e.Data}"); };

//        _process.Start();
//        _process.BeginOutputReadLine();
//        _process.BeginErrorReadLine();
//        Log("P2P核心进程已启动。");
//    }

//    /// <summary>
//    /// 停止核心进程。
//    /// </summary>
//    public void Stop()
//    {
//        if (_process != null && !_process.HasExited)
//        {
//            try
//            {
//                _process.Kill(true); // Kill the entire process tree if necessary
//                _process.WaitForExit();
//                Log("P2P核心已停止。");
//            }
//            catch (Exception ex)
//            {
//                Log($"停止核心时发生错误: {ex.Message}");
//            }
//            finally
//            {
//                _process.Dispose();
//                _process = null;
//            }
//        }
//    }

//    private void Log(string message) => OnLogReceived?.Invoke(message);

//    public void Dispose() => Stop();
//}