using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using OneLauncher.Views;
using Avalonia.Controls;
using OneLauncher.Views.Panes;
using OneLauncher.Core;
namespace OneLauncher.Views.ViewModels
{
    internal partial class DownloadPageViewModel : BaseViewModel
    {
        public DownloadPageViewModel
            (
                // 先传string作为ListBox数据，未来可拓展自定义类
                List<VersionBasicInfo> AllVersionList,
                List<VersionBasicInfo> ReleaseVersionList,
                List<VersionBasicInfo> SnapshotVersionList
            )
        {
            this.AllItems = AllVersionList;
            this.ReleaseItems = ReleaseVersionList;
            this.SnapshotItems = SnapshotVersionList;
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

        private VersionBasicInfo selectedItem;
        public VersionBasicInfo SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (EqualityComparer<VersionBasicInfo>.Default.Equals(selectedItem, value))
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
