using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Net.Account.Microsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels.Factories;

internal class UserModelLoginPaneViewModelFactory
{
    private readonly MsalAuthenticator _msaManager;
    private readonly AccountManager _accountManager;
    public UserModelLoginPaneViewModelFactory(MsalAuthenticator msaManager, AccountManager accountManager)
    {
        _msaManager = msaManager;
        _accountManager = accountManager;
    }
    public UserModelLoginPaneViewModel Create(Action onCloseCallback)
    {
        return new UserModelLoginPaneViewModel(_msaManager, _accountManager,onCloseCallback);
    }
}
