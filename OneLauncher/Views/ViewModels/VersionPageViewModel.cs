using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.Minecraft.JsonModels;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
        if (a.modType.IsNeoForge || a.modType.IsFabric)
            IsMod = true;
        switch (a.preferencesLaunchMode.LaunchModType)
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
    public bool IsMod {  get; set; } = false;
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
        var version = Init.ConfigManger.config.VersionList[index];
        var prefs = version.preferencesLaunchMode;
        prefs.LaunchModType = ModEnum.none;
        version.preferencesLaunchMode = prefs;
        Init.ConfigManger.config.VersionList[index] = version;
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
        var version = Init.ConfigManger.config.VersionList[index];
        var prefs = version.preferencesLaunchMode;
        prefs.LaunchModType = ModEnum.fabric;
        version.preferencesLaunchMode = prefs;
        Init.ConfigManger.config.VersionList[index] = version;
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
        var version = Init.ConfigManger.config.VersionList[index];
        var prefs = version.preferencesLaunchMode; 
        prefs.LaunchModType = ModEnum.neoforge; 
        version.preferencesLaunchMode = prefs; 
        Init.ConfigManger.config.VersionList[index] = version; 
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
        try
        {
            Tools.OpenFolder(path);
        }
        catch (OlanException ex)
        {
            OlanExceptionWorker.ForOlanException(ex,
                () => OpenModsFolder());  
        }
    }
    [RelayCommand]
    public void OpenServerFolder()
    {
        string path = Path.Combine(Init.GameRootPath,"versions",versionExp.VersionID,"servers");
        if (!Directory.Exists(path))
            OlanExceptionWorker.ForOlanException(
                new OlanException("无法打开服务端文件夹","服务端尚未初始化",OlanExceptionAction.Error));
        else
            Tools.OpenFolder(path);
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
    public async Task OpenServer(UserVersion versionExp)
    {
        try
        {
            // 去尝试读取，判断这个服务端版本是否启用了版本隔离
            bool IsVI = true;
            if (Directory.Exists(Path.Combine(Init.GameRootPath, "versoins", versionExp.VersionID, "servers")))
                IsVI = true;
            else if (Directory.Exists(Path.Combine(Init.GameRootPath, "servers")))
                IsVI = false;
            string versionPath = Path.Combine(Init.GameRootPath, "versions", versionExp.VersionID);
            // 判断服务端是否已经完成初始化
            if (!File.Exists(Path.Combine(versionPath, "server.jar")))
            {
                IsPaneShow = true;
                RefDownPane = new InitServerPane(versionExp.VersionID);
            }
            else
                MinecraftServerManger.Run(versionPath, "",
                    // 读取源文件获取Java版本
                    ((MinecraftVersionInfo)await JsonSerializer.DeserializeAsync<MinecraftVersionInfo>(
                        File.OpenRead(
                            Path.Combine(versionPath, $"{versionExp.VersionID}.json")),
                        MinecraftJsonContext.Default.MinecraftVersionInfo
                    )).JavaVersion.MajorVersion, IsVI);
        }
        catch (OlanException ex)
        {
            await OlanExceptionWorker.ForOlanException(ex);
        }
    }
}

