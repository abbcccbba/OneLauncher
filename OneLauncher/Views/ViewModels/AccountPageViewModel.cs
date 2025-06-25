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
        UserModel? defaultUser = Init.AccountManager.GetDefaultUser();

        UserModelList = Init.AccountManager.GetAllUsers()
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
            foreach (var user in UserModelList)
            {
                using (var task = new MojangProfile(user.um))
                    await task.GetSkinHeadImage();
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
                    um = new UserModel(new Guid(),"steve",new Guid(UserModel.nullToken))

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
        Init.AccountManager.SetDefaultAsync(user.um.UserID);
        RefList();
        MainWindow.mainwindow.ShowFlyout($"已将默认用户模型设置为{user.um.Name}");
    }
    [RelayCommand]
    public void DeleteUser(UserModel user)
    {
        if (user.IsMsaUser)
            Init.MMA.RemoveAccount(
                Tools.UseAccountIDToFind(user.AccountID).Result);
        Init.AccountManager.RemoveUserAsync(user.UserID);
        RefList();
        MainWindow.mainwindow.ShowFlyout($"已移除用户模型{user.Name}", true);
    }
}
