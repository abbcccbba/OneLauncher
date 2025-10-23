using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels.Factories;

internal class EditGameDataPaneViewModelFactory
{
    private readonly GameDataManager gameDataManager;
    private readonly AccountManager accountManager;
    public EditGameDataPaneViewModelFactory(GameDataManager gameDataManager,AccountManager accountManager)
    {
        this.gameDataManager = gameDataManager;
        this.accountManager = accountManager;
    }
    public EditGameDataPaneViewModel Create(GameData gameData,Action onCloseCallback)
    {
        return new EditGameDataPaneViewModel(gameData,gameDataManager,accountManager,onCloseCallback);
    }
}
