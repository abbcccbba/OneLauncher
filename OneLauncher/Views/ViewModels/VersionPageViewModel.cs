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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal partial class VersionItem
{
    public VersionItem(aVersion a)
    {
        V = a;
        if (a.modType.IsFabric || a.modType.IsNeoForge)
            IsMod = true;
        else
            IsMod = false;
    }
    public aVersion V { get; set; }
    public bool IsMod {  get; set; }
    #region 启动
    [RelayCommand]
    public void LaunchGameWithFabric() => Views.version.EasyGameLauncher(
        new aVersion() 
        {
            VersionID = V.VersionID,
            IsVersionIsolation = V.IsVersionIsolation,
            modType = new ModType() { IsFabric = true,IsNeoForge = false}
        });
    [RelayCommand]
    public void LaunchGameWithNeoforge() => Views.version.EasyGameLauncher(
        new aVersion()
        {
            VersionID = V.VersionID,
            IsVersionIsolation = V.IsVersionIsolation,
            modType = new ModType() { IsFabric = false, IsNeoForge = true }
        });
    [RelayCommand]
    // 原版模式
    public void LaunchGameOriginal() => Views.version.EasyGameLauncher(
        new aVersion()
        {
            VersionID = V.VersionID,
            IsVersionIsolation = V.IsVersionIsolation,
            modType = new ModType() { IsFabric = false, IsNeoForge = false }
        });
    [RelayCommand]
    // 调试模式
    public void LaunchGameDebug(aVersion version) => Views.version.EasyGameLauncher(version,UseGameTasker: true);
    #endregion

    [RelayCommand]
    public async void PinToDesktop()
    {
        /*
        await File.WriteAllTextAsync(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"启动{V.VersionID}." + (Init.systemType == SystemType.windows ? "bat" : "sh")),
            "cd " + (Init.systemType == SystemType.windows ? "/D " : "") // 不同的操作系统切换工作目录可能需要加上 /D 参数
            + $"{Init.GameRootPath}{Environment.NewLine}java " + await new LaunchCommandBuilder
            (
                Init.GameRootPath,
                V.VersionID,
                Init.ConfigManger.config.DefaultUserModel,
                Init.systemType,
                V.IsVersionIsolation,
                V.IsMod
            ).BuildCommand
            (
                OtherArgs: string.Join
                (
                    " ",
                    "-XX:+UseG1GC"
                )
        ));
        MainWindow.mainwindow.ShowFlyout("已创建启动脚本到桌面！");
        */
    }
    [RelayCommand]
    public void PinInDashboard()
    {
        Init.ConfigManger.config.DefaultVersion = V;
        Init.ConfigManger.Write(Init.ConfigManger.config);
        MainWindow.mainwindow.ShowFlyout($"已将{V.VersionID}固定到仪表盘并设为默认版本！");
    }
    [RelayCommand]
    public void OpenModsFolder()
    {
        string path = ((V.IsVersionIsolation)
                ? Path.Combine(Init.GameRootPath, "versions", V.VersionID, "mods")
                : Path.Combine(Init.GameRootPath, "mods"));
        var processOpenInfo = new ProcessStartInfo() 
        {
            Arguments = $"\"{path}\"",
            UseShellExecute = true
        };
        Directory.CreateDirectory(path);
        try
        {
            switch (Init.systemType) 
            {
                case SystemType.windows:
                    processOpenInfo.FileName = "explorer.exe";
                    break;
                case SystemType.osx:
                    processOpenInfo.FileName = "open";
                    break;
                case SystemType.linux:
                    processOpenInfo.FileName = "xdg-open";
                    break;
            }
            Process.Start(processOpenInfo);
        }
        catch (Exception ex)
        {
            OlanExceptionWorker.ForOlanException(
                new OlanException(
                    "无法打开Mods文件夹",
                    "无法执行启动操作",
                    OlanExceptionAction.Error),
                    () => OpenModsFolder()
                );  
        }
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
                new VersionItem(new aVersion() {VersionID="1.21.5",AddTime=DateTime.Now})
            };
        }
        else
#endif
            VersionList = Init.ConfigManger.config.VersionList.Select(x => new VersionItem(x)).ToList();
    }
    [ObservableProperty]
    public List<VersionItem> _VersionList;
    [ObservableProperty]
    public UserControl _RefDownPane;
    [ObservableProperty]
    public bool _IsPaneShow;
    [RelayCommand]
    public void ToDownloadGame()
    {
        MainWindow.mainwindow.MainPageControl(MainWindow.MainPage.DownloadPage);
    }
    [RelayCommand]
    public void DownloadGameAgain(aVersion version)
    {
        IsPaneShow = true;
        RefDownPane = new DownloadPane(version);
    }
}

