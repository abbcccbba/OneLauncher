using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper;

public enum OptimizationMode
{
    /// <summary>保守模式</summary>
    Conservative,
    /// <summary>标准模式</summary>
    Standard,
    /// <summary>激进模式</summary>
    Aggressive
}
/// <summary>
/// 存放所有可配置的JVM参数。
/// 设计为纯数据类（POCO），方便进行序列化/反序列化（如存为JSON配置文件）。
/// </summary>
public class JvmArguments
{
    public OptimizationMode mode { get; set; }
    #region 核心内存设置 (Core Memory Settings)
    public int MaxHeapSize { get; set; } = 0;
    public int InitialHeapSize { get; set; } = 0;
    #endregion

    #region 垃圾收集器选择 (Garbage Collector Selection)
    public bool UseG1GC { get; set; } = true;
    public bool UseZGC { get; set; } = false;
    public bool UseShenandoahGC { get; set; } = false;
    #endregion

    #region G1GC 专用参数 (G1GC Specifics)
    public int MaxGCPauseMillis { get; set; } = 50;
    public int G1HeapRegionSize { get; set; } = 0; // 0 = 自动
    public int G1NewSizePercent { get; set; } = 30;
    public int G1MaxNewSizePercent { get; set; } = 60;
    public int G1ReservePercent { get; set; } = 15;
    public bool G1UseStringDeduplication { get; set; } = true;
    #endregion

    #region ZGC/Shenandoah 专用参数
    public bool UseGenerationalGCForZOrShenandoah { get; set; } = true;
    #endregion

    #region 通用优化标志 (Common Optimization Flags)
    public bool DisableExplicitGC { get; set; } = true;
    public bool ParallelRefProcEnabled { get; set; } = true;
    public bool AlwaysPreTouch { get; set; } = false;
    public bool PerfDisableSharedMem { get; set; } = true;
    public bool UseAikarFlags { get; set; } = false;
    #endregion

    public JvmArguments() { }

    /// <summary>
    /// [硬件感知的预设工厂] 根据用户的硬件和选择的模式，生成一份智能的推荐配置。
    /// 这份配置可以被用户的自定义文件覆盖。
    /// </summary>
    public static JvmArguments CreateFromMode(OptimizationMode mode = OptimizationMode.Standard)
    {
        var args = new JvmArguments();
        args.mode = mode;
        // --- 1. 使用跨平台API获取硬件信息 ---
        long totalSystemMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        long totalSystemMemoryGB = totalSystemMemoryBytes / 1024 / 1024 / 1024;
        int coreCount = Environment.ProcessorCount;

        // --- 2. 根据模式和硬件信息设置优化标志 ---
        switch (mode)
        {
            case OptimizationMode.Aggressive:
                // --- 激进模式 ---
                args.AlwaysPreTouch = true;
                args.UseAikarFlags = true;

                // 智能GC选择：内存充裕 (>=12GB) 的中高端机，默认推荐使用ZGC以获得更低的延迟
                // ZGC非常适合大内存下的MC客户端，可以显著减少卡顿
                if (totalSystemMemoryGB >= 12)
                {
                    args.UseZGC = true;
                    args.UseG1GC = false;
                    args.UseGenerationalGCForZOrShenandoah = true; // 为ZGC启用分代
                }
                else
                {
                    // 内存不足16GB，则使用高度优化的G1GC
                    args.UseG1GC = true;
                    args.MaxGCPauseMillis = 40;
                    args.G1HeapRegionSize = totalSystemMemoryGB >= 12 ? 32 : 16; // 内存稍大时，用32M的Region
                    args.G1NewSizePercent = 40;
                    args.G1ReservePercent = 10;
                }
                break;

            case OptimizationMode.Conservative:
                // --- 保守模式 ---
                args.UseG1GC = true; // 总是使用稳定可靠的G1GC
                args.MaxGCPauseMillis = 200; // 更宽松的停顿时间，注重吞吐
                args.G1HeapRegionSize = totalSystemMemoryGB < 8 ? 8 : 16; // 低内存系统使用更小的Region Size
                args.G1NewSizePercent = 20;
                args.G1MaxNewSizePercent = 50;
                args.AlwaysPreTouch = false;
                args.UseAikarFlags = false;
                break;

            // 默认 Standard 模式
            default:
                // --- 标准模式 ---
                args.UseG1GC = true;
                args.UseAikarFlags = true;
                args.MaxGCPauseMillis = 50;

                // 智能调整G1 Region Size：内存大于等于12GB时，使用32M可以提升性能
                args.G1HeapRegionSize = totalSystemMemoryGB >= 12 ? 32 : 16;
                args.G1NewSizePercent = 30;
                args.G1MaxNewSizePercent = 60;
                args.AlwaysPreTouch = false;
                break;
        }
        return args;
    }

