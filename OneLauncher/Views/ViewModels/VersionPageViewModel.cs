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
        MainWindow.mainwindow.Showfyt("正在启动游戏...");
        var game = new Game();
        game.GameStartedEvent += async () => await Dispatcher.UIThread.InvokeAsync(() =>MainWindow.mainwindow.Showfyt("游戏已启动！"));
        game.GameClosedEvent += async () => await Dispatcher.UIThread.InvokeAsync(() => MainWindow.mainwindow.Showfyt("游戏已关闭！"));
        Task.Run(() => game.LaunchGame(version.VersionID,Init.ConfigManger.config.DefaultUserModel,Init.BasePath));
    }
    [RelayCommand]
    public void PinToDesktop(aVersion version)
    {
        File.WriteAllTextAsync(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"启动{version.VersionID}." + (Init.systemType == SystemType.windows ? "bat" : "sh")),
            "java " + new LaunchCommandBuilder
            (
                Init.BasePath,
                version.VersionID,
                Init.ConfigManger.config.DefaultUserModel,
                Init.systemType
            ).BuildCommand
            (
                string.Join
                (
                    " ",
                    "-XX:+UseG1GC",
                    "-XX:+UnlockExperimentalVMOptions",
                    "-XX:-OmitStackTraceInFastThrow",
                    $"-XX:ParallelGCThreads={Init.CPUPros}",
                    "-Djdk.lang.Process.allowAmbiguousCommands=true",
                    "-Dlog4j2.formatMsgNoLookups=true",
                    "-Dfml.ignoreInvalidMinecraftCertificates=True",
                    "-Dfml.ignorePatchDiscrepancies=True",
                    // 指定目录，避免在桌面出现logs文件夹
                    $"-Duser.dir=\"{Path.Combine(Init.BasePath, ".minecraft")}\"",
                    "--enable-native-access=ALL-UNNAMED"
                )
        ));
        MainWindow.mainwindow.Showfyt("已创建启动脚本到桌面！");
    }
    [RelayCommand]
    public void ManGame(aVersion version)
    {
        //MainWindow.mainwindow.MainPageNavigate(new VersionMangerPage(version));
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

