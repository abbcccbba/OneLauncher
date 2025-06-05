using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Views.Panes.PaneViewModels;

namespace OneLauncher.Views.Panes;

public partial class InitServerPane : UserControl
{
#if DEBUG
    public InitServerPane()
    {
        InitializeComponent();
        this.DataContext = new InitServerPaneViewModel();
    }
#endif
    public InitServerPane(string version)
    {
        InitializeComponent();
        this.DataContext = new InitServerPaneViewModel(version);
    }
}