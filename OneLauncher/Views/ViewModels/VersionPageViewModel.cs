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
        if (a.modType.IsNeoForge || a.modType.IsFabric)
            IsMod = true;
        switch (a.preferencesLaunchMode.LaunchModType)
        {
            case ModEnum.none:
                IsOriginalLaunchMode = true;
                VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/basic.png")));
                break;
            case ModEnum.fabric:
                IsFabricLaunchMode = true;
                VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/fabric.png")));
                break;
            case ModEnum.neoforge:
                IsNeoforgeLaunchMode = true;
                VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/neoforge.png")));
                break;
            case ModEnum.forge:
                IsForgeLaunchMode = true;
                VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/forge.jpg")));
                break;
        }
    }
    int index;
    public bool IsMod {  get; set; } = false;
    [ObservableProperty]
    public Bitmap versionIcon;
    public UserVersion versionExp { get; set; }
    [ObservableProperty]
    public bool isOriginalLaunchMode;
    partial void OnIsOriginalLaunchModeChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        var version = Init.ConfigManger.config.VersionList[index];
        var prefs = version.preferencesLaunchMode;
        prefs.LaunchModType = ModEnum.none;
        version.preferencesLaunchMode = prefs;
        Init.ConfigManger.config.VersionList[index] = version;
        Init.ConfigManger.Save();
        VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/basic.png")));
    }
    [ObservableProperty]
    public bool isFabricLaunchMode;
    partial void OnIsFabricLaunchModeChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        var version = Init.ConfigManger.config.VersionList[index];
        var prefs = version.preferencesLaunchMode;
        prefs.LaunchModType = ModEnum.fabric;
        version.preferencesLaunchMode = prefs;
        Init.ConfigManger.config.VersionList[index] = version;
        Init.ConfigManger.Save();
        VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/fabric.png")));
    }
    [ObservableProperty]
    public bool isNeoforgeLaunchMode;
    partial void OnIsNeoforgeLaunchModeChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        var version = Init.ConfigManger.config.VersionList[index];
        var prefs = version.preferencesLaunchMode; 
        prefs.LaunchModType = ModEnum.neoforge; 
        version.preferencesLaunchMode = prefs; 
        Init.ConfigManger.config.VersionList[index] = version; 
        Init.ConfigManger.Save();
        VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/neoforge.png")));
    }
    [ObservableProperty]
    public bool isForgeLaunchMode;
    partial void OnIsForgeLaunchModeChanged(bool value)
    {
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        var version = Init.ConfigManger.config.VersionList[index];
        var prefs = version.preferencesLaunchMode;
        prefs.LaunchModType = ModEnum.forge;
        version.preferencesLaunchMode = prefs;
        Init.ConfigManger.config.VersionList[index] = version;
        Init.ConfigManger.Save();
        VersionIcon = new Bitmap(AssetLoader.Open(new Uri("avares://OneLauncher/Assets/Imgs/forge.jpg")));
    }
    [ObservableProperty]
    public bool isUseDebugModLaunch;
    [RelayCommand]
    public void LaunchGame()
    {
        //Views.version.EasyGameLauncher(versionExp,IsUseDebugModLaunch);
    }
    [RelayCommand]
    public async Task PinToDesktop()
    {
        
        //await File.WriteAllTextAsync(
        //    Path.Combine(
        //        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        //        $"启动{versionExp.VersionID}." + (Init.SystemType == SystemType.windows ? "bat" : "sh")),
        //    "cd " + (Init.SystemType == SystemType.windows ? "/D " : "") // 不同的操作系统切换工作目录可能需要加上 /D 参数
        //    + $"{Init.GameRootPath}{Environment.NewLine}java " + await new LaunchCommandBuilder
        //    (
        //        Init.GameRootPath,
        //        versionExp.VersionID,
        //        Init.ConfigManger.config.DefaultUserModel,
        //        versionExp.preferencesLaunchMode.LaunchModType,
        //        Init.SystemType,
        //        versionExp.IsVersionIsolation
        //    ).BuildCommand
        //    (
        //        OtherArgs: "-XX:+UseG1GC"
        //));
        //MainWindow.mainwindow.ShowFlyout("已创建启动脚本到桌面！");
    }
    [RelayCommand]
    public void PinInDashboard()
    {
        Init.ConfigManger.config.DefaultVersion = versionExp;
        Init.ConfigManger.Write(Init.ConfigManger.config);
        MainWindow.mainwindow.ShowFlyout($"已将{versionExp.VersionID}固定到仪表盘并设为默认版本！");
    }
    [RelayCommand]
    public void OpenModsFolder()
    {
        //string path = ((versionExp.IsVersionIsolation)
        //        ? Path.Combine(Init.GameRootPath, "versions", versionExp.VersionID, "mods")
        //        : Path.Combine(Init.GameRootPath, "mods"));
        //try
        //{
        //    Tools.OpenFolder(path);
        //}
        //catch (OlanException ex)
        //{
        //    OlanExceptionWorker.ForOlanException(ex,
        //        () => OpenModsFolder());  
        //}
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
public enum SortingType
{
    AnTime_OldFront,
    AnTime_NewFront,
    AnVersion_OldFront,
    AnVersion_NewFront,
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
    public async Task Import()
    {
        var topLevel = TopLevel.GetTopLevel(MainWindow.mainwindow);
        if (topLevel?.StorageProvider is { } storageProvider && storageProvider.CanOpen)
        {
            var mrpackFileType = new FilePickerFileType("Modrinth整合包文件")
            {
                Patterns = new[] { "*.mrpack" },
                MimeTypes = new[] { "application/mrpack" } 
            };

            var options = new FilePickerOpenOptions
            {
                Title = "选择 Modrinth Pack 文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { mrpackFileType },
            };
            var files = await storageProvider.OpenFilePickerAsync(options);
            var selectedFile = files.FirstOrDefault();

            if (files == null || !files.Any() || selectedFile == null)
                return;

            string filePath = selectedFile.Path.LocalPath;
            MainWindow.mainwindow.ShowFlyout("正在导入。。。（这可能需要较长时间）");
            await ModpackImporter.ImportFromMrpackAsync(filePath,Init.GameRootPath,CancellationToken.None);
            MainWindow.mainwindow.ShowFlyout("导入完成！");
            var tempVersoinList = new List<VersionItem>(Init.ConfigManger.config.VersionList.Count);
            for (int i = 0; i < Init.ConfigManger.config.VersionList.Count; i++)
            {
                tempVersoinList.Add(new VersionItem(
                    Init.ConfigManger.config.VersionList[i],
                    i
                    ));
            }
            VersionList = tempVersoinList;
        }
    }
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
            OlanExceptionWorker.ForOlanException(ex);
        }
    }
    private PowerPlayPane powerPlayGo = new PowerPlayPane();
    [RelayCommand]
    public void PowerPlay()
    {
        IsPaneShow = true;
        RefDownPane = powerPlayGo;
    }
}

