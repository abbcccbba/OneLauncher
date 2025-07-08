using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
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
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.Panes.PaneViewModels.Factories;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Core.Helper.Models;

namespace OneLauncher.Views.ViewModels;
internal class DownloadPageClosePaneControlMessage { public bool value = false; }
internal partial class DownloadPageViewModel : BaseViewModel
{
    private readonly DownloadPaneViewModelFactory _viewFactory;
    // 这里要异步初始化的
    private async Task VersionManifestReader()
    {
        try
        {
            var versionInfos =
                await VersionsList.GetOrRefreshVersionListAsync();
            Dispatcher.UIThread.Post(() =>
            {
                ReleaseItems = new ObservableCollection<VersionBasicInfo>(versionInfos); // 安全地创建集合
                IsLoaded = true;
            });
        }
        catch (OlanException e)
        {
            await OlanExceptionWorker.ForOlanException(e,() => _=VersionManifestReader());
        }
    }
    public DownloadPageViewModel(DownloadPaneViewModelFactory viewFactory)
    {
        this._viewFactory = viewFactory;
        _=VersionManifestReader();
        // 
        WeakReferenceMessenger.Default.Register<DownloadPageClosePaneControlMessage>(this,(re,message) => IsPaneShow = message.value);
    }
    [ObservableProperty] private bool isLoaded = false;
    [ObservableProperty] private ObservableCollection<VersionBasicInfo> releaseItems;
    [ObservableProperty] private VersionBasicInfo? selectedItem;
    partial void OnSelectedItemChanged(VersionBasicInfo value)
    {
        if(value != null)
            ToDownload(value);
    }
    [ObservableProperty] public UserControl _DownloadPaneContent;
    [ObservableProperty] public bool _IsPaneShow = false;
    [ObservableProperty] public bool _IsAllowDownloading;

    [RelayCommand]
    private void ToDownload(VersionBasicInfo vbi)
    {
        IsPaneShow = true;
        DownloadPaneContent = new DownloadPane()
        {DataContext = _viewFactory.Create(vbi)};
    }
}