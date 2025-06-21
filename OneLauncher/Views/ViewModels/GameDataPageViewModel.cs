using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;

internal partial class GameDataPageViewModel : BaseViewModel
{
    private List<GameData> gameDataList;
    public GameDataPageViewModel()
    {
        gameDataList = Init.GameDataManger.AllGameData;
    }
    [RelayCommand]
    public void Launch(GameData gameData)
    {
        version.EasyGameLauncher(gameData);
    }
}
