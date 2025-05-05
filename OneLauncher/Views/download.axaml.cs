using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Views;
using System.Threading.Tasks;
using System;
using System.IO;
using OneLauncher;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneLauncher.Views;

public partial class download : UserControl
{
    private ObservableCollection<DownloadItem> downloadItem;
    

    private List<DownloadItem> _allVersionsCache;
    private List<DownloadItem> _latestVersionsCache;
    private List<DownloadItem> _releaseVersionsCache;

    public download()
    {
        InitializeComponent();
        // 初始化数据
        Task.Run(async () =>
        {
            VersionsList versionsList;
            try
            {
                versionsList = new VersionsList(File.ReadAllText($"{Init.BasePath}/version_manifest.json"));
            }
            catch (FileNotFoundException)
            {
                // 在后台线程下载
                await Core.Download.DownloadToMinecraft(
                    "https://piston-meta.mojang.com/mc/game/version_manifest.json",
                    Init.BasePath + "version_manifest.json"
                );
                versionsList = new VersionsList(File.ReadAllText($"{Init.BasePath}/version_manifest.json"));
            }

            var allVersions = versionsList.GetAllVersionList()
                .Select(v => new DownloadItem { vbi = v })
                .ToList();
            var latestVersions = versionsList.GetLatestVersionList()
                .Select(v => new DownloadItem { vbi = v })
                .ToList();
            var releaseVersions = versionsList.GetReleaseVersionList()
                .Select(v => new DownloadItem { vbi = v })
                .ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _allVersionsCache = allVersions;
                _latestVersionsCache = latestVersions;
                _releaseVersionsCache = releaseVersions;
                downloadItem = new ObservableCollection<DownloadItem>(_allVersionsCache);
                VersionListViews.ItemsSource = downloadItem;
            });
        });
    }


    private async void DownloadButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is DownloadItem item)
        {
            if (!item.IsExpanded)
            {
                item.IsExpanded = true;
                item.ButtonText = "继续";
                return;
            }

            // 点击“继续”，检查版本名称并开始下载
            if (string.IsNullOrWhiteSpace(item.VersionName))
            {
                // 可添加错误提示
                return;
            }

            // 隐藏扩展区域，显示下载框和进度条
            item.IsExpanded = false;
            item.IsDownloading = true;
            item.ButtonText = "下载中...";
            item.IsButtonEnabled = false;

            // 写入配置文件
            Init.ConfigManger.AddVersion(new aVersion { name = item.VersionName, versionBasicInfo = item.vbi });

            // 调用下载方法
            Task.Run(() => ToDownload(item.vbi.url, item.vbi.name, item));
        }
    }

    private async Task ToDownload(string VersionFileDownloadUrl, string name, DownloadItem item)
    {
        string VersionFilePath = $"{Init.BasePath}.minecraft/versions/{name}/{name}.json";
        string GamePath = Init.BasePath;

        // 阶段1：下载版本文件（JSON）
        item.CurrentStage = "正在下载版本文件...";
        item.DownloadProgress = 0;
        await Core.Download.DownloadToMinecraft(VersionFileDownloadUrl, VersionFilePath);
        item.DownloadProgress = 100;

        // 阶段2：下载库文件
        VersionInfomations a = new VersionInfomations(File.ReadAllText(VersionFilePath));
        item.CurrentStage = "正在下载库文件...";
        await Core.Download.DownloadToMinecraft(a.GetLibrarys(GamePath), new Progress<(int downloadedFiles, int totalFiles, int verifiedFiles)>(progress =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                item.CurrentStage = progress.verifiedFiles > 0 ? "正在校验库文件..." : "正在下载库文件...";
                double percentage = (double)progress.downloadedFiles / progress.totalFiles * 100;
                item.DownloadProgress = percentage;
            });
        }), 24, 24);

        // 阶段3：下载主文件
        item.CurrentStage = "正在下载主文件...";
        item.DownloadProgress = 0;
        await Core.Download.DownloadToMinecraft(a.GetMainFile(GamePath, name));
        item.DownloadProgress = 100;

        // 阶段4：下载资源索引文件（JSON）
        item.CurrentStage = "正在下载资源索引文件...";
        item.DownloadProgress = 0;
        var a1 = a.GetAssets(GamePath);
        await Core.Download.DownloadToMinecraft(a1);
        item.DownloadProgress = 100;

        // 阶段5：下载资源文件
        item.CurrentStage = "正在下载资源文件...";
        await Core.Download.DownloadToMinecraft(VersionAssetIndex.ParseAssetsIndex(File.ReadAllText(a1.path), GamePath), new Progress<(int downloadedFiles, int totalFiles, int verifiedFiles)>(progress =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                item.CurrentStage = progress.verifiedFiles > 0 ? "正在校验资源文件..." : "正在下载资源文件...";
                double percentage = (double)progress.downloadedFiles / progress.totalFiles * 100;
                item.DownloadProgress = percentage;
            });
        }), 64, 32);

        // 下载完成
        Dispatcher.UIThread.Post(() =>
        {
            item.CurrentStage = "下载完成";
            item.IsDownloading = false;
            item.IsExpanded = false; // 新增：关闭扩展区域
            item.ButtonText = "重新下载"; // 修改：完成时显示“重新下载”
            item.IsButtonEnabled = true; // 新增：启用按钮
        });
    }

    private void RadioButton_Checked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.IsChecked == true)
        {
            List<DownloadItem> versionsToShow = radioButton switch
            {
                _ when radioButton == AllVersionsRadio => _allVersionsCache,
                _ when radioButton == LatestVersionRadio => _latestVersionsCache,
                _ when radioButton == ReleaseVersionRadio => _releaseVersionsCache,
                _ => null
            };

            if (versionsToShow != null)
            {
                downloadItem.Clear();
                foreach (var item in versionsToShow)
                {
                    downloadItem.Add(item);
                }
            }
        }
    }
}

public class DownloadItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private bool _isExpanded;
    private bool _isDownloading;
    private bool _isButtonEnabled = true;
    private double _downloadProgress;
    private string _versionName = string.Empty;
    private string _buttonText = "下载";
    private string _currentStage = string.Empty;

    public VersionBasicInfo vbi { get; set; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        set { _isDownloading = value; OnPropertyChanged(nameof(IsDownloading)); }
    }

    public bool IsButtonEnabled
    {
        get => _isButtonEnabled;
        set { _isButtonEnabled = value; OnPropertyChanged(nameof(IsButtonEnabled)); }
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        set { _downloadProgress = value; OnPropertyChanged(nameof(DownloadProgress)); }
    }

    public string VersionName
    {
        get => _versionName;
        set { _versionName = value; OnPropertyChanged(nameof(VersionName)); }
    }

    public string ButtonText
    {
        get => _buttonText;
        set { _buttonText = value; OnPropertyChanged(nameof(ButtonText)); }
    }

    public string CurrentStage
    {
        get => _currentStage;
        set { _currentStage = value; OnPropertyChanged(nameof(CurrentStage)); }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}