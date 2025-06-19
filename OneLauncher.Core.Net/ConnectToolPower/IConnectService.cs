using System;
using System.Collections.Generic;
using System.Text;

namespace OneLauncher.Core.Net.ConnectToolPower;

public interface IConnectService
{
    void StartAsHost(
        string? nodeName, 
        string? token
        );
    void Join(
        string? nodeName, 
        string peerNodeName, 
        int? sourcePort,
        int destPort, 
        string? destIp, 
        string? appName, 
        string? token);
}
