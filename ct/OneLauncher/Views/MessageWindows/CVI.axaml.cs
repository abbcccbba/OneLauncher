using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using System.Collections.Generic;
using OneLauncher.Codes;
using OneLauncher.Views;

namespace OneLauncher;

public partial class CVI : Window
{
    public bool isOK = false;
    public CVI()
    {
        InitializeComponent();
    }
    private async void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (VersionName.Text == null)
            return;
        isOK = true;
        this.Close();
    }
    public string GetReturnInfo()
    {
        return VersionName.Text;
    }
}