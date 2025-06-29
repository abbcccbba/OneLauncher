using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.ViewModels;

namespace OneLauncher.Views.Panes;

internal partial class SkinMangerPane : UserControl
{
    public SkinMangerPane()
    {
        InitializeComponent();
#if DEBUG
        this.DataContext = new SkinMangerPaneViewModel();
#endif
    }
    public SkinMangerPane(AccountPageViewModel accountPageViewModel, UserModel SelUserModel)
    {
        InitializeComponent();
        this.DataContext = new SkinMangerPaneViewModel(accountPageViewModel, SelUserModel);
    }
}