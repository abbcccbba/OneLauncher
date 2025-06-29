using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;

internal partial class EditGameDataPaneViewModel : BaseViewModel
{
    private readonly GameDataManager _gameDataManager;
    private readonly GameData editingGameData;
    [ObservableProperty] private string instanceName;
    [ObservableProperty] private Bitmap currentIcon;

    public EditGameDataPaneViewModel(GameData gameData,GameDataManager gameDataManager)
    {
        this._gameDataManager = gameDataManager;
        editingGameData = gameData;
        InstanceName = gameData.Name;
        LoadCurrentIcon();
    }

    private void UpdateGameData()
    {
        try
        {
            _gameDataManager.Data.Instances[editingGameData.InstanceId] = editingGameData;
        }
        catch(KeyNotFoundException e)
        {
            throw new OlanException(
                "无法更新游戏数据",
                "尝试更新一个不存在的游戏数据实例",
                OlanExceptionAction.Error, e);
        }
    }
    private void LoadCurrentIcon()
    {
        if (!string.IsNullOrEmpty(editingGameData.CustomIconPath) && File.Exists(editingGameData.CustomIconPath))
        {
            try { CurrentIcon = new Bitmap(editingGameData.CustomIconPath); return; }
            catch (Exception) { /* 忽略错误，使用默认图标 */ }
        }

        string iconUri = editingGameData.ModLoader switch
        {
            ModEnum.fabric => "avares://OneLauncher/Assets/Imgs/fabric.png",
            ModEnum.neoforge => "avares://OneLauncher/Assets/Imgs/neoforge.png",
            ModEnum.forge => "avares://OneLauncher/Assets/Imgs/forge.jpg",
            _ => "avares://OneLauncher/Assets/Imgs/basic.png",
        };
        CurrentIcon = new Bitmap(AssetLoader.Open(new Uri(iconUri)));
    }
    private void DeleteGameData()
    {
        // 未来可以加一个对话框确认
        _ = _gameDataManager.RemoveGameDataAsync(editingGameData);

        try
        {
            if (Directory.Exists(editingGameData.InstancePath))
                Directory.Delete(editingGameData.InstancePath, true);
        }
        catch (Exception ex)
        {
            MainWindow.mainwindow.ShowFlyout($"删除文件夹失败: {ex.Message}", true);
        }

        WeakReferenceMessenger.Default.Send(
            new GameDataPageDisplayListRefreshMessage()); // 重新加载列表
        WeakReferenceMessenger.Default.Send(
            new MainWindowShowFlyoutMessage($"已删除实例“{editingGameData.Name}”！"));
    }

    [RelayCommand]
    private async Task ChangeIcon()
    {
        var topLevel = TopLevel.GetTopLevel(MainWindow.mainwindow);
        if (topLevel?.StorageProvider is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择一个新的图标",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImagePng }
        });

        var selectedFile = files?.FirstOrDefault();
        if (selectedFile == null) return;

        string imageDir = Path.Combine(editingGameData.InstancePath, "image");
        Directory.CreateDirectory(imageDir);
        string destPath = Path.Combine(imageDir, $"{editingGameData.InstanceId}.png");

        { // 确保文件流被释放
            await using var sourceStream = await selectedFile.OpenReadAsync();
            await using var destStream = File.Create(destPath);
            await sourceStream.CopyToAsync(destStream);
        }

        editingGameData.CustomIconPath = destPath;
        CurrentIcon = new Bitmap(destPath); // 显示预览
        UpdateGameData();

        WeakReferenceMessenger.Default.Send(
            new GameDataPageDisplayListRefreshMessage());

        WeakReferenceMessenger.Default.Send(
            new MainWindowShowFlyoutMessage($"已更改实例“{editingGameData.Name}”的图标！"));
    }

    [RelayCommand]
    private void OpenInstanceFolder()
    {
        string modsPath = Path.Combine(editingGameData.InstancePath);
        Tools.OpenFolder(modsPath);
    }
    [RelayCommand]
    private void AddToQuicklyPlay()
    {
        File.WriteAllTextAsync(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),$"快速启动 - {editingGameData.Name}"+
            #if WINDOWS
            ".bat"
            #else
            ".sh"
            #endif
            ), 
            $"{Environment.ProcessPath} --quicklyPlay {editingGameData.InstanceId}"
            );
    }
    [RelayCommand]
    private void Save()
    {
        editingGameData.Name = InstanceName;
        //parentViewModel.UpdateGameData(editingGameData);
        _gameDataManager.Data.Instances.GetValueOrDefault(editingGameData.InstanceId);
        _=_gameDataManager.Save();

        WeakReferenceMessenger.Default.Send(new GameDataPageDisplayListRefreshMessage());
        WeakReferenceMessenger.Default.Send(new GameDataPageClosePaneControlMessage());
        WeakReferenceMessenger.Default.Send(
            new MainWindowShowFlyoutMessage($"实例“{InstanceName}”已保存"));
    }
    [RelayCommand]
    private void DeleteInstance()
    {
        DeleteGameData();
        WeakReferenceMessenger.Default.Send(new GameDataPageClosePaneControlMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        WeakReferenceMessenger.Default.Send(new GameDataPageClosePaneControlMessage());
    }
}