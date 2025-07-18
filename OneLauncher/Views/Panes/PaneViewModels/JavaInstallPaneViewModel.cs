using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Core.Global;
using OneLauncher.Core.Minecraft;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;
internal partial class JavaInstallPaneViewModel : BaseViewModel
{
    private readonly JavaManager _javaManager;
    public JavaInstallPaneViewModel(JavaManager javaManager)
    {
        _javaManager = javaManager;
    }
    private readonly CancellationTokenSource _cts = new();
    #region 用户可选安装属性
    [ObservableProperty] ObservableCollection<JavaProvider> _javaProviders = 
        new ObservableCollection<JavaProvider>(
            [
            JavaProvider.Adoptium,
            JavaProvider.AzulZulu,
            JavaProvider.OracleGraalVM,
            JavaProvider.MicrosoftOpenJDK,
            JavaProvider.OracleJDK
            ]);
    [ObservableProperty] JavaProvider _selectedJavaProvider = JavaProvider.Adoptium;
    [ObservableProperty] ObservableCollection<int> _javaVersions = 
        new ObservableCollection<int>(
            [
            8, 11, 16,17, 21,24
            ]);
    [ObservableProperty] int _selectedJavaVersion = 21;
    #endregion
    #region 进度报告属性
    [ObservableProperty] string _titText = string.Empty;
    [ObservableProperty] double _progressValue = 0.0;
    #endregion
    [RelayCommand]
    private async Task Install()
    {
        //_javaManager.InstallJava();
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
        // 发送消息以通知设置页面关闭此窗格
        WeakReferenceMessenger.Default.Send(new SettingsPageClosePaneControlMessage());
    }
}