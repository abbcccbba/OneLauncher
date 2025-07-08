using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.Models;

public enum OptimizationMode
{
    /// <summary>关闭优化（仅允许在配置文件中启用）</summary>
    None,
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
        long totalSystemMemoryBytes = (long)SystemMemoryHelper.GetMemoryMetrics().TotalMB;
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
}