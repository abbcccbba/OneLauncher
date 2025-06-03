using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Minecraft;
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
internal partial class VersionItem : BaseViewModel
{
    /// <param name="a">UserVersion实例</param>
    /// <param name="IndexInInit">UserVsersion实例在整个Init.ConfigManger.config.VersionList中的索引值</param>
    public VersionItem(UserVersion a,int IndexInInit)
    {
        versionExp = a;
        index = IndexInInit;
        switch(a.preferencesLaunchMode.LaunchModType)
        {
            case ModEnum.none:
                IsOriginalLaunchMode = true;
                VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/basic.png")));
                break;
            case ModEnum.fabric:
                IsFabricLaunchMode = true;
                VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/fabric.png")));
                break;
            case ModEnum.neoforge:
                IsNeoforgeLaunchMode = true;
                VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/neoforge.png")));
                break;
        }
    }
    int index;
    [ObservableProperty]
    public Bitmap versionIcon;
    public UserVersion versionExp { get; set; }
    [ObservableProperty]
    public bool isOriginalLaunchMode;
    partial void OnIsOriginalLaunchModeChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.VersionList[index].preferencesLaunchMode.LaunchModType = ModEnum.none;
        Init.ConfigManger.Save();
        VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/basic.png")));
    }
    [ObservableProperty]
    public bool isFabricLaunchMode;
    partial void OnIsFabricLaunchModeChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.VersionList[index].preferencesLaunchMode.LaunchModType = ModEnum.fabric;
        Init.ConfigManger.Save();
        VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/fabric.png")));
    }
    [ObservableProperty]
    public bool isNeoforgeLaunchMode;
    partial void OnIsNeoforgeLaunchModeChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.VersionList[index].preferencesLaunchMode.LaunchModType = ModEnum.neoforge;
        Init.ConfigManger.Save();
        VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/neoforge.png")));
    }
    [ObservableProperty]
    public bool isUseDebugModLaunch;
    [RelayCommand]
    public void LaunchGame()
    {
        Views.version.EasyGameLauncher(versionExp,IsUseDebugModLaunch);
    }
    [RelayCommand]
    public async Task PinToDesktop()
    {
        
        await File.WriteAllTextAsync(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"启动{versionExp.VersionID}." + (Init.systemType == SystemType.windows ? "bat" : "sh")),
            "cd " + (Init.systemType == SystemType.windows ? "/D " : "") // 不同的操作系统切换工作目录可能需要加上 /D 参数
            + $"{Init.GameRootPath}{Environment.NewLine}java " + await new LaunchCommandBuilder
            (
                Init.GameRootPath,
                versionExp.VersionID,
                Init.ConfigManger.config.DefaultUserModel,
                versionExp.preferencesLaunchMode.LaunchModType,
                Init.systemType,
                versionExp.IsVersionIsolation
            ).BuildCommand
            (
                OtherArgs: "-XX:+UseG1GC"
        ));
        await MainWindow.mainwindow.ShowFlyout("已创建启动脚本到桌面！");
    }
    [RelayCommand]
    public void PinInDashboard()
    {
        Init.ConfigManger.config.DefaultVersion = versionExp;
        Init.ConfigManger.Write(Init.ConfigManger.config);
        MainWindow.mainwindow.ShowFlyout($"已将{versionExp.VersionID}固定到仪表盘并设为默认版本！");
    }
    [RelayCommand]
    public void OpenModsFolder()
    {
        string path = ((versionExp.IsVersionIsolation)
                ? Path.Combine(Init.GameRootPath, "versions", versionExp.VersionID, "mods")
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
                new VersionItem(new UserVersion() 
                {
                    VersionID="1.21.5",
                    AddTime=DateTime.Now,
                    preferencesLaunchMode = new PreferencesLaunchMode(){LaunchModType = ModEnum.neoforge}
                },0)
            };
        }
        else
#endif
        {
            var tempVersoinList = new List<VersionItem>(Init.ConfigManger.config.VersionList.Count);
            for(int i = 0;i < tempVersoinList.Count;i++)
            {
                tempVersoinList.Add(new VersionItem(
                    Init.ConfigManger.config.VersionList[i],
                    i
                    ));
            }
            VersionList = tempVersoinList;
        } 
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
    public void DownloadGameAgain(UserVersion version)
    {
        IsPaneShow = true;
        RefDownPane = new DownloadPane(version);
    }
}

