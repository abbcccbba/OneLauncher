using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using OneLauncher.Views;

namespace OneLauncher;

public partial class App : Application
{
    public override void Initialize() =>
        AvaloniaXamlLoader.Load(this);

    public async override void OnFrameworkInitializationCompleted()
    {
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await Codes.Init.Initialize();
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
