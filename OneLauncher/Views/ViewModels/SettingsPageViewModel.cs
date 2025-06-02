using CommunityToolkit.Mvvm.ComponentModel;
using OneLauncher.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;

internal partial class SettingsPageViewModel : BaseViewModel
{
    [ObservableProperty]
    public int _MaxDownloadThreadsValue;
    partial void OnMaxDownloadThreadsValueChanged(int value)
    {
        Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads = value;
        Init.ConfigManger.Save();
    }
    [ObservableProperty]
    public int _MaxSha1ThreadsValue;
    partial void OnMaxSha1ThreadsValueChanged(int value)
    {
        Init.ConfigManger.config.OlanSettings.MaximumSha1Threads = value;
        Init.ConfigManger.Save();
    }
    [ObservableProperty]
    public bool _IsSha1Enabled;
    partial void OnIsSha1EnabledChanged(bool value)
    {
        Init.ConfigManger.config.OlanSettings.IsSha1Enabled = value;
        Init.ConfigManger.Save();
    }
    public SettingsPageViewModel()
    { 
        MaxDownloadThreadsValue = Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads;
        MaxSha1ThreadsValue = Init.ConfigManger.config.OlanSettings.MaximumSha1Threads;
        IsSha1Enabled = Init.ConfigManger.config.OlanSettings.IsSha1Enabled;
    }
}
