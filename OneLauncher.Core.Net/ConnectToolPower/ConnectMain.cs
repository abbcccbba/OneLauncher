//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace OneLauncher.Core.Net.ConnectToolPower;

//// 定义一个事件参数类，用于传递日志信息
//public class LogEventArgs : EventArgs
//{
//    public string Message { get; }
//    public LogEventArgs(string message) { Message = message; }
//}

//public class P2PService : IDisposable
//{
//    // 核心进程的可执行文件名
//    private const string CoreExecutableName = "main.exe";

//    // 外部进程的引用，用于后续管理（如停止）
//    private Process? _coreProcess;

//    // 日志事件，用于通知ViewModel更新UI
//    public event EventHandler<LogEventArgs>? OnLogReceived;

//    // 构造函数
//    public P2PService()
//    {
//        // 可以在这里进行一些初始化
//    }

//    /// <summary>
//    /// 创建一个联机房间（主机方）
//    /// </summary>
//    /// <returns>返回生成的提示码</returns>
//    public async Task<string> CreateRoomAsync()
//    {
//        if (_coreProcess != null && !_coreProcess.HasExited)
//        {
//            throw new InvalidOperationException("P2P核心已经在运行中，请先停止。");
//        }

//        // 1. 下载并校验核心文件
//        await EnsureCoreFileAsync();

//        // 2. 生成提示码
//        string machineName = Environment.MachineName;
//        var random = new Random();
//        string peerCode = $"{machineName}{random.Next(1, 65536)}";
//        if (peerCode.Length <= 8) // 原作者的逻辑
//        {
//            peerCode += random.Next(100, 1000).ToString();
//        }

//        // 3. 构造启动参数
//        string token = "17073157824633806511"; // 硬编码的Token
//        string arguments = $"-node {peerCode} -token {token} -d";

//        // 4. 启动进程
//        StartCoreProcess(arguments);

//        Log($"房间创建成功，您的提示码是: {peerCode}");
//        return peerCode;
//    }

//    /// <summary>
//    /// 加入一个联机房间（加入方）
//    /// </summary>
//    /// <param name="peerCode">主机的提示码</param>
//    /// <param name="gamePort">主机的游戏端口</param>
//    /// <returns>返回本地需要连接的地址和端口，如 "127.0.0.1:54321"</returns>
//    public async Task<string> JoinRoomAsync(string peerCode, int gamePort)
//    {
//        if (_coreProcess != null && !_coreProcess.HasExited)
//        {
//            throw new InvalidOperationException("P2P核心已经在运行中，请先停止。");
//        }

//        if (string.IsNullOrWhiteSpace(peerCode))
//            throw new ArgumentNullException(nameof(peerCode));
//        if (gamePort <= 0 || gamePort > 65535)
//            throw new ArgumentOutOfRangeException(nameof(gamePort));

//        // 1. 下载并校验核心文件
//        await EnsureCoreFileAsync();

//        // 2. 生成本地监听端口
//        int localListenPort = new Random().Next(10000, 65535); // 使用高位端口减少冲突

//        // 3. 构造启动参数
//        string machineName = Environment.MachineName;
//        string token = "17073157824633806511";
//        string arguments = $"-node {machineName} -appname Minecraft{localListenPort} -peernode {peerCode} -dstip 127.0.0.1 -dstport {gamePort} -srcport {localListenPort} -token {token} -d";

//        // 4. 启动进程
//        StartCoreProcess(arguments);

//        string localAddress = $"127.0.0.1:{localListenPort}";
//        Log($"成功加入房间，请在Minecraft中连接地址: {localAddress}");
//        return localAddress;
//    }

//    /// <summary>
//    /// 停止P2P核心进程
//    /// </summary>
//    public void Stop()
//    {
//        if (_coreProcess != null && !_coreProcess.HasExited)
//        {
//            try
//            {
//                _coreProcess.Kill();
//                _coreProcess.WaitForExit();
//                Log("P2P核心已停止。");
//            }
//            catch (Exception ex)
//            {
//                Log($"停止核心时发生错误: {ex.Message}");
//            }
//            finally
//            {
//                _coreProcess.Dispose();
//                _coreProcess = null;
//            }
//        }
//    }

