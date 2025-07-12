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
}

