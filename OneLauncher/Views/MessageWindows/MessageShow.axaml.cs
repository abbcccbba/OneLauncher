using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Views.MessageWindows;

namespace OneLauncher;

public partial class MessageShow : Window
{
    public MessageShow(string description)
    {
        InitializeComponent();
        Dialog_massage.Text = description;
    }
    public string needsp { get; set; }
    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Dialog_textbox.Text != null)
        {
            needsp = Dialog_textbox.Text;
            this.Close();
        }
    }
}