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

internal partial class NewGameDataPane : BaseViewModel
{
    //private readonly GameData? editingGameData; 
    //private readonly bool isEditing;

    //[ObservableProperty]
    //private string paneTitle;

    //[ObservableProperty]
    //private string gameDataName;

    //[ObservableProperty]
    //private List<UserVersion> availableVersions;

    //[ObservableProperty]
    //private UserVersion selectedVersion;

    //[ObservableProperty]
    //private bool isVersionSelectionEnabled;

    //// 构造函数 - 用于新建
    //public GameDataPaneViewModel()
    //{
    //    IsEditing = false;
    //    PaneTitle = "新建游戏数据";
    //    IsVersionSelectionEnabled = true;

    //    // 加载所有已安装的基础版本供选择
    //    AvailableVersions = Init.ConfigManger.config.VersionList;
    //}

    //// 构造函数 - 用于编辑
    //public GameDataPaneViewModel(GameDataPageViewModel parentViewModel, GameData gameDataToEdit)
    //{
    //    _parentViewModel = parentViewModel;
    //    _editingGameData = gameDataToEdit;
    //    _isEditing = true;
    //    PaneTitle = "编辑游戏数据";
    //    IsVersionSelectionEnabled = false; // 编辑模式下不允许更改基础版本

    //    GameDataName = gameDataToEdit.Name;
    //    AvailableVersions = Init.ConfigManger.config.VersionList;
    //    // 设置当前选中的版本
    //    SelectedVersion = AvailableVersions.FirstOrDefault(v => v.VersionID == gameDataToEdit.VersionId);
    //}

    //[RelayCommand]
    //private async Task Save()
    //{
    //    if (string.IsNullOrWhiteSpace(GameDataName))
    //    {
    //        MainWindow.mainwindow.ShowFlyout("游戏数据名称不能为空！", true);
    //        return;
    //    }

    //    if (!_isEditing && SelectedVersion == null)
    //    {
    //        MainWindow.mainwindow.ShowFlyout("请选择一个基础游戏版本！", true);
    //        return;
    //    }

    //    if (_isEditing)
    //    {
    //        // 编辑模式
    //        var data = _editingGameData.Value;
    //        data.Name = GameDataName;
    //        // 注意：我们不直接修改列表，而是通过GameDataManager来处理，但此处是直接修改对象属性
    //    }
    //    else
    //    {
    //        // 新建模式
    //        var newGameData = new GameData(
    //            name: GameDataName,
    //            versionId: SelectedVersion.VersionID,
    //            loader: SelectedVersion.preferencesLaunchMode.LaunchModType, // 继承所选版本的默认加载器
    //            userModel: Init.ConfigManger.config.DefaultUserModel
    //        );
    //        await Init.GameDataManger.AddGameDataAsync(newGameData);
    //    }

    //    // 保存所有更改到文件
    //    await Init.GameDataManger.SaveAsync();

    //    // 通知主页面刷新并关闭侧栏
    //    _parentViewModel.Refresh();
    //    _parentViewModel.IsPaneShow = false;
    //}

    //[RelayCommand]
    //private void Cancel()
    //{
    //    // 直接关闭侧栏
    //    _parentViewModel.IsPaneShow = false;
    //}
}
