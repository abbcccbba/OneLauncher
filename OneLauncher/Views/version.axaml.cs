using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using OneLauncher.Views.ViewModels;
using OneLauncher.Core;
using OneLauncher.Codes;
namespace OneLauncher;

public partial class version : UserControl
{
    public version()
    {
        InitializeComponent();
        this.DataContext = new VersionPageViewModel();
    }
}