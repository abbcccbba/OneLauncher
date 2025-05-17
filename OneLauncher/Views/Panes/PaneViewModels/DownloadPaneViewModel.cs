

using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Views.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace OneLauncher.Views.Panes.PaneViewModels;
internal partial class DownloadPaneViewModel : BaseViewModel
{
#if DEBUG
    // 供设计器预览
    public DownloadPaneViewModel()
    {
        VersionName = "1.21.5";
    }
#endif
    public DownloadPaneViewModel(VersionBasicInfo Version, DownloadPageViewModel downloadPane)
    {
        VersionName = Version.name;
        thisVersionBasicInfo = Version;
        this.downloadPage = downloadPane;
        // 无网络时拒绝下载
        //if (!Init.IsNetwork) 
        //{
        //    IsAllowDownloading = false;
        //    VersionName = "网络不可用";
        //}
    }
    private VersionBasicInfo thisVersionBasicInfo;
    DownloadPageViewModel downloadPage;
    [ObservableProperty]
    public string _VersionName;
    [ObservableProperty]
    public bool _IsAllowDownloading = true; // 默认允许下载

    [RelayCommand]
    public async void ToDownload()
    {
        // 新建线程避免阻塞UI
        await ToDownload(thisVersionBasicInfo);
        // 下载完毕后禁用下载按钮
        IsAllowDownloading = false;
        // 在配置文件中添加版本信息
        Init.ConfigManger.AddVersion(new aVersion
        {
            VersionID = VersionName,
            IsMod = false,
            AddTime = DateTime.Now
        });
    }
    [ObservableProperty]
    public string _D_DM = "下载未开始";
    [ObservableProperty]
    public double _CurrentProgress = 0;
    private async Task ToDownload(VersionBasicInfo versionBasicInfo)
    {
        string VersionJsonFol = Path.Combine(Init.BasePath,".minecraft","versions",versionBasicInfo.name);
        // 阶段1：下载版本文件（JSON）
        D_DM = "正在下载版本文件...";
        CurrentProgress = 0;
        await Core.Download.DownloadToMinecraft(versionBasicInfo.url, Path.Combine( VersionJsonFol, $"{versionBasicInfo.name}.json"));
        CurrentProgress = 100;
        
        // 阶段2：下载库文件
        VersionInfomations a = new VersionInfomations(File.ReadAllText(Path.Combine(VersionJsonFol, $"{versionBasicInfo.name}.json")),Init.BasePath,Init.systemType);
        D_DM = "正在下载库文件...";
        
        await Core.Download.DownloadToMinecraft(a.GetLibrarys(), new Progress<(int downloadedFiles, int totalFiles, int verifiedFiles)>(progress
        => //Dispatcher.UIThread.Post(() =>
        {
            D_DM = progress.verifiedFiles > 0 ? "正在校验库文件..." : "正在下载库文件...";
            double percentage = (double)progress.downloadedFiles / progress.totalFiles * 100;
            CurrentProgress = percentage;
        }), 24, Init.CPUPros*2, true);
        // 释放 Natives 库
        foreach (var i in a.NativesLibs)
        {
            Download.ExtractFile(Path.Combine(Init.BasePath,".minecraft", "libraries", i),Path.Combine(VersionJsonFol,"natives"));
        }

        // 阶段3：下载主文件
        D_DM = "正在下载主文件...";
        CurrentProgress = 0;
        await Core.Download.DownloadToMinecraft(a.GetMainFile());
        CurrentProgress = 100;

        // 阶段4：下载资源索引文件（JSON）
        D_DM = "正在下载资源索引文件...";
        CurrentProgress = 0;
        var a1 = a.GetAssets();
        await Core.Download.DownloadToMinecraft(a1);
        CurrentProgress = 100;

        // 阶段5：下载资源文件
        D_DM = "正在下载资源文件...";
        await Core.Download.DownloadToMinecraft(VersionAssetIndex.ParseAssetsIndex(File.ReadAllText(a1.path), Init.BasePath), new Progress<(int downloadedFiles, int totalFiles, int verifiedFiles)>(progress
        => //Dispatcher.UIThread.Post(() =>
        {
            D_DM = progress.verifiedFiles > 0 ? "正在校验资源文件..." : "正在下载资源文件...";
            double percentage = (double)progress.downloadedFiles / progress.totalFiles * 100;
            CurrentProgress = percentage;
        }), 64, Init.CPUPros*3, true);

        // 阶段6：下载日志配置文件
        D_DM = "正在下载日志配置文件...";
        CurrentProgress = 0;
        NdDowItem tmd = a.GetLoggingConfig();
        if (tmd != null)
            await Core.Download.DownloadToMinecraft(tmd);
        CurrentProgress = 100;

        // 下载完成
        D_DM = "下载完成";
    }
    [RelayCommand]
    public void ClosePane()
    {
        downloadPage.IsPaneShow = false;
    }
    [RelayCommand]
    public void CheckOnWeb()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://zh.minecraft.wiki/w/Java版{VersionName}",
            UseShellExecute = true
        });
    }
}