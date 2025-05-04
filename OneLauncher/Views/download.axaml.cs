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
using OneLauncher.Views;
using Avalonia.Threading;
using System.Linq;
using OneLauncher.Codes;
namespace OneLauncher;
public partial class download : UserControl
{
    VersionsList a;
    //bool IsDownload = false;
    public download()
    {
        InitializeComponent();
        this.a = Codes.Init.Versions;
        VersionListViews.ItemsSource = a.GetAllVersionList();
    }
    private async void DownloadButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        if (button.DataContext is VersionBasicInfo version)
        {
            var Dialog = new MessageShow("输入新版本名称");
            await Dialog.ShowDialog(MainWindow.mainwindow);
            Init.ConfigManger.AddVersion(new aVersion() { name = Dialog.needsp, versionBasicInfo = version });
            Task.Run(async () => ToDownload(version.url, version.name));
            
        }
    }
    private async Task ToDownload(string VersionFileDownloadUrl,string name)
    {
        string VersionFilePath = $"{Init.BasePath}.minecraft/versions/{name}/{name}.json";
        string GamePath = Init.BasePath;
        //var ms = new MessageShow();
        //ms.Show();
        await Core.Download.DownloadToMinecraft(VersionFileDownloadUrl, VersionFilePath);
        VersionInfomations a = new VersionInfomations(File.ReadAllText(VersionFilePath));
        await Download.DownloadToMinecraft(a.GetLibrarys(GamePath), new Progress<(int downloadedFiles, int totalFiles, int verifiedFiles)>(progress =>
        {
            Debug.WriteLine($"已下载: {progress.downloadedFiles}/{progress.totalFiles}, 已校验: {progress.verifiedFiles}/{progress.totalFiles}");
            //ms.st(progress.downloadedFiles.ToString());
        }),24, 24);

        await Core.Download.DownloadToMinecraft(a.GetMainFile(GamePath, name));
        var a1 = a.GetAssets(GamePath);
        await Core.Download.DownloadToMinecraft(a1);
        await Download.DownloadToMinecraft(VersionAssetIndex.ParseAssetsIndex(File.ReadAllText(a1.path), GamePath), new Progress<(int downloadedFiles, int totalFiles, int verifiedFiles)>(progress =>
        {
            Debug.WriteLine($"已下载: {progress.downloadedFiles}/{progress.totalFiles}, 已校验: {progress.verifiedFiles}/{progress.totalFiles}");
        }),64, 32);
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