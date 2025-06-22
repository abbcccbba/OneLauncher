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

        // 提前确定最终的默认实例名称
        string modLoaderName = (IsMod) ? "Fabric" : (IsNeoForge) ? "NeoForge" : (IsForge) ? "Forge" : "原版";
        string finalInstanceName = $"{VersionName} - {modLoaderName}";

        return Task.Run(async () =>
        {
            var newUserVersion = new UserVersion
            {
                VersionID = VersionName,
                modType = VersionModType,
                AddTime = DateTime.Now
            };

            // 创建 GameData 实例
            var newGameData = new GameData(
                finalInstanceName,
                thisVersionBasicInfo.ID,
                newUserVersion.preferencesLaunchMode.LaunchModType,
                Init.ConfigManger.config.DefaultUserModel);

            cts = new();
            try
            {
                using (Download download = new Download())
                {
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

                    await new DownloadMinecraft(
                        download,
                        newUserVersion,
                        thisVersionBasicInfo,
                        newGameData,
                        Init.GameRootPath,
                        progressReporter,
                        cts.Token
                    ).MinecraftBasic(
                        // 避免用户乱改配置文件这里手动限制一下
                        Math.Clamp(Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads, 1, 256),
                        Math.Clamp(Init.ConfigManger.config.OlanSettings.MaximumSha1Threads, 1, 256),
                        Init.ConfigManger.config.OlanSettings.IsSha1Enabled,
                        IsDownloadFabricWithAPI,
                        IsAllowToUseBetaNeoforge,
                        IsJava,
                        Init.ConfigManger.config.OlanSettings.IsAllowToDownloadUseBMLCAPI);
                }

                // 检查版本是否已经存在
                // 深入贯彻学习全局单版本实例思想
                var existingUserVersion = Init.ConfigManger.config.VersionList
                    .FirstOrDefault(v => v.VersionID == newUserVersion.VersionID);

                if (existingUserVersion == null)
                    Init.ConfigManger.config.VersionList.Add(newUserVersion);
                else
                {
                    // 如果已存在，则正确地更新其 modType 结构体
                    ModType updatedModType = existingUserVersion.modType;
                    if (newUserVersion.modType.IsFabric) updatedModType.IsFabric = true;
                    if (newUserVersion.modType.IsNeoForge) updatedModType.IsNeoForge = true;
                    if (newUserVersion.modType.IsForge) updatedModType.IsForge = true;
                    existingUserVersion.modType = updatedModType; // 将修改后的整个副本赋值回去
                }

                // 添加游戏实例并设为默认，因为是全局单例我也没做删除功能
                await Init.GameDataManger.AddGameDataAsync(newGameData);
                await Init.GameDataManger.SetDefaultInstanceAsync(newGameData);

                await Init.ConfigManger.Save();
                MainWindow.mainwindow.ShowFlyout($"“{finalInstanceName}”已成功创建并设为默认启动项。");
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