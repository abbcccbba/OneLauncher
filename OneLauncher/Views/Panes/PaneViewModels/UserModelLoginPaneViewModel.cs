using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Net.msa;
using OneLauncher.Views.ViewModels;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;

internal partial class UserModelLoginPaneViewModel : BaseViewModel
{
#if DEBUG
    public UserModelLoginPaneViewModel() { }
#endif
    private AccountPageViewModel accountPageViewModel;
    public UserModelLoginPaneViewModel(AccountPageViewModel accountPageViewModel)
    {
        this.accountPageViewModel = accountPageViewModel;
    }
    [ObservableProperty]
    public string _UserName;
    [RelayCommand]
    public void Done()
    {
        if (string.IsNullOrEmpty(UserName)) return;
        if (!Regex.IsMatch(UserName, @"^[a-zA-Z0-9_]+$"))
        {
            MainWindow.mainwindow.ShowFlyout("用户名包含非法字符！", true);
            return;
        }
        Init.AccountManager.AddUser(new UserModel(
            UserID : Guid.NewGuid(),
            name : UserName,
            uuid : Guid.NewGuid()
        ));
        accountPageViewModel.RefList();
        accountPageViewModel.IsPaneShow = false;
        MainWindow.mainwindow.ShowFlyout($"已新建用户:{UserName}");
    }
    [RelayCommand]
    public void Back()
    {
        accountPageViewModel.IsPaneShow = false;
        MainWindow.mainwindow.ShowFlyout("已暂存修改！");
    }

    [ObservableProperty]
    public bool _IsYaLogin = true;
    [ObservableProperty]
    public bool _IsMsaLogin = false;
    private ListBoxItem _whiceLoginType;
    public ListBoxItem WhiceLoginType
    {
        set
        {
            _whiceLoginType = value;
            if (value.Content == "离线登入")
            {
                Debug.WriteLine("离线登入");
                IsYaLogin = true;
                IsMsaLogin = false;
            }
            if (value.Content == "微软登入")
            {
                Debug.WriteLine("微软登入");
                IsYaLogin = false;
                IsMsaLogin = true;
            }
        }
        get
        {
            return _whiceLoginType;
        }
    }
    [RelayCommand]
    public async Task LoginWithMicrosoft()
    {
        try
        {
            UserModel um;
#if WINDOWS
            if (Init.SystemType == SystemType.windows)
            {
                um = 
                    await Init.MMA.LoginNewAccountToGetMinecraftMojangAccessTokenUseWindowsWebAccountManger(
                        (MainWindow.mainwindow.TryGetPlatformHandle().Handle))
                    ?? throw new OlanException("认证失败", "无法认证你的微软账号"); ;
            }
            else
#endif
            {
                um =
                    await Init.MMA.LoginNewAccountToGetMinecraftMojangAccessTokenOnSystemBrowser()
                    ?? throw new OlanException("认证失败", "无法认证你的微软账号");
            }
            await Init.AccountManager.AddUser(um);
            using (var task = new MojangProfile(um))
                await task.GetSkinHeadImage();
            accountPageViewModel.RefList();
            accountPageViewModel.IsPaneShow = false;
            MainWindow.mainwindow.ShowFlyout($"已登入账号:{UserName}");
        }
        catch (OlanException ex)
        {
            OlanExceptionWorker.ForOlanException(ex);
            return;
        }
    }
}
