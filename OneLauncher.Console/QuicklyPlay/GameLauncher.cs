using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Console.QuicklyPlay;

internal class GameLauncher
{
    public static async Task Launch(string InstanceId)
    {
        //#region 初始化基本游戏构建类
        //GameData launchInstance = Init.GameDataManger.AllGameData.FirstOrDefault(x => x.InstanceId == InstanceId);
        //if (launchInstance == null)
        //    throw new Exception("无法找到您的游戏实例");
        //await Init.AccountManager.GetUser(launchInstance.DefaultUserModelID).IntelligentLogin(Init.MMA);
        //var Builder = new LaunchCommandBuilder
        //                (
        //                    Init.GameRootPath,
        //                    launchInstance,
        //                    null
        //                );
        //#endregion

        //using (Process process = new Process())
        //{
        //    process.StartInfo = new ProcessStartInfo()
        //    {
        //        Arguments =
        //        await Builder.BuildCommand(
        //            Init.ConfigManger.config.OlanSettings.MinecraftJvmArguments.ToString(Builder.versionInfo.GetJavaVersion())),
        //        FileName = Builder.GetJavaPath(),
        //        WorkingDirectory = Init.GameRootPath,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true,
        //        StandardOutputEncoding = Encoding.UTF8,
        //        StandardErrorEncoding = Encoding.UTF8
        //    };
        //    process.OutputDataReceived += (s,e) => System.Console.WriteLine(e.Data);
        //    process.ErrorDataReceived += (s, e) => System.Console.WriteLine(e.Data);
        //    process.Start();
        //    process.BeginOutputReadLine();
        //    process.BeginErrorReadLine();
        //    await process.WaitForExitAsync();
        //}  
    }
}
