using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper;
public static class SystemInfoHelper
{
    public class MemoryMetrics
    {
        public double TotalMB { get; set; }
        public double UsedMB { get; set; }
        public double FreeMB { get; set; }
    }

    public static MemoryMetrics GetMemoryMetrics()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsMemoryMetrics();
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxMemoryMetrics();
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // 在macOS上，命令行工具仍然是最佳选择
            return GetOsxMemoryMetrics();
        }

        // 未知系统的安全默认值
        return new MemoryMetrics { TotalMB = 4096, FreeMB = 1024, UsedMB = 3072 };
    }

    private static MemoryMetrics GetWindowsMemoryMetrics()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                double totalMB = memStatus.ullTotalPhys / 1024.0 / 1024.0;
                double freeMB = memStatus.ullAvailPhys / 1024.0 / 1024.0;
                double usedMB = totalMB - freeMB;

                return new MemoryMetrics
                {
                    TotalMB = Math.Round(totalMB),
                    FreeMB = Math.Round(freeMB),
                    UsedMB = Math.Round(usedMB)
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get Windows memory metrics via P/Invoke: {ex.Message}");
        }

        // 如果API调用失败，返回安全默认值
        return new MemoryMetrics { TotalMB = 4096, FreeMB = 1024, UsedMB = 3072 };
    }

    private static MemoryMetrics GetLinuxMemoryMetrics()
    {
        try
        {
            var output = "";
            var info = new ProcessStartInfo("/bin/bash", "-c \"free -m\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(info)) { output = process.StandardOutput.ReadToEnd(); }

            var lines = output.Split('\n');
            var memory = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var metrics = new MemoryMetrics();
            metrics.TotalMB = double.Parse(memory[1], CultureInfo.InvariantCulture);

            if (memory.Length > 6)
                metrics.FreeMB = double.Parse(memory[6], CultureInfo.InvariantCulture);
            else
            {
                double free = double.Parse(memory[3], CultureInfo.InvariantCulture);
                double bufferCache = double.Parse(memory[5], CultureInfo.InvariantCulture);
                metrics.FreeMB = free + bufferCache;
            }

            metrics.UsedMB = metrics.TotalMB - metrics.FreeMB;
            return metrics;
        }
        catch (Exception ex) { return new MemoryMetrics { TotalMB = 4096, FreeMB = 1024, UsedMB = 3072 }; }
    }

    private static MemoryMetrics GetOsxMemoryMetrics()
    {
        try
        {
            double totalMB = GetOsxTotalMemory() / 1024.0 / 1024.0;

            // 获取页面信息来计算已用内存
            var output = "";
            var info = new ProcessStartInfo("/bin/bash", "-c \"vm_stat\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = Process.Start(info)) { output = process.StandardOutput.ReadToEnd(); }

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var infoDic = lines
                .Skip(1)
                .Select(line => line.Split(new[] { ':' }, 2))
                .ToDictionary(parts => parts[0].Trim(), parts => long.Parse(Regex.Match(parts[1], @"\d+").Value));

            long pageSize = (long)GetOsxPageSize();
            long pagesActive = infoDic.GetValueOrDefault("Pages active");
            long pagesInactive = infoDic.GetValueOrDefault("Pages inactive");
            long pagesSpeculative = infoDic.GetValueOrDefault("Pages speculative");
            long pagesWiredDown = infoDic.GetValueOrDefault("Pages wired down");
            long pagesPurgeable = infoDic.GetValueOrDefault("Pages purgeable");

            // ProjBobcat的算法比较复杂，一个简化的、被广泛接受的“可用内存”估算
            // 是 (inactive + free + speculative) * pageSize。我们这里用更简单的总-已用
            double usedMB = (pagesActive + pagesWiredDown) * pageSize / 1024.0 / 1024.0;
            double freeMB = totalMB - usedMB;

            return new MemoryMetrics
            {
                TotalMB = Math.Round(totalMB),
                FreeMB = Math.Round(freeMB),
                UsedMB = Math.Round(usedMB)
            };
        }
        catch (Exception ex) { /* ... */ return new MemoryMetrics { TotalMB = 4096, FreeMB = 1024, UsedMB = 3072 }; }
    }

    // macOS 辅助方法
    private static ulong GetOsxTotalMemory()
    {
        var info = new ProcessStartInfo("/usr/sbin/sysctl", "hw.memsize") { RedirectStandardOutput = true };
        using var process = Process.Start(info);
        var output = process.StandardOutput.ReadToEnd();
        var value = output.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
        return ulong.TryParse(value, out var outVal) ? outVal : 0;
    }
    private static uint GetOsxPageSize()
    {
        var info = new ProcessStartInfo("/bin/bash", "-c \"vm_stat\"") { RedirectStandardOutput = true };
        using var process = Process.Start(info);
        var output = process.StandardOutput.ReadLine(); // 只读第一行
        return uint.TryParse(Regex.Match(output, @"\d+").Value, out var pageSizeOut) ? pageSizeOut : 4096;
    }

    #region Windows P/Invoke Definitions (现在是唯一需要的Windows部分)

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    #endregion
}

