using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;

internal partial class InstallModPaneViewModel : BaseViewModel
{
#if DEBUG
    public InstallModPaneViewModel()
    {
        if (Design.IsDesignMode)
            ModName = "暮色森林";
    }
#endif
    public InstallModPaneViewModel(ModItem item)
    {
        ModName = item.Title;
        SupportVersions = item.SupportVersions;
        OwnedVersoins = Init.ConfigManger.config.VersionList;
    }
    [ObservableProperty]
    public string _ModName;
    [ObservableProperty]
    public List<Version> _SupportVersions;
    [ObservableProperty]
    public List<aVersion> _OwnedVersoins;
}
