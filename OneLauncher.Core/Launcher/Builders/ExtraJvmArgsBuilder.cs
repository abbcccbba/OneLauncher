using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;
/// <summary>
/// 为 JvmArguments 类提供扩展方法，用于生成实际的JVM参数列表。
/// </summary>
public static class JvmArgumentsExtensions
{
    public static IEnumerable<string> GetArguments(this JvmArguments jvmConfig, int jvmVersion, GameData? gameData)
    {
        ModEnum modLoader = gameData?.ModLoader ?? ModEnum.none;
        int modCount = 0;
        if(gameData != null)
            modCount = Directory.GetFiles(gameData.InstancePath, "*.jar").Length;
        
        var args = new List<string>(20);
        // 计算基本的内存分配大小
        (int initialHeapMb, int maxHeapMb) = CalculateSmartHeapSize(jvmConfig, modLoader, modCount);
        args.Add($"-Xms{initialHeapMb}m");
        args.Add($"-Xmx{maxHeapMb}m");

        #region 添加其他GC和通用优化参数
        args.Add("-XX:+UnlockExperimentalVMOptions");

        if (jvmConfig.UseG1GC)
        {
            args.Add("-XX:+UseG1GC");
            if (jvmConfig.G1UseStringDeduplication && jvmVersion >= 8) args.Add("-XX:+UseStringDeduplication");
            args.Add($"-XX:MaxGCPauseMillis={jvmConfig.MaxGCPauseMillis}");
            args.Add($"-XX:G1NewSizePercent={jvmConfig.G1NewSizePercent}");
            args.Add($"-XX:G1MaxNewSizePercent={jvmConfig.G1MaxNewSizePercent}");
            if (jvmConfig.G1HeapRegionSize > 0) args.Add($"-XX:G1HeapRegionSize={jvmConfig.G1HeapRegionSize}M");
            args.Add($"-XX:G1ReservePercent={jvmConfig.G1ReservePercent}");
        }
        else if (jvmConfig.UseZGC)
        {
            args.Add("-XX:+UseZGC");
            if (jvmConfig.UseGenerationalGCForZOrShenandoah && jvmVersion >= 21) args.Add("-XX:+ZGenerational");
        }
        else if (jvmConfig.UseShenandoahGC)
        {
            args.Add("-XX:+UseShenandoahGC");
        }

        if (jvmVersion >= 11)
        {
            if (jvmConfig.DisableExplicitGC) args.Add("-XX:+DisableExplicitGC");
            if (jvmConfig.ParallelRefProcEnabled) args.Add("-XX:+ParallelRefProcEnabled");
            if (jvmConfig.AlwaysPreTouch) args.Add("-XX:+AlwaysPreTouch");
            if (jvmConfig.PerfDisableSharedMem && !jvmConfig.UseAikarFlags) args.Add("-XX:+PerfDisableSharedMem");
        }

        if (jvmConfig.UseG1GC && jvmConfig.UseAikarFlags && jvmVersion >= 11)
        {
            args.Add("-XX:+UseNUMA");
            args.Add("-XX:G1MixedGCCountTarget=4");
            args.Add("-XX:G1MixedGCLiveThresholdPercent=90");
            args.Add("-XX:G1RSetUpdatingPauseTimePercent=5");
            args.Add("-XX:SurvivorRatio=32");
            args.Add("-XX:+PerfDisableSharedMem");
            args.Add("-XX:MaxTenuringThreshold=1");
            args.Add("-Dusing.aikars.flags=true");
        }
        #endregion
        return args;
    }

    /// <summary>
    /// 核心智能内存计算逻辑。
    /// </summary>
    private static (int InitialHeap, int MaxHeap) CalculateSmartHeapSize(JvmArguments jvmConfig, ModEnum modLoader, int modCount)
    {
        if (jvmConfig.MaxHeapSize > 0)
        {
            int initialSize = jvmConfig.InitialHeapSize > 0 ? jvmConfig.InitialHeapSize : jvmConfig.MaxHeapSize / 2;
            return (Math.Min(initialSize, jvmConfig.MaxHeapSize), jvmConfig.MaxHeapSize);
        }

        // 根据模组加载器类型决定最小内存分配
        int baseMinMemoryMB = modLoader switch
        {
            ModEnum.forge or ModEnum.neoforge => 1448,
            ModEnum.fabric or ModEnum.quilt => 1024,
            _ => 512,
        };

        // 每个模组增加6MB内存
        int modMemoryMB = modCount * 6;
        int recommendedMemoryMB = baseMinMemoryMB + modMemoryMB;

        var memoryMetrics = SystemMemoryHelper.GetMemoryMetrics();
        long availableMemoryMB = (long)memoryMetrics.FreeMB;
        long memoryForGame = availableMemoryMB - 512;
        //Debug.WriteLine($"可用{availableMemoryMB}");
        long systemMaxSuggestMB;
        const long thresholdMB = 8L * 1024;
        const long hardCapMB = 16L * 1024;

        if (memoryForGame <= thresholdMB)
            systemMaxSuggestMB = (long)(memoryForGame * 0.8);
        else
            systemMaxSuggestMB = (long)(thresholdMB * 0.8) + (long)((memoryForGame - thresholdMB) * 0.2);
        

        long finalMaxCapMB = jvmConfig.mode switch
        {
            OptimizationMode.Aggressive => hardCapMB,
            OptimizationMode.Standard => Math.Min(systemMaxSuggestMB, hardCapMB),
            OptimizationMode.Conservative => (long)(Math.Min(systemMaxSuggestMB, hardCapMB) * 0.8),
            _ => hardCapMB
        };

        int finalMaxHeapSize = (int)Math.Clamp(finalMaxCapMB,baseMinMemoryMB,hardCapMB);

        int finalInitialHeapSize = jvmConfig.mode == OptimizationMode.Aggressive
            ? finalMaxHeapSize
            : Math.Clamp(finalMaxHeapSize / 2, 512, finalMaxHeapSize);
        Debug.WriteLine($"初始堆内存：{finalInitialHeapSize}，最大堆内存：{finalMaxHeapSize}");
        return (finalInitialHeapSize, finalMaxHeapSize);
    }
}