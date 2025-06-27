using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Views.ViewModels;
using OneLauncher.Views.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        return Task.Run(async () =>
        {
            DownloadInfo content;

            cts = new();
            try
            {
                using (Download download = new Download())
                {
                    // 创建下载上下文信息
                    content = await DownloadInfo.Create(
                        VersionName,
                        new ModType()
                        {
                            IsFabric = IsMod,
                            IsNeoForge = IsNeoForge,
                            IsForge = IsForge
                        },
                        download, 
                        IsAllowToUseBetaNeoforge, 
                        IsUseRecommendedToInstallForge, 
                        IsDownloadFabricWithAPI, 
                        IsJava
                        );
                    // 创建进度回调
                    var progressReporter = new Progress<(DownProgress d, int a, int b, string c)>(p =>
                        Dispatcher.UIThread.Post(() =>
                        {
                            double progressPercentage = (p.a == 0) ? 0 : (double)p.b / p.a * 100;
                            if (p.d == DownProgress.Done)
                            {
                                Dp = "已下载完毕";
                                CurrentProgress = 100;
                            }
                            else if ((DateTime.UtcNow - lastUpdateTime) > _updateInterval)
                            {
                                Dp = p.d switch
                                {
                                    DownProgress.Meta => "准备元数据...",
                                    DownProgress.DownAndInstModFiles => "下载并安装模组加载器...",
                                    DownProgress.DownLog4j2 => "下载日志配置...",
                                    DownProgress.DownLibs => "下载库文件...",
                                    DownProgress.DownAssets => "下载资源文件...",
                                    DownProgress.DownMain => "下载游戏主文件...",
                                    DownProgress.Verify => "校验文件中..."
                                };
                                CurrentProgress = progressPercentage;
                                lastUpdateTime = DateTime.UtcNow;
                            }
                            Fs = $"{p.b}/{p.a}";
                            FileName = p.c;
                        }));
                    // 执行下载
                    await new DownloadMinecraft(
                        content,
                        progressReporter,
                        cts.Token
                    ).MinecraftBasic(
                        Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads,
                        Init.ConfigManger.config.OlanSettings.MaximumSha1Threads,
                        Init.ConfigManger.config.OlanSettings.IsSha1Enabled);
                }
                var mayInstalledVersion = Init.ConfigManger.config.VersionList.FirstOrDefault(x => x.VersionID == VersionName);
                if (mayInstalledVersion == null)
                    Init.ConfigManger.config.VersionList.Add(content.VersionInstallInfo);
                else
                {
                    var updatedModType = mayInstalledVersion.modType;
                    if (content.VersionInstallInfo.modType.IsFabric) updatedModType.IsFabric = true;
                    if (content.VersionInstallInfo.modType.IsNeoForge) updatedModType.IsNeoForge = true;
                    if (content.VersionInstallInfo.modType.IsForge) updatedModType.IsForge = true;
                    mayInstalledVersion.modType = updatedModType; // 将修改后的整个副本赋值回去
                }
                await Init.GameDataManger.AddGameDataAsync(content.UserInfo);
                await Init.GameDataManger.SetDefaultInstanceAsync(content.UserInfo);
                await Init.ConfigManger.Save();
                MainWindow.mainwindow.ShowFlyout($"“{content.UserInfo.Name}”已成功创建并设为默认启动项。");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("下载任务被用户取消");
            }
            catch (OlanException ex)
            {
                await cts.CancelAsync();
                await OlanExceptionWorker.ForOlanException(ex);
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