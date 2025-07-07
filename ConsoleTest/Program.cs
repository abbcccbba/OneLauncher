using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Launcher;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OneLauncher.Core.Global;
using OneLauncher.Core.Launcher;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[MemoryDiagnoser]
public class LaunchCommandBuilderBenchmarks
{
    private LaunchCommandBuilder _commandBuilder;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        await Init.Initialize();

        _commandBuilder = (await LaunchCommandBuilder.CreateAsync(Init.GameRootPath, "1.21"))
            //.SetModType(ModEnum.fabric)
            .WithServerInfo(new ServerInfo()
            {
                Ip = "localhost",
                Port = "25565"
            })
            .SetLoginUser(new UserModel(Guid.NewGuid(), "Test", Guid.NewGuid()))
            .WithExtraJvmArgs(new string[]
            {
                "-Xmx2G",
                "-Xms1G",
                "-XX:+UseG1GC",
                "-XX:+UnlockExperimentalVMOptions"
            });
    }

    // [Benchmark]
    // 修改返回类型为 Task<List<string>>
    [Benchmark]
    public async Task<List<string>> BuildNeoForgeCommand()
    {
        var commandEnumerable = await _commandBuilder.BuildCommand();
        // 调用 .ToList() 来“物化”结果，强制执行
        return commandEnumerable.ToList();
    }
}
public class Program
{
    public static async Task Main(string[] args)
    {
        // 这行代码会自动运行你刚才创建的基准测试类
        var summary = BenchmarkRunner.Run<LaunchCommandBuilderBenchmarks>();

        // 你原来的代码可以注释掉或删除了
        // await Init.Initialize();
        // ...
    }
}