    public string ToString(int jvmVersion)
    {
        var argsBuilder = new StringBuilder();

        #region 1. 动态内存计算
        long totalSystemMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        long totalSystemMemoryMB = totalSystemMemoryBytes / 1024 / 1024;

        int finalMaxHeapSize;
        if (MaxHeapSize > 0)
        {
            // 用户在配置文件中强制指定了值
            finalMaxHeapSize = MaxHeapSize;
        }
        else
        {
            // 根据模式动态计算
            finalMaxHeapSize = mode switch
            {
                // 激进: 分配可用内存的50%，最低4G，最高推荐32G（过高可能导致GC问题）
                OptimizationMode.Aggressive => (int)Math.Clamp(totalSystemMemoryMB * 0.5, 4096, 32768),
                // 保守: 分配可用内存的25%，最低2G，最高4G
                OptimizationMode.Conservative => (int)Math.Clamp(totalSystemMemoryMB * 0.25, 512, 4096),
                // 标准: 分配可用内存的40%，最低2G，最高10G
                _ => (int)Math.Clamp(totalSystemMemoryMB * 0.4, 1024, 10240)
            };
        }

        int finalInitialHeapSize;
        if (InitialHeapSize > 0)
        {
            finalInitialHeapSize = InitialHeapSize;
        }
        else
        {
            finalInitialHeapSize = mode switch
            {
                // 激进模式下，初始和最大值设为一致，避免运行时堆伸缩带来的性能抖动
                OptimizationMode.Aggressive => finalMaxHeapSize,
                // 其他模式下，可以设置一个较小的初始值
                _ => Math.Clamp(finalMaxHeapSize / 2, 1024, 4096)
            };
        }
        argsBuilder.Append($" -Xms{finalInitialHeapSize}m -Xmx{finalMaxHeapSize}m");
        #endregion

        #region 2. GC 和其他参数组装

        argsBuilder.Append(" -XX:+UnlockExperimentalVMOptions");

        // --- GC 选择 ---
        if (UseG1GC)
        {
            argsBuilder.Append(" -XX:+UseG1GC");
            if (G1UseStringDeduplication && jvmVersion >= 8) argsBuilder.Append(" -XX:+UseStringDeduplication");
            argsBuilder.Append($" -XX:MaxGCPauseMillis={MaxGCPauseMillis}");
            argsBuilder.Append($" -XX:G1NewSizePercent={G1NewSizePercent}");
            argsBuilder.Append($" -XX:G1MaxNewSizePercent={G1MaxNewSizePercent}");
            if (G1HeapRegionSize > 0) argsBuilder.Append($" -XX:G1HeapRegionSize={G1HeapRegionSize}M");
            argsBuilder.Append($" -XX:G1ReservePercent={G1ReservePercent}");
        }
        else if (UseZGC)
        {
            argsBuilder.Append(" -XX:+UseZGC");
            // ZGC 分代在 JDK 21+ 成为正式特性
            if (UseGenerationalGCForZOrShenandoah && jvmVersion >= 21)
            {
                argsBuilder.Append(" -XX:+ZGenerational");
            }
        }
        else if (UseShenandoahGC)
        {
            argsBuilder.Append(" -XX:+UseShenandoahGC");
            // Shenandoah 分代支持情况类似, 需查阅具体JDK版本文档
        }

        // --- 通用优化 ---
        // 这些参数在较新版JVM(11+)上普遍适用
        if (jvmVersion >= 11)
        {
            if (DisableExplicitGC) argsBuilder.Append(" -XX:+DisableExplicitGC");
            if (ParallelRefProcEnabled) argsBuilder.Append(" -XX:+ParallelRefProcEnabled");
            if (AlwaysPreTouch) argsBuilder.Append(" -XX:+AlwaysPreTouch");
            // PerfDisableSharedMem 和 Aikar's flags 有重叠，为了清晰，在此处条件化
            if (PerfDisableSharedMem && !UseAikarFlags) argsBuilder.Append(" -XX:+PerfDisableSharedMem");
        }

        // --- Aikar's Flags (社区验证的高级优化) ---
        // 主要针对 G1GC, 适用于 Minecraft 这类负载
        if (UseG1GC && UseAikarFlags && jvmVersion >= 11)
        {
            argsBuilder.Append(" -XX:+UseNUMA") // 在支持NUMA的服务器硬件上有用
                       .Append(" -XX:G1MixedGCCountTarget=4")
                       .Append(" -XX:G1MixedGCLiveThresholdPercent=90")
                       .Append(" -XX:G1RSetUpdatingPauseTimePercent=5")
                       .Append(" -XX:SurvivorRatio=32")
                       .Append(" -XX:+PerfDisableSharedMem")
                       .Append(" -XX:MaxTenuringThreshold=1")
                       .Append(" -Dusing.aikars.flags=true");
        }

        argsBuilder.Append(" -Dfile.encoding=UTF-8");

        #endregion

        return $" {argsBuilder.ToString().Trim()} ";
    }
}