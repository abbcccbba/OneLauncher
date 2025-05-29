﻿using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Net.java;
using OneLauncher.Views.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace OneLauncher.Views.Panes.PaneViewModels;
internal partial class DownloadPaneViewModel : BaseViewModel
{
#if DEBUG
    // 供设计器预览
    public DownloadPaneViewModel()
    {
        VersionName = "1.21.5";
    }
#endif
    public DownloadPaneViewModel(VersionBasicInfo Version, DownloadPageViewModel downloadPane)
    {
        VersionName = Version.ID.ToString();
        thisVersionBasicInfo = Version;
        this.downloadPage = downloadPane;
    }
    private VersionBasicInfo thisVersionBasicInfo;
    DownloadPageViewModel downloadPage;
    [ObservableProperty]
    public string _VersionName;
    [ObservableProperty]
    public bool _IsVI;
    [ObservableProperty]
    public bool _IsMod;
    [ObservableProperty]
    public bool _IsNeoForge;
    [ObservableProperty]
    public bool _IsJava;
    [ObservableProperty]
    public bool _IsAllowDownloading = true; // 默认允许下载

    [RelayCommand]
    public async void ToDownload()
    {
        IsAllowDownloading = false;
        var VersionModType = new ModType()
        {
            IsFabric = IsMod,
            IsNeoForge = IsNeoForge,
        };
        using (Download download = new Download())
        {
            int i = 0;
            await download.StartAsync(thisVersionBasicInfo, Init.GameRootPath, Init.systemType, new Progress<(DownProgress d, int a, int b, string c)>
                (p =>
                {
                    Dp = p.d switch { 
                        DownProgress.DownMod => "正在下载Mod（Fabric）相关文件...",
                        DownProgress.DownLog4j2 => "正在下载日志配置文件",
                        DownProgress.DownLibs => "正在下载库文件...",
                        DownProgress.DownAssets => "正在下载资源文件...",
                        DownProgress.DownMain => "正在下载主文件",
                        DownProgress.Verify => "正在校验，请稍后...",
                        DownProgress.Done => "已下载完毕",
                    };
                    /*
                    Dp = (p.d == DownProgress.DownMod) ? "正在下载Mod（Fabric）相关文件..."
                    : (p.d == DownProgress.DownLog4j2) ? "正在下载日志配置文件"
                    : (p.d == DownProgress.DownLibs) ? "正在下载库文件..." 
                    : (p.d == DownProgress.DownAssets) ? "正在下载资源文件..." 
                    : (p.d == DownProgress.DownMain) ? "正在下载主文件"
                    : (p.d == DownProgress.Verify) ? "正在校验，请稍后..."
                    : (p.d == DownProgress.Done) ? "已下载完毕" : string.Empty;
                    */
                    Fs = $"{p.a}/{p.b}";
                    CurrentProgress = (double)p.b / p.a * 100;
                }), IsVersionIsolation: IsVI,modType: VersionModType,AndJava: this.IsJava);
        }
        // 在配置文件中添加版本信息
        Init.ConfigManger.config.VersionList.Add(new aVersion
        {
            VersionID = VersionName,
            modType = VersionModType,
            AddTime = DateTime.Now,
            IsVersionIsolation = IsVI
        });
        Init.ConfigManger.Save();
    }
    [ObservableProperty]
    public string _Dp = "下载未开始";
    [ObservableProperty]
    public string _Fs = "?/0";
    [ObservableProperty]
    public double _CurrentProgress = 0;
    [RelayCommand]
    public void ClosePane()
    {
        downloadPage.IsPaneShow = false;
    }
    [RelayCommand]
    public void CheckOnWeb()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://zh.minecraft.wiki/w/Java版{VersionName}",
            UseShellExecute = true
        });
    }
}