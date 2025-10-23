using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels.Factories;

internal class DownloadPaneViewModelFactory
{
    private readonly DBManager _dbManager;
    private readonly GameDataManager _gameDataManager;

    public DownloadPaneViewModelFactory(
        
        DBManager dbManager,
        GameDataManager gameDataManager)
    {
        _dbManager = dbManager;
        _gameDataManager = gameDataManager;
    }

    public DownloadPaneViewModel Create(VersionBasicInfo version,Action onCloseCallback)
    {
        return new DownloadPaneViewModel(version, _dbManager,_gameDataManager,onCloseCallback);
    }
}
