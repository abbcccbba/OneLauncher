using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DynamicData;
using OneLauncher.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static OneLauncher.Core.StartArguments;
using OneLauncher.Codes;
using OneLauncher.Views;
using Avalonia.Threading;
namespace OneLauncher;
public partial class download : UserControl
{
    VersionsList a;
    bool IsDownload = false;
    public download()
    {
        InitializeComponent();
    }
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if(a != null )
        {
            VersionListViews.ItemsSource = a.GetAllVersionList();
            return;
        }
        if (!IsDownload)
        {
            await Core.Download.DownloadToMinecraft("https://piston-meta.mojang.com/mc/game/version_manifest.json", GAR.BasePath + "version_manifest.json");
            IsDownload = true;
        }
        a = new VersionsList(File.ReadAllText($"{GAR.BasePath}/version_manifest.json"));
        VersionListViews.ItemsSource = a.GetAllVersionList();
    }
    private async void DownloadButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        if (button.DataContext is VersionBasicInfo version)
        {
            var Dialog = new CVI();
            await Dialog.ShowDialog((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as MainWindow);
            if (Dialog.isOK)
            {
                GAR.configManger.AddVersion(new aVersion() { name = Dialog.GetReturnInfo(), versionBasicInfo = version });
                Task.Run(async () => ToDownload(version.url, version.name));
            }
        }
    }
    private async Task ToDownload(string VersionFileDownloadUrl,string name)
    {
        string VersionFilePath = $"{GAR.BasePath}.minecraft/versions/{name}/{name}.json";
        string GamePath = GAR.BasePath;
        await Core.Download.DownloadToMinecraft(VersionFileDownloadUrl, VersionFilePath);
        VersionInfomations a = new VersionInfomations(File.ReadAllText(VersionFilePath));
        await Core.Download.DownloadToMinecraft(a.GetLibrarys(GamePath), new Progress<double>(progressValue =>
        {
            //Debug.WriteLine($"下载进度{progressValue}%");
        }), 24, 24);
        await Core.Download.DownloadToMinecraft(a.GetMainFile(GamePath, name));
        var a1 = a.GetAssets(GamePath);
        await Core.Download.DownloadToMinecraft(a1);
        await Core.Download.DownloadToMinecraft(VersionAssetIndex.ParseAssetsIndex(File.ReadAllText(a1.path), GamePath), new Progress<double>(progressValue =>
        {
            //Debug.WriteLine($"下载进度{progressValue}%");
        }), 64, 32);
    }
    private void RadioButton_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.IsChecked == true)
        {
            if (radioButton == AllVersionsRadio)
            VersionListViews.ItemsSource = a.GetAllVersionList();
            
            else if (radioButton == LatestVersionRadio)
            VersionListViews.ItemsSource = a.GetLatestVersionList();
            
            else if (radioButton == StableVersionRadio)
            VersionListViews.ItemsSource = a.GetReleaseVersionList();
            
        }
    }
}