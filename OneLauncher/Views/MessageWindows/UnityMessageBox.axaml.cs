using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OneLauncher;

public partial class UnityMessageBox : Window
{
    public void setgp(double vl)
    {
        prg.Text = $"下载进度 {vl}%";
    }
    public UnityMessageBox()
    {
        InitializeComponent();
    }
}