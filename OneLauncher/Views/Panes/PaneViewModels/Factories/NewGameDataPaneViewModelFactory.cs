using OneLauncher.Core.Global.ModelDataMangers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels.Factories;
internal class NewGameDataPaneViewModelFactory
{
    private readonly GameDataManager _gameDataManager;
    private readonly DBManager _dBManager;
    private readonly AccountManager _accountManager;
    public NewGameDataPaneViewModelFactory(GameDataManager gameDataManager, AccountManager accountManager,DBManager dBManager)
    {
        _dBManager = dBManager;
        _gameDataManager = gameDataManager;
        _accountManager = accountManager;
    }
    public NewGameDataPaneViewModel Create(Action onCloseCallback)
    {
        return new NewGameDataPaneViewModel(_dBManager, _accountManager,_gameDataManager,onCloseCallback);
    }
}
