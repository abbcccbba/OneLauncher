using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using OneLauncher.Core.Global;
using OneLauncher.Core.Launcher;
using OneLauncher.Core.Helper.Models;
using BenchmarkDotNet.Running;
using OneLauncher.Core.Minecraft;
using System.Diagnostics;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class LaunchCommandBuilderBenchmark
{
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        await Init.Initialize(true);
    }

    [Benchmark(Description = "1.21 Vanilla")]
    public async Task<string> BuildVanillaArgs()
    {
        var commandBuilder = await LaunchCommandBuilder.CreateAsync(Init.GameRootPath, "1.21");
        var command = await commandBuilder.BuildCommand();
        return await command.GetArguments();
    }

    [Benchmark(Description = "1.21 Fabric")]
    public async Task<string> BuildFabricArgs()
    {
        var commandBuilder = await LaunchCommandBuilder.CreateAsync(Init.GameRootPath, "1.21");
        commandBuilder.SetModType(ModEnum.fabric);
        var command = await commandBuilder.BuildCommand();
        return await command.GetArguments();
    }

    [Benchmark(Description = "1.21.4 NeoForge")]
    public async Task<string> BuildNeoForgeArgs()
    {
        var commandBuilder = await LaunchCommandBuilder.CreateAsync(Init.GameRootPath,"1.21.4");
        commandBuilder.SetModType(ModEnum.neoforge);
        var command = await commandBuilder.BuildCommand();
        return await command.GetArguments();
    }
}
public class Program
{
    public static async Task Main(string[] args)
    {
        //var summary = BenchmarkRunner.Run<LaunchCommandBuilderBenchmark>();
        var newsReader = new MinecraftNewsReader();
        await newsReader.GetCurrentNewsAsync();
        for(int i = 0; i < 10; i++) 
            Console.WriteLine((await newsReader.GetNextNewsAsync()).Title);
        Console.ReadKey();
    }
}