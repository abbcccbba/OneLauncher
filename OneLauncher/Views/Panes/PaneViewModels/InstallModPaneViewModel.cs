﻿using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Modrinth;
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
    public string _ModName;
    [ObservableProperty]
    public List<string> _SupportVersions;
    [ObservableProperty]
    public List<aVersion> _OwnedVersoins;
    [ObservableProperty]
    public aVersion _SelectedItem;
    [RelayCommand]
    public async Task ToInstall()
    {
        // 安装前先检查版本是否符合要求
        if(!(SupportModType.IsFabric == SelectedItem.modType.IsFabric || SupportModType.IsNeoForge == SelectedItem.modType.IsNeoForge)) 
        {
            await MainWindow.mainwindow.ShowFlyout("你的游戏不支持所对应加载器",true);
            return;
        }
        foreach (var needVersion in SupportVersions)
            if (needVersion == SelectedItem.VersionID)
            {
                using (Download downTask = new())
                    await downTask.StartDownloadMod(
                        new Progress<(long a, long b, string c)>
                        (p => Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            CurrentProgress = (double)p.b / p.a * 100;
                            Dp = p.c;
                            Fs = $"{p.a}/{p.b}";
                            Debug.WriteLine($"a: {p.a} b: {p.b} c: {p.c}");
                        })),
                        modItem.ID,
                        ((SelectedItem.IsVersionIsolation)
                        ? Path.Combine(Init.GameRootPath, "versions", SelectedItem.VersionID, "mods")
                        : Path.Combine(Init.GameRootPath, "mods")),
                        SelectedItem.VersionID, IsIncludeDependencies: IsICS);
                return;
            }
        await MainWindow.mainwindow.ShowFlyout("你的游戏不支持所对应版本", true);
        return;
    }
    [ObservableProperty]
    public bool _IsICS;
    [ObservableProperty]
    public string _Dp = "下载未开始";
    [ObservableProperty]
    public string _Fs = "?/0";
    [ObservableProperty]
    public double _CurrentProgress = 0;
}
