using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using OneLauncher.Views;
using OneLauncher.Core;
using OneLauncher.Codes;
namespace OneLauncher;

public partial class version : UserControl
{
    public version()
    {
        InitializeComponent();
    }
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        if (VersionListViews.ItemsSource == null)
            VersionListViews.ItemsSource = Init.ConfigManger.config.VersionList;
        else return;
    }
    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button)
        if (button.DataContext is aVersion version)
        {
            Task.Run(async () => OneLauncher.Home.LaunchGame(Codes.Init.BasePath,version.versionBasicInfo.name));
        }
    }
}