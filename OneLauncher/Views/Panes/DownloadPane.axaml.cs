using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core.Helper;
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.ViewModels;
using OneLauncher.Views.Windows;

namespace OneLauncher.Views.Panes;
internal partial class DownloadPane : UserControl
{
    // 以后别他妈的打DEBUG了，傻逼编译器报错
//#if DEBUG
    // 供设计器预览
    public DownloadPane()
    {
        InitializeComponent();
    }

}