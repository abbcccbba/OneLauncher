using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using OneLauncher.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
namespace OneLauncher;

public partial class Home : UserControl
{
    public Home()
    {
        InitializeComponent();
    }
    public async static Task LaunchGame(string GamePath,string GameVersion)
    {
        using (Process process = new Process())
        {
            Debug.WriteLine(StartArguments.GetArguments(
                new StartArguments(GameVersion, "release", GamePath, MainWindow.configManger.config.DefaultUserModel)));
            process.StartInfo.FileName = "java";
            process.StartInfo.Arguments = StartArguments.GetArguments(
                new StartArguments(GameVersion, "release", GamePath,MainWindow.configManger.config.DefaultUserModel));
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine(e.Data); // 输出到控制台
                    Console.WriteLine(e.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine(e.Data); // 输出到控制台
                    Console.WriteLine(e.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Run(() => process.WaitForExit());
        }
    }
}