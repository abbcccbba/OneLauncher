using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using OneLauncher.Core.Global;
using OneLauncher.Core.Launcher;
using OneLauncher.Core.Helper.Models;
using BenchmarkDotNet.Running; // 确保引用ModEnum

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using OneLauncher.Core.Global;
using OneLauncher.Core.Launcher;
using OneLauncher.Core.Helper.Models; // 确保引用ModEnum

[MemoryDiagnoser]
// 【已修正】将测试任务的目标框架从 .NET 8.0 修改为 .NET 9.0
[SimpleJob(RuntimeMoniker.Net90)]
public class LaunchCommandBuilderBenchmark
{
    // 根据您的指正，不再硬编码路径，将从 Init 类获取
    private string _gameRootPath;

    // 根据您的要求，使用正确的版本ID
    private const string VanillaVersionId = "1.21";
    private const string FabricVersionId = "1.21";
    private const string NeoForgeVersionId = "1.21.4";

    /// <summary>
    /// 全局初始化。BenchmarkDotNet 会在所有测试运行前执行此方法一次。
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // 根据您的要求，执行初始化
        await Init.Initialize(true);
        // 调用初始化后，从 Init 类获取游戏根目录
        _gameRootPath = Init.GameRootPath;

        // 添加一个检查，确保 GameRootPath 被成功赋值
        if (string.IsNullOrEmpty(_gameRootPath) || !Directory.Exists(_gameRootPath))
        {
            throw new DirectoryNotFoundException($"测试失败：无法从 Init.GameRootPath 获取到有效的游戏根目录。请确保 Init.Initialize() 正确设置了该路径。当前路径: '{_gameRootPath}'");
        }
    }

    [Benchmark(Description = "1.21 Vanilla")]
    public async Task<string> BuildVanillaArgs()
    {
        var commandBuilder = await LaunchCommandBuilder.CreateAsync(_gameRootPath, VanillaVersionId);
        var command = await commandBuilder.BuildCommand();
        return await command.GetArguments();
    }

    [Benchmark(Description = "1.21 Fabric")]
    public async Task<string> BuildFabricArgs()
    {
        // Fabric测试将使用 "1.21" 作为VersionId
        var commandBuilder = await LaunchCommandBuilder.CreateAsync(_gameRootPath, FabricVersionId);
        commandBuilder.SetModType(ModEnum.fabric);
        var command = await commandBuilder.BuildCommand();
        return await command.GetArguments();
    }

    [Benchmark(Description = "1.21.4 NeoForge")]
    public async Task<string> BuildNeoForgeArgs()
    {
        // NeoForge测试将使用 "1.21.4" 作为VersionId
        var commandBuilder = await LaunchCommandBuilder.CreateAsync(_gameRootPath, NeoForgeVersionId);
        commandBuilder.SetModType(ModEnum.neoforge);
        var command = await commandBuilder.BuildCommand();
        return await command.GetArguments();
    }
}
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("开始 OneLauncher.Core 启动参数拼接性能测试...");
        Console.WriteLine("测试将在 Release 配置下提供最准确的结果。");
        Console.WriteLine("请确保已按照说明准备好了相关的游戏文件。");

        // 运行基准测试
        var summary = BenchmarkRunner.Run<LaunchCommandBuilderBenchmark>();

        Console.WriteLine("\n测试完成。");
        Console.WriteLine(summary);
    }
}