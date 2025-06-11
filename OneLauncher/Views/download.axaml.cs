using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Views;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace OneLauncher.Views;

public partial class download : UserControl
{
    public download()
    {
        InitializeComponent();
        /*
         * 初始化Download页的版本列表
         * 
         * 使用fire-and-forget是因为用户不一定在启动瞬时打开页面，留有了富足的时间来等待完成
         * 因此这么做可以提前加载提高性能
         */
        VersionManifestReader();
    }
    public async Task VersionManifestReader()
    {
        VersionsList vl;
        /*
         此方法中代码可能经过三个路径
        1、不存在清单文件，下载成功，读取
        2、不存在清单文件，下载失败，写入失败信息
        3、存在清单文件，读取
         */
        if (!File.Exists(Path.Combine(Init.BasePath, "version_manifest.json")))
        {
            // 如果不存在版本清单则调用下载方法
            try
            {
                // 路径（1）
                using (Download download = new Download())
                    await download.DownloadFile(
                        "https://piston-meta.mojang.com/mc/game/version_manifest.json",
                        Path.Combine(Init.BasePath, "version_manifest.json")
                    );
                vl = new VersionsList(await File.ReadAllTextAsync(Path.Combine(Init.BasePath, "version_manifest.json")));
            }
            catch (HttpRequestException)
            {
                await OlanExceptionWorker.ForOlanException(
                    new OlanException("无法加载下载版本列表", "无法进行网络请求，且本地文件不存在", OlanExceptionAction.Error),
                    () => VersionManifestReader()
                        );
                return;
            }
        }
        // 路径（3）
        vl = new VersionsList(await File.ReadAllTextAsync(Path.Combine(Init.BasePath, "version_manifest.json")));
        // 提前缓存避免UI线程循环卡顿
        List<VersionBasicInfo> releaseVersions = vl.GetReleaseVersionList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        viewmodel = new Views.ViewModels.DownloadPageViewModel(releaseVersions));
        this.DataContext = viewmodel;
    }
    internal DownloadPageViewModel viewmodel;
}