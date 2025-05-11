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
namespace OneLauncher.Views.ViewModels
{
    internal partial class DownloadPageViewModel : BaseViewModel
    {
        public DownloadPageViewModel
            (
                // 先传string作为ListBox数据，未来可拓展自定义类
                List<string> AllVersionList,
                List<string> ReleaseVersionList,
                List<string> SnapshotVersionList
            )
        {
            _AllItems = AllVersionList;
            _ReleaseItems = ReleaseVersionList;
            _SnapshotItems = SnapshotVersionList;
        }
        [ObservableProperty]
        public List<string> _AllItems;
        [ObservableProperty]
        public List<string> _ReleaseItems;
        [ObservableProperty]
        public List<string> _SnapshotItems;

        [ObservableProperty]
        public UserControl _DownloadPaneContent;
        [ObservableProperty]
        public bool _IsPaneShow = false;

        private string selectedItem;
        public string SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (selectedItem == value)
                    return;
                selectedItem = value;

                // 点击操作
                //Debug.WriteLine($"{value}");
                IsPaneShow = true;
                DownloadPaneContent = new DownloadPane(value,this);
            }
        }
    }
}
