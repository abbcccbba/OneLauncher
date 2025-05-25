using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Net.msa;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal partial class UserItem
{
    public UserModel um { get; set; }
}
internal partial class AccountPageViewModel : BaseViewModel
{
    // 刷新
    private void RefList()
    {
        UserModelList = Init.ConfigManger.config.UserModelList
                .Select(x => new UserItem()
                {
                    um = x
                }).ToList();
    }
    public AccountPageViewModel()
    {
#if DEBUG
        if (Design.IsDesignMode)
            UserModelList = new List<UserItem>()
            { new UserItem()
            {
                um = new UserModel()
                {
                    Name="ZhiWei",
                    uuid=Guid.NewGuid()
                }
            } };
        else
#endif
            RefList();
    }
    [ObservableProperty]
    public List<UserItem> _UserModelList;
    [ObservableProperty]
    public bool _IsPaneShow = false;
    [ObservableProperty]
    public string _UserName;

    public object WebApplication { get; private set; }

    [RelayCommand]
    public void NewUserModel()
    {
        IsPaneShow = true;
    }
    [RelayCommand]
    public void Done()
    {
        if (string.IsNullOrEmpty(UserName)) return;
        if (!Regex.IsMatch(UserName, @"^[a-zA-Z0-9_]+$"))
        {
            MainWindow.mainwindow.ShowFlyout("用户名包含非法字符！", true);
            return;
        }
        Init.ConfigManger.config.UserModelList.Add(new UserModel()
        {
            Name = UserName,
            uuid = Guid.NewGuid()
        });
        Init.ConfigManger.Save();
        RefList();
        IsPaneShow = false;
        MainWindow.mainwindow.ShowFlyout($"已新建用户:{UserName}");
    }
    [RelayCommand]
    public void Back()
    {
        IsPaneShow = false;
        MainWindow.mainwindow.ShowFlyout("已暂存修改！");
    }
    [ObservableProperty]
    public string _UserCode;
    [ObservableProperty]
    public bool _IsYaLogin= true;
    [ObservableProperty]
    public bool _IsMsaLogin = false;
    private ListBoxItem _whiceLoginType;
    public ListBoxItem WhiceLoginType
    {
        set {
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
        get {
            return _whiceLoginType;
        }
    }
    [RelayCommand]
    public async Task LoginWithMicrosoft()
    {
        /*
         这里替换为你实际的Azure应用ID
         */
        var ApiKey = Environment.GetEnvironmentVariable("AzureApplicationID");

        if (ApiKey == null)
            throw new Exception("请替换为你正确的Azure应用ID");
        
        Debug.WriteLine($"ApiKey: {ApiKey}");

        using (var verTask = new MicrosoftAuthenticator(ApiKey))
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
                RefList();
            }
            catch (MsaException ex)
            {
                await MainWindow.mainwindow.ShowFlyout(ex.Message, true);
                return;
            }

        }
    }
    [RelayCommand]
    public void SetDefault(UserModel user)
    {
        Init.ConfigManger.config.DefaultUserModel = user;
        Init.ConfigManger.Save();
        MainWindow.mainwindow.ShowFlyout($"已将默认用户模型设置为{user.Name}");
    }
    [RelayCommand]
    public void DeleteUser(UserModel user)
    {
        Init.ConfigManger.config.UserModelList.Remove(user);
        Init.ConfigManger.Save();
        RefList();
        MainWindow.mainwindow.ShowFlyout($"已移除用户模型{user.Name}", true);
    }
}
