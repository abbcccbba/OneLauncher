using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Views.ViewModels;

namespace OneLauncher.Views;

public partial class gamedata : UserControl
{
    public gamedata()
    {
        InitializeComponent();
        this.DataContext = new GameDataPageViewModel();
    }
}