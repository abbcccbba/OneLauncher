using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Views.Panes.PaneViewModels;

namespace OneLauncher.Views.Panes;

public partial class PowerPlayPane : UserControl
{
    public PowerPlayPane()
    {
        InitializeComponent();
        this.DataContext = new PowerPlayPaneViewModel();
    }
}