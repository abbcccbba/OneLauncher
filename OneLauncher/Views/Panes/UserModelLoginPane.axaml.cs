using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.ViewModels;

namespace OneLauncher.Views.Panes;

internal partial class UserModelLoginPane : UserControl
{
#if DEBUG
    public UserModelLoginPane()
    {
        InitializeComponent();
        this.DataContext = new UserModelLoginPaneViewModel();
    }
#endif
    public UserModelLoginPane(AccountPageViewModel accountPageViewModel)
    {
        InitializeComponent();
        this.DataContext = new UserModelLoginPaneViewModel(accountPageViewModel);
    }
}