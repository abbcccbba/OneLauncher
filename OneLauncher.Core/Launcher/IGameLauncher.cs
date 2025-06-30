using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;
/* 开发中重构 */
public interface IGameLauncher
{
    event Action? GameStartedEvent;
    event Action? GameClosedEvent;
    event Action<string>? GameOutputEvent;
    // 最简单，通过实例ID启动游戏
    Task Play(string InstanceID);
    // 通过游戏数据实例启动游戏
    Task Play(GameData gameData, ServerInfo? serverInfo = null); // 调试窗口的显示是UI层的事情
    // 通过用户版本实例启动游戏
    Task Play(UserVersion userVersion, ServerInfo? serverInfo = null,bool useRootMode = false);
    Task Stop();
}
