

using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Views.ViewModels;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    public DownloadPaneViewModel(string Version,DownloadPageViewModel downloadPane)
    {
        VersionName = Version;
        this.downloadPage = downloadPane;
        // 无网络时拒绝下载
        if(!Init.IsNetwork)
            IsAllowDownloading = false;
    }
    DownloadPageViewModel downloadPage;
    [ObservableProperty]
    public string _VersionName;
    [ObservableProperty]
    public bool _IsAllowDownloading = true; // 默认允许下载

    [RelayCommand]
    public void ToDownload()
    {
        Init.ConfigManger.AddVersion(new aVersion 
        {
            VersionID = VersionName,
            IsMod = false,
            AddTime = DateTime.Now
        });
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