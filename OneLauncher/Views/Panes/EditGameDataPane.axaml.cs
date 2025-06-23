using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core.Helper;
using OneLauncher.Views.Panes.PaneViewModels;

namespace OneLauncher.Views.Panes;

public partial class EditGameDataPane : UserControl
{
    public EditGameDataPane()
    {
        InitializeComponent();
    }
    public EditGameDataPane(GameData gameData)
    {
        InitializeComponent();
        this.DataContext = new EditGameDataPaneViewModel(gameData);
    }
}