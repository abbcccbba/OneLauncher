using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Views.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
namespace OneLauncher;

public partial class version : UserControl
{
    public version()
    {
        InitializeComponent();
        this.DataContext = new VersionPageViewModel();
    }
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        navVL.ItemsSource = Init.ConfigManger.config.VersionList.Select(x => new VersionItem(x)).ToList();
    }
}