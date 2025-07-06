using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Codes;
using OneLauncher.Core.Compatible.ImportPCL2Version;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.ImportPCL2Version;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Launcher;
using OneLauncher.Core.Minecraft.Server;
using OneLauncher.Views.Panes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels;
internal class VersionPageClosePaneControlMessage { public bool value = false; }
internal partial class VersionItem : BaseViewModel
{
    /// <param Name="a">UserVersion实例</param>
    /// <param Name="IndexInInit">UserVsersion实例在整个Init.ConfigManager.config.VersionList中的索引值</param>
    public VersionItem(UserVersion a)
    {
        versionExp = a;
    }
    public UserVersion versionExp { get; set; }
    [RelayCommand]
    public async Task LaunchGame()
    {
        _=new GameLauncher().Play(versionExp, useRootMode: true);
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
    private readonly DBManager _dBManager;
    private void RefList()
    {
        //var tempVersoinList = new List<VersionItem>(_dBManager.Data.VersionList.Count);
        //for (int i = 0; i < tempVersoinList.Count; i++)
        //{
        //    tempVersoinList.Add(new VersionItem(
        //        Init.ConfigManger.Data.VersionList[i], i));
        //}
        VersionList = _dBManager.Data.VersionList.Select(x => new VersionItem(x)).ToList();
    }
    public VersionPageViewModel(DBManager dBManager)
    {
        this._dBManager = dBManager;
#if DEBUG
        // 设计时数据
        if (Design.IsDesignMode)
        {
            VersionList = new List<VersionItem>()
            {
                new VersionItem(new UserVersion() 
                {
                    VersionID="1.21.5",
                    AddTime=DateTime.Now
                })
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
            WeakReferenceMessenger.Default.Register<VersionPageClosePaneControlMessage>(this, (re, message) =>IsPaneShow = message.value);
        } 
    }
    [RelayCommand]
    protected void PageLoaded()
    {
        try
        {
            RefList();
        }
        catch (NullReferenceException ex)
        {
            throw new OlanException(
                "内部异常",
                "配置文件特定部分版本列表部分为空，这可能是新版和旧版配置文件不兼容导致的",
                OlanExceptionAction.FatalError,
                ex,
               () =>
               {
                   File.Delete(Path.Combine(Init.BasePath, "config.json"));
                   Init.Initialize().GetAwaiter().GetResult();
               });
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
        _dBManager.Data.VersionList = VersionList.Select(x => x.versionExp).ToList();
        _=_dBManager.Save();
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
    public async Task ImportVersionByPCL2()
    {
        try 
        { 
            var topLevel = TopLevel.GetTopLevel(MainWindow.mainwindow);
            if (topLevel?.StorageProvider is { } storageProvider && storageProvider.CanOpen)
            {
                var options = new FolderPickerOpenOptions
                {
                    Title = "选择你的PCL2版本文件夹",
                    AllowMultiple = false,
                };
                var files = await storageProvider.OpenFolderPickerAsync(options);
                var selectedFile = files.FirstOrDefault();

                if (files == null || !files.Any() || selectedFile == null)
                    return;

                string path = selectedFile.Path.LocalPath;
                if(!File.Exists(Path.Combine(path,"PCL", "Setup.ini")))
                {
                    WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("这不是有效的PCL版本文件夹", NotificationType.Warning,"导入失败"));
                    return;
                }
                WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("正在导入。。。（这可能需要较长时间）"));
                await new PCL2Importer(new Progress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)>(p =>
                {
                    WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage(
                        $"[{p.DownedFiles}/{p.AllFiles}] 操作:{p.DowingFileName}",
                        NotificationType.Information,
                        p.Title switch
                        {
                            DownProgress.Meta => "正在分析PCL2实例",
                            DownProgress.Done => "导入完成",
                            _ => "操作中"
                        } + " - 正在导入"));
                    Debug.WriteLine($"Titli:{p.Title}\nAll:{p.AllFiles},Down:{p.DownedFiles}\nOutput:\n{p.DowingFileName}");
                })).ImportAsync(path);
                WeakReferenceMessenger.Default.Send(new MainWindowShowFlyoutMessage("导入完成！", NotificationType.Success));
                RefList();
            }
        }
        catch(OlanException ex)
        {
            await OlanExceptionWorker.ForOlanException(ex,() => _=ImportVersionByPCL2());
        }
        catch (Exception ex)
        {
            await OlanExceptionWorker.ForUnknowException(ex,() => _=ImportVersionByPCL2());
        }
    }
}

