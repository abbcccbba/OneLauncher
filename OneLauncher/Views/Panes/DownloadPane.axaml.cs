using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core.Helper;
using OneLauncher.Views.ViewModels;

namespace OneLauncher.Views.Panes;
internal partial class DownloadPane : UserControl
{
#if DEBUG
    // 供设计器预览
    public DownloadPane()
    {
        InitializeComponent();
        this.DataContext = new PaneViewModels.DownloadPaneViewModel();
    }
#endif
    public DownloadPane(VersionBasicInfo Version, DownloadPageViewModel downloadPage)
    {
        InitializeComponent();
        this.DataContext = new PaneViewModels.DownloadPaneViewModel(Version);
    }
}