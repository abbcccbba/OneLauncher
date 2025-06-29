using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;

internal partial class SettingsPageViewModel : BaseViewModel
{
    // 将 manager 重命名为 _dbManger 以遵循常见的私有字段命名约定
    private readonly DBManager _dbManger;

    [ObservableProperty]
    public bool isM1, isM2, isM3;

    partial void OnIsM1Changed(bool value)
    {
        if (!value) return; // 避免在取消选中时也执行
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        // 使用注入的实例
        _dbManger.Data.OlanSettings.MinecraftJvmArguments = JvmArguments.CreateFromMode(OptimizationMode.Conservative);
        _dbManger.Save();
    }
    partial void OnIsM2Changed(bool value)
    {
        if (!value) return;
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        // 使用注入的实例
        _dbManger.Data.OlanSettings.MinecraftJvmArguments = JvmArguments.CreateFromMode(OptimizationMode.Standard);
        _dbManger.Save();
    }
    partial void OnIsM3Changed(bool value)
    {
        if (!value) return;
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        // 使用注入的实例
        _dbManger.Data.OlanSettings.MinecraftJvmArguments = JvmArguments.CreateFromMode(OptimizationMode.Aggressive);
        _dbManger.Save();
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
        // 使用注入的实例
        _dbManger.Data.OlanSettings.MaximumDownloadThreads = value;
        _dbManger.Save();
    }

    [ObservableProperty]
    public int _MaxSha1ThreadsValue;
    partial void OnMaxSha1ThreadsValueChanged(int value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        // 使用注入的实例
        _dbManger.Data.OlanSettings.MaximumSha1Threads = value;
        _dbManger.Save();
    }

    [ObservableProperty]
    public bool _IsSha1Enabled;
    partial void OnIsSha1EnabledChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        // 使用注入的实例
        _dbManger.Data.OlanSettings.IsSha1Enabled = value;
        _dbManger.Save();
    }

    [ObservableProperty]
    public bool _IsAllowUseBMLCAPI;
    partial void OnIsAllowUseBMLCAPIChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        // 使用注入的实例
        _dbManger.Data.OlanSettings.IsAllowToDownloadUseBMLCAPI = value;
        _dbManger.Save();
    }

    #endregion

    // 构造函数接收正确的 DBManger 类型
    public SettingsPageViewModel(DBManager configManager)
    {
        this._dbManger = configManager;
#if DEBUG
        if (Design.IsDesignMode)
        {
            MaxDownloadThreadsValue = 24;
            MaxSha1ThreadsValue = 24;
            IsSha1Enabled = true;
            return; // 在设计模式下提前返回
        }
#endif
        // else 块不再需要，因为非 DEBUG 模式下总会执行下面的代码

        try
        {
            // 使用注入的实例来初始化属性
            switch (_dbManger.Data.OlanSettings.MinecraftJvmArguments.mode)
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
            MaxDownloadThreadsValue = _dbManger.Data.OlanSettings.MaximumDownloadThreads;
            MaxSha1ThreadsValue = _dbManger.Data.OlanSettings.MaximumSha1Threads;
            IsSha1Enabled = _dbManger.Data.OlanSettings.IsSha1Enabled;
            IsAllowUseBMLCAPI = _dbManger.Data.OlanSettings.IsAllowToDownloadUseBMLCAPI;
        }
        catch (NullReferenceException ex)
        {
            // 异常处理中的静态调用暂时保留，因为这是更深层次的重构问题
            throw new OlanException(
                "内部异常",
                "配置文件特定部分设置部分为空，这可能是新版和旧版配置文件不兼容导致的",
                OlanExceptionAction.FatalError,
                ex,
               () =>
               {
                   File.Delete(Path.Combine(Init.BasePath, "config.json"));
                   Init.Initialize(); // 注意：这个静态调用最终也应被移除
               }
            );
        }
    }
}