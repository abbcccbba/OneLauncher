using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Views.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
namespace OneLauncher.Views;

public partial class version : UserControl
{
    public version()
    {
        InitializeComponent();
        this.DataContext = new VersionPageViewModel();
    }
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        navVL.ItemsSource = Init.ConfigManger.config.VersionList.Select(x => new VersionItem(x)).ToList();
    }
    /// <summary>
    /// 真·一键启动游戏函数
    /// </summary>
    /// <param name="IsOriginal">是否以原版模式启动（不加载Mod加载器）</param>
    /// <returns>异步任务Task</returns>
    public static Task EasyGameLauncher(aVersion LaunchGameInfo,bool IsOriginal = false,bool UseGameTasker = false)
    {
        // 用多线程而不是异步，否则某些特定版本会阻塞
        MainWindow.mainwindow.ShowFlyout("正在启动游戏...");
        var game = new Game();
        game.GameStartedEvent += () => MainWindow.mainwindow.ShowFlyout("游戏已启动！");
        game.GameClosedEvent += () => MainWindow.mainwindow.ShowFlyout("游戏已关闭！");

       return Task.Run(() => game.LaunchGame(
            LaunchGameInfo.VersionID, 
            Init.ConfigManger.config.DefaultUserModel, 
            LaunchGameInfo.ModType,
            LaunchGameInfo.IsVersionIsolation, 
            UseGameTasker));
    }
}