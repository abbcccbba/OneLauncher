using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using System.Runtime.InteropServices;

namespace OneLauncher.Views.ViewModels;
internal partial class UserItem
{
    public string Name { get; set; }
    public string uuid { get; set; }
    [RelayCommand]
    public void SetDefault(UserItem user)
    {
        Init.ConfigManger.config.DefaultUserModel = new UserModel
            (Name, Guid.NewGuid());
        Init.ConfigManger.Write(Init.ConfigManger.config);
        MainWindow.mainwindow.ShowFlyout($"已将默认用户模型设置为{user.Name}");
    }
}
internal partial class AccountPageViewModel : BaseViewModel
{
    public AccountPageViewModel()
    {
#if DEBUG
        if (Design.IsDesignMode)
            UserModelList = new List<UserItem>()
            { new UserItem()
            { 
                Name="ZhiWei",
                uuid="0000-0000-0000-0000-0000"
            } };
        else
#endif
        UserModelList = Init.ConfigManger.config.UserModelList
                .Select(x => new UserItem() 
                { 
                    Name = x.Name ,
                    uuid = "UUID:"+x.uuid.ToString()
                }).ToList();
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
        if (string.IsNullOrEmpty(UserName))
        {
            MainWindow.mainwindow.ShowFlyout("用户名不能为空！",true);
            return;
        }
        UserModelList = Init.ConfigManger.config.UserModelList
                .Select(x => new UserItem()
                {
                    Name = x.Name,
                    uuid = "UUID:" + x.uuid.ToString()
                }).ToList();
        Init.ConfigManger.AddUserModel(new UserModel(
            UserName,
            Guid.NewGuid()
        ));
        IsPaneShow = false;
        MainWindow.mainwindow.ShowFlyout($"已新建用户:{UserName}");
    }
    [RelayCommand]
    public void Back()
    {
        IsPaneShow = false;
        MainWindow.mainwindow.ShowFlyout("已暂存修改！");
    }
}
