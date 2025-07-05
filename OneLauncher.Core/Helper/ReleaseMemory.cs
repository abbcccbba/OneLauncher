using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper;

public static class ReleaseMemory
{
    /// <summary>
    /// 当优化操作失败时抛出的特定异常。
    /// </summary>
    public class OptimizationFailedException : Exception
    {
        public OptimizationFailedException(string message) : base(message) { }
        public OptimizationFailedException(string message, Exception innerException) : base(message, innerException) { }
    }

#if WINDOWS
    #region Windows P/Invoke 导入

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    private const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";

    #endregion

    #region Windows P/Invoke Definitions (Memory Management)
    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetSystemFileCacheSize(UIntPtr minimumFileCacheSize, UIntPtr maximumFileCacheSize, uint flags);

    #endregion

    /// <summary>
    /// 为当前进程设置指定的系统特权。
    /// </summary>
    /// <param name="privilegeName">要设置的特权名称。</param>
    /// <param name="enable">True以启用，False以禁用。</param>
    private static void SetPrivilege(string privilegeName, bool enable)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle))
        {
            throw new OptimizationFailedException($"无法打开进程令牌。Win32 错误码: {Marshal.GetLastWin32Error()}");
        }

        if (!LookupPrivilegeValue(null, privilegeName, out LUID luid))
        {
            throw new OptimizationFailedException($"无法查找特权 '{privilegeName}'。Win32 错误码: {Marshal.GetLastWin32Error()}");
        }

        TOKEN_PRIVILEGES newState = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges = new LUID_AND_ATTRIBUTES
            {
                Luid = luid,
                Attributes = enable ? SE_PRIVILEGE_ENABLED : 0
            }
        };

        if (!AdjustTokenPrivileges(tokenHandle, false, ref newState, 0, IntPtr.Zero, IntPtr.Zero))
        {
            throw new OptimizationFailedException($"无法调整令牌特权。Win32 错误码: {Marshal.GetLastWin32Error()}");
        }
    }

    private static Task OptimizeForWindowsAsync()
    {
        return Task.Run(() =>
        {
            // 1. 清理系统文件缓存
            try
            {
                // 在调用API前后，启用和禁用必需的特权
                SetPrivilege(SE_INCREASE_QUOTA_NAME, true);
                if (!SetSystemFileCacheSize(new UIntPtr(0xFFFFFFFF), new UIntPtr(0xFFFFFFFF), 0))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new OptimizationFailedException($"清理系统文件缓存失败。Win32 错误码: {errorCode}");
                }
            }
            finally
            {
                // 确保无论成功与否，都尝试禁用特权
                SetPrivilege(SE_INCREASE_QUOTA_NAME, false);
            }

            // 2. 削减其他进程的工作集 (此部分逻辑不变)
            var currentProcess = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcesses())
            {
                if (process.Id == currentProcess.Id) continue;
                try
                {
                    EmptyWorkingSet(process.Handle);
                }
                catch { /* 静默忽略单个进程的失败 */ }
                finally
                {
                    process.Dispose();
                }
            }
        });
    }

#endif

#if MACOS
    private static Task OptimizeForMacOsAsync()
    {
        // macOS implementation remains the same
        return Task.Run(() =>
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/purge",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        throw new OptimizationFailedException("无法启动 'purge' 进程。");
                    }
                    
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        throw new OptimizationFailedException($"执行 'purge' 命令失败。错误信息: {error}");
                    }
                }
            }
            catch (Exception ex) when (ex is not OptimizationFailedException)
            {
                throw new OptimizationFailedException("执行 'purge' 命令时发生意外错误。", ex);
            }
        });
    }
#endif

    public static Task OptimizeAsync()
    {
#if WINDOWS
        return OptimizeForWindowsAsync();
#elif MACOS
        return OptimizeForMacOsAsync();
#else
        return Task.CompletedTask;
#endif
    }
}