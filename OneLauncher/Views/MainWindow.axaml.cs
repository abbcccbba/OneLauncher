using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Views.Panes;
using OneLauncher.Views.Panes.PaneViewModels;
using OneLauncher.Views.Panes.PaneViewModels.Factories;
using OneLauncher.Views.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OneLauncher.Views;
internal class ApplicationClosingMessage { }
internal class MainWindowShowFlyoutMessage
{
    public readonly string Context;
    public readonly bool IsWarn;
    public MainWindowShowFlyoutMessage(string context,bool isWarn = false)
    {
        this.Context = context;
        this.IsWarn = isWarn;
    }
}
public partial class MainWindow : Window
{
    public Home HomePage;
    public version versionPage;
    public download downloadPage;
    public settings settingsPage;
    public account accountPage;
    public ModsBrowser modsBrowserPage;
    public gamedata gamedataPage;
    public static MainWindow mainwindow;
    bool IsError;
    IServiceCollection servises;
    public readonly IServiceProvider provider;
    public MainWindow()
    {
        InitializeComponent();
        mainwindow = this;
        try
        {
            servises = Init.InitTask.GetAwaiter().GetResult();

            servises.AddSingleton<AccountPageViewModel>();
            servises.AddSingleton<DownloadPageViewModel>();
            servises.AddSingleton<GameDataPageViewModel>();
            servises.AddSingleton<HomePageViewModel>();
            servises.AddSingleton<ModsBrowserViewModel>();
            servises.AddSingleton<SettingsPageViewModel>();
            servises.AddSingleton<VersionPageViewModel>();

            servises.AddTransient<NewGameDataPaneViewModel>();
            servises.AddTransient<DownloadPaneViewModelFactory>();

            provider = servises.BuildServiceProvider();
            PageContent.Content = new Home();
            // 注册消息
            WeakReferenceMessenger.Default.Register<MainWindowShowFlyoutMessage>(this, (re, message) => ShowFlyout(message.Context, message.IsWarn));
        }
        catch(OlanException e)
        {
            IsError = true;
            OlanExceptionWorker.ForOlanException(e);
        }
    }
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (!IsError)
        {
            try
            {
                HomePage = new Home()
                { DataContext = provider.GetRequiredService<HomePageViewModel>()};
                versionPage = new version() 
                { DataContext = provider.GetRequiredService<VersionPageViewModel>() }; 
                accountPage = new account()
                { DataContext = provider.GetRequiredService<AccountPageViewModel>() };
                modsBrowserPage = new ModsBrowser()
                { DataContext = provider.GetRequiredService<ModsBrowserViewModel>() };
                downloadPage = new download()
                { DataContext = provider.GetRequiredService<DownloadPageViewModel>() };
                settingsPage = new settings()
                { DataContext = provider.GetRequiredService<SettingsPageViewModel>() };
                gamedataPage = new gamedata()
                { DataContext = provider.GetRequiredService<GameDataPageViewModel>() };
            }
            catch (OlanException ex)
            {
                OlanExceptionWorker.ForOlanException(ex);
            }
        }
    }
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        Debug.WriteLine("释放残余资源...");
        foreach(var dis in Init.OnApplicationClosingReleaseSourcesList)
            dis.Dispose();
        // 发送关闭消息
        WeakReferenceMessenger.Default.Send(new ApplicationClosingMessage());
    }
    public enum MainPage
    {
        HomePage,VersionPage,AccountPage,DownloadPage,SettingsPage, ModsBrowserPage,GameDataPage //, ServerPage
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
            case MainPage.GameDataPage:
                SplitListBox.SelectedItem = GameDataListBoxItem;
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
            case "GameData":
                PageContent.Content = gamedataPage;
                break;
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