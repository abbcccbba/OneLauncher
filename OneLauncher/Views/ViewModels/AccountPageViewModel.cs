using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
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
    public bool IsDefault { get; set; }
    public bool IsNotDefault => !IsDefault;
}
internal partial class AccountPageViewModel : BaseViewModel
{
    // 刷新
    public void RefList()
    {
        // 在刷新列表时，判断每一项是否为默认用户
        UserModel? defaultUser = Init.ConfigManger.config.DefaultUserModel;

        UserModelList = Init.ConfigManger.config.UserModelList
            .Select(user => new UserItem()
            {
                um = user,
                HeadImg = (user.IsMsaUser && File.Exists(Path.Combine(Init.BasePath, "playerdata", "body", $"{user.uuid}.png")))
                    ? new Bitmap(Path.Combine(Init.BasePath, "playerdata", "body", $"{user.uuid}.png"))
                    : new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/steve.png"))),
                // 在创建 UserItem 时，就设置好 IsDefault 属性
                IsDefault = (defaultUser != null && user.uuid == defaultUser.uuid)
            }).ToList();
    }
    [RelayCommand]
    public async Task Refresh()
    {
        try
        {
            for (int i = 0; i < Init.ConfigManger.config.UserModelList.Count; i++)
            {
                var UserModelItem = Init.ConfigManger.config.UserModelList[i];
                if (UserModelItem.IsMsaUser
                && Init.ConfigManger.config.UserModelList.Count != 0
                )
                {
                    // 更新令牌
                    UserModel temp =
                        await Init.MMA.TryToGetMinecraftMojangAccessTokenForLoginedAccounts(
                            await Tools.UseAccountIDToFind(Init.ConfigManger.config.UserModelList[i].AccountID)
                            ?? throw new OlanException("无法刷新", "无法通过用户标识符找到你的账号"))
                        ?? throw new OlanException("无法刷新", "无法刷新你的微软正版账户登入令牌");
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
                }
            }
            RefList();
            MainWindow.mainwindow.ShowFlyout("刷新完毕");
        }
        catch (OlanException oex)
        {
            OlanExceptionWorker.ForOlanException(oex);
        }
        catch (Exception ex) { 
            OlanExceptionWorker.ForUnknowException(ex);
        }
    }
    public AccountPageViewModel()
    {
#if DEBUG
        if (Design.IsDesignMode)
            UserModelList = new List<UserItem>()
            {
                new UserItem()
                {
                    um = new UserModel("steve",new Guid(UserModel.nullToken))

                }
            };
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
    public void SetDefault(UserItem user)
    {
        UserModelList.Select(x => x.IsDefault = false);
        user.IsDefault = true;
        Init.ConfigManger.config.DefaultUserModel = user.um;
        Init.ConfigManger.Save();
        RefList();
        MainWindow.mainwindow.ShowFlyout($"已将默认用户模型设置为{user.um.Name}");
    }
    [RelayCommand]
    public void DeleteUser(UserModel user)
    {
        if (user.IsMsaUser)
            Init.MMA.RemoveAccount(
                Tools.UseAccountIDToFind(user.AccountID).Result);
        Init.ConfigManger.config.UserModelList.Remove(user);
        Init.ConfigManger.Save();
        RefList();
        MainWindow.mainwindow.ShowFlyout($"已移除用户模型{user.Name}", true);
    }
}
