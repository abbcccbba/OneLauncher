using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;
/* 开发中重构 */
internal interface IGameLauncher
{
    // 最简单，通过实例ID或版本ID启动游戏，可以选择根启动；通过传参标识
    Task Play(string InstanceOrVersionId,bool isVersionLauncherMode);
    // 通过游戏数据实例启动游戏
    Task Play(GameData gameData, ServerInfo? serverInfo = null); // 调试窗口的显示是UI层的事情
    // 通过用户版本实例启动游戏
    Task Play(UserVersion userVersion, ServerInfo? serverInfo = null,bool useRootMode = false);
    Task Stop();
}
