using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;

internal partial class HomePageViewModel : BaseViewModel
{
    public HomePageViewModel()
    {
        var InitResult = Init.InitTask.Result;
        if (InitResult != null)
            throw InitResult;
        UserName = "用户名："+Init.ConfigManger?.config?.DefaultUserModel?.Name ?? "未指定";
        VersionName = "版本：" + Init.ConfigManger?.config?.DefaultVersion ?? "未指定";
    }
    [ObservableProperty]
    public string userName;
    [ObservableProperty] 
    public string versionName;
    //[RelayCommand]
    //public async Task ToPlay()
    //{
    //    if(Init.ConfigManger.config.DefaultVersion == null)
    //    {
    //        await OlanExceptionWorker.ForOlanException(
    //            new OlanException("无法启动","未指定默认版本",OlanExceptionAction.Error),
    //            () => 
    //            {
    //                if (Init.ConfigManger.config.UserModelList.Count > 0) 
    //                { 
    //                    Init.ConfigManger.config.DefaultVersion = Init.ConfigManger.config.VersionList[0];
    //                    Init.ConfigManger.Save();
    //                }
    //                ToPlay();
    //            }
    //        );
    //        return;
    //    }
    //    _=version.EasyGameLauncher(false, Init.ConfigManger.config.DefaultUserModel);
    //}
}
