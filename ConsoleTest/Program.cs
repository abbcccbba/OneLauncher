using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using OneLauncher.Core.Global;
using OneLauncher.Core.Launcher;
using OneLauncher.Core.Helper.Models;
using BenchmarkDotNet.Running;
using OneLauncher.Core.Minecraft;
using System.Diagnostics;

public class Program
{
    public static async Task Main(string[] args)
    {
        await Init.Initialize();
        JavaManager javaManager = new JavaManager();
        await javaManager.InstallJava(21);
    }
}