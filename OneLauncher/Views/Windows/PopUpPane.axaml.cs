using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OneLauncher.Views.Windows;

public partial class PopUpPane : Window
{
    public PopUpPane(UserControl contont)
    {
        InitializeComponent();
        Content = contont;
    }
}