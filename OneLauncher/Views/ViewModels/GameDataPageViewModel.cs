using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.Mod.ModPack;
using OneLauncher.Views.Panes;
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.Panes.PaneViewModels.Factories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal class GameDataPageClosePaneControlMessage { public bool value = false;}
internal class GameDataPageDisplayListRefreshMessage { }
internal partial class GameDataItem : BaseViewModel
{
    public GameData data { get; set; }
    public bool IsDefault { get; }
    public Bitmap Icon { get; set; }
    public bool IsUseDebugModLaunch {  get; set; }
    [RelayCommand]
    public void Launch(GameData gameData)
    {
        _=version.EasyGameLauncher(gameData,IsUseDebugModLaunch);
    }
    public GameDataItem(GameData gameData,GameDataManager gameDataManager)
    {
        data = gameData;

        // 检查自己是否是其对应版本的默认实例
        var defaultInstance = gameDataManager.GetDefaultInstance(gameData.VersionId);
        IsDefault = (defaultInstance != null && defaultInstance.InstanceId == gameData.InstanceId);
        if (!string.IsNullOrEmpty(data.CustomIconPath) && File.Exists(data.CustomIconPath))
        {
            try { Icon = new Bitmap(data.CustomIconPath); return; }
            catch (Exception) { /* 忽略错误，使用默认图标 */ }
        }
        string iconUri = data.ModLoader switch
        {
            ModEnum.fabric => "avares://OneLauncher/Assets/Imgs/fabric.png",
            ModEnum.neoforge => "avares://OneLauncher/Assets/Imgs/neoforge.png",
            ModEnum.forge => "avares://OneLauncher/Assets/Imgs/forge.jpg",
            _ => "avares://OneLauncher/Assets/Imgs/basic.png", // 草方块
        };
        Icon = new Bitmap(AssetLoader.Open(new Uri(iconUri)));
    }
}
internal partial class GameDataPageViewModel : BaseViewModel
{
    private readonly GameDataManager _gameDataManager;
    private readonly NewGameDataPaneViewModel _newVM;
    private readonly EditGameDataPaneViewModelFactory _editVMFactory;
    private readonly PowerPlayPaneViewModelFactory _mctVMFactory;
    [ObservableProperty] public List<GameDataItem> gameDataList = new();
    [ObservableProperty] public string type;
    [ObservableProperty] public UserControl paneContent;
    [ObservableProperty] public bool isPaneShow;
    // 刷新列表
    public void RefList()
    {
        GameDataList = _gameDataManager.Data.Instances.Select(x => new GameDataItem(x.Value,_gameDataManager)).ToList();
    }
    /* 由PaneViewModel实现 */
    //// 修改特定的游戏数据实例
    //public void UpdateGameData(GameData updatedData)
    //{
    //    var index = Init.GameDataManger.AllGameData.FindIndex(gd => gd.InstanceId == updatedData.InstanceId);
    //    if (index != -1)
    //    {
    //        Init.GameDataManger.AllGameData[index] = updatedData;
    //    }
    //}
    public GameDataPageViewModel(
        GameDataManager gameDataManager,
        NewGameDataPaneViewModel NewVM,
        EditGameDataPaneViewModelFactory editGameDataPaneViewModelFactory,
        PowerPlayPaneViewModelFactory powerPlayPaneViewModelFactory
        )
    {
        this._mctVMFactory = powerPlayPaneViewModelFactory;
        this._editVMFactory = editGameDataPaneViewModelFactory;
        this._newVM = NewVM;
        this._gameDataManager = gameDataManager;
#if DEBUG
        // 造密码的Avalonia设计器天天报错
        // 设计时数据
        if (Design.IsDesignMode)
        {
            // 创建一个临时的、仅用于设计的假用户模型
            var designTimeUser = Guid.NewGuid();
            var gameData1 = new GameData(
                name: "纯净生存 (设计时)",
                versionId: "1.21",
                loader: ModEnum.none,
                userModel: designTimeUser
            );

            var gameData2 = new GameData(
                name: "Fabric 模组包 (设计时)",
                versionId: "1.20.4",
                loader: ModEnum.fabric,
                userModel: designTimeUser
            );

            // 将创建好的 GameData 包装成 GameDataItem 并添加到列表
            GameDataList = new List<GameDataItem>()
        {
            new GameDataItem(gameData1,_gameDataManager),
            new GameDataItem(gameData2,_gameDataManager)
        };
        }
        else
#endif
        {
            // 把配置文件的游戏数据列表显示到UI
            OnPageLoaded();
            // 提前初始化联机模块
            _connentServiceInitializationTasker = _mctVMFactory.CreateAsync();
            // 注册消息
            WeakReferenceMessenger.Default.Register<GameDataPageClosePaneControlMessage>(this,(re,message) => IsPaneShow = message.value);
            WeakReferenceMessenger.Default.Register<GameDataPageDisplayListRefreshMessage>(this, (re, message) => RefList());
        }
    }
    [RelayCommand]
    void OnPageLoaded()
    {
        try
        {
            RefList();
        }
        catch (NullReferenceException ex)
        {
            throw new OlanException(
                "无法初始化",
                "在游戏数据管理器页面读取配置文件时失败",
                OlanExceptionAction.FatalError,
                ex,
               () =>
               {
                   File.Delete(Path.Combine(Init.GameRootPath, "instance", "instance.json"));
                   Init.Initialize().Wait();
               }
                );
        }
    }
    [RelayCommand]
    public void NewGameData()
    {
        IsPaneShow = true;
        PaneContent = new NewGameDataPane()
        { DataContext =  _newVM};
    }
    [RelayCommand]
    public void ShowEditPane(GameData data)
    {
        IsPaneShow = true;
        PaneContent = new EditGameDataPane()
        { DataContext = _editVMFactory.Create(data) };
    }
    [RelayCommand]
    private async Task SetAsDefaultInstance(GameData targetData)
    {
        if (targetData == null) return;
        await _gameDataManager.SetDefaultInstanceAsync(targetData);
        RefList();
        WeakReferenceMessenger.Default.Send(
            new MainWindowShowFlyoutMessage($"已将 '{targetData.Name}' 设为版本 {targetData.VersionId} 的默认实例。"));
        //MainWindow.mainwindow.ShowFlyout($"已将 '{targetData.Name}' 设为版本 {targetData.VersionId} 的默认实例。");
    }
    [RelayCommand]
    public void Sorting(SortingType type)
    {
        List<GameDataItem> orderedList = type switch
        {
            SortingType.AnTime_OldFront => GameDataList.OrderBy(x => x.data.CreationTime).ToList(),
            SortingType.AnTime_NewFront => GameDataList.OrderByDescending(x => x.data.CreationTime).ToList(),
            SortingType.AnVersion_OldFront => GameDataList.OrderBy(x => new Version(x.data.VersionId)).ToList(),
            SortingType.AnVersion_NewFront => GameDataList.OrderByDescending(x => new Version(x.data.VersionId)).ToList(),
            _ => GameDataList // 默认不排序
        };

        GameDataList = orderedList;
        _gameDataManager.Data.Instances = GameDataList.ToDictionary(x => x.data.InstanceId,x => x.data);
        _=_gameDataManager.Save();
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
            //MainWindow.mainwindow.ShowFlyout("正在导入。。。（这可能需要较长时间）");
            await ModpackImporter.ImportFromMrpackAsync(filePath, Init.GameRootPath, CancellationToken.None);
            WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("导入完成！"));
            //MainWindow.mainwindow.ShowFlyout("导入完成！");
            RefList();
        }
    }
}