//    // 确保核心文件存在且有效
//    private async Task EnsureCoreFileAsync()
//    {
//        string coreDirectory = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp");
//        Directory.CreateDirectory(coreDirectory);
//        string coreFilePath = Path.Combine(coreDirectory, CoreExecutableName);

//        string url_64 = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew.exe";
//        string md5_64 = "08160296509deac13e7d12c8754de9ef";
//        string url_32 = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/main32.exe";
//        string md5_32 = "e8f1007a43eb520eecf9c0fade0300b0";

//        string targetUrl = Environment.Is64BitOperatingSystem ? url_64 : url_32;
//        string targetMd5 = Environment.Is64BitOperatingSystem ? md5_64 : md5_32;

//        bool needsDownload = true;
//        if (File.Exists(coreFilePath))
//        {
//            string currentMd5 = await CalculateFileMD5Async(coreFilePath);
//            if (currentMd5.Equals(targetMd5, StringComparison.OrdinalIgnoreCase) ||
//                currentMd5.Equals(md5_64, StringComparison.OrdinalIgnoreCase) ||
//                currentMd5.Equals(md5_32, StringComparison.OrdinalIgnoreCase)) // 允许已存在另一版本
//            {
//                needsDownload = false;
//                Log("P2P核心已存在且校验通过。");
//            }
//        }

//        if (needsDownload)
//        {
//            Log($"开始下载P2P核心文件从: {targetUrl}");
//            using (var client = new HttpClient())
//            {
//                var response = await client.GetAsync(targetUrl);
//                response.EnsureSuccessStatusCode();
//                using (var fs = new FileStream(coreFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
//                {
//                    await response.Content.CopyToAsync(fs);
//                }
//            }
//            Log("P2P核心下载完成。");
//        }
//    }

//    // 启动核心进程并重定向输出
//    private void StartCoreProcess(string arguments)
//    {
//        string coreDirectory = Path.Combine(Path.GetTempPath(), "MCZLFAPP", "Temp");
//        string coreFilePath = Path.Combine(coreDirectory, CoreExecutableName);

//        _coreProcess = new Process
//        {
//            StartInfo = new ProcessStartInfo
//            {
//                FileName = coreFilePath,
//                Arguments = arguments,
//                WorkingDirectory = coreDirectory,
//                UseShellExecute = false,
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                CreateNoWindow = true,
//                StandardOutputEncoding = Encoding.UTF8, // 明确编码
//                StandardErrorEncoding = Encoding.UTF8
//            },
//            EnableRaisingEvents = true
//        };

//        _coreProcess.OutputDataReceived += (sender, args) =>
//        {
//            if (!string.IsNullOrEmpty(args.Data)) Log(args.Data);
//        };
//        _coreProcess.ErrorDataReceived += (sender, args) =>
//        {
//            if (!string.IsNullOrEmpty(args.Data)) Log($"[ERROR] {args.Data}");
//        };

//        _coreProcess.Start();
//        _coreProcess.BeginOutputReadLine();
//        _coreProcess.BeginErrorReadLine();
//        Log("P2P核心进程已启动。");
//    }

//    // 封装的日志方法，触发事件
//    private void Log(string message)
//    {
//        // 过滤Token
//        message = message.Replace("17073157824633806511", "[TOKEN]");
//        OnLogReceived?.Invoke(this, new LogEventArgs(message));
//    }

//    private async Task<string> CalculateFileMD5Async(string filePath)
//    {
//        using (var md5 = MD5.Create())
//        {
//            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)) // 使用异步流
//            {
//                byte[] hash = await md5.ComputeHashAsync(stream);
//                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
//            }
//        }
//    }

//    // 实现IDisposable，确保程序退出时能清理进程
//    public void Dispose()
//    {
//        Stop();
//    }
//}
