using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Net.msa;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        Init.ConfigManger.config.UserModelList.Add(new UserModel(
        
            Name : UserName,
            uuid : Guid.NewGuid()
        ));
        Init.ConfigManger.Save();
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
    public string _UserCode;
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
        using (var verTask = new MicrosoftAuthenticator())
        {
            try
            {
                var um = await verTask.AuthUseCode(new Progress<(string a, string b)>((x) =>
                {
                    // 打开网页并提醒用户
                    Process.Start(new ProcessStartInfo { FileName = x.a, UseShellExecute = true });
                    UserCode = x.b;
                }));
                Init.ConfigManger.config.UserModelList.Add((UserModel)um);
                Init.ConfigManger.Save();
                accountPageViewModel.RefList();
            }
            catch (OlanException ex)
            {
                await OlanExceptionWorker.ForOlanException(ex);
                return;
            }
        }
    }
}
