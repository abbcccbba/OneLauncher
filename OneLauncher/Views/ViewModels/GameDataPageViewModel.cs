using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal partial class GameDataItem : BaseViewModel
{
    public GameDataItem(GameData gameData)
    {
        data = gameData;
//#if DEBUG
//        if (Design.IsDesignMode)
//        {

//        }
//#endif
        #region 定义图标
        if (!string.IsNullOrEmpty(data.CustomIconPath) && File.Exists(data.CustomIconPath))
        {
            try
            {
                Icon = new Bitmap(data.CustomIconPath);
                return;
            }
            catch (Exception) {} // 若无法加载图标，回退到默认图标
        }
        string iconUri = data.ModLoader switch
        {
            ModEnum.fabric => "avares://OneLauncher/Assets/Imgs/fabric.png",
            ModEnum.neoforge => "avares://OneLauncher/Assets/Imgs/neoforge.png",
            ModEnum.forge => "avares://OneLauncher/Assets/Imgs/forge.jpg",
            _ => "avares://OneLauncher/Assets/Imgs/basic.png", 
        };
        Icon = new Bitmap(AssetLoader.Open(new Uri(iconUri)));
        #endregion
        
    }
    public Bitmap Icon { get; set; }
    public GameData data { get; set; }
    string Type { get; set; }
}
internal partial class GameDataPageViewModel : BaseViewModel
{
    [ObservableProperty] public List<GameDataItem> gameDataList = new();
    [ObservableProperty] public string type;
    [ObservableProperty] public UserControl paneContent;
    [ObservableProperty] public bool isPaneShow;
    // 刷新列表
    public void RefList()
    {
        GameDataList = Init.GameDataManger.AllGameData.Select(x => new GameDataItem(x)).ToList();
    }
    // 修改特定的游戏数据实例
    public void UpdateGameData(GameData updatedData)
    {
        var index = Init.GameDataManger.AllGameData.FindIndex(gd => gd.InstanceId == updatedData.InstanceId);
        if (index != -1)
        {
            Init.GameDataManger.AllGameData[index] = updatedData;
        }
    }
    public GameDataPageViewModel()
    {
        
#if DEBUG
        if (Design.IsDesignMode)
        {
            Init.GameRootPath = "AVALONIA";
            GameDataList.Add(new GameDataItem(new GameData()
            {
                CreationTime = DateTime.Now,
                InstanceId = "1.21.5",
                Name = "设计时游戏数据-1"
            }));
            GameDataList.Add(new GameDataItem(new GameData()
            {
                CreationTime = DateTime.Now.AddDays(-5),
                InstanceId = "1.20.4",
                Name = "设计时游戏数据-2"
            }));
        }
        else
#endif
            GameDataList = Init.GameDataManger.AllGameData.Select(x => new GameDataItem(x)).ToList();
    }
    [RelayCommand]
    public void Launch(GameData gameData)
    {
        version.EasyGameLauncher(gameData);
    }
    [RelayCommand]
    public void NewGameData()
    {
        IsPaneShow = true;
        PaneContent = new NewGameDataPane();
    }
    [RelayCommand]
    public void ShowEditPane(GameData data)
    {
        IsPaneShow = true;
        PaneContent = new EditGameDataPane(data);
    }
    [RelayCommand]
    public async Task DeleteInstance(GameData data)
    {
        // 未来可以加一个对话框确认
        await Init.GameDataManger.RemoveGameDataAsync(data);

        try
        {
            if (Directory.Exists(data.InstancePath))
                Directory.Delete(data.InstancePath, true);
        }
        catch (Exception ex)
        {
            MainWindow.mainwindow.ShowFlyout($"删除文件夹失败: {ex.Message}", true);
        }

        RefList(); // 重新加载列表
        MainWindow.mainwindow.ShowFlyout($"已删除: {data.Name}");
    }
}
