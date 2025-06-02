using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using OneLauncher.Views.ViewModels;
namespace OneLauncher.Views;

public partial class settings : UserControl
{
    public settings()
    {
        InitializeComponent();
        this.DataContext = new SettingsPageViewModel();
    }
}