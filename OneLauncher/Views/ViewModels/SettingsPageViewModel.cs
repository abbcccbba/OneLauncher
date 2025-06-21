using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;

internal partial class SettingsPageViewModel : BaseViewModel
{
    [ObservableProperty]
    public bool isM1, isM2, isM3;
    partial void OnIsM1Changed(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.OlanSettings.MinecraftJvmArguments = JvmArguments.CreateFromMode(OptimizationMode.Conservative);
        Init.ConfigManger.Save();
    }
    partial void OnIsM2Changed(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.OlanSettings.MinecraftJvmArguments = JvmArguments.CreateFromMode(OptimizationMode.Standard);
        Init.ConfigManger.Save();
    }
    partial void OnIsM3Changed(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.OlanSettings.MinecraftJvmArguments = JvmArguments.CreateFromMode(OptimizationMode.Aggressive);
        Init.ConfigManger.Save();
    }
    #region 下载选项
    [ObservableProperty]
    public int _MaxDownloadThreadsValue;
    partial void OnMaxDownloadThreadsValueChanged(int value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads = value;
        Init.ConfigManger.Save();
    }
    [ObservableProperty]
    public int _MaxSha1ThreadsValue;
    partial void OnMaxSha1ThreadsValueChanged(int value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.OlanSettings.MaximumSha1Threads = value;
        Init.ConfigManger.Save();
    }
    [ObservableProperty]
    public bool _IsSha1Enabled;
    partial void OnIsSha1EnabledChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.OlanSettings.IsSha1Enabled = value;
        Init.ConfigManger.Save();
    }
    [ObservableProperty]
    public bool _IsAllowUseBMLCAPI;
    partial void OnIsAllowUseBMLCAPIChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        Init.ConfigManger.config.OlanSettings.IsAllowToDownloadUseBMLCAPI = value;
        Init.ConfigManger.Save();
    }
    
    #endregion
    public SettingsPageViewModel()
    {
#if DEBUG
        if (Design.IsDesignMode)
        {
            MaxDownloadThreadsValue = 24;
            MaxSha1ThreadsValue = 24;
            IsSha1Enabled = true;
        }
        else
#endif
        {
            try
            {
                switch (Init.ConfigManger.config.OlanSettings.MinecraftJvmArguments.mode)
                {
                    case OptimizationMode.Conservative:
                        IsM1 = true;
                        break;
                    case OptimizationMode.Standard:
                        IsM2 = true;
                        break;
                    case OptimizationMode.Aggressive:
                        IsM3 = true;
                        break;
                }
                MaxDownloadThreadsValue = Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads;
                MaxSha1ThreadsValue = Init.ConfigManger.config.OlanSettings.MaximumSha1Threads;
                IsSha1Enabled = Init.ConfigManger.config.OlanSettings.IsSha1Enabled;
                IsAllowUseBMLCAPI = Init.ConfigManger.config.OlanSettings.IsAllowToDownloadUseBMLCAPI;
            }
            catch(NullReferenceException ex)
            {
                throw new OlanException(
                    "内部异常",
                    "配置文件特定部分设置部分为空，这可能是新版和旧版配置文件不兼容导致的",
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
}
