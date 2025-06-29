using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels.Factories;

internal class DownloadPaneViewModelFactory
{
    //private readonly AccountManager _accountManager;
    private readonly DBManager _dbManager;
    private readonly GameDataManager _gameDataManager;

    // 工厂的构造函数：让DI容器把工具都送进来
    public DownloadPaneViewModelFactory(
        
        DBManager dbManager,
        GameDataManager gameDataManager)
    {
        //_accountManager = accountManager;
        _dbManager = dbManager;
        _gameDataManager = gameDataManager;
    }

    // 实现接口的Create方法：接收“特殊材料”，开始生产！
    public DownloadPaneViewModel Create(VersionBasicInfo version)
    {
        // 在这里，你终于可以把两边的参数完美地结合在一起了！
        return new DownloadPaneViewModel(version, _dbManager,_gameDataManager);
    }
}
