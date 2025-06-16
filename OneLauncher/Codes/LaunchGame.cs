using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Core;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Views.Windows;
using OneLauncher.Views.Windows.WindowViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OneLauncher.Codes;
public delegate void GameEvents();
internal class Game
{
    public event GameEvents GameStartedEvent;
    public event GameEvents GameClosedEvent;
    /// <param Name="GameVersion">游戏版本</param>
    /// <param Name="loginUserModel">登入用户模型</param>
    /// <param Name="IsVersionInsulation">版本是否启用了版本隔离</param>
    /// <param Name="UseGameTasker">是否使用游戏监视器</param>
    public async Task LaunchGame(
        string GameVersion, 
        UserModel loginUserModel, 
        ModEnum modType,
        bool IsVersionInsulation,
        bool UseGameTasker = false)
    {
        #region 初始化基本游戏构建类
        var Builder = new LaunchCommandBuilder
                        (
                            Init.GameRootPath,
                            GameVersion,
                            loginUserModel,
                            modType,
                            Init.systemType,
                            IsVersionInsulation     
                        );
        #endregion
        
        #region 初次启动时帮用户设置语言和在调试模式下打开调试窗口
        var optionsPath = Path.Combine(
            (IsVersionInsulation
            ? Path.Combine(Init.GameRootPath, "versions",GameVersion)
            : Init.GameRootPath), "options.txt");
        if (!File.Exists(optionsPath))
        {
            await File.WriteAllTextAsync(optionsPath, $"lang:zh_CN");
        }
        if (UseGameTasker)
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var gameTasker = new GameTasker();
                gameTasker.Show();
            });
        #endregion  
        
        try
        {
            // 刷新登入令牌
            await loginUserModel.IntelligentLogin(Init.MMA);
            string launchArgumentsPath = Path.GetTempFileName();
                await File.WriteAllTextAsync(
                launchArgumentsPath,
                (await Builder.BuildCommand(
                     Init.ConfigManger.config.OlanSettings.MinecraftJvmArguments.ToString(Builder.versionInfo.GetJavaVersion())))
#if WINDOWS
                // 避免微软万年屎山导致的找不到路径问题
                .Replace("\\",@"\\")
#endif
                );
            using (Process process = new Process())
            {
                process.StartInfo.Arguments =
                    // 如果你想自定义标题，可以从Github下载OneLauncher.Agent.jar，然后把路径输入到这里，后面的就是新标题
                    //"-javaagent:\"F:\\OneLauncherAgent.jar\"=\"Hello World by OneLauncher\"" +
                    $"@{launchArgumentsPath}";
                Debug.WriteLine(process.StartInfo.Arguments);
                process.StartInfo.FileName = Builder.GetJavaPath();
                process.StartInfo.WorkingDirectory =
                    (IsVersionInsulation)
                    ? Path.Combine(Init.GameRootPath, "versions",GameVersion)
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
                    if (e.Data.Contains("java.lang.ClassNotFoundException")) 
                        await OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败","Jvm无法找到主类，请尝试重新安装游戏",OlanExceptionAction.Error),
                        () => _=LaunchGame(
                                GameVersion,
                                loginUserModel,
                                modType,
                                IsVersionInsulation,
                                UseGameTasker
                            ));
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();
                if(process.ExitCode != 0)
                    await OlanExceptionWorker.ForOlanException(
                        new OlanException("启动失败", "未知错误，请尝试以调试模式启动游戏以查找出错原因", OlanExceptionAction.Error),
                        () => _=LaunchGame(
                                GameVersion,
                                loginUserModel,
                                modType,
                                IsVersionInsulation,
                                true
                            ));
            }
            GameClosedEvent?.Invoke();
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

