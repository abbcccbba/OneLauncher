using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core;
using OneLauncher.Core.Modrinth;
using OneLauncher.Core.Modrinth.JsonModelSearch;
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
        IsAllowDownloading = false;
        MainWindow.mainwindow.ShowFlyout("出现错误：无法下载版本清单",true);
    }
    public DownloadPageViewModel (List<VersionBasicInfo> ReleaseVersionList)
    {
        IsAllowDownloading = true;
        this.ReleaseItems = ReleaseVersionList;
        AutoVersionList = ReleaseVersionList;
    }
    
    [ObservableProperty]
    public List<VersionBasicInfo> _ReleaseItems;
    private VersionBasicInfo selectedItem;
    public VersionBasicInfo SelectedItem 
    {
        set 
        { 
            selectedItem = value;
            if(value != null)
                ToDownload(value);
        }
        get { return selectedItem; }
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
        DownloadPaneContent = new DownloadPane(vbi, this);
    }
}