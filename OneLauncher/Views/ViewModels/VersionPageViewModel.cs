using OneLauncher.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OneLauncher.Codes;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using OneLauncher.Views.Panes;

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
        var game = new Game();
        game.GameStartedEvent += () =>
        {
            Debug.WriteLine("游戏启动事件触发！");
        };
        Task.Run(() =>game.LaunchGame(version.VersionID,Init.ConfigManger.config.DefaultUserModel,Init.BasePath));
    }
    [RelayCommand]
    public void PinToDesktop(aVersion version)
    {
        // 避免阻塞UI线程
        Task.Run(() =>
        {
            File.WriteAllText(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"启动{version.VersionID}."+(Init.systemType == SystemType.windows ? "bat" : "sh")),
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
                            "-XX:ParallelGCThreads=4",
                            "-Djdk.lang.Process.allowAmbiguousCommands=true",
                            "-Dlog4j2.formatMsgNoLookups=true",
                            "-Dfml.ignoreInvalidMinecraftCertificates=True",
                            "-Dfml.ignorePatchDiscrepancies=True",
                            // 指定目录，避免在桌面出现logs文件夹
                            $"-Duser.dir=\"{Path.Combine(Init.BasePath, ".minecraft")}\"",
                            "--enable-native-access=ALL-UNNAMED"
                        )
                    ));
        });
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

