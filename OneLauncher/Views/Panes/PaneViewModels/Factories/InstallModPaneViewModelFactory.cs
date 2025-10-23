using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels.Factories;

internal class InstallModPaneViewModelFactory
{
    private readonly GameDataManager _gameDataManager;
    public InstallModPaneViewModelFactory(GameDataManager gameDataManager)
    {
        _gameDataManager = gameDataManager;
    }
    public InstallModPaneViewModel Create(ModItem item,Action onCloseCallback)
    {
        return new InstallModPaneViewModel(item, _gameDataManager,onCloseCallback);
    }
}
