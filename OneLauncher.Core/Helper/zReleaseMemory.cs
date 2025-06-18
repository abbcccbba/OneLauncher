//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace OneLauncher.Core.Helper;

//public static class zReleaseMemory
//{
//    /// <summary>
//    /// 当优化操作失败时抛出的特定异常。
//    /// </summary>
//    public class OptimizationFailedException : Exception
//    {
//        public OptimizationFailedException(string message) : base(message) { }
//        public OptimizationFailedException(string message, Exception innerException) : base(message, innerException) { }
//    }

//#if WINDOWS
//    // --- Windows P/Invoke 定义 ---
//    [DllImport("psapi.dll", SetLastError = true)]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    private static extern bool EmptyWorkingSet(IntPtr hProcess);

//    [DllImport("kernel32.dll", SetLastError = true)]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    private static extern bool SetSystemFileCacheSize(UIntPtr minimumFileCacheSize, UIntPtr maximumFileCacheSize, uint flags);
//#endif

//    /// <summary>
//    /// 异步执行启动前内存优化。
//    /// - 在 Windows 上，会清理系统缓存并尝试削减其他进程的工作集。
//    /// - 在 macOS 上，会执行 'purge' 命令清理磁盘缓存。
//    /// - 在 Linux 及其他平台上，此方法不执行任何操作并直接成功返回。
//    /// 如果在支持的平台（Windows/macOS）上优化失败，则会抛出 OptimizationFailedException。
//    /// </summary>
//    public static Task OptimizeAsync()
//    {
//#if WINDOWS
//        return OptimizeForWindowsAsync();
//#elif MACOS
//    return OptimizeForMacOsAsync();
//#else
//    // 在 Linux 和其他所有平台上，直接返回一个已完成的任务，啥也不干。
//    return Task.CompletedTask;
//#endif
//    }

//#if WINDOWS
//    private static Task OptimizeForWindowsAsync()
//    {
//        return Task.Run(() =>
//        {
//            try
//            {
//                if (!SetSystemFileCacheSize(new UIntPtr(0xFFFFFFFF), new UIntPtr(0xFFFFFFFF), 0))
//                {
//                    // 获取具体的Win32错误码
//                    int errorCode = Marshal.GetLastWin32Error();
//                    // 抛出包含错误码的异常
//                    throw new OptimizationFailedException($"清理系统文件缓存失败。Win32 错误码: {errorCode}");
//                }
//            }
//            catch (Exception ex) when (ex is not OptimizationFailedException)
//            {
//                throw new OptimizationFailedException("调用Windows API清理缓存时发生意外错误。", ex);
//            }

//            // 2. 削减其他进程的工作集
//            var currentProcess = Process.GetCurrentProcess();
//            foreach (var process in Process.GetProcesses())
//            {
//                if (process.Id == currentProcess.Id) continue;
//                try
//                {
//                    // 这个操作允许失败（例如无权限访问的进程），所以我们不在此处抛出异常。
//                    // 只有在整个优化策略的关键步骤失败时才应该抛异常。
//                    EmptyWorkingSet(process.Handle);
//                }
//                catch { /* 静默忽略单个进程的失败 */ }
//            }
//        });
//    }
//#endif

//#if MACOS
//private static Task OptimizeForMacOsAsync()
//{
//    return Task.Run(() =>
//    {
//        try
//        {
//            var processStartInfo = new ProcessStartInfo
//            {
//                FileName = "/usr/sbin/purge",
//                UseShellExecute = false,
//                CreateNoWindow = true,
//                RedirectStandardError = true
//            };

//            using (var process = Process.Start(processStartInfo))
//            {
//                if (process == null)
//                {
//                    throw new OptimizationFailedException("无法启动 'purge' 进程。");
//                }
                    
//                process.WaitForExit();

//                if (process.ExitCode != 0)
//                {
//                    string error = process.StandardError.ReadToEnd();
//                    throw new OptimizationFailedException($"执行 'purge' 命令失败。错误信息: {error}");
//                }
//            }
//        }
//        catch (Exception ex) when (ex is not OptimizationFailedException)
//        {
//            // 捕获预料之外的异常，例如 'purge' 命令不存在
//            throw new OptimizationFailedException("执行 'purge' 命令时发生意外错误。", ex);
//        }
//    });
//}
//#endif
//}
