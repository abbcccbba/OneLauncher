using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Codes;
using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper.ImportPCL2Version;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Mod.ModPack;
using OneLauncher.Views.Panes;
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.Panes.PaneViewModels.Factories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;

internal partial class HomePageViewModel : BaseViewModel
{
    private readonly GameDataManager _gameDataManager;
    private readonly DBManager _configManager;
    private readonly PowerPlayPaneViewModelFactory _mctVMFactory;
    [ObservableProperty] private List<GameData> launchItems;
    [ObservableProperty] private GameData? selectedGameData = null;
    [ObservableProperty] private bool isShowServerOption;

    [ObservableProperty] private bool isPaneShow;
    [ObservableProperty] private UserControl paneContent;
    public HomePageViewModel(
        GameDataManager gameDataManager,
        DBManager configManager,
        PowerPlayPaneViewModelFactory powerPlayPaneViewModelFactory
        )
    {
        this._configManager = configManager;
        this._gameDataManager = gameDataManager;
        this._mctVMFactory = powerPlayPaneViewModelFactory;
        LaunchItems = _gameDataManager.AllGameData;
        if (_configManager.Data.DefaultInstanceID != null)
            SelectedGameData = _gameDataManager.Data.Instances.GetValueOrDefault(_configManager.Data.DefaultInstanceID);
        //SelectedGameData = 
        //    _gameDataManager.Data.Instances
        //    .TryGetValue(_configManager.Data.DefaultInstanceID, out var gameData) ? gameData : null;
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        // 提前初始化联机模块
        _connentServiceInitializationTasker = _mctVMFactory.CreateAsync();
    }
    [RelayCommand]
    public void Launch()
    {
        if(SelectedGameData == null)
        {
            WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("请选择一个游戏数据实例！", Avalonia.Controls.Notifications.NotificationType.Warning));
            return;
        }
        _configManager.Data.DefaultInstanceID = SelectedGameData.InstanceId;
        _ = version.EasyGameLauncher(SelectedGameData);
    }
    [RelayCommand]
    public async Task ImportVersionByPCL2()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(MainWindow.mainwindow);
            if (topLevel?.StorageProvider is { } storageProvider && storageProvider.CanOpen)
            {
                var options = new FolderPickerOpenOptions
                {
                    Title = "选择你的PCL2版本文件夹",
                    AllowMultiple = false,
                };
                var files = await storageProvider.OpenFolderPickerAsync(options);
                var selectedFile = files.FirstOrDefault();

                if (files == null || !files.Any() || selectedFile == null)
                    return;

                string path = selectedFile.Path.LocalPath;
                if (!File.Exists(Path.Combine(path, "PCL", "Setup.ini")))
                {
                    WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("这不是有效的PCL版本文件夹", NotificationType.Warning, "导入失败"));
                    return;
                }
                WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("正在导入。。。（这可能需要较长时间）"));
                await new PCL2Importer(new Progress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)>(p =>
                {
                    // 避免安装器进度报告过于频繁
                    if (p.Title == DownProgress.DownAndInstModFiles)
                        return;
                    WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage(
                        $"[{p.DownedFiles}/{p.AllFiles}] 操作:{p.DowingFileName}",
                        NotificationType.Information,
                        p.Title switch
                        {
                            DownProgress.Meta => "正在分析PCL2实例",
                            DownProgress.Done => "导入完成",
                            _ => "操作中"
                        } + " - 正在导入"));
                    Debug.WriteLine($"Titli:{p.Title}\nAll:{p.AllFiles},Down:{p.DownedFiles}\nOutput:\n{p.DowingFileName}");
                })).ImportAsync(path);
                WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("导入完成！", NotificationType.Success));
            }
        }
        catch (OlanException ex)
        {
            await OlanExceptionWorker.ForOlanException(ex, () => _ = ImportVersionByPCL2());
        }
        catch (Exception ex)
        {
            await OlanExceptionWorker.ForUnknowException(ex, () => _ = ImportVersionByPCL2());
        }
    }
    private PowerPlayPane? _powerPlayGo;
    private Task<PowerPlayPaneViewModel> _connentServiceInitializationTasker;
    [RelayCommand]
    public async Task PowerPlay()
    {
        // 只有当第一次点击时才创建
        if (_powerPlayGo == null)
        {
            WeakReferenceMessenger.Default.Send(
                new MainWindowShowFlyoutMessage("正在加载联机模块，请稍后..."));

            try
            {
                // 外部创建好
                var viewModel = await _connentServiceInitializationTasker;
                _powerPlayGo = new PowerPlayPane { DataContext = viewModel };
            }
            catch (OlanException ex)
            {
                await OlanExceptionWorker.ForOlanException(ex);
                IsPaneShow = false; // 出错时隐藏Pane
                return;
            }
            catch (Exception ex)
            {
                await OlanExceptionWorker.ForUnknowException(ex);
                IsPaneShow = false; // 出错时隐藏Pane
                return;
            }
        }

        // 将已经创建好的、包含完整数据的Pane设置为内容并显示
        PaneContent = _powerPlayGo;
        IsPaneShow = true;
    }
    [RelayCommand]
    public async Task Import()
    {
        var topLevel = TopLevel.GetTopLevel(MainWindow.mainwindow);
        if (topLevel?.StorageProvider is { } storageProvider && storageProvider.CanOpen)
        {
            var mrpackFileType = new FilePickerFileType("Modrinth整合包文件")
            {
                Patterns = new[] { "*.mrpack" },
                MimeTypes = new[] { "application/mrpack" }
            };

            var options = new FilePickerOpenOptions
            {
                Title = "选择 Modrinth Pack 文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { mrpackFileType },
            };
            var files = await storageProvider.OpenFilePickerAsync(options);
            var selectedFile = files.FirstOrDefault();

            if (files == null || !files.Any() || selectedFile == null)
                return;

            string filePath = selectedFile.Path.LocalPath;
            WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("正在导入。。。（这可能需要较长时间）"));
            await ModpackImporter.ImportFromMrpackAsync(filePath,
                Init.GameRootPath,
                new Progress<(DownProgress a, int b, int c, string d)>
                (p =>
                {
                    Debug.WriteLine($"导入进度：{p.a}, 总文件数：{p.b}, 已下载文件数：{p.c}, 当前文件：{p.d}");
                })
                , CancellationToken.None);
            WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("导入完成！"));
        }
    }
}