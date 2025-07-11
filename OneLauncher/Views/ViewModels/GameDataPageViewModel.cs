using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Codes;
using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Minecraft;
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
    public Bitmap Icon { get; set; }
    public bool IsDefault { get; set; }
    public bool IsUseDebugModLaunch {  get; set; }
    public bool IsMod => data.ModLoader != ModEnum.none;
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
        var customIconPath = Path.Combine(data.InstancePath, ".olc", "customicon");
        if (File.Exists(customIconPath))
        {
            try { Icon = new Bitmap(customIconPath); return; }
            catch (Exception) { /* 忽略错误，使用默认图标 */ }
        }
        string iconUri = data.ModLoader switch
        {
            ModEnum.fabric => "avares://OneLauncher/Assets/Imgs/fabric.png",
            ModEnum.quilt => "avares://OneLauncher/Assets/Imgs/quilt.png", 
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
    private readonly EditGameDataPaneViewModelFactory _editVMFactory;
    private readonly NewGameDataPaneViewModelFactory _newGameDataPaneViewModelFactory;
    [ObservableProperty] public List<GameDataItem> gameDataList = new();
    [ObservableProperty] public string type;
    [ObservableProperty] public UserControl paneContent;
    [ObservableProperty] public bool isPaneShow;
    // 刷新列表
    public void RefList()
    {
        GameDataList = _gameDataManager.Data.Instances.Select(x => new GameDataItem(x.Value, _gameDataManager)).ToList();
    }
    public GameDataPageViewModel(
        GameDataManager gameDataManager,
        EditGameDataPaneViewModelFactory editGameDataPaneViewModelFactory,
        NewGameDataPaneViewModelFactory newGameDataPaneViewModelFactory
        )
    {
        this._newGameDataPaneViewModelFactory = newGameDataPaneViewModelFactory;
        this._editVMFactory = editGameDataPaneViewModelFactory;
        this._gameDataManager = gameDataManager;
        AvailableTags = new ObservableCollection<GameDataTag>( _gameDataManager.Data.Tags.Values.ToList());
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
                   _=Init.Initialize();
               }
                );
        }
    }
    [RelayCommand]
    public void ModsManager(GameData data)
    {
        IsPaneShow = true;
        PaneContent = new GameDataModsManagerPane()
        { DataContext = new GameDataModsManagerPaneViewModel(data) };
    }
    [RelayCommand]
    public void NewGameData()
    {
        IsPaneShow = true;
        PaneContent = new NewGameDataPane()
        { DataContext =  _newGameDataPaneViewModelFactory.Create()};
    }
    [RelayCommand]
    public void ShowEditPane(GameData data)
    {
        IsPaneShow = true;
        PaneContent = new EditGameDataPane()
        { DataContext = _editVMFactory.Create(data) };
    }
    [RelayCommand]
    private async Task SetAsDefaultInstance(GameDataItem targetData)
    {
        //GameDataList.Select(x => x.IsDefault = false); // 先将所有实例的IsDefault设为false
        //targetData.IsDefault = true; // 将目标实例的IsDefault设为true
        await _gameDataManager.SetDefaultInstanceAsync(targetData.data);
        RefList();
        WeakReferenceMessenger.Default.Send(
            new MainWindowShowFlyoutMessage($"已将 '{targetData.data.Name}' 设为版本 {targetData.data.VersionId} 的默认实例。"));
    }
    #region 顶层按钮事件
    [ObservableProperty]
    private ObservableCollection<GameDataTag> availableTags;
    [ObservableProperty]
    private GameDataTag? selectedTag;
    [ObservableProperty]
    private string? newTagName = string.Empty;
    [RelayCommand]
    private async Task CreateNewTag()
    {
        if(string.IsNullOrWhiteSpace(NewTagName)) return;
        var newTagId = Guid.NewGuid();
        await _gameDataManager.CreateTag(new GameDataTag
        {
            Name = NewTagName,
            ID = newTagId
        },newTagId);
        NewTagName = string.Empty;
        AvailableTags = new ObservableCollection<GameDataTag>(_gameDataManager.Data.Tags.Values.ToList());
        WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage($"标签 '{NewTagName}' 已创建", NotificationType.Success));
    }
    partial void OnSelectedTagChanged(GameDataTag? value)
    {
        if (value == null) return;
        GameDataList =
                _gameDataManager.GetInstancesFromTag(value.ID)
                .Select(x => new GameDataItem(x,_gameDataManager))
                .ToList();
    }
    [RelayCommand]
    private void ResetFilter()
    {
        SelectedTag = null; // 里面有空值检查所以这里手动刷新
        RefList();
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
    #endregion
}
