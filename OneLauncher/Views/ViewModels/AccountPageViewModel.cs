using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using System;
using System.Collections.Generic;
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
        UserModelList = Init.ConfigManger.config.UserModelList
                .Select(x => new UserItem()
                {
                    um = x
                }).ToList();
        IsPaneShow = false;
        MainWindow.mainwindow.ShowFlyout($"已新建用户:{UserName}");
    }
    [RelayCommand]
    public void Back()
    {
        IsPaneShow = false;
        MainWindow.mainwindow.ShowFlyout("已暂存修改！");
    }
    [RelayCommand]
    public void SetDefault(UserModel user)
    {
        Init.ConfigManger.config.DefaultUserModel = user;
        Init.ConfigManger.Write(Init.ConfigManger.config);
        MainWindow.mainwindow.ShowFlyout($"已将默认用户模型设置为{user.Name}");
    }
    [RelayCommand]
    public void DeleteUser(UserModel user)
    {
        Init.ConfigManger.config.UserModelList.Remove(user);
        Init.ConfigManger.Write(Init.ConfigManger.config);
        RefList();
        MainWindow.mainwindow.ShowFlyout($"已移除用户模型{user.Name}", true);
    }
}
