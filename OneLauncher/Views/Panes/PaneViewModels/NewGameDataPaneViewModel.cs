using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;

internal partial class NewGameDataPaneViewModel : BaseViewModel
{
    // --- 可绑定属性 ---

    [ObservableProperty]
    private string gameDataName;

    [ObservableProperty]
    private List<UserVersion> availableBaseVersions;

    [ObservableProperty]
    private UserVersion selectedBaseVersion;

    [ObservableProperty]
    private List<ModEnum> availableModLoaders;

    [ObservableProperty]
    private ModEnum selectedModLoader;

    [ObservableProperty]
    private List<UserModel> availableUsers;

    [ObservableProperty]
    private UserModel selectedUser;

    // --- 构造函数 ---

    public NewGameDataPaneViewModel()
    {
        // 从全局静态类 Init 加载所需数据
        AvailableBaseVersions = Init.ConfigManger.config.VersionList;
        AvailableUsers = Init.ConfigManger.config.UserModelList;
        SelectedUser = Init.ConfigManger.config.DefaultUserModel ?? AvailableUsers.FirstOrDefault();

        // 默认情况下，模组加载器列表为空，等待用户选择基础版本
        AvailableModLoaders = new List<ModEnum> { ModEnum.none };
        SelectedModLoader = ModEnum.none;
    }

    // --- 属性变化响应 ---

    // 当用户选择了不同的“基础版本”时，这个方法会被自动调用
    partial void OnSelectedBaseVersionChanged(UserVersion value)
    {
        if (value == null)
        {
            // 如果清空选择，则重置加载器列表
            AvailableModLoaders = new List<ModEnum> { ModEnum.none };
            SelectedModLoader = ModEnum.none;
            GameDataName = string.Empty;
            return;
        }

        // 根据所选版本支持的模组加载器，动态更新加载器下拉列表
        var loaders = new List<ModEnum> { ModEnum.none };
        if (value.modType.IsFabric) loaders.Add(ModEnum.fabric);
        if (value.modType.IsNeoForge) loaders.Add(ModEnum.neoforge);
        if (value.modType.IsForge) loaders.Add(ModEnum.forge);
        AvailableModLoaders = loaders;

        // 自动选择一个默认加载器（或保持“无”）
        SelectedModLoader = value.preferencesLaunchMode.LaunchModType;
        // 自动生成一个建议的实例名称
        GameDataName = $"{value.VersionID} - Instance";
    }

    [RelayCommand]
    private async Task Save()
    {
        // 数据验证
        if (SelectedBaseVersion == null)
        {
            MainWindow.mainwindow.ShowFlyout("请先选择一个基础游戏版本！", true);
            return;
        }
        if (string.IsNullOrWhiteSpace(GameDataName))
        {
            MainWindow.mainwindow.ShowFlyout("游戏数据名称不能为空！", true);
            return;
        }

        // 创建新的 GameData 对象
        var newGameData = new GameData(
            name: GameDataName,
            versionId: SelectedBaseVersion.VersionID,
            loader: SelectedModLoader,
            userModel: SelectedUser
        );

        // 使用 GameDataManager 添加并保存到 instance.json
        await Init.GameDataManger.AddGameDataAsync(newGameData);
        MainWindow.mainwindow.ShowFlyout($"已成功创建游戏数据: {GameDataName}");

        MainWindow.mainwindow.gamedataPage.viewmodel.RefList();

        Cancel(); 
    }

    [RelayCommand]
    private void Cancel()
    {
        MainWindow.mainwindow.gamedataPage.viewmodel.IsPaneShow = false;
    }
}