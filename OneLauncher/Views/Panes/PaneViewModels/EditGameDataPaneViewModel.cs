using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Views.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;

internal partial class EditGameDataPaneViewModel : BaseViewModel
{
    private GameData editingGameData;
    private GameDataPageViewModel parentViewModel = MainWindow.mainwindow.gamedataPage.viewmodel;
    [ObservableProperty] private string instanceName;
    [ObservableProperty] private Bitmap currentIcon;

    public EditGameDataPaneViewModel(GameData gameData)
    {
        editingGameData = gameData;
        InstanceName = gameData.Name;
        LoadCurrentIcon();
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

    [RelayCommand]
    private async Task ChangeIcon()
    {
        var topLevel = TopLevel.GetTopLevel(MainWindow.mainwindow);
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

        // 把文件复制过去
        await using var sourceStream = await selectedFile.OpenReadAsync();
        await using var destStream = File.Create(destPath);
        await sourceStream.CopyToAsync(destStream);

        editingGameData.CustomIconPath = destPath;
        CurrentIcon = new Bitmap(AssetLoader.Open(new Uri(destPath))); // 显示一个预览
        parentViewModel.UpdateGameData(editingGameData);

        LoadCurrentIcon();
        MainWindow.mainwindow.ShowFlyout("图标已更新！");
    }

    [RelayCommand]
    private void OpenModsFolder()
    {
        string modsPath = Path.Combine(editingGameData.InstancePath, "mods");
        Tools.OpenFolder(modsPath);
    }

    [RelayCommand]
    private async Task Save()
    {
        editingGameData.Name = InstanceName;
        parentViewModel.UpdateGameData(editingGameData);
        await Init.GameDataManger.SaveAsync();

        parentViewModel.RefList();
        parentViewModel.IsPaneShow = false;
        MainWindow.mainwindow.ShowFlyout("保存成功！");
    }

    [RelayCommand]
    private void Cancel()
    {
        parentViewModel.IsPaneShow = false;
    }
}