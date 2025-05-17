using Avalonia.Threading;
using OneLauncher.Core;
using OneLauncher.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Codes;
public delegate void GameStarted();
public delegate void GameClosed();
internal class Game
{
    public event GameStarted GameStartedEvent;
    public event GameClosed GameClosedEvent;
    /// <param name="GameVersion">游戏版本</param>
    /// <param name="loginUserModel">以哪个用户模型启动游戏</param>
    /// <param name="GamePath">游戏基本路径</param>
    /// <returns></returns>
    public async Task LaunchGame(string GameVersion, UserModel loginUserModel, string GamePath = null)
    {
        try
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "C:\\Program Files\\Eclipse Adoptium\\jdk-8.0.452.9-hotspot\\bin\\java.exe";
                process.StartInfo.Arguments =
                    new LaunchCommandBuilder
                    (
                        GamePath ?? Init.BasePath,
                        GameVersion,
                        loginUserModel,
                        Init.systemType
                    ).BuildCommand
                    (
                        OtherArgs: string.Join
                        (
                            " ",
                            "-XX:+UseG1GC",
                            "-XX:+UnlockExperimentalVMOptions",
                            "-XX:-OmitStackTraceInFastThrow",
                            "-Xmn512m -Xmx4096m",
                            "-Djdk.lang.Process.allowAmbiguousCommands=true",
                            "-Dlog4j2.formatMsgNoLookups=true", // 针对 Log4Shell 漏洞
                            "-Dfml.ignoreInvalidMinecraftCertificates=True",
                            "-Dfml.ignorePatchDiscrepancies=True"
                        //"--enable-native-access=ALL-UNNAMED"
                        )

                    );
                // 指定工作目录，不要用-Duser.dir，不然可能会出现一些奇奇怪怪的问题
                process.StartInfo.WorkingDirectory = Path.Combine(Init.BasePath, ".minecraft");
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.OutputDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    Debug.WriteLine($"[STDOUT] {e.Data}"); // 输出到控制台
                    if (e.Data.Contains("Backend library: LWJGL version"))
                        GameStartedEvent?.Invoke();
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    Debug.WriteLine($"[ERROR] {e.Data}"); // 输出到控制台
                    if(e.Data.Contains("java.lang.ClassNotFoundException: net.minecraft.client.main.Main"))
                        MainWindow.mainwindow.ShowFlyout("启动失败，游戏文件缺失",true);
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            GameClosedEvent?.Invoke(); // 游戏关闭事件
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(()=> 
            MainWindow.mainwindow.ShowFlyout($"启动失败，错误信息：{ex.Message}。请尝试安装Java "+
            new VersionInfomations(
                File.ReadAllText(Path.Combine(Init.BasePath, ".minecraft", "versions", GameVersion, $"{GameVersion}.json")),Init.BasePath,Init.systemType)
            .GetJavaVersion(),true));
        }
    }
}

