using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal partial class GameDataItem : BaseViewModel
{
    public GameDataItem(GameData gameData)
    {
        data = gameData;
    }
    public GameData data { get; set; }
    string Type { get; set; }
}
internal partial class GameDataPageViewModel : BaseViewModel
{
    [ObservableProperty] public List<GameDataItem> gameDataList = new();
    [ObservableProperty] public string type;
    public GameDataPageViewModel()
    {
        
#if DEBUG
        if (Design.IsDesignMode)
        {
            Init.GameRootPath = "AVALONIA";
            GameDataList.Add(new GameDataItem(new GameData()
            {
                CreationTime = DateTime.Now,
                InstanceId = "1.21.5",
                Name = "设计时游戏数据-1"
            }));
            GameDataList.Add(new GameDataItem(new GameData()
            {
                CreationTime = DateTime.Now.AddDays(-5),
                InstanceId = "1.20.4",
                Name = "设计时游戏数据-2"
            }));
        }
        else
#endif
            GameDataList = Init.GameDataManger.AllGameData.Select(x => new GameDataItem(x)).ToList();
    }
    [RelayCommand]
    public void Launch(GameData gameData)
    {
        version.EasyGameLauncher(gameData);
    }
}
