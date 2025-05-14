using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Views;
using OneLauncher.Views.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
namespace OneLauncher;

public partial class Home : UserControl
{
    public Home()
    {
        InitializeComponent();
        //this.DataContext = new MinecraftDashboardViewModel();
        //Task.Run(() => LaunchGame("1.16.5", new UserModel("ZhiWei", "0"), Init.BasePath));
        //Debug.WriteLine(new LaunchCommandBuilder(Init.BasePath,"1.21.1",new UserModel("ZhiWei","0"),Init.systemType).BuildCommand());
    }
    /// <param name="GameVersion">游戏版本</param>
    /// <param name="loginUserModel">以哪个用户模型启动游戏</param>
    /// <param name="GamePath">游戏基本路径</param>
    /// <returns></returns>
    public async static Task LaunchGame(string GameVersion,UserModel loginUserModel,string GamePath = null)
    {
        using (Process process = new Process())
        {
            process.StartInfo.FileName = "java";
            process.StartInfo.Arguments =
                new LaunchCommandBuilder
                (
                    GamePath ?? Init.BasePath,
                    GameVersion,
                    loginUserModel,
                    Init.systemType
                ).BuildCommand
                (
                    string.Join
                    (
                        " ",
                        "-XX:+UseG1GC",
                        "-XX:+UnlockExperimentalVMOptions",
                        "-XX:-OmitStackTraceInFastThrow",
                        "-Xmn322m -Xmx2150m",
                        "-Djdk.lang.Process.allowAmbiguousCommands=true",
                        "-Dlog4j2.formatMsgNoLookups=true",
                        "-Dfml.ignoreInvalidMinecraftCertificates=True",
                        "-Dfml.ignorePatchDiscrepancies=True",
                        "--enable-native-access=ALL-UNNAMED"
                    )
                );
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine(e.Data); // 输出到控制台
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine(e.Data); // 输出到控制台
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
    } 
}