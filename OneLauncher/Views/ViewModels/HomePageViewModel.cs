using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;

internal partial class HomePageViewModel : BaseViewModel
{
    private readonly GameDataManager _gameDataManager;
    private readonly DBManager _configManager;
    [ObservableProperty] private List<GameData> launchItems;
    [ObservableProperty] private GameData? selectedGameData = null;
    [ObservableProperty] private bool isShowServerOption;
    public HomePageViewModel(GameDataManager gameDataManager,DBManager configManager)
    {
        this._configManager = configManager;
        this._gameDataManager = gameDataManager;
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
    }
    [RelayCommand]
    public void Launch()
    {
        if(SelectedGameData == null)
        {
            WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("请选择一个游戏数据实例！", Avalonia.Controls.Notifications.NotificationType.Warning));
            //MainWindow.mainwindow.ShowFlyout("未指定默认实例！",true);
            return;
        }
        _ = version.EasyGameLauncher(SelectedGameData);
    }
}