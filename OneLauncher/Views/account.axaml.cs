using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using System.Diagnostics;
using OneLauncher.Views;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using System;
using OneLauncher.Codes;
using OneLauncher.Views.ViewModels;
using System.Linq;
using OneLauncher.Core.Net.msa;

namespace OneLauncher.Views;
public partial class account : UserControl
{
    public account()
    {
        InitializeComponent();
        viewmodel = new AccountPageViewModel();
        this.DataContext = viewmodel;
        // 刷新用户令牌
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    // 检查是否有accesstoken 过期的用户模型
                    for (int i = 0; i < Init.ConfigManger.config.UserModelList.Count; i++)
                    {
                        var UserModelItem = Init.ConfigManger.config.UserModelList[i];
                        if (UserModelItem.IsMsaUser
                        && MicrosoftAuthenticator.IsExpired((DateTime)UserModelItem.AuthTime)
                        && Init.ConfigManger.config.UserModelList.Count != 0
                        )
                        {
                            Debug.WriteLine($"用户 {UserModelItem.Name} 的 accessToken 已过期，正在更新...");
                            // 如果过期了，则更新
                            var temp = (UserModel)await new MicrosoftAuthenticator().RefreshToken(UserModelItem!.refreshTokenID);
                            lock (Init.ConfigManger.config.UserModelList)
                            {
                                // 如果是默认用户模型也更新
                                if (UserModelItem.uuid == Init.ConfigManger.config.DefaultUserModel.uuid)
                                    Init.ConfigManger.config.DefaultUserModel = temp;
                                Init.ConfigManger.config.UserModelList[i] = temp;
                                Init.ConfigManger.Save();
                            }
                        }
                    }
                    await Task.Delay(1000 * 60 * 60 * 23); // 每23小时检查一次
                }
                catch (Exception ex)
                {
                    await OlanExceptionWorker.ForOlanException(
                        new OlanException("无法更新用户令牌","已捕获到错误，线程已终止",OlanExceptionAction.Warning,ex));
                    break;
                }
            }
        });
    }
    internal AccountPageViewModel viewmodel;
}