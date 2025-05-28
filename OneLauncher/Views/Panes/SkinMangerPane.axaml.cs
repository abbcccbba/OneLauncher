using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.ViewModels;

namespace OneLauncher.Views.Panes;

internal partial class SkinMangerPane : UserControl
{
#if DEBUG
    public SkinMangerPane()
    {
        InitializeComponent();
        this.DataContext = new SkinMangerPaneViewModel();
    }
#endif
    public SkinMangerPane(AccountPageViewModel accountPageViewModel, UserModel SelUserModel)
    {
        InitializeComponent();
        this.DataContext = new SkinMangerPaneViewModel(accountPageViewModel, SelUserModel);
    }
}