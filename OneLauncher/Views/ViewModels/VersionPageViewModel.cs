using OneLauncher.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OneLauncher.Codes;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace OneLauncher.Views.ViewModels;
internal partial class VersionItem
{
    public VersionItem(aVersion a) 
    {
        V = a;
    }
    public aVersion V { get; set; }
    [RelayCommand]
    public void LaunchGame(aVersion version)
    {
        Task.Run (() =>Home.LaunchGame(version.VersionID,Init.ConfigManger.config.DefaultUserModel,Init.BasePath));
    }
}
internal partial class VersionPageViewModel : BaseViewModel
{
    public VersionPageViewModel()
    {
#if DEBUG
        // 设计时数据
        if (Design.IsDesignMode)
        {
            VersionList = new List<VersionItem>()
            {
                new VersionItem(new aVersion() {VersionID="1.21.5",IsMod=false,AddTime=DateTime.Now})
            };
        }
        else
#endif
        VersionList = Init.ConfigManger.config.VersionList.Select(x => new VersionItem(x)).ToList();
    }
    [ObservableProperty]
    public List<VersionItem> _VersionList;
    [RelayCommand]
    public void RefreshList()
    {
        VersionList = Init.ConfigManger.config.VersionList.Select(x => new VersionItem(x)).ToList();
    }
    [RelayCommand]
    public void ToDownloadGame()
    {
        Debug.Write(2);
    }
}

