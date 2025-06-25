using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
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
    public event Action? GameStartedEvent;
    public event Action? GameClosedEvent;
    public event Action<string>? GamePutEvent;
    public async Task LaunchGame(
        GameData gameData,
        ServerInfo? serverInfo = null,
        bool useRootLaunch = false)
    {
        #region 初始化基本游戏构建类
        await Init.AccountManager.GetUser(gameData.DefaultUserModelID).IntelligentLogin(Init.MMA);
        var Builder = new LaunchCommandBuilder
                        (
                            Init.GameRootPath,
                            gameData,
                            serverInfo
                        );
        #endregion
        
        try
        {
            string launchArgumentsPath = Path.GetTempFileName();
            await File.WriteAllTextAsync(
                launchArgumentsPath,
                (await Builder.BuildCommand(
                     Init.ConfigManger.config.OlanSettings.MinecraftJvmArguments.ToString(Builder.versionInfo.GetJavaVersion()),useRootLaunch))
#if WINDOWS
                // 避免微软万年屎山导致的找不到路径问题
                .Replace("\\",@"\\")
#endif
                );
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    Arguments =
                    // 如果你想自定义标题，可以从Github下载OneLauncher.Agent.jar，然后把路径输入到这里，后面的就是新标题
                    //"-javaagent:\"F:\\OneLauncherAgent.jar\"=\"Hello World by OneLauncher\"" +
                    $"@{launchArgumentsPath}",
                    FileName = Builder.GetJavaPath(),
                    WorkingDirectory = Init.GameRootPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                process.OutputDataReceived += async (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    Debug.WriteLine(e.Data);
                    GamePutEvent?.Invoke($"[STDOUT] {e.Data}{Environment.NewLine}");
                    if (e.Data.Contains("Backend library: LWJGL version"))
                        GameStartedEvent?.Invoke();
                };
                process.ErrorDataReceived += async (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    Debug.WriteLine(e.Data);
                    GamePutEvent?.Invoke($"[ERROR] {e.Data}{Environment.NewLine}");
                    if (e.Data.Contains("java.lang.ClassNotFoundException")) 
                        await OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败","Jvm无法找到主类，请尝试重新安装游戏",OlanExceptionAction.Error),
                        () => _=LaunchGame(
                                gameData));
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();
                if(process.ExitCode != 0)
                    await OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败", "未知错误，请尝试以调试模式启动游戏以查找出错原因", OlanExceptionAction.Error),
                        () => _=LaunchGame(
                                gameData));
            }
            GameClosedEvent?.Invoke();
            File.Delete(launchArgumentsPath);
        }
        catch(FileNotFoundException fex)
        {
            await OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败", $"无法找到启动所需的文件{Environment.NewLine}{fex}", OlanExceptionAction.Error, fex));
        }
        catch (DirectoryNotFoundException fex)
        {
            await OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败", $"无法找到启动所需的文件夹{Environment.NewLine}{fex}", OlanExceptionAction.Error,fex));
        }
        catch(Exception ex)
        {
            await OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败", $"系统未安装Java或系统错误{Environment.NewLine}{ex}", OlanExceptionAction.Error,ex));
        }
    }
}

