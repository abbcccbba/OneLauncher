using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using OneLauncher.Views;
using Avalonia.Controls;
using OneLauncher.Views.Panes;
using OneLauncher.Core;
using System.Runtime.CompilerServices;
namespace OneLauncher.Views.ViewModels
{
    internal partial class DownloadPageViewModel : BaseViewModel
    {
        /// <summary>
        /// 无网络重载方法，会在版本列表里显示“无网络链接”并拒绝下载
        /// </summary>
        public DownloadPageViewModel()
        {
            IsAllowDownloading = false;
            MainWindow.mainwindow.Showfyt("出现错误：无法下载版本清单");
        }
        public DownloadPageViewModel
            (
                // 先传string作为ListBox数据，未来可拓展自定义类
                List<VersionBasicInfo> AllVersionList,
                List<VersionBasicInfo> ReleaseVersionList,
                List<VersionBasicInfo> SnapshotVersionList
            )
        {
            IsAllowDownloading = true;
            this.AllItems = AllVersionList;
            this.ReleaseItems = ReleaseVersionList;
            this.SnapshotItems = SnapshotVersionList;
            AutoVersionList = AllVersionList;
        }
        
        [ObservableProperty]
        public List<VersionBasicInfo> _AllItems;
        [ObservableProperty]
        public List<VersionBasicInfo> _ReleaseItems;
        [ObservableProperty]
        public List<VersionBasicInfo> _SnapshotItems;

        [ObservableProperty]
        public UserControl _DownloadPaneContent;
        [ObservableProperty]
        public bool _IsPaneShow = false;
        [ObservableProperty]
        public bool _IsAllowDownloading;
        [ObservableProperty]
        public List<VersionBasicInfo> _AutoVersionList;

        private VersionBasicInfo selectedItem;
        public VersionBasicInfo SelectedItem
        {
            get { return selectedItem; }
            set
            {
                // 避免未选中时转换类型导致异常
                if (value == null)
                    return;
                selectedItem = value;

                // 点击操作
                // 展开并显示Pane
                IsPaneShow = true;
                DownloadPaneContent = new DownloadPane(value, this);
            }
        }
    }
}
