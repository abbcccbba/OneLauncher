using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;

internal partial class HomePageViewModel : BaseViewModel
{
    [ObservableProperty] private List<GameData> launchItems = Init.GameDataManger.AllGameData;
    [ObservableProperty] private GameData selectedGameData = 
        Init.GameDataManger.AllGameData.FirstOrDefault(x => x.InstanceId == Init.ConfigManger.config.DefaultInstanceID);
    public HomePageViewModel()
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        // 确保在访问 Init 之前，它的初始化任务已经完成
        var initResult = Init.InitTask.Result;
        if (initResult != null)
            throw initResult;
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