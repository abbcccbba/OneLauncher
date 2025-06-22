using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Core.Mod.ModPack;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal partial class VersionItem : BaseViewModel
{
    /// <param Name="a">UserVersion实例</param>
    /// <param Name="IndexInInit">UserVsersion实例在整个Init.ConfigManger.config.VersionList中的索引值</param>
    public VersionItem(UserVersion a,int IndexInInit)
    {
        versionExp = a;
        index = IndexInInit;
    }
    int index;
    public UserVersion versionExp { get; set; }
    [RelayCommand]
    public async Task LaunchGame()
    {
        GameData gameData = await Init.GameDataManger.GetOrCreateInstanceAsync(versionExp);
        _ =version.EasyGameLauncher(gameData);
    }
    [RelayCommand]
    public void ReadMoreInformations()
    {

    }
    [RelayCommand]
    public void OpenServerFolder()
    {
        string path = Path.Combine(Init.GameRootPath,"versions",versionExp.VersionID,"servers");
        if (!Directory.Exists(path))
            OlanExceptionWorker.ForOlanException(
                new OlanException("无法打开服务端文件夹","服务端尚未初始化",OlanExceptionAction.Error));
        else
            Tools.OpenFolder(path);
    }
}

internal partial class VersionPageViewModel : BaseViewModel
{
    private void RefList()
    {
        var tempVersoinList = new List<VersionItem>(Init.ConfigManger.config.VersionList.Count);
        for (int i = 0; i < tempVersoinList.Count; i++)
        {
            tempVersoinList.Add(new VersionItem(
                Init.ConfigManger.config.VersionList[i], i));
        }
        VersionList = tempVersoinList;
    }
    public VersionPageViewModel()
    {
#if DEBUG
        // 设计时数据
        if (Design.IsDesignMode)
        {
            VersionList = new List<VersionItem>()
            {
                new VersionItem(new UserVersion() 
                {
                    VersionID="1.21.5",
                    AddTime=DateTime.Now,
                    preferencesLaunchMode = new PreferencesLaunchMode(){LaunchModType = ModEnum.neoforge}
                },0)
            };
        }
        else
#endif
        {
            try
            {
                RefList();
            }
            catch (NullReferenceException ex)
            {
                throw new OlanException(
                    "内部异常",
                    "配置文件特定部分版本部分为空，这可能是新版和旧版配置文件不兼容导致的",
                    OlanExceptionAction.FatalError,
                    ex,
                   () => {
                       File.Delete(Path.Combine(Init.BasePath, "config.json"));
                       Init.Initialize();
                   });
            }
        } 
    }
    [ObservableProperty]
    public List<VersionItem> _versionList;
    [ObservableProperty]
    public UserControl _refDownPane;
    [ObservableProperty]
    public bool _isPaneShow;
    [RelayCommand]
    public void Sorting(SortingType type)
    {
        List<VersionItem> orderedList = type switch
        {
            SortingType.AnTime_OldFront => VersionList.OrderBy(x => x.versionExp.AddTime).ToList(),
            SortingType.AnTime_NewFront => VersionList.OrderByDescending(x => x.versionExp.AddTime).ToList(),
            SortingType.AnVersion_OldFront => VersionList.OrderBy(x => new Version(x.versionExp.VersionID)).ToList(),
            SortingType.AnVersion_NewFront => VersionList.OrderByDescending(x => new Version(x.versionExp.VersionID)).ToList(),
            _ => VersionList // 默认不排序
        };

        VersionList = orderedList; 
        Init.ConfigManger.config.VersionList = VersionList.Select(x => x.versionExp).ToList();
        Init.ConfigManger.Save();
    }
    [RelayCommand]
    public async Task OpenServer(UserVersion versionExp)
    {
        try
        {
            // 去尝试读取，判断这个服务端版本是否启用了版本隔离
            bool IsVI = true;
            if (Directory.Exists(Path.Combine(Init.GameRootPath, "versoins", versionExp.VersionID, "servers")))
                IsVI = true;
            else if (Directory.Exists(Path.Combine(Init.GameRootPath, "servers")))
                IsVI = false;
            string versionPath = Path.Combine(Init.GameRootPath, "versions", versionExp.VersionID);
            // 判断服务端是否已经完成初始化
            if (!File.Exists(Path.Combine(versionPath, "server.jar")))
            {
                IsPaneShow = true;
                RefDownPane = new InitServerPane(versionExp.VersionID);
            }
            else       
                MinecraftServerManger.Run(versionPath,
                    // 读取源文件获取Java版本
                    (await JsonNode.ParseAsync(
                        File.OpenRead(
                            Path.Combine(versionPath, $"{versionExp.VersionID}.json"))))
                                ?["javaVersion"]
                                ?["majorVersion"]
                                ?.GetValue<int>()
                                ?? Tools.ForNullJavaVersion(versionExp.VersionID)
                                , IsVI);
            
        }
        catch (OlanException ex)
        {
            await OlanExceptionWorker.ForOlanException(ex);
        }
    }
    [RelayCommand]
    public void GoToDownload()
    {
        MainWindow.mainwindow.MainPageControl(MainWindow.MainPage.DownloadPage);
    }
}

