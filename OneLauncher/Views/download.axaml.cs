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
        /*
         * 初始化Download页的版本列表
         * 
         * 使用fire-and-forget是因为用户不一定在启动瞬时打开页面，留有了富足的时间来等待完成
         * 因此这么做可以提前加载提高性能
         */
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
                    using (Download download = new Download())
                        await download.DownloadFile(
                            "https://piston-meta.mojang.com/mc/game/version_manifest.json",
                            Path.Combine(Init.BasePath, "version_manifest.json")
                        );
                    vl = new VersionsList(await File.ReadAllTextAsync($"{Init.BasePath}/version_manifest.json"));
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    // 路径（2）
                    await Dispatcher.UIThread.InvokeAsync(() => this.DataContext = new Views.ViewModels.DownloadPageViewModel());
                    return;
                }
            }
            // 路径（3）
            vl = new VersionsList(await File.ReadAllTextAsync(Path.Combine(Init.BasePath, "version_manifest.json")));
            // 提前缓存避免UI线程循环卡顿
            List<VersionBasicInfo> releaseVersions = vl.GetReleaseVersionList();        
            await Dispatcher.UIThread.InvokeAsync(() =>
            this.DataContext = new Views.ViewModels.DownloadPageViewModel(releaseVersions));
        });  
    }
}