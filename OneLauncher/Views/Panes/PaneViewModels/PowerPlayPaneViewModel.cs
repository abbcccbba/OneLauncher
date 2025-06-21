using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Net.ConnectToolPower;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;
public enum PowerPlayMode { Host, Join }
public partial class PowerPlayPaneViewModel : BaseViewModel
{
    ~PowerPlayPaneViewModel() => Stop();
    private PowerPlayPaneViewModel(IConnectService connectService,MCTPower mainPower)
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IsHostModeChecked) && IsHostModeChecked)
            {
                SetProperty(ref isJoinModeChecked, false, nameof(IsJoinModeChecked));
            }
            else if (e.PropertyName == nameof(IsJoinModeChecked) && IsJoinModeChecked)
            {
                SetProperty(ref isHostModeChecked, false, nameof(IsHostModeChecked));
            }
        };
        WeakReferenceMessenger.Default.Register<ApplicationClosingMessage>(this, (recipient, message) =>
        {
            mainPower.Dispose();
            Debug.WriteLine("核心已停止");
        });
        InstalledVersions = Init.ConfigManger.config.VersionList;
        mainPower.CoreLog += OnCoreLogReceived;
        this.mainPower = mainPower;
        this.connectService = connectService;
    }
    public static async Task<PowerPlayPaneViewModel> CreateAsync()
    {
        var mctPower = await MCTPower.InitializationAsync();
        return new PowerPlayPaneViewModel(new P2PMode(mctPower),mctPower);
    }
    private readonly MCTPower mainPower;
    private readonly IConnectService connectService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(CanStop))]
    private bool isConnected = false;

    [ObservableProperty]
    private bool isHostModeChecked = true;

    [ObservableProperty]
    private bool isJoinModeChecked = false;

    [ObservableProperty] private string hostRoomCode = string.Empty;
    [ObservableProperty] private string joinRoomCode = string.Empty;
    [ObservableProperty] private string joinPort = string.Empty;
    [ObservableProperty] private string localServerAddress = string.Empty;
    [ObservableProperty] private string logOutput = string.Empty;

    [ObservableProperty]
    private UserVersion selectedHostVersion;
    [ObservableProperty]
    public List<UserVersion> installedVersions;

    private readonly StringBuilder logBuilder = new();

    public bool CanStart => !isConnected;
    public bool CanStop => isConnected;
    
    //[RelayCommand]
    //private async Task Host()
    //{
    //    try
    //    {
    //        if (SelectedHostVersion == null)
    //        {
    //            // 异常处理修改: 抛出OlanException
    //            throw new OlanException("未能创建房间", "请先在下拉框中选择一个要进行联机的游戏版本。", OlanExceptionAction.Warning);
    //        }

    //        string p2pNodeName = "OLANNODE" + RandomNumberGenerator.GetInt32(100000, 1000000000);
    //        string combinedInfo = $"{p2pNodeName}:{SelectedHostVersion.VersionID}";
    //        string finalRoomCode = TextHelper.Base64Encode(combinedInfo);

    //        HostRoomCode = finalRoomCode;
    //        IsConnected = true;
    //        connectService.StartAsHost(p2pNodeName, null);
    //        _ = version.EasyGameLauncher(SelectedHostVersion);
    //    }
    //    catch (OlanException olanEx)
    //    {
    //        await OlanExceptionWorker.ForOlanException(olanEx);
    //    }
    //    catch (Exception ex)
    //    {
    //        await OlanExceptionWorker.ForUnknowException(ex);
    //    }
    //}

    //[RelayCommand]
    //private async Task JoinAndLaunch()
    //{
    //    try
    //    {
    //        string[] parts;
    //        string p2pNodeName;
    //        string versionId;
    //        bool isMCTMode = false;
    //        if (string.IsNullOrWhiteSpace(JoinRoomCode))
    //            throw new OlanException("输入无效", "必须输入房间码。", OlanExceptionAction.Warning);
    //        try
    //        {
    //            string decodedInfo = TextHelper.Base64Decode(JoinRoomCode);
    //            parts = decodedInfo.Split(':', 2);
    //            p2pNodeName= parts[0];
    //            versionId = parts[1];
    //        }
    //        // 可能代表了用户使用的是MCT格式的提示码，尝试兼容
    //        catch(FormatException)
    //        {
    //            if (!(JoinRoomCode.StartsWith("M") && JoinRoomCode.EndsWith("C")))
    //                throw new OlanException("输入无效","无法检测输入为OLANNODE格式或MCT格式的房间码");
    //            p2pNodeName = JoinRoomCode;
    //            versionId = "";
    //            isMCTMode = true;
    //        }

    //        // 数据源修改: 从全局配置中查找版本
    //        UserVersion? targetVersion = Init.ConfigManger.config.VersionList
    //            .FirstOrDefault(v => v.VersionID == versionId);

    //        if (targetVersion == null && !isMCTMode)
    //            throw new OlanException("加入失败", $"你没有安装房主所使用的游戏版本 ({versionId})。", OlanExceptionAction.Error);

    ////        if (!int.TryParse(JoinPort, out int port) || port is < 1 or > 65535)
    ////            throw new OlanException("输入无效", "端口号必须是 1-65535 之间的数字。", OlanExceptionAction.Warning);

    ////        // --- 业务逻辑 ---
            
    ////        int localPort = Tools.GetFreeTcpPort();
    ////        LocalServerAddress = $"127.0.0.1:{localPort}";
    ////        IsConnected = true;
    ////        mainPower.ConnectionEstablished += () => version.EasyGameLauncher(targetVersion, serverInfo: new ServerInfo
    ////        {
    ////            Ip = "127.0.0.1",
    ////            Port = localPort.ToString()
    ////        });
    ////        connectService.Join(null, p2pNodeName, localPort, port, null, null, null);
    ////        LogMessage($"P2P启动");
            
    ////    }
    ////    catch (OlanException olanEx)
    ////    {
    ////        await OlanExceptionWorker.ForOlanException(olanEx);
    ////        Stop(); 
    ////    }
    ////    catch (Exception ex)
    ////    {
    ////        await OlanExceptionWorker.ForUnknowException(ex);
    ////        Stop(); 
    ////    }
    ////}
    [RelayCommand]
    private Task CopyCode() =>
        TopLevel.GetTopLevel(MainWindow.mainwindow)?.Clipboard?.SetTextAsync(IsHostModeChecked ? HostRoomCode : JoinRoomCode);
    
    [RelayCommand]
    private void Stop()
    {
        mainPower.Dispose();
        IsConnected = false;
        HostRoomCode = string.Empty;
        LocalServerAddress = string.Empty;
        LogMessage("连接已断开。");
    }

    private void OnCoreLogReceived(string logMessage) => Dispatcher.UIThread.Post(() => LogMessage(logMessage));
    private void LogMessage(string message) { logBuilder.AppendLine(message); LogOutput = logBuilder.ToString(); }
}