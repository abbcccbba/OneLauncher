using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    [ObservableProperty] private GameData? selectedGameData;
    [ObservableProperty] private bool isShowServerOption;
    public HomePageViewModel(GameDataManager gameDataManager,DBManager configManager)
    {
        this._configManager = configManager;
        this._gameDataManager = gameDataManager;
        LaunchItems = _gameDataManager.AllGameData;
        SelectedGameData = _gameDataManager.Data.Instances.GetValueOrDefault(_configManager.Data.DefaultInstanceID);
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
            MainWindow.mainwindow.ShowFlyout("未指定默认实例！",true);
            return;
        }
        _ = version.EasyGameLauncher(SelectedGameData);
    }
}