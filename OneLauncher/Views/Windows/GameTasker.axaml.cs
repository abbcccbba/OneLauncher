using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Views.Windows.WindowViewModels;

namespace OneLauncher.Views.Windows;

public partial class GameTasker : Window
{
    public GameTasker()
    {
        InitializeComponent();
        this.DataContext = new GameTaskerViewModel();
    }
}