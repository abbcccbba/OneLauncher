using OneLauncher.Core.Helper;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace OneLauncher.Core.Net.ConnectToolPower;

public class P2PMode : IConnectService
{
    // 这里就不搞什么花招了，防君子不防小人反编译一样看得到
    const string defaultToken = "17073157824633806511";
    const string defaultAppName = "OneLauncherConnentService";
    const string defaultDestIP = "127.0.0.1";
    private MCTPower mainPower;
    public P2PMode(MCTPower mainPower)
    {
        if (Init.systemType != SystemType.windows)
            throw new OlanException("无法初始化联机模块","你的操作系统不受支持");
        this.mainPower = mainPower;
    }

    public void Join(string? nodeName, string peerNodeName, int? sourcePort, int destPort, string? destIp, string? appName, string? token)
    {
        // 获取可用端口
        int localPort = sourcePort ?? Tools.GetFreeTcpPort();

        string localNodeName = nodeName ?? "OLANNODE" + RandomNumberGenerator.GetInt32(100000, 1000000000).ToString();
        string finalAppName = (appName ?? defaultAppName) + localPort;

        var args = new StringBuilder();
        args.Append($"-node \"{localNodeName}\" ");
        args.Append($"-appname \"{finalAppName}\" ");
        args.Append($"-peernode \"{peerNodeName}\" ");
        args.Append($"-dstip \"{destIp ?? defaultDestIP}\" ");
        args.Append($"-dstport {destPort} ");
        args.Append($"-srcport {localPort} ");
        args.Append($"-token \"{token ?? defaultToken}\" ");

        mainPower.LaunchCore(args.ToString());
    }

    public void StartAsHost(string? nodeName, string? token)
    {
        string? node = nodeName;
        if (nodeName == null)
                node = "OLANNODE" + RandomNumberGenerator.GetInt32(100000,1000000000).ToString();
        mainPower.LaunchCore($"-node \"{node}\" -token \"{token ?? defaultToken}\"");
    }
}
