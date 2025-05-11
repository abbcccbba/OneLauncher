

using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Views.ViewModels;
using System.Diagnostics;
using System.Runtime.CompilerServices;
namespace OneLauncher.Views.Panes.PaneViewModels;
internal partial class DownloadPaneViewModel : BaseViewModel
{
#if DEBUG
    // 供设计器预览
    public DownloadPaneViewModel()
    {
        VersionName = "1.21.5";
    }
#endif
    public DownloadPaneViewModel(string Version,DownloadPageViewModel downloadPane)
    {
        VersionName = Version;
        this.downloadPage = downloadPane;
    }
    DownloadPageViewModel downloadPage;
    [ObservableProperty]
    public string _VersionName;

    [RelayCommand]
    public void ToDownload()
    {
        Debug.WriteLine("ToDownload");
    }
    [RelayCommand]
    public void ClosePane()
    {
        downloadPage.IsPaneShow = false;
    }
    [RelayCommand]
    public void CheckOnWeb()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://zh.minecraft.wiki/w/Java版{VersionName}",
            UseShellExecute = true
        });
    }
}