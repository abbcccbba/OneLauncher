using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Minecraft;
using OneLauncher.Views.ViewModels;
using OneLauncher.Views.Windows;
using OneLauncher.Views.Windows.WindowViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
namespace OneLauncher.Views;

public partial class version : UserControl
{
    public version()
    {
        InitializeComponent();
    }
    /// <summary>
    /// 真·一键启动游戏函数
    /// </summary>
    /// <returns>异步任务Task</returns>
    public static async Task<Task> EasyGameLauncher(
        GameData gameData,
        bool UseGameTasker = false,
        ServerInfo? serverInfo = null
        )
            => Game.EasyGameLauncher(gameData, serverInfo, UseGameTasker);
}