using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core;
using OneLauncher.Core.Modrinth;
using OneLauncher.Core.Modrinth.JsonModelSearch;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OneLauncher.Views.ViewModels;
internal partial class ModItem : BaseViewModel
{
    public static List<ModItem> Create(ModrinthSearch info)
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
                    SupportVersions = Tools.McVsFilter(item.Versions),
                    IconUrl = new Uri((
                    // 处理某个缺德作者不加图标的情况
                    (item.IconUrl == string.Empty)
                    ? "https://img.icons8.com/carbon-copy/100/border-none.png" : item.IconUrl)),
                };
                modItems.Add(i);
            }
        }
        return modItems;
    }
    public string Title { get; set; }
    public Uri IconUrl { get; set; }
    public string Description { get; set; }
    public string ID { get; set; }
    public List<string> SupportVersions { get; set; } = new List<string>();
    public DateTime time { get; set; }
}

internal partial class ModsBrowserViewModel : BaseViewModel
{
    [ObservableProperty]
    public bool _IsPaneShow = false;
    [ObservableProperty]
    public UserControl _InstallModPaneContent = new UserControl();
    [ObservableProperty]
    public string _SearchContent;
    [ObservableProperty]
    public List<ModItem> _SearchItems;
    [RelayCommand]
    public async Task ToSearch()
    {
        using (SearchModrinth SearchTask = new SearchModrinth())
        {
            SearchItems = ModItem.Create(await SearchTask.ToSearch(SearchContent));
        }
    }
    [RelayCommand]
    public void ToInstallMod(ModItem item)
    {
        IsPaneShow = true;
        InstallModPaneContent = new InstallModPane(item);
    }
}
