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
    [ObservableProperty]
    private GameDataItem? primaryGameInstance;

    [ObservableProperty]
    private ObservableCollection<GameDataItem> quickLaunchItems;

    public HomePageViewModel()
    {
        // 确保在访问 Init 之前，它的初始化任务已经完成
        var initResult = Init.InitTask.Result;
        if (initResult != null)
        {
            // 如果初始化失败，则不加载数据，并报告错误
            OlanExceptionWorker.ForOlanException(initResult);
            quickLaunchItems = new ObservableCollection<GameDataItem>();
            return;
        }

        LoadGameData();
    }

    private void LoadGameData()
    {
        //// 从 GameDataManager 加载所有实例，并包装成 GameDataItem
        //var allGameDataItems = Init.GameDataManger.AllGameData
        //                           .Select(x => new GameDataItem(x))
        //                           .OrderByDescending(x => x.data.CreationTime) // 按创建时间倒序排序
        //                           .ToList();

        //if (allGameDataItems.Any())
        //{
        //    // 默认选择最新创建的或用户上次游玩的实例作为主实例
        //    // (这里简化为选择最新的)
        //    PrimaryGameInstance = allGameDataItems.First();

        //    // 将所有实例加载到快捷启动列表
        //    QuickLaunchItems = new ObservableCollection<GameDataItem>(allGameDataItems);
        //}
        //else
        //{
        //    // 处理没有游戏实例的情况
        //    PrimaryGameInstance = null;
        //    QuickLaunchItems = new ObservableCollection<GameDataItem>();
        //}
    }

    [RelayCommand]
    private async Task LaunchPrimaryInstance()
    {
        //if (PrimaryGameInstance == null)
        //{
        //    MainWindow.mainwindow.ShowFlyout("没有可启动的游戏实例！", true);
        //    // 引导用户去下载或创建一个
        //    MainWindow.mainwindow.MainPageControl(MainWindow.MainPage.GameDataPage);
        //    return;
        //}

        // 使用已经非常完善的 EasyGameLauncher
        //await version.EasyGameLauncher(PrimaryGameInstance.data, PrimaryGameInstance.IsUseDebugModLaunch);
    }

    [RelayCommand]
    private void SelectItemAsPrimary(GameDataItem? item)
    {
        if (item != null)
        {
            //PrimaryGameInstance = item;
        }
    }

    // 可选: 提供一个刷新命令，当其他页面修改了GameData后可以调用
    [RelayCommand]
    public void RefreshData()
    {
        LoadGameData();
    }
}