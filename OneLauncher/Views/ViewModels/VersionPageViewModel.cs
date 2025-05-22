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
    // 直接启动
    public void LaunchGame(aVersion version) => Views.version.EasyGameLauncher(version);
    [RelayCommand]
    // 原版模式
    public void LaunchGameOriginal(aVersion version) => Views.version.EasyGameLauncher(version,IsOriginal: true);
    [RelayCommand]
    // 调试模式
    public void LaunchGameDebug(aVersion version) => Views.version.EasyGameLauncher(version,UseGameTasker: true);
    [RelayCommand]
    // 原版调试模式
    public void LaunchGameOriginalDebug(aVersion version) => Views.version.EasyGameLauncher(version,IsOriginal: true ,UseGameTasker: true);
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

