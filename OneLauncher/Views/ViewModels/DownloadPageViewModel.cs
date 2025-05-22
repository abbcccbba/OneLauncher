using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core;
using OneLauncher.Core.Modrinth;
using OneLauncher.Core.Modrinth.JsonModelSearch;
using OneLauncher.Views;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
namespace OneLauncher.Views.ViewModels;

internal partial class DownloadPageViewModel : BaseViewModel
{
    /// <summary>
    /// 无网络重载方法，会在版本列表里显示“无网络链接”并拒绝下载
    /// </summary>
    public DownloadPageViewModel()
    {
        IsAllowDownloading = false;
        MainWindow.mainwindow.ShowFlyout("出现错误：无法下载版本清单",true);
    }
    public DownloadPageViewModel (List<VersionBasicInfo> ReleaseVersionList)
    {
        IsAllowDownloading = true;
        this.ReleaseItems = ReleaseVersionList;
        AutoVersionList = ReleaseVersionList;
    }
    
    [ObservableProperty]
    public List<VersionBasicInfo> _ReleaseItems;

    [ObservableProperty]
    public UserControl _DownloadPaneContent;
    [ObservableProperty]
    public bool _IsPaneShow = false;
    [ObservableProperty]
    public bool _IsAllowDownloading;
    [ObservableProperty]
    public List<VersionBasicInfo> _AutoVersionList;

    private VersionBasicInfo selectedItem;
    public VersionBasicInfo SelectedItem
    {
        get { return selectedItem; }
        set
        {
            // 避免未选中时转换类型导致异常
            if (value == null)
                return;
            selectedItem = value;

            // 点击操作
            // 展开并显示Pane
            IsPaneShow = true;
            DownloadPaneContent = new DownloadPane(value, this);
        }
    }

    [ObservableProperty]
    public string _SearchContent;
    [ObservableProperty]
    public List<ModItem> _SearchItems;
    [RelayCommand]
    public async void ToSearch()
    {
        using (SearchModrinth SearchTask = new SearchModrinth())
        {
            SearchItems = await ModItem.Create(await SearchTask.ToSearch(SearchContent));
        }
    }
    [RelayCommand]
    public void ToInstallMod(ModItem item)
    {
        IsPaneShow = true;
        DownloadPaneContent = new InstallModPane(item);
    }
}
internal partial class ModItem : BaseViewModel
{
    public static async Task<List<ModItem>> Create(ModrinthSearch info)
    {
        List<ModItem> modItems = new List<ModItem>();
        using (var httpClient = new HttpClient())
        {
            foreach (var item in info.Hits)
            {
                var i = new ModItem()
                {
                    Title = item.Title,
                    Description = item.Description,
                    ID = item.ProjectId,
                    time = item.DateCreated,
                };
                foreach (var v in item.Versions)
                {
                    try
                    {
                        i.SupportVersions.Add(new Version(v));
                    }
                    catch (FormatException)
                    {
                        continue;
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                }
                using (HttpResponseMessage response = await httpClient.GetAsync(info.Hits[0].IconUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                        i.Icon = new Bitmap(stream);
                }
                modItems.Add(i);
            }
        }
        return modItems;
    }
    public string Title { get; set; }
    public Bitmap Icon { get; set; }
    public string Description { get; set; }
    public string ID { get; set; }
    public List<Version> SupportVersions { get; set; } = new List<Version>();
    public DateTime time { get; set; }
}
