using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using OneLauncher.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OneLauncher;
using System.IO;
using System.Runtime.CompilerServices;

namespace OneLauncher.Views;

public partial class MainWindow : Window
{
    public static string BasePath;
    public static DBManger configManger;
    public static Window mainwindow;

    private Home HomePage;
    private version versionPage;
    private download downloadPage;
    private settings settingsPage = new settings();
    private account accountPage;

    VersionsList a;
    public MainWindow()
    {
        InitializeComponent();
        mainwindow = this;
        // 初始页
        PageContent.Content = new Welcome();
        Task.Run(async () =>
        {
            // 初始化一些东西
            BasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/OneLauncher/";
            configManger = new DBManger(new AppConfig(), BasePath);
            try
            {
                a = new VersionsList(File.ReadAllText($"{BasePath}/version_manifest.json"));
            }
            catch (System.IO.FileNotFoundException)
            {
                await Core.Download.DownloadToMinecraft
                (
                    "https://piston-meta.mojang.com/mc/game/version_manifest.json", 
                    BasePath + "version_manifest.json"
                );
                a = new VersionsList(File.ReadAllText($"{BasePath}/version_manifest.json"));
            }
        });
    }  
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        HomePage = new Home();
        versionPage = new version();
        accountPage = new account();
        downloadPage = new download(a);
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
