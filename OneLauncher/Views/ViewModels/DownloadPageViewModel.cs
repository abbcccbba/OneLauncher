﻿using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core.Helper;
using OneLauncher.Views;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
namespace OneLauncher.Views.ViewModels;

internal partial class DownloadPageViewModel : BaseViewModel
{
    /// <summary>
    /// 无网络重载方法，会在版本列表里显示“无网络链接”并拒绝下载
    /// </summary>
    public DownloadPageViewModel()
    {
        
    }
    public DownloadPageViewModel (List<VersionBasicInfo> ReleaseVersionList)
    {
        IsAllowDownloading = true;
        ReleaseItems = ReleaseVersionList;
        AutoVersionList = ReleaseVersionList;
    }

    [ObservableProperty]
    public List<VersionBasicInfo> releaseItems;
    [ObservableProperty]
    public VersionBasicInfo selectedItem;
    partial void OnSelectedItemChanged(VersionBasicInfo value)
    {
        if(value != null)
            ToDownload(value);
    }
    [ObservableProperty]
    public UserControl _DownloadPaneContent;
    [ObservableProperty]
    public bool _IsPaneShow = false;
    [ObservableProperty]
    public bool _IsAllowDownloading;
    [ObservableProperty]
    public List<VersionBasicInfo> _AutoVersionList;

    [RelayCommand]
    public void ToDownload(VersionBasicInfo vbi)
    {
        IsPaneShow = true;
        DownloadPaneContent = new DownloadPane(vbi);
    }
}