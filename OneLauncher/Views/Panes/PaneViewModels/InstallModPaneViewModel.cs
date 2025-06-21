using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;

internal partial class InstallModPaneViewModel : BaseViewModel
{
#if DEBUG
    // 给设计器预览的
    public InstallModPaneViewModel()
    {
        if (Design.IsDesignMode)
            ModName = "暮色森林";
    }
#endif
    private readonly ModItem modItem;
    public InstallModPaneViewModel(ModItem item)
    {
        modItem = item;
        ModName = item.Title;
        SupportVersions = item.SupportVersions;
        OwnedVersoins = Init.ConfigManger.config.VersionList;
        this.SupportModType = item.SupportModType;
    }
    private ModType SupportModType;
    [ObservableProperty]
    public bool _IsShowMoreInfo = true;
    [ObservableProperty]
    public string _ModName;
    [ObservableProperty]
    public List<string> _SupportVersions;
    [ObservableProperty]
    public List<UserVersion> _OwnedVersoins;
    [ObservableProperty]
    public UserVersion _SelectedItem;
    [RelayCommand]
    public async Task ToInstall()
    {
        //// 安装前先检查版本是否符合要求
        //if(!(SupportModType.IsFabric == SelectedItem.modType.IsFabric || SupportModType.IsNeoForge == SelectedItem.modType.IsNeoForge)) 
        //{
        //    MainWindow.mainwindow.ShowFlyout("你的游戏不支持所对应加载器",true);
        //    return;
        //}
        //IsShowMoreInfo = false;
        //foreach (var needVersion in SupportVersions)
        //    if (needVersion == SelectedItem.VersionID)
        //    {
        //        using (Download downTask = new())
        //            await downTask.StartDownloadMod(
        //                new Progress<(long all, long done, string c)>
        //                (p => Dispatcher.UIThread.Post(() =>
        //                {
        //                    CurrentProgress = (double)p.done / p.all * 100;
        //                    Dp = p.c;
        //                    Fs = $"{p.done}/{p.all}";
        //                })),
        //                modItem.ID,
        //                ((SelectedItem.IsVersionIsolation)
        //                ? Path.Combine(Init.GameRootPath, "versions", SelectedItem.VersionID, "mods")
        //                : Path.Combine(Init.GameRootPath, "mods")),
        //                SelectedItem.VersionID, IsIncludeDependencies: IsICS, IsSha1: Init.ConfigManger.config.OlanSettings.IsSha1Enabled);
        //        return;
        //    }
        //MainWindow.mainwindow.ShowFlyout("你的游戏不支持所对应版本", true);
    }
    [ObservableProperty]
    public bool _IsICS;
    [ObservableProperty]
    public string _Dp = "下载未开始";
    [ObservableProperty]
    public string _Fs = "?/0";
    [ObservableProperty]
    public double _CurrentProgress = 0;
    [RelayCommand]
    public void ClosePane()
    {
        MainWindow.mainwindow.modsBrowserPage.viewmodel.IsPaneShow = false;
    }
}
