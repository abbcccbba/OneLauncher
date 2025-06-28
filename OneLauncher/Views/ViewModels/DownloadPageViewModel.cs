using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Views;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
namespace OneLauncher.Views.ViewModels;

internal partial class DownloadPageViewModel : BaseViewModel
{
    // 这里要异步初始化的，但是屎山懒得修了
    private async Task VersionManifestReader()
    {
        VersionsList vl;
        if (!File.Exists(Path.Combine(Init.BasePath, "version_manifest.json")))
        {
            // 如果不存在版本清单则调用下载方法
            try
            {
                // 路径（1）
                using (Download download = new Download())
                    await download.DownloadFile(
                        "https://piston-meta.mojang.com/mc/game/version_manifest.json",
                        Path.Combine(Init.BasePath, "version_manifest.json")
                    );
                vl = new VersionsList(await File.ReadAllTextAsync(Path.Combine(Init.BasePath, "version_manifest.json")));
            }
            catch (HttpRequestException)
            {
                await OlanExceptionWorker.ForOlanException(
                    new OlanException("无法加载下载版本列表", "无法进行网络请求，且本地文件不存在", OlanExceptionAction.Error),
                    () => _=VersionManifestReader()
                        );
                return;
            }
        }
        // 路径（3）
        vl = new VersionsList(await File.ReadAllTextAsync(Path.Combine(Init.BasePath, "version_manifest.json")));
        // 提前缓存避免UI线程循环卡顿
        List<VersionBasicInfo> releaseVersions = Init.MojangVersionList = vl.GetReleaseVersionList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        viewmodel = new DownloadPageViewModel(releaseVersions));
        this.DataContext = viewmodel;
    }
    public DownloadPageViewModel (List<VersionBasicInfo> ReleaseVersionList)
    {
        IsAllowDownloading = true;
        ReleaseItems = ReleaseVersionList;
        
    }
    [ObservableProperty] private bool isLoaded = false;
    [ObservableProperty] private List<VersionBasicInfo> releaseItems;
    [ObservableProperty] public VersionBasicInfo selectedItem;
    partial void OnSelectedItemChanged(VersionBasicInfo value)
    {
        if(value != null)
            ToDownload(value);
    }
    [ObservableProperty] public UserControl _DownloadPaneContent;
    [ObservableProperty] public bool _IsPaneShow = false;
    [ObservableProperty] public bool _IsAllowDownloading;
    public List<VersionBasicInfo> _AutoVersionList => ReleaseItems;

    [RelayCommand]
    private void ToDownload(VersionBasicInfo vbi)
    {
        IsPaneShow = true;
        DownloadPaneContent = new DownloadPane(vbi);
    }
}