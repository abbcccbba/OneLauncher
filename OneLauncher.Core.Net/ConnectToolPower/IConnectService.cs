using System;
using System.Collections.Generic;
using System.Text;

namespace OneLauncher.Core.Net.ConnectToolPower;

public interface IConnectService : IDisposable
{
    Task StartAsHost(
        string? nodeName, 
        string? token
        );
    Task Join(
        string? nodeName, 
        string peerNodeName, 
        int? sourcePort,
        int destPort, 
        string? destIp, 
        string? appName, 
        string? token);
}
