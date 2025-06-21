using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Net.java;
using OneLauncher.Views.ViewModels;
using OneLauncher.Views.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
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
        IsAllowFabric = new Version(Version.ID) < new System.Version("1.14") ? false : true;
        IsAllowNeoforge = new Version(Version.ID) < new System.Version("1.20.2") ? false : true;
        IsAllowForge = new Version(Version.ID) < new System.Version("1.2") ? false : true;
    }
    ~DownloadPaneViewModel()
    {
        Debug.WriteLine("页面释放");
        cts?.Dispose();
    }
    private CancellationTokenSource cts;
    #region 数据绑定区
    [ObservableProperty] public bool _IsForge;
    [ObservableProperty] public bool _IsAllowForge;
    [ObservableProperty] public bool _IsUseRecommendedToInstallForge = true;
    [ObservableProperty] public bool isLaunchGameAfterDone;
    [ObservableProperty] public bool _IsAllowFabric;
    [ObservableProperty] public bool _IsAllowNeoforge;
    [ObservableProperty] public string _VersionName;
    [ObservableProperty] public bool _IsMod;
    [ObservableProperty] public bool _IsNeoForge;
    [ObservableProperty] public bool _IsAllowToUseBetaNeoforge = false;
    [ObservableProperty] public bool _IsDownloadFabricWithAPI = true;
    [ObservableProperty] public bool _IsJava;
    [ObservableProperty] public bool _IsAllowDownloading = true; // 默认允许下载
    [ObservableProperty] public string _Dp = "下载未开始";
    [ObservableProperty] public string _Fs = "?/0";
    [ObservableProperty] public string _FileName = "操作文件名：（下载未开始）";
    [ObservableProperty] public double _CurrentProgress = 0;
    private VersionBasicInfo thisVersionBasicInfo;
    #endregion
    [RelayCommand]
    public Task ToDownload()
    {
        IsAllowDownloading = false;
        var VersionModType = new ModType()
        {
            IsFabric = IsMod,
            IsNeoForge = IsNeoForge,
            IsForge = IsForge,
        };
        DateTime lastUpdateTime = DateTime.MinValue;
        TimeSpan _updateInterval = TimeSpan.FromMilliseconds(50);
        return Task.Run(async () => // 避免实际下载任务在UI线程执行导致线程卡死，别改这个，因为真的会卡死
        {
            var newUserVersion = new UserVersion
            {
                VersionID = VersionName,
                modType = VersionModType,
                AddTime = DateTime.Now,
                preferencesLaunchMode = new PreferencesLaunchMode()
                {
                    LaunchModType = 
                    (IsMod) ? ModEnum.fabric : 
                    (IsNeoForge) ? ModEnum.neoforge : 
                    (IsForge) ? ModEnum.forge : ModEnum.none,
                    IsUseDebugModeLaunch = false
                }
            };
            var newGameData = new GameData(
                "新游戏数据",
                thisVersionBasicInfo.ID,
                newUserVersion.preferencesLaunchMode.LaunchModType,
                Init.ConfigManger.config.DefaultUserModel);
            cts = new();
            try
            {
                using (Download download = new Download()) // 在后台任务内部创建和管理Download对象
                {
                    var progressReporter = new Progress<(DownProgress d, int a, int b, string c)>(p =>
                        Dispatcher.UIThread.Post(() =>
                        {
                            double progressPercentage = (p.a == 0) ? 0 : (double)p.b / p.a * 100;
                            bool isInitialReport = (lastUpdateTime == DateTime.MinValue && p.b == 0); // 更明确的初始条件

                            if (p.d == DownProgress.Done)
                            {
                                Dp = "已下载完毕";
                                Fs = $"{p.b}/{p.a}";
                                CurrentProgress = 100;
                                FileName = p.c;
                                lastUpdateTime = DateTime.UtcNow; // 完成时也更新时间戳
                            }
                            // 应用基于时间的节流策略，或在初始状态时更新
                            else if (isInitialReport || (DateTime.UtcNow - lastUpdateTime) > _updateInterval)
                            {
                                Dp = p.d switch
                                {
                                    DownProgress.Meta => "下载原信息...",
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
                                lastUpdateTime = DateTime.UtcNow;
                            }
                        }));

                    // 现在可以安全地 await LaunchCore
                    await new DownloadMinecraft(
                        download,
                        newUserVersion,
                        thisVersionBasicInfo,
                        newGameData,
                        Init.GameRootPath,
                        progressReporter,
                        cts.Token
                        )
                    .MinecraftBasic(
                        Math.Clamp(Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads,1,256),
                        Math.Clamp(Init.ConfigManger.config.OlanSettings.MaximumSha1Threads,1,256),
                        Init.ConfigManger.config.OlanSettings.IsSha1Enabled,
                        IsDownloadFabricWithAPI,
                        IsAllowToUseBetaNeoforge,
                        IsJava,
                        Init.ConfigManger.config.OlanSettings.IsAllowToDownloadUseBMLCAPI);  
                }
                
                if (IsLaunchGameAfterDone)
                    _ = version.EasyGameLauncher(newGameData);

                // 在配置文件中添加版本信息
                Init.ConfigManger.config.VersionList.Add(newUserVersion);
                await Init.GameDataManger.AddGameDataAsync(newGameData);
                await Init.ConfigManger.Save();
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("下载任务被取消");
                return;
            }
            catch (OlanException ex)
            {
                await cts.CancelAsync();
                OlanExceptionWorker.ForOlanException(ex);
            }
#if !DEBUG
            catch (Exception ex)
            {
                await cts.CancelAsync();
                await OlanExceptionWorker.ForUnknowException(ex);
            }
#endif
        });
    }
    [RelayCommand]
    public void ToCancellationDownloadTask()
    {
        cts.Cancel();
    }
    [RelayCommand]
    public void ClosePane()
    {
        MainWindow.mainwindow.downloadPage.viewmodel.IsPaneShow = false;
    }
    [RelayCommand]
    public void PopUp()
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        new PopUpPane(new DownloadPane(thisVersionBasicInfo,this)).Show();
    }
    //public void CheckOnWeb()
    //{
    //    Process.Start(new ProcessStartInfo
    //    {
    //        FileName = $"https://zh.minecraft.wiki/w/Java版{VersionName}",
    //        UseShellExecute = true
    //    });
    //}
}