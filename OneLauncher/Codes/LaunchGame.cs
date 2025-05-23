using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Core;
using OneLauncher.Views;
using OneLauncher.Views.Windows;
using OneLauncher.Views.Windows.WindowViewModels;
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
    /// <param name="loginUserModel">登入用户模型</param>
    /// <param name="IsVersionInsulation">版本是否启用了版本隔离</param>
    /// <param name="IsMod">版本是否是mod版本</param>
    /// <param name="UseGameTasker">是否使用游戏监视器</param>
    public async Task LaunchGame(
        string GameVersion, 
        UserModel loginUserModel, 
        bool IsVersionInsulation = false,
        bool IsMod = false,
        bool UseGameTasker = false)
    {
        // 初次启动时帮用户设置语言
        var optionsPath = Path.Combine(
            (IsVersionInsulation
            ? Path.Combine(Init.GameRootPath, $"v{GameVersion}")
            : Init.GameRootPath), "options.txt");
        if (!File.Exists(optionsPath))
        {
            File.WriteAllText(optionsPath, $"lang:zh_cn");
        }
        if (UseGameTasker)
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                var gameTasker = new GameTasker();
                gameTasker.Show();
            });
        try {            
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "C:\\Program Files\\Eclipse Adoptium\\jdk-17.0.15.6-hotspot\\bin\\java.exe";
                process.StartInfo.Arguments =
                    await new LaunchCommandBuilder
                    (
                        Init.GameRootPath,
                        GameVersion,
                        loginUserModel,
                        Init.systemType,
                        IsVersionInsulation,
                        IsMod
                    ).BuildCommand
                    (
                        OtherArgs: string.Join
                        (
                            " ",
                            "-XX:+UseG1GC",
                            "-XX:G1ReservePercent=20",
                            "-XX:MaxGCPauseMillis=50",
                            "-XX:G1HeapRegionSize=32M",
                            "-XX:+UnlockExperimentalVMOptions",
                            "-XX:-OmitStackTraceInFastThrow",
                            "-Djdk.lang.Process.allowAmbiguousCommands=true",
                            "-Dlog4j2.formatMsgNoLookups=true",
                            "-Dfml.ignoreInvalidMinecraftCertificates=True",
                            "-Dfml.ignorePatchDiscrepancies=True"
                        //"-DFabricMcEmu=net.minecraft.client.main.Main"
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
                process.OutputDataReceived += async (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return; 
                    Debug.WriteLine(e.Data);
                    WeakReferenceMessenger.Default.Send(new GameMessage($"[STDOUT] {e.Data}{Environment.NewLine}"));
                    if (e.Data.Contains("Backend library: LWJGL version"))
                        GameStartedEvent?.Invoke();
                };
                process.ErrorDataReceived += async (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    Debug.WriteLine(e.Data);
                    WeakReferenceMessenger.Default.Send(new GameMessage($"[ERROE] {e.Data}{Environment.NewLine}"));
                    if (e.Data.Contains("java.lang.ClassNotFoundException: net.minecraft.client.main.Main"))
                        await MainWindow.mainwindow.ShowFlyout("启动失败，游戏文件缺失", true);
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit(); // 不等待就不会有输出
            }
            GameClosedEvent?.Invoke();
        }
        catch (Exception)
        {
            await MainWindow.mainwindow.ShowFlyout($"[致命性错误] 请尝试安装合适的 Java",true);
        }
    }
}

