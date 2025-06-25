using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using OneLauncher.Core.Global;
using System;
using System.IO;
using System.Threading.Tasks;
namespace OneLauncher.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Init.InitTask = Init.Initialize();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) 
        {
            string crashLogPath = Path.Combine(AppContext.BaseDirectory, "crash_log.txt");
            string errorMessage = $"程序启动时发生致命错误:{ex}";
            Console.WriteLine(errorMessage);
            File.WriteAllText(crashLogPath, errorMessage);
            // 抛出，方便调试器看到
            throw;
        }
    } 
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions()
            {
                RenderingMode = [Win32RenderingMode.AngleEgl, Win32RenderingMode.Vulkan,Win32RenderingMode.Software]
            })
            .With(new X11PlatformOptions()
            { 
                RenderingMode = [X11RenderingMode.Vulkan,X11RenderingMode.Egl,X11RenderingMode.Glx,X11RenderingMode.Software]
            })
            .LogToTrace();
}
