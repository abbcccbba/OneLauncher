using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Launcher;
using OneLauncher.Core.Minecraft;
using OneLauncher.Views;
using OneLauncher.Views.Windows;
using OneLauncher.Views.Windows.WindowViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Codes;
internal class Game
{
    public static Task EasyGameLauncher(GameData gameData,ServerInfo? serverInfo,bool useDebugMode)
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("正在启动游戏..."));
            IGameLauncher gameLauncher = new GameLauncher();
            gameLauncher.GameStartedEvent += () =>
                WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("游戏已启动！"));
            gameLauncher.GameClosedEvent += () =>
                WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("游戏已关闭！"));
            if (useDebugMode)
            {
                // 如果是调试模式，使用调试窗口
                new GameTasker().Show();
                gameLauncher.GameOutputEvent += (m)
                    => WeakReferenceMessenger.Default.Send(new GameMessage(m));
            }
            _ = gameLauncher.Play(gameData, serverInfo);
            return Task.CompletedTask;
        }
        #region 错误处理
        catch (OlanException ex)
        {
            return OlanExceptionWorker.ForOlanException(ex);
        }
        catch (UnauthorizedAccessException uex)
        {
            return OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败", $"没有权限访问游戏文件夹{Environment.NewLine}{uex}", OlanExceptionAction.Error, uex));
        }
        catch (FileNotFoundException fex)
        {
            return OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败", $"无法找到启动所需的文件{Environment.NewLine}{fex}", OlanExceptionAction.Error, fex));
        }
        catch (DirectoryNotFoundException fex)
        {
            return OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败", $"无法找到启动所需的文件夹{Environment.NewLine}{fex}", OlanExceptionAction.Error, fex));
        }
        catch (Exception ex)
        {
            return OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败", $"系统未安装Java或系统错误{Environment.NewLine}{ex}", OlanExceptionAction.Error, ex));
        }
        #endregion
    }

    //    public event Action? GameStartedEvent;
    //    public event Action? GameClosedEvent;
    //    public event Action<string>? GamePutEvent;
    //    public async Task LaunchGame(
    //        GameData gameData,
    //        ServerInfo? serverInfo = null,
    //        bool useRootLaunch = false)
    //    {
    //        #region 初始化基本游戏构建类
    //        var Builder = new LaunchCommandBuilder
    //                        (
    //                            Init.GameRootPath,
    //                            gameData,
    //                            serverInfo
    //                        );
    //        #endregion

    //        try
    //        {
    //            string launchArgumentsPath = Path.GetTempFileName();
    //            await File.WriteAllTextAsync(
    //                launchArgumentsPath,
    //                (
    //                string.Join(" " ,await Builder.BuildCommand(
    //                     Init.ConfigManger.Data.OlanSettings.MinecraftJvmArguments.ToString(Builder.versionInfo.GetJavaVersion()),useRootLaunch)))
    //#if WINDOWS
    //                // 避免微软万年屎山导致的找不到路径问题
    //                .Replace("\\",@"\\")
    //#endif
    //                );
    //            using (Process process = new Process())
    //            {
    //                process.StartInfo = new ProcessStartInfo()
    //                {
    //                    Arguments =
    //                    // 如果你想自定义标题，可以从Github下载OneLauncher.Agent.jar，然后把路径输入到这里，后面的就是新标题
    //                    //"-javaagent:\"F:\\OneLauncherAgent.jar\"=\"Hello World by OneLauncher\"" +
    //                    $"@{launchArgumentsPath}",
    //                    FileName = Builder.GetJavaPath(),
    //                    WorkingDirectory = Init.GameRootPath,
    //                    RedirectStandardOutput = true,
    //                    RedirectStandardError = true,
    //                    UseShellExecute = false,
    //                    CreateNoWindow = true,
    //                    StandardOutputEncoding = Encoding.UTF8,
    //                    StandardErrorEncoding = Encoding.UTF8
    //                };
    //                process.OutputDataReceived += async (sender, e) =>
    //                {
    //                    if (string.IsNullOrEmpty(e.Data)) return;
    //                    Debug.WriteLine(e.Data);
    //                    GamePutEvent?.Invoke($"[STDOUT] {e.Data}{Environment.NewLine}");
    //                    if (e.Data.Contains("Backend library: LWJGL version"))
    //                        GameStartedEvent?.Invoke();
    //                };
    //                process.ErrorDataReceived += async (sender, e) =>
    //                {
    //                    if (string.IsNullOrEmpty(e.Data)) return;
    //                    Debug.WriteLine(e.Data);
    //                    GamePutEvent?.Invoke($"[ERROR] {e.Data}{Environment.NewLine}");
    //                    if (e.Data.Contains("java.lang.ClassNotFoundException")) 
    //                        await OlanExceptionWorker.ForOlanException(
    //                        new OlanException("启动失败","Jvm无法找到主类，请尝试重新安装游戏",OlanExceptionAction.Error),
    //                        () => _=LaunchGame(
    //                                gameData));
    //                };
    //                process.Start();
    //                process.BeginOutputReadLine();
    //                process.BeginErrorReadLine();
    //                await process.WaitForExitAsync();
    //                if(process.ExitCode != 0)
    //                    await OlanExceptionWorker.ForOlanException(
    //                        new OlanException("启动失败", "未知错误，请尝试以调试模式启动游戏以查找出错原因", OlanExceptionAction.Error),
    //                        () => _=LaunchGame(
    //                                gameData));
    //            }
    //            GameClosedEvent?.Invoke();
    //            File.Delete(launchArgumentsPath);
    //        }
    //        catch(FileNotFoundException fex)
    //        {
    //            await OlanExceptionWorker.ForOlanException(
    //                        new OlanException("启动失败", $"无法找到启动所需的文件{Environment.NewLine}{fex}", OlanExceptionAction.Error, fex));
    //        }
    //        catch (DirectoryNotFoundException fex)
    //        {
    //            await OlanExceptionWorker.ForOlanException(
    //                        new OlanException("启动失败", $"无法找到启动所需的文件夹{Environment.NewLine}{fex}", OlanExceptionAction.Error,fex));
    //        }
    //        catch(Exception ex)
    //        {
    //            await OlanExceptionWorker.ForOlanException(
    //                        new OlanException("启动失败", $"系统未安装Java或系统错误{Environment.NewLine}{ex}", OlanExceptionAction.Error,ex));
    //        }
    //    }
}

