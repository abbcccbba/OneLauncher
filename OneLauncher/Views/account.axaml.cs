using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using System.Diagnostics;
using OneLauncher.Views;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using System;
using OneLauncher.Codes;
namespace OneLauncher;

public partial class account : UserControl
{
    public account()
    {
        InitializeComponent(); 
    }
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        if (AccountListViews.ItemsSource == null) 
            AccountListViews.ItemsSource = Init.ConfigManger.config.UserModelList;
        else return;
    }
    private void SetDefault(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button)
            if (button.DataContext is UserModel aUserModel)
            {
                Init.ConfigManger.config.DefaultUserModel = aUserModel;
                Init.ConfigManger.Write(Init.ConfigManger.config);
            }
    }

    private async void new_Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var Dialog = new MessageShow("输入你的用户名（自定义）");
        await Dialog.ShowDialog(MainWindow.mainwindow);
        
        Init.ConfigManger.AddUserModel(new UserModel() 
        { 
            Name = Dialog.needsp,
            uuid = Guid.NewGuid().ToString(),
            accessToken = "0"
        });
        AccountListViews.ItemsSource = Init.ConfigManger.config.UserModelList;
        
    }
}