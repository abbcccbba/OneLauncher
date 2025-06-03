﻿using Avalonia;
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
                        (string.IsNullOrEmpty(item.IconUrl))
                        ? "https://img.icons8.com/carbon-copy/100/border-none.png" : item.IconUrl)),
                    // 默认初始化 SupportModType
                    SupportModType = new ModType() // 初始化为默认值（bool为false）
                };

                // 获取 SupportModType 的副本
                ModType currentModType = i.SupportModType;

                foreach (var j in item.Categories)
                {
                    if (j == "fabric")
                        currentModType.IsFabric = true; // 修改副本
                    if (j == "neoforge")
                        currentModType.IsNeoForge = true; // 修改副本
                }

                // 将修改后的副本赋值回原属性
                i.SupportModType = currentModType;

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
    public ModType SupportModType { get; set; }
    public DateTime time { get; set; }
}

internal partial class ModsBrowserViewModel : BaseViewModel
{
    public ModsBrowserViewModel()
    {
        // 初始搜索显示热门结果
        ToSearch();
    }
    [ObservableProperty]
    public bool _IsPaneShow = false;
    [ObservableProperty]
    public UserControl _InstallModPaneContent = new UserControl();
    [ObservableProperty]
    public string _SearchContent = string.Empty;
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
