using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OneLauncher.Views;

public partial class gamedata : UserControl
{
    public gamedata()
    {
        InitializeComponent();
        this.DataContext = new GameDataPageViewModel();
    }
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        try
        {
            navVL.ItemsSource = Init.GameDataManger.AllGameData.Select(x => new GameDataItem(x)).ToList();
        }
        catch (NullReferenceException ex)
        {
            throw new OlanException(
                "内部异常",
                "配置文件特定部分版本列表部分为空，这可能是新版和旧版配置文件不兼容导致的",
                OlanExceptionAction.FatalError,
                ex,
               () =>
               {
                   File.Delete(Path.Combine(Init.BasePath, "config.json"));
                   Init.Initialize();
               }
                );
        }
    }
}