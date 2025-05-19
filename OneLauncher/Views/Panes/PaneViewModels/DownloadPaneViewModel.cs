using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
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
        // 无网络时拒绝下载
        //if (!Init.IsNetwork) 
        //{
        //    IsAllowDownloading = false;
        //    VersionName = "网络不可用";
        //}
    }
    private VersionBasicInfo thisVersionBasicInfo;
    DownloadPageViewModel downloadPage;
    [ObservableProperty]
    public string _VersionName;
    [ObservableProperty]
    public bool _IsAllowDownloading = true; // 默认允许下载

    [RelayCommand]
    public async void ToDownload()
    {
        await ToDownload(thisVersionBasicInfo);
        // 下载完毕后禁用下载按钮
        //IsAllowDownloading = false;
        // 在配置文件中添加版本信息
        Init.ConfigManger.WriteVersion(new aVersion
        {
            VersionID = VersionName,
            IsMod = false,
            AddTime = DateTime.Now
        });
    }
    [ObservableProperty]
    public string _D_DM = "下载未开始";
    [ObservableProperty]
    public double _CurrentProgress = 0;
    private async Task ToDownload(VersionBasicInfo versionBasicInfo)
    {
        string GameRootPath = Path.Combine(Init.BasePath,".minecraft");
        using (Download download = new Download())
        {
            await download.StartAsync(versionBasicInfo, GameRootPath, Init.systemType, new Progress<(DownProgress dProgress, int a, int b, string c)>
                (p => {
                    Debug.WriteLine(p.c);
                }));
        }
        
    }
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