using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.Models;

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
        // 使用Helper API获取硬件信息 ---
        long totalSystemMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        long totalSystemMemoryGB = totalSystemMemoryBytes / 1024 / 1024 / 1024;

        // --- 2. 根据模式和硬件信息设置优化标志 ---
        switch (mode)
        {
            case OptimizationMode.Aggressive:
                // --- 激进模式 ---
                args.AlwaysPreTouch = true;
                args.UseAikarFlags = true;

                // 智能GC选择：内存充裕 (>=16GB) 的中高端机，默认推荐使用ZGC以获得更低的延迟
                // ZGC非常适合大内存下的MC客户端，可以显著减少卡顿
                if (totalSystemMemoryGB >= 16)
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

        #region 1. 真正的智能内存分配模型

        // 1.获取最真实的系统可用内存
        var memoryMetrics = SystemMemoryHelper.GetMemoryMetrics();
        long availableMemoryMB = (long)memoryMetrics.FreeMB;

        int finalMaxHeapSize;

        if (MaxHeapSize > 0) // Case 1: 用户手动指定了内存大小
        {
            finalMaxHeapSize = MaxHeapSize;
        }
        else // Case 2: 用户选择了“自动”模式
        {
            const long minMemoryMB = 1024; // 游戏能启动的最小内存

            // 2. 为系统和其他应用保留512MB的绝对安全底线
            long memoryForGame = availableMemoryMB - 512;

            if (memoryForGame <= minMemoryMB)
            {
                // 如果可用内存极低，只能分配最小值
                finalMaxHeapSize = (int)minMemoryMB;
            }
            else
            {
                // 3. 应用非线性智能算法
                const long thresholdMB = 8L * 1024; // 8GB 阈值
                const long hardCapMB = 16L * 1024;  // 16GB 硬上限
                long suggestedMB;

                if (memoryForGame <= thresholdMB)
                {
                    // 8GB以内，慷慨分配80%
                    suggestedMB = (long)(memoryForGame * 0.8);
                }
                else
                {
                    // 超过8GB的部分，只吝啬地分配20%
                    suggestedMB = (long)(thresholdMB * 0.8) + (long)((memoryForGame - thresholdMB) * 0.2);
                }

                // 4. 应用硬上限，并确保不低于我们设定的最小启动内存
                finalMaxHeapSize = (int)Math.Max(minMemoryMB, Math.Min(suggestedMB, hardCapMB));
            }
        }

        // 5. 计算初始堆大小 (-Xms)
        int finalInitialHeapSize;
        if (InitialHeapSize > 0) // 用户手动设置了初始值
        {
            finalInitialHeapSize = InitialHeapSize;
        }
        else // 自动计算初始值
        {
            // 激进模式下设为与最大值相同，其他模式为最大值一半（但不小于1024MB）
            finalInitialHeapSize = mode == OptimizationMode.Aggressive
                ? finalMaxHeapSize
                : Math.Clamp(finalMaxHeapSize / 2, 1024, finalMaxHeapSize);
        }

        // 最终校验，确保初始值不会大于最大值
        finalInitialHeapSize = Math.Min(finalInitialHeapSize, finalMaxHeapSize);

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

        #endregion

        return $" {argsBuilder.ToString().Trim()} ";
    }
}