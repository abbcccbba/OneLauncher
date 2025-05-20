using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal partial class VersionItem
{
    public VersionItem(aVersion a)
    {
        V = a;
    }
    public aVersion V { get; set; }
    [RelayCommand]
    public void LaunchGame(aVersion version)
    {
        // 用多线程而不是异步，否则某些特定版本会阻塞
        MainWindow.mainwindow.ShowFlyout("正在启动游戏...");
        var game = new Game();
        game.GameStartedEvent += async () => await Dispatcher.UIThread.InvokeAsync(() => MainWindow.mainwindow.ShowFlyout("游戏已启动！"));
        game.GameClosedEvent += async () => await Dispatcher.UIThread.InvokeAsync(() => MainWindow.mainwindow.ShowFlyout("游戏已关闭！"));

        Task.Run(() => game.LaunchGame(version.VersionID, Init.ConfigManger.config.DefaultUserModel, version.IsVersionIsolation, version.IsMod));
    }
    [RelayCommand]
    public void PinToDesktop(aVersion version)
    {
        File.WriteAllTextAsync(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"启动{version.VersionID}." + (Init.systemType == SystemType.windows ? "bat" : "sh")),
            "cd " + (Init.systemType == SystemType.windows ? "/D " : "") // 不同的操作系统切换工作目录可能需要加上 /D 参数
            + $"{Init.GameRootPath}{Environment.NewLine}java " + new LaunchCommandBuilder
            (
                Init.GameRootPath,
                version.VersionID,
                Init.ConfigManger.config.DefaultUserModel,
                Init.systemType,
                version.IsVersionIsolation,
                version.IsMod
            ).BuildCommand
            (
                OtherArgs: string.Join
                (
                    " ",
                    "-XX:+UseG1GC",
                    "-XX:G1NewSizePercent=20",
                    "-XX:G1ReservePercent=20",
                    "-XX:MaxGCPauseMillis=50",
                    "-XX:G1HeapRegionSize=32M",
                    "-XX:+UnlockExperimentalVMOptions",
                    "-XX:-OmitStackTraceInFastThrow",
                    "-Djdk.lang.Process.allowAmbiguousCommands=true",
                    "-Dlog4j2.formatMsgNoLookups=true",
                    "-Dfml.ignoreInvalidMinecraftCertificates=True",
                    "-Dfml.ignorePatchDiscrepancies=True"
                //"--enable-native-access=ALL-UNNAMED" // 1.13以下的版本可能会因此报错
                )
        ));
        MainWindow.mainwindow.ShowFlyout("已创建启动脚本到桌面！");
    }
    [RelayCommand]
    public void PinInDashboard(aVersion version)
    {
        Init.ConfigManger.config.DefaultVersion = version;
        Init.ConfigManger.Write(Init.ConfigManger.config);
        MainWindow.mainwindow.ShowFlyout($"已将{version.VersionID}固定到仪表盘并设为默认版本！");
    }
}
internal partial class VersionPageViewModel : BaseViewModel
{
    public VersionPageViewModel()
    {
#if DEBUG
        // 设计时数据
        if (Design.IsDesignMode)
        {
            VersionList = new List<VersionItem>()
            {
                new VersionItem(new aVersion() {VersionID="1.21.5",IsMod=false,AddTime=DateTime.Now})
            };
        }
        else
#endif
            VersionList = Init.ConfigManger.config.VersionList.Select(x => new VersionItem(x)).ToList();
    }
    [ObservableProperty]
    public List<VersionItem> _VersionList;
    [RelayCommand]
    public void ToDownloadGame()
    {
        MainWindow.mainwindow.MainPageControl(MainWindow.MainPage.DownloadPage);
    }
}

