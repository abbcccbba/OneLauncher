using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Core.Mod.ModPack;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal partial class GameDataItem : BaseViewModel
{
    public GameData data { get; set; }
    public bool IsDefault { get; }
    public Bitmap Icon { get; set; }
    public bool IsUseDebugModLaunch {  get; set; }
    [RelayCommand]
    public void Launch(GameData gameData)
    {
        version.EasyGameLauncher(gameData,IsUseDebugModLaunch);
    }
    public GameDataItem(GameData gameData)
    {
        data = gameData;

        // 检查自己是否是其对应版本的默认实例
        var defaultInstance = Init.GameDataManger.GetDefaultInstance(gameData.VersionId);
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
    [ObservableProperty] public List<GameDataItem> gameDataList = new();
    [ObservableProperty] public string type;
    [ObservableProperty] public UserControl paneContent;
    [ObservableProperty] public bool isPaneShow;
    // 刷新列表
    public void RefList()
    {
        GameDataList = Init.GameDataManger.AllGameData.Select(x => new GameDataItem(x)).ToList();
    }
    // 修改特定的游戏数据实例
    public void UpdateGameData(GameData updatedData)
    {
        var index = Init.GameDataManger.AllGameData.FindIndex(gd => gd.InstanceId == updatedData.InstanceId);
        if (index != -1)
        {
            Init.GameDataManger.AllGameData[index] = updatedData;
        }
    }
    public GameDataPageViewModel()
    {
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
            new GameDataItem(gameData1),
            new GameDataItem(gameData2)
        };
        }
        else
#endif
        {
            // 把配置文件的游戏数据列表显示到UI
            GameDataList = Init.GameDataManger.AllGameData.Select(x => new GameDataItem(x)).ToList();
        }
    }
    [RelayCommand]
    public void NewGameData()
    {
        IsPaneShow = true;
        PaneContent = new NewGameDataPane();
    }
    [RelayCommand]
    public void ShowEditPane(GameData data)
    {
        IsPaneShow = true;
        PaneContent = new EditGameDataPane(data);
    }
    [RelayCommand]
    public void DeleteInstance(GameData data)
    {
        // 未来可以加一个对话框确认
        _=Init.GameDataManger.RemoveGameDataAsync(data);

        try
        {
            if (Directory.Exists(data.InstancePath))
                Directory.Delete(data.InstancePath, true);
        }
        catch (Exception ex)
        {
            MainWindow.mainwindow.ShowFlyout($"删除文件夹失败: {ex.Message}", true);
        }

        RefList(); // 重新加载列表
        MainWindow.mainwindow.ShowFlyout($"已删除: {data.Name}");
    }
    [RelayCommand]
    private async Task SetAsDefaultInstance(GameData targetData)
    {
        if (targetData == null) return;
        await Init.GameDataManger.SetDefaultInstanceAsync(targetData);
        RefList();
        MainWindow.mainwindow.ShowFlyout($"已将 '{targetData.Name}' 设为版本 {targetData.VersionId} 的默认实例。");
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
        Init.GameDataManger.AllGameData = GameDataList.Select(x => x.data).ToList();
        Init.ConfigManger.Save();
    }
    private PowerPlayPane powerPlayGo = new PowerPlayPane();
    [RelayCommand]
    public void PowerPlay()
    {
        IsPaneShow = true;
        PaneContent = powerPlayGo;
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
            MainWindow.mainwindow.ShowFlyout("正在导入。。。（这可能需要较长时间）");
            await ModpackImporter.ImportFromMrpackAsync(filePath, Init.GameRootPath, CancellationToken.None);
            MainWindow.mainwindow.ShowFlyout("导入完成！");
            RefList();
        }
    }
}
