using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;
public partial class LaunchCommandBuilder
{
    private string BuildGameArgs(bool useRootLaunch)
    {
        var userModel = Init.AccountManager.GetUser(gameData.DefaultUserModelID);
        string serverArgs = string.Empty;
        if (serverInfo != null)
        {
            serverArgs = $"--server \"{((ServerInfo)serverInfo).Ip}\" --port\"{((ServerInfo)serverInfo).Port}\" ";
            if (new Version(version) > new Version("1.20"))
            {
                serverArgs += $"--quickPlayMultiplayer \"{serverInfo.Value.Ip}:{serverInfo.Value.Port}\" ";
            }
        }
        string GameArgs =
            $"--username \"{userModel.Name}\" " +
            $"--version \"{version}\" " +
            $"--gameDir \"{(useRootLaunch ? basePath : gameData.InstancePath)}\" " +
            $"--assetsDir \"{(Path.Combine(basePath, "assets"))}\" " +
            // 1.7版本及以上启用新用户验证机制
            (new Version(version) > new Version("1.7") ?
            $"--assetIndex \"{versionInfo.GetAssetIndexVersion()}\" " +
            $"--uuid \"{userModel.uuid}\" " +
            $"--accessToken \"{userModel.AccessToken.ToString()}\" " +
            $"--userType \"{(userModel.IsMsaUser ? "msa" : "legacy")}\" " +
            $"--versionType \"OneLauncher\" " +
            serverArgs +
            "--userProperties {} "
            // 针对旧版用户验证机制
            : $"--session \"{userModel.AccessToken}\" ");
        if (modType == ModEnum.neoforge || modType == ModEnum.forge)
            GameArgs +=
                string.Join(" ", neoForgeParser.info.Arguments.Game);
        Debug.WriteLine(GameArgs);
        return GameArgs;
    }
}
