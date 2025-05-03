using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OneLauncher;

public partial class MessageShow : Window
{
    public MessageShow()
    {
        InitializeComponent();
    }
    public void st(string sa)
    {
        sf.Text = sa;
    }
}