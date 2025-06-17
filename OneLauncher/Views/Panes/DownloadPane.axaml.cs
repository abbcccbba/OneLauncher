using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core.Helper;
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.ViewModels;
using OneLauncher.Views.Windows;

namespace OneLauncher.Views.Panes;
internal partial class DownloadPane : UserControl
{
#if DEBUG
    // 供设计器预览
    public DownloadPane()
    {
        InitializeComponent();
        this.DataContext = new DownloadPaneViewModel();
    }
#endif
    public DownloadPane(VersionBasicInfo Version)
    {
        InitializeComponent();
        this.DataContext = new DownloadPaneViewModel(Version);
    }
    public DownloadPane(VersionBasicInfo Version,DownloadPaneViewModel viewmodel)
    {
        InitializeComponent();
        this.DataContext = viewmodel;
    }
}