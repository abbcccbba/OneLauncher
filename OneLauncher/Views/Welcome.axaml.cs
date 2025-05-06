using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace OneLauncher;

public partial class Welcome : Window
{
    public Welcome()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // 切换侧边栏的展开/折叠状态
    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        var splitView = this.FindControl<SplitView>("SidebarSplitView");
        if (splitView != null)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }
    }
}