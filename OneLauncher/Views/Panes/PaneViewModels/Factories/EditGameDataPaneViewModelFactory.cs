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
    public EditGameDataPaneViewModelFactory(GameDataManager gameDataManager)
    {
        this.gameDataManager = gameDataManager;
    }
    public EditGameDataPaneViewModel Create(GameData gameData)
    {
        return new EditGameDataPaneViewModel(gameData,gameDataManager);
    }
}
