using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using OneLauncher.Core.fabric;
using OneLauncher.Core.Net.msa;
using OneLauncher.Views;
using System;
using System.Diagnostics;

namespace OneLauncher;

public partial class App : Application
{
    public override void Initialize() =>
        AvaloniaXamlLoader.Load(this);

    public async override void OnFrameworkInitializationCompleted()
    {
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Init.Initialize();
            desktop.MainWindow = new MainWindow();
            Debug.WriteLine(Init.ConfigManger.config.UserModelList[0].accessToken);
            using (MojangProfile profile = new MojangProfile(Init.ConfigManger.config.DefaultUserModel))
            {
                await profile.Set(new MojangSkin() { SkinUrl = "https://s.namemc.com/i/9eb4195583336074.png", IsSlimModel=true});
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
