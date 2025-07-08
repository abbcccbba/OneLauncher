using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Mod;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;
internal partial class InstanceModItem : BaseViewModel
{
    private ModInfo Info { get; set; }
    private Bitmap Icon { get; set; }

    public InstanceModItem(ModInfo info)
    {
        Info = info;
        if(info.Icon != null)
            using (var stream = new MemoryStream(info.Icon))
                Icon = new Bitmap(stream);
        else
            Icon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/basic.png"))); // 默认图标
    }
}
internal partial class GameDataModsManagerPaneViewModel : BaseViewModel
{
    // 存放包装后的 Mod 列表
    [ObservableProperty]
    private List<InstanceModItem> mods;

    // 假设通过构造函数注入了实例信息
    private readonly GameData _gameData;
    private readonly InstanceModService _modService;

    public GameDataModsManagerPaneViewModel(GameData gameData)
    {
#if DEBUG
        if(Design.IsDesignMode)
        {
            // 设计时数据
            Mods = new List<InstanceModItem>
            {
                new InstanceModItem(new ModInfo { Id = "mod1", Name = "Mod 1", Version = "1.0", Description = "这是一个测试Mod", IsEnabled = true }),
                new InstanceModItem(new ModInfo { Id = "mod2", Name = "Mod 2", Version = "1.1", Description = "这是另一个测试Mod", IsEnabled = false }),
            };
        }
        else
#endif
        {
            _gameData = gameData;
            _modService = new InstanceModService(gameData);
            _ = RefreshModsAsync(); // 初始化时加载
        }
    }

    [RelayCommand]
    private async Task RefreshModsAsync()
    {
        Mods = (await _modService.GetModsAsync()).Select(x => new InstanceModItem(x)).ToList();
    }

    [RelayCommand]
    private void OpenModsFolder()
    {
        // 你可以复用你的 Tools.OpenFolder 方法
        // Tools.OpenFolder(Path.Combine(_gameData.InstancePath, "mods"));
    }
}