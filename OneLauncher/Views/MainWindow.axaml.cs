using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Net.msa;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Views;
public class ApplicationClosingMessage { }
public partial class MainWindow : Window
{
    public Home HomePage;
    public version versionPage;
    public download downloadPage;
    public settings settingsPage;
    public account accountPage;
    public ModsBrowser modsBrowserPage;
    public static MainWindow mainwindow;
    public MainWindow()
    {
        InitializeComponent();
        mainwindow = this;
        PageContent.Content = new Home();
    }
    protected async override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        try
        {
            HomePage = (Home)PageContent.Content;
            versionPage = new version();
            accountPage = new account();
            modsBrowserPage = new ModsBrowser();
            downloadPage = new download();
            settingsPage = new settings();
        }
        catch (OlanException ex)
        {
            await OlanExceptionWorker.ForOlanException(ex);
        }
    }
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        Debug.WriteLine("释放残余资源...");
        WeakReferenceMessenger.Default.Send(new ApplicationClosingMessage());
    }
    public enum MainPage
    {
        HomePage,VersionPage,AccountPage,DownloadPage,SettingsPage, ModsBrowserPage//, ServerPage
    }
    /// <summary>
    /// 手动管理页面切换
    /// </summary>
    /// <param ID="page">主页面</param>
    public void MainPageControl(MainPage page)
    {
        switch(page)
        {
            case MainPage.HomePage:
                SplitListBox.SelectedItem = HomeListBoxItem;
                break;
            case MainPage.VersionPage:
                SplitListBox.SelectedItem = VersionListBoxItem;
                break;
            case MainPage.AccountPage:
                SplitListBox.SelectedItem = AccountListBoxItem;
                break;
            case MainPage.ModsBrowserPage:
                SplitListBox.SelectedItem = ModsBrowserListBoxItem;
                break;
            case MainPage.DownloadPage:
                SplitListBox.SelectedItem = DownloadListBoxItem;
                break;
            case MainPage.SettingsPage:
                SplitListBox.SelectedItem = SettingsListBoxItem;
                break;
        }
    }
    /// <summary>
    /// 在右下角显示提示信息
    /// </summary>
    /// <param ID="text">提示信息内容</param>
    public void ShowFlyout(string text,bool IsWarning = false) =>
    Dispatcher.UIThread.Post(async() =>
    {
        FytFkA.Text = text;
        if (IsWarning)
            FytB.Background = new SolidColorBrush(Colors.Red);
        else
            FytB.Background = new SolidColorBrush(Colors.LightBlue);
        FytB.IsVisible = true;
        await Task.Delay(3000);
        FytB.IsVisible = false;
    });
    
    // 统一事件方法
    private void ListBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        var listBox = sender as ListBox;
        if (listBox == null) return;

        var selectedItem = listBox.SelectedItem as ListBoxItem;
        if (selectedItem == null) return;

        switch (selectedItem.Tag)
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
            case "ModsBrowser":
                PageContent.Content = modsBrowserPage;
                break;
            case "Download":
                PageContent.Content = downloadPage;
                break;
            //case "Server":
            //    PageContent.Content = serverPage;
            //    break;
            case "Settings":
                PageContent.Content = settingsPage;
                break;
        }
        
    }
    // 切换侧边栏展开/折叠
    private void MangePaneOpenAndClose(bool IsOpen)
    {
        HomeText.IsVisible = IsOpen;
        VersionText.IsVisible = IsOpen;
        AccountText.IsVisible = IsOpen;
        DownloadText.IsVisible = IsOpen;
        SettingsText.IsVisible = IsOpen;
        //ServerText.IsVisible = IsOpen;
        ModsBrowserText.IsVisible = IsOpen;
        SidebarSplitView.IsPaneOpen = IsOpen;
    }
    // 鼠标进入事件
    private void Sb_in(object? sender, Avalonia.Input.PointerEventArgs e) => MangePaneOpenAndClose(true);
    // 鼠标离开事件
    private void Sb_out(object? sender, Avalonia.Input.PointerEventArgs e) => MangePaneOpenAndClose(false);
}