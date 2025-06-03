using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Minecraft;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
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
        var tempVersoinList = new List<VersionItem>(Init.ConfigManger.config.VersionList.Count);
        for (int i = 0; i < Init.ConfigManger.config.VersionList.Count; i++)
        {
            tempVersoinList.Add(new VersionItem(
                Init.ConfigManger.config.VersionList[i],
                i
                ));
        }
        navVL.ItemsSource = tempVersoinList;
    }
    /// <summary>
    /// 真·一键启动游戏函数
    /// </summary>
    /// <param name="IsOriginal">是否以原版模式启动（不加载Mod加载器）</param>
    /// <returns>异步任务Task</returns>
    public static Task EasyGameLauncher(
        UserVersion LaunchGameInfo,
        bool UseGameTasker = false,
        UserModel loginUserModel = default
        )
    {
        // 用多线程而不是异步，否则某些特定版本会阻塞
        MainWindow.mainwindow.ShowFlyout("正在启动游戏...");
        var game = new Game();
        game.GameStartedEvent += () => MainWindow.mainwindow.ShowFlyout("游戏已启动！");
        game.GameClosedEvent += () => MainWindow.mainwindow.ShowFlyout("游戏已关闭！");

       return Task.Run(() => game.LaunchGame(
            LaunchGameInfo.VersionID, 
            (loginUserModel.Name == null) ? Init.ConfigManger.config.DefaultUserModel : loginUserModel,
            LaunchGameInfo.preferencesLaunchMode.LaunchModType,
            LaunchGameInfo.IsVersionIsolation, 
            UseGameTasker));
    }
}