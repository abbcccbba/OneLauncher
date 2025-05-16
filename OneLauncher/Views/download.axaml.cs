using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Views;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace OneLauncher.Views;

public partial class download : UserControl
{
    public download()
    {
        InitializeComponent();
        // 初始化数据
        Task.Run(async () =>
        {
            VersionsList vl;
            /*
             此方法中代码可能经过三个路径
            1、不存在清单文件，下载成功，读取
            2、不存在清单文件，下载失败，写入失败信息
            3、存在清单文件，读取
             */
            if (!File.Exists($"{Init.BasePath}/version_manifest.json"))
            {
                // 如果不存在版本清单则调用下载方法
                try
                {
                    // 路径（1）
                    await Core.Download.DownloadToMinecraft(
                        "https://piston-meta.mojang.com/mc/game/version_manifest.json",
                        Path.Combine(Init.BasePath, "version_manifest.json")
                    );
                    vl = new VersionsList(File.ReadAllText($"{Init.BasePath}/version_manifest.json"));
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    // 路径（2）
                    await Dispatcher.UIThread.InvokeAsync(() => this.DataContext = new Views.ViewModels.DownloadPageViewModel());
                    return;
                }
            }
            // 路径（3）
            vl = new VersionsList(File.ReadAllText($"{Init.BasePath}/version_manifest.json"));
            // 提前缓存避免UI线程循环卡顿
            List<VersionBasicInfo> allVersions = vl.GetAllVersionList();
            List<VersionBasicInfo> releaseVersions = vl.GetReleaseVersionList();
            List<VersionBasicInfo> snapshotVersions = vl.GetSnapshotVersionList();          
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.DataContext = new Views.ViewModels.DownloadPageViewModel
                (
                    allVersions,releaseVersions,snapshotVersions
                );
            });
        });  
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
    /*
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
await Core.Download.DownloadToMinecraft(a.GetLibrarys(GamePath), new Progress<(int downloadedFiles, int totalFiles, int verifiedFiles)>(progress
=> Dispatcher.UIThread.Post(() =>
{
  item.CurrentStage = progress.verifiedFiles > 0 ? "正在校验库文件..." : "正在下载库文件...";
  double percentage = (double)progress.downloadedFiles / progress.totalFiles * 100;
  item.DownloadProgress = percentage;
})), 24, 24,true);

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
await Core.Download.DownloadToMinecraft(VersionAssetIndex.ParseAssetsIndex(File.ReadAllText(a1.path), GamePath), new Progress<(int downloadedFiles, int totalFiles, int verifiedFiles)>(progress
=> Dispatcher.UIThread.Post(() =>
{
  item.CurrentStage = progress.verifiedFiles > 0 ? "正在校验资源文件..." : "正在下载资源文件...";
  double percentage = (double)progress.downloadedFiles / progress.totalFiles * 100;
  item.DownloadProgress = percentage;
})), 64, 32,true);

// 下载完成
Dispatcher.UIThread.Post(() =>
{
  item.CurrentStage = "下载完成";
  item.IsDownloading = false;
  item.IsExpanded = false; // 新增：关闭扩展区域
  item.ButtonText = "重新下载"; // 修改：完成时显示“重新下载”
  item.IsButtonEnabled = true; // 新增：启用按钮
});
} */
}