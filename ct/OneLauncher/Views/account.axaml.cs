using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using System.Diagnostics;
using OneLauncher.Codes;
using OneLauncher.Views;

namespace OneLauncher;

public partial class account : UserControl
{
    public account()
    {
        InitializeComponent();
        AccountListViews.ItemsSource = GAR.configManger.config.UserModelList;
    }
    private void SetDefault(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button)
            if (button.DataContext is UserModel aUserModel)
            {
                GAR.configManger.config.DefaultUserModel = aUserModel;
                GAR.configManger.Write(GAR.configManger.config);
            }
    }
}