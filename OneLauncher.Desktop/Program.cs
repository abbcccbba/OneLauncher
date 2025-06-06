using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
namespace OneLauncher.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) 
        {
            string crashLogPath = Path.Combine(AppContext.BaseDirectory, "crash_log.txt");
            string errorMessage = $"程序启动时发生致命错误:{ex}";
            File.WriteAllText(crashLogPath, errorMessage);
            // 抛出，方便调试器看到
            throw;
        }
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            //.UseAvaloniaNative()
            .UseSkia();
}
