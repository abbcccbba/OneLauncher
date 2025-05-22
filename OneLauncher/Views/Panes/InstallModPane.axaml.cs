using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.ViewModels;

namespace OneLauncher.Views.Panes;

internal partial class InstallModPane : UserControl
{
#if DEBUG
    public InstallModPane()
    {
        InitializeComponent();
        this.DataContext = new InstallModPaneViewModel();
    }
#endif
    public InstallModPane(ModItem item)
    {
        InitializeComponent();
        this.DataContext = new InstallModPaneViewModel(item);
    }
}