using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core.Helper;
using OneLauncher.Views.Panes.PaneViewModels;

namespace OneLauncher.Views.Panes;

public partial class EditGameDataPane : UserControl
{
#if DEBUG
    public EditGameDataPane()
    {
        InitializeComponent();
    }
#endif
    public EditGameDataPane(GameData gameData)
    {
        InitializeComponent();
        this.DataContext = new EditGameDataPaneViewModel(gameData);
    }
}