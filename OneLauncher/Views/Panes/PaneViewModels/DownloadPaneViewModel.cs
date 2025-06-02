using Avalonia.Controls;
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
        IsAllowNeoforge = true;
        IsAllowFabric = true;
    }
#endif
    public DownloadPaneViewModel(VersionBasicInfo Version, DownloadPageViewModel downloadPane)
    {
        VersionName = Version.ID.ToString();
        thisVersionBasicInfo = Version;
        this.downloadPage = downloadPane;
        // 这个版本以下不支持模组加载器
        IsAllowFabric = new System.Version(Version.ID) < new System.Version("1.14") ? false : true;
        IsAllowNeoforge = new System.Version(Version.ID) < new System.Version("1.20.2") ? false : true;
    }
    public DownloadPaneViewModel(VersionBasicInfo Version,aVersion userVersion)
    {
        VersionName = Version.ID.ToString();
        thisVersionBasicInfo = Version;
        IsVI = userVersion.IsVersionIsolation;
        IsMod = userVersion.modType.IsFabric;
        IsNeoForge = userVersion.modType.IsNeoForge;
        IsJava = true;
        Task.Run(async () => 
        {
            // 给用户二秒选择时间，然后开始下载
            await Task.Delay(2000);
            await ToDownload();
        });
    }
    #region 数据绑定区
    [ObservableProperty]
    public bool _IsAllowFabric;
    [ObservableProperty]
    public bool _IsAllowNeoforge;
    private VersionBasicInfo thisVersionBasicInfo;
    DownloadPageViewModel downloadPage;
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
        try
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
                        // 每三次更新UI，减轻UI线程压力
                        if (i % 3 == 1)
                        {
                            Dp = p.d switch
                            {
                                DownProgress.DownAndInstModFiles => "正在下载Mod相关文件...",
                                DownProgress.DownLog4j2 => "正在下载日志配置文件",
                                DownProgress.DownLibs => "正在下载库文件...",
                                DownProgress.DownAssets => "正在下载资源文件...",
                                DownProgress.DownMain => "正在下载主文件",
                                DownProgress.Verify => "正在校验，请稍后..."
                            };
                            Fs = $"{p.b}/{p.a}";
                            CurrentProgress = (double)p.b / p.a * 100;
                            FileName = p.c;
                        }
                        if(p.d == DownProgress.Done)
                        {
                            Dp = "已下载完毕";
                            Fs = $"{p.b}/{p.a}";
                            CurrentProgress = (double)p.b / p.a * 100;
                            FileName = p.c;
                        }
                        i++;
                    }), 
                    IsVersionIsolation: IsVI, 
                    maxConcurrentDownloads:Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads,
                    maxConcurrentSha1:Init.ConfigManger.config.OlanSettings.MaximumSha1Threads,
                    modType: VersionModType, 
                    AndJava: this.IsJava,
                    fS:IsDownloadFabricWithAPI,
                    nS:IsAllowToUseBetaNeoforge,
                    IsSha1:Init.ConfigManger.config.OlanSettings.IsSha1Enabled
                    );
            }
            // 在配置文件中添加版本信息
            if(downloadPage != null)
                Init.ConfigManger.config.VersionList.Add(new aVersion
                {
                    VersionID = VersionName,
                    modType = VersionModType,
                    AddTime = DateTime.Now,
                    IsVersionIsolation = IsVI
                });
            Init.ConfigManger.Save();
        }
        catch(OlanException ex) 
        {
            await OlanExceptionWorker.ForOlanException(ex);
        }
    }
    [RelayCommand]
    public void ClosePane()
    {
        if (downloadPage != null)
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