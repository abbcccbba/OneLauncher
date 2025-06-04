﻿using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;

internal partial class InitServerPaneViewModel : BaseViewModel
{
#if DEBUG
    public InitServerPaneViewModel() { }
#endif
    public InitServerPaneViewModel(string version)
    {
        serverVersion = version;    
    }
    private string serverVersion;
    [ObservableProperty]
    public bool isAgreeMinecraftEULA;
    [RelayCommand]
    public void ReadMinecraftEULA()
        => Process.Start(new ProcessStartInfo { FileName = "https://aka.ms/MinecraftEULA", UseShellExecute = true });

    [ObservableProperty]
    public bool isVI = true;
    [RelayCommand]
    public async Task ToInstallServer()
    {
        if (!IsAgreeMinecraftEULA)
            await OlanExceptionWorker.ForOlanException(
                new Core.OlanException("无法初始化服务端","你必须在安装前同意Minecraft的最终用户许可协议",Core.OlanExceptionAction.Error),
                () => ToInstallServer());
        else
        {
            // 反正后面也要用，这里也读取了索性直接返回一个java版本
            int javaVersion = await MinecraftServerManger.Init(serverVersion,IsVI);
            //完成后打开服务端
            MinecraftServerManger.Run(serverVersion,"",javaVersion,IsVI);
            MainWindow.mainwindow.versionPage.viewmodel.IsPaneShow = false;
        }
    }
}
