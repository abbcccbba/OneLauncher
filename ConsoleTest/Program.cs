using OneLauncher.Core.Global;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.Net.JavaProviders;
using System;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 初始化 OneLauncher 核心服务
        await Init.Initialize();
        Console.WriteLine("OneLauncher Core Initialized.");
        Console.WriteLine("--- JavaManager Test Suite ---");

        var javaManager = new JavaManager();

        // --- 1. GetJavaExecutablePath Test ---
        Console.WriteLine("\n[TEST 1: Path Resolution]");

        // Test Case 1.1: 已配置的Java 8
        string java8Path = javaManager.GetJavaExecutablePath(8);
        Console.WriteLine($"Java 8 Path: {java8Path}");
        // 预期输出: C:\Program Files\Eclipse Adoptium\jdk-8.0.452.9-hotspot\bin\javaw.exe

        // Test Case 1.2: 已配置的Java 24
        string java24Path = javaManager.GetJavaExecutablePath(24);
        Console.WriteLine($"Java 24 Path: {java24Path}");
        // 预期输出: C:\Program Files\Eclipse Adoptium\jdk-24.0.1.9-hotspot\bin\java.exe

        // Test Case 1.3: 未配置的Java 17 (应回退)
        string java17Path = javaManager.GetJavaExecutablePath(17);
        Console.WriteLine($"Java 17 Path (Fallback): {java17Path}");
        // 预期输出: java

        // --- 2. InstallJava Test ---
        Console.WriteLine("\n[TEST 2: Installation]");
        try
        {
            Console.WriteLine("Attempting to install Java 11 from Adoptium...");

            // 创建一个简单的进度报告器，用于在控制台显示进度
            var progressReporter = new Progress<string>(p =>
                Console.WriteLine(p));

            // 调用安装方法
            await javaManager.InstallJava(11, JavaProvider.MicrosoftOpenJDK, true, progressReporter);

            Console.WriteLine("\nJava 11 installation completed successfully!");

            // 验证安装后的路径
            string java11Path = javaManager.GetJavaExecutablePath(11);
            Console.WriteLine($"Java 11 Path after install: {java11Path}");
            // 预期输出: ...\OneLauncher\installed\runtimes\11\...

        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nAn error occurred during Java 11 installation: {ex.Message}");
        }

        Console.WriteLine("\n--- Test Suite Finished ---");
    }
}