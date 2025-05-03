using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using OneLauncher.Codes;
using OneLauncher.Views;
using OneLauncher.Core;

namespace OneLauncher;

public partial class version : UserControl
{
    public version()
    {
        InitializeComponent();
    }
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        VersionListViews.ItemsSource = GAR.configManger.config.VersionList;
    }
    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button)
        if (button.DataContext is aVersion version)
        {
            Task.Run(async () => OneLauncher.Home.LaunchGame(GAR.BasePath,version.versionBasicInfo.name));
        }
    }
}