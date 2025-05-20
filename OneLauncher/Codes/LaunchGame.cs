using Avalonia.Controls.Documents;
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
    /// <param ID="GameVersion">游戏版本</param>
    /// <param ID="loginUserModel">以哪个用户模型启动游戏</param>
    /// <param ID="GamePath">游戏基本路径</param>
    /// <returns></returns>
    public async Task LaunchGame(string GameVersion, UserModel loginUserModel, bool IsVersionInsulation = false)
    {
        try
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "java";
                process.StartInfo.Arguments =
                    new LaunchCommandBuilder
                    (
                        Init.GameRootPath,
                        GameVersion,
                        loginUserModel,
                        Init.systemType,
                        IsVersionInsulation
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
                            "-Dlog4j2.formatMsgNoLookups=true", 
                            "-Dfml.ignoreInvalidMinecraftCertificates=True",
                            "-Dfml.ignorePatchDiscrepancies=True"
                        )

                    );
                process.StartInfo.WorkingDirectory = 
                    (IsVersionInsulation)
                    ? Path.Combine(Init.GameRootPath,$"v{GameVersion}")
                    : Init.GameRootPath;
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
                process.ErrorDataReceived += async (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    Debug.WriteLine($"[ERROR] {e.Data}"); // 输出到控制台
                    if(e.Data.Contains("java.lang.ClassNotFoundException: net.minecraft.client.main.Main"))
                        await Dispatcher.UIThread.InvokeAsync(() => MainWindow.mainwindow.ShowFlyout("启动失败，游戏文件缺失", true));
                };
                process.Exited += (sender, e) => GameClosedEvent?.Invoke(); // 游戏关闭事件
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(()=> 
            MainWindow.mainwindow.ShowFlyout($"启动失败，错误信息：{ex.Message}。请尝试安装Java "+
            new VersionInfomations(
                File.ReadAllText(
                    (IsVersionInsulation)
                    ? Path.Combine(Init.GameRootPath,$"v{GameVersion}",$"{GameVersion}.json")
                    : Path.Combine(Init.GameRootPath, "versions", GameVersion, $"{GameVersion}.json"))
                ,Init.GameRootPath
                ,Init.systemType)
            .GetJavaVersion(),true));
        }
    }
}

