using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Views.Panes.PaneViewModels;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes;

public partial class PowerPlayPane : UserControl
{
    public PowerPlayPane()
    {
        InitializeComponent();
        _=InitializationAsync();
    }
    private async Task InitializationAsync()
    {
        {
            if(!File.Exists(Path.Combine(Init.BasePath,"installed","main.exe")))
                MainWindow.mainwindow.ShowFlyout("正在初始化联机模块...");
            try
            {
                var viewmodel = await PowerPlayPaneViewModel.CreateAsync();
                this.DataContext = viewmodel;
            }
            catch (OlanException ex)
            {
                OlanExceptionWorker.ForOlanException(ex);
            }
            catch (Exception ex)
            {
                OlanExceptionWorker.ForUnknowException(ex);
            }
        }
    }
}