using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var NdSG = Init.ConfigManger.config.DefaultVersion;
       
        if (NdSG != null)
        {
            var game = new Game();
            MainWindow.mainwindow.ShowFlyout("正在启动游戏...");
            game.GameStartedEvent += async () => await Dispatcher.UIThread.InvokeAsync(() => MainWindow.mainwindow.ShowFlyout("游戏已启动！"));
            game.GameClosedEvent += async () => await Dispatcher.UIThread.InvokeAsync(() => MainWindow.mainwindow.ShowFlyout("游戏已关闭！"));

            Task.Run(() => game.LaunchGame(NdSG.VersionID, Init.ConfigManger.config.DefaultUserModel, NdSG.IsVersionIsolation,NdSG.IsMod));
        }
        else
            MainWindow.mainwindow.ShowFlyout("还没有指定默认版本！", true);

    }
}