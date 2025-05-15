using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using OneLauncher.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OneLauncher.Codes;

namespace OneLauncher.Views;

public partial class MainWindow : Window
{
    private Home HomePage;
    private version versionPage;
    private download downloadPage;
    private settings settingsPage;
    private account accountPage;
    public static MainWindow mainwindow;
    public MainWindow()
    {
        InitializeComponent();
        Codes.Init.Initialize();
        mainwindow = this;
    }
    public enum MainPage
    {
        HomePage,VersionPage,AccountPage,DownloadPage, SettingsPage
    }
    public void MainPageControl(MainPage page)
    {
        switch(page)
        {
            case MainPage.HomePage:
                PageContent.Content = HomePage;
                break;
            case MainPage.VersionPage:
                PageContent.Content = versionPage;
                break;
            case MainPage.AccountPage:
                PageContent.Content = accountPage;
                break;
            case MainPage.DownloadPage:
                PageContent.Content = downloadPage;
                break;
            case MainPage.SettingsPage:
                PageContent.Content = settingsPage;
                break;
        }
    }
    public void MainPageNavigate(UserControl page)
    {
        PageContent.Content = page;
    }
    public async void Showfyt(string text)
    {
        FytFkA.Text = text;
        FytB.IsVisible = true;
        await Task.Delay(1000);
        FytB.IsVisible = false;
    }
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        HomePage = new Home();
        versionPage = new version();
        accountPage = new account();
        downloadPage = new download();
        settingsPage = new settings();
        PageContent.Content = HomePage;
    }
    // 统一的事件处理函数
    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            switch (tag)
            {
                case "Home":
                    PageContent.Content = HomePage;
                    break;
                case "Version":
                    PageContent.Content = versionPage;
                    break;
                case "Account":
                    PageContent.Content = accountPage;
                    break;
                case "Download":
                    PageContent.Content = downloadPage;
                    break;
                case "Settings":
                    PageContent.Content = settingsPage;
                    break;
            }
        }
    }

    // 切换侧边栏展开/折叠
    private void TSK()
    {
        var splitView = this.FindControl<SplitView>("SidebarSplitView");
        if (splitView != null)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;

            // 动态调整文字显示
            var textBlocks = new[]
            {
                this.FindControl<TextBlock>("HomeText"),
                this.FindControl<TextBlock>("VersionText"),
                this.FindControl<TextBlock>("AccountText"),
                this.FindControl<TextBlock>("DownloadText"),
                this.FindControl<TextBlock>("SettingsText")
            };

            foreach (var textBlock in textBlocks)
            {
                if (textBlock != null)
                {
                    textBlock.IsVisible = splitView.IsPaneOpen;
                }
            }
        }
    }

    private void Grid_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e) => TSK();

    private void Grid_PointerExited_1(object? sender, Avalonia.Input.PointerEventArgs e) => TSK();
}