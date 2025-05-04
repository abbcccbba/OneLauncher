using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using OneLauncher.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OneLauncher.Codes;

namespace OneLauncher.Views;

public partial class MainWindow : Window
{
    private Home HomePage;
    private version versionPage;
    private download downloadPage;
    private settings settingsPage = new settings();
    private account accountPage;
    public static MainWindow mainwindow;
    public MainWindow()
    {
        InitializeComponent();
        Codes.Init.Initialize();
        // 默认页
        PageContent.Content = new Welcome();
        mainwindow = this;
    }
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        HomePage = new Home();
        versionPage = new version();
        accountPage = new account();
        downloadPage = new download();
    }
    private void Home_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PageContent.Content = HomePage;
    }
    private void version_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PageContent.Content = versionPage;
    }
    private void Account_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PageContent.Content = accountPage;
    }
    private void download_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PageContent.Content = downloadPage;
    }
    private void settings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PageContent.Content = settingsPage;
    }
}