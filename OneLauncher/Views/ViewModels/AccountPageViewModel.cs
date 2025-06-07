using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Net.msa;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal partial class UserItem
{
    public UserModel um { get; set; }
    public Bitmap HeadImg { get; set; } = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/steve.png")));
}
internal partial class AccountPageViewModel : BaseViewModel
{
    // 刷新
    public void RefList()
    {
        UserModelList = Init.ConfigManger.config.UserModelList
                .Select(x => new UserItem()
                {
                    um = x,
                    HeadImg = (x.IsMsaUser)
                    ? new Bitmap(Path.Combine(Init.BasePath, "MsaPlayerData", "body", $"{x.uuid}.png"))
                    : new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/steve.png")))
                }).ToList();
    }
    [RelayCommand]
    public async Task Refresh()
    {
        for (int i = 0; i < Init.ConfigManger.config.UserModelList.Count; i++)
        {
            var UserModelItem = Init.ConfigManger.config.UserModelList[i];
            // 无23小时检查机制直接更新
            if (UserModelItem.IsMsaUser
            && Init.ConfigManger.config.UserModelList.Count != 0
            )
            {
                // 更新令牌
                var temp = (UserModel)await new MicrosoftAuthenticator().RefreshToken(UserModelItem!.refreshTokenID);
                lock (Init.ConfigManger.config.UserModelList)
                {
                    // 如果是默认用户模型也更新
                    if (UserModelItem.uuid == Init.ConfigManger.config.DefaultUserModel.uuid)
                        Init.ConfigManger.config.DefaultUserModel = temp;
                    Init.ConfigManger.config.UserModelList[i] = temp;
                    Init.ConfigManger.Save();
                }
                // 更新皮肤
                using (var task = new MojangProfile(Init.ConfigManger.config.UserModelList[i]))
                    await task.GetSkinHeadImage();
               RefList();
               await MainWindow.mainwindow.ShowFlyout("刷新完毕");           
            }
        }
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
        {
            try
            {
                RefList();
            }
            catch (NullReferenceException ex)
            {
                throw new OlanException(
                    "内部异常",
                    "配置文件特定部分账户部分为空，这可能是新版和旧版配置文件不兼容导致的",
                    OlanExceptionAction.FatalError,
                    ex,
                   () =>
                   {
                       File.Delete(Path.Combine(Init.BasePath, "config.json"));
                       Init.Initialize();
                   }
                    );
            }
        }
    }
    [ObservableProperty]
    public List<UserItem> _UserModelList;
    [ObservableProperty]
    public bool _IsPaneShow= false;
    [ObservableProperty]
    public UserControl _AccountPane;

    [RelayCommand]
    public void NewUserModel()
    {
        IsPaneShow = true;
        AccountPane = new UserModelLoginPane(this);
    }
    [RelayCommand]
    public void SkinManger(UserModel userModel)
    {
        IsPaneShow = true;
        AccountPane = new SkinMangerPane(this,userModel);
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
        if(user.IsMsaUser) 
            Init.Security.Del(user.refreshTokenID);
        Init.ConfigManger.config.UserModelList.Remove(user);
        Init.ConfigManger.Save();
        RefList();
        MainWindow.mainwindow.ShowFlyout($"已移除用户模型{user.Name}", true);
    }
}
