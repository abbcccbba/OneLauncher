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
    private CancellationTokenSource _cts;
    [RelayCommand]
    private async Task Install()
    {
        
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