﻿using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Downloader;
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
        IsAllowNeoforge = true;
        IsAllowFabric = true;
    }
#endif
    public DownloadPaneViewModel(VersionBasicInfo Version)
    {
        VersionName = Version.ID.ToString();
        thisVersionBasicInfo = Version;
        // 这个版本以下不支持模组加载器
        IsAllowFabric = new System.Version(Version.ID) < new System.Version("1.14") ? false : true;
        IsAllowNeoforge = new System.Version(Version.ID) < new System.Version("1.20.2") ? false : true;
    }
    public DownloadPaneViewModel(VersionBasicInfo Version,UserVersion userVersion)
    {
        VersionName = Version.ID.ToString();
        thisVersionBasicInfo = Version;
        IsVI = userVersion.IsVersionIsolation;
        IsMod = userVersion.modType.IsFabric;
        IsNeoForge = userVersion.modType.IsNeoForge;
        IsJava = true;
        ToDownload();
    }
    #region 数据绑定区
    [ObservableProperty]
    public bool _IsAllowFabric;
    [ObservableProperty]
    public bool _IsAllowNeoforge;
    private VersionBasicInfo thisVersionBasicInfo;
    [ObservableProperty]
    public string _VersionName;
    [ObservableProperty]
    public bool _IsVI;
    [ObservableProperty]
    public bool _IsMod;
    partial void OnIsModChanged(bool value)
    {
        if (value)
            IsVI = true;
    }
    [ObservableProperty]
    public bool _IsNeoForge;
    partial void OnIsNeoForgeChanged(bool value)
    {
        if (value)
            IsVI = true;
    }
    [ObservableProperty]
    public bool _IsAllowToUseBetaNeoforge = false;
    [ObservableProperty]
    public bool _IsDownloadFabricWithAPI = true;
    [ObservableProperty]
    public bool _IsJava;
    [ObservableProperty]
    public bool _IsAllowDownloading = true; // 默认允许下载
    [ObservableProperty]
    public string _Dp = "下载未开始";
    [ObservableProperty]
    public string _Fs = "?/0";
    [ObservableProperty]
    public string _FileName = "操作文件名：（下载未开始）";
    [ObservableProperty]
    public double _CurrentProgress = 0;
    #endregion
    [RelayCommand]
    public async Task ToDownload()
    {
        IsAllowDownloading = false;
        var VersionModType = new ModType()
        {
            IsFabric = IsMod,
            IsNeoForge = IsNeoForge,
        };
        DateTime _lastUpdateTime = DateTime.MinValue;
        TimeSpan _updateInterval = TimeSpan.FromMilliseconds(50);
        _ = Task.Run(async () => // 避免实际下载任务在UI线程执行导致线程卡死，别改这个，因为真的会卡死
        {
            try
            {
                using (Download download = new Download()) // 在后台任务内部创建和管理Download对象
                {
                    var progressReporter = new Progress<(DownProgress d, int a, int b, string c)>(p =>
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            double progressPercentage = (p.a == 0) ? 0 : (double)p.b / p.a * 100;
                            bool isInitialReport = (_lastUpdateTime == DateTime.MinValue && p.b == 0); // 更明确的初始条件

                            if (p.d == DownProgress.Done)
                            {
                                Dp = "已下载完毕";
                                Fs = $"{p.b}/{p.a}";
                                CurrentProgress = 100;
                                FileName = p.c;
                                _lastUpdateTime = DateTime.UtcNow; // 完成时也更新时间戳
                            }
                            // 应用基于时间的节流策略，或在初始状态时更新
                            else if (isInitialReport || (DateTime.UtcNow - _lastUpdateTime) > _updateInterval)
                            {
                                Dp = p.d switch
                                {
                                    DownProgress.DownAndInstModFiles => "正在下载Mod相关文件...",
                                    DownProgress.DownLog4j2 => "正在下载日志配置文件...",
                                    DownProgress.DownLibs => "正在下载库文件...",
                                    DownProgress.DownAssets => "正在下载资源文件...",
                                    DownProgress.DownMain => "正在下载主文件...",
                                    DownProgress.Verify => "正在校验，请稍后..."
                                };
                                Fs = $"{p.b}/{p.a}";
                                CurrentProgress = progressPercentage;
                                FileName = p.c; // 确保文件名也更新
                                _lastUpdateTime = DateTime.UtcNow;
                            }
                        });
                    });

                    // 现在可以安全地 await StartAsync
                    await download.StartAsync(thisVersionBasicInfo, Init.GameRootPath, Init.systemType,
                        progressReporter, // 传递创建的 progressReporter
                        IsVersionIsolation: IsVI,
                        maxConcurrentDownloads: Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads,
                        maxConcurrentSha1: Init.ConfigManger.config.OlanSettings.MaximumSha1Threads,
                        modType: VersionModType,
                        AndJava: this.IsJava,
                        fS: IsDownloadFabricWithAPI,
                        nS: IsAllowToUseBetaNeoforge,
                        IsSha1: Init.ConfigManger.config.OlanSettings.IsSha1Enabled
                    );
                } // 当 Task.Run 中的这个 using 块结束时，download.Dispose() 才会被调用，此时 StartAsync 已完成。
            }
            catch (OlanException ex)
            {
                await OlanExceptionWorker.ForOlanException(ex);
            }
            //catch (Exception ex)
            //{
            //    await OlanExceptionWorker.ForUnknowException(ex);
            //}
        });
        // 在配置文件中添加版本信息
        Init.ConfigManger.config.VersionList.Add(new UserVersion
        {
            VersionID = VersionName,
            modType = VersionModType,
            AddTime = DateTime.Now,
            IsVersionIsolation = IsVI,
            preferencesLaunchMode = new PreferencesLaunchMode()
            {
                LaunchModType = (IsMod) ? ModEnum.fabric : (IsNeoForge) ? ModEnum.neoforge : ModEnum.none,
                IsUseDebugModeLaunch = false
            }
        });
        Init.ConfigManger.Save();
    }
    [RelayCommand]
    public void ClosePane()
    {
        MainWindow.mainwindow.downloadPage.viewmodel.IsPaneShow = false;
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