using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Launcher.Strategys;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Launcher;
public partial class LaunchCommandBuilder
{
    private IEnumerable<string> BuildGameArgs(string gamePath,IModStrategy? strategy)
    {
        var userModel = Init.AccountManager.GetUser(gameData.DefaultUserModelID);
        List<string> Args = new List<string>();
        if (serverInfo != null)
        {
            Args.Add($"--server");
            Args.Add($"\"{((ServerInfo)serverInfo).Ip}\"");
            Args.Add($"--port");
            Args.Add($"\"{((ServerInfo)serverInfo).Port}\"");
            if (new Version(version) > new Version("1.20"))
            {
                Args.Add($"--quickPlayMultiplayer");
                Args.Add($"\"{serverInfo.Value.Ip}:{serverInfo.Value.Port}\"");
            }
        }
        // 添加基本的
        Args.AddRange([
            $"--username \"{userModel.Name}\"",
            $"--version \"{version}\"",
            $"--gameDir \"{gamePath}\"",
            $"--assetsDir \"{(Path.Combine(basePath, "assets"))}\""
            ]);
        if(new Version(version) > new Version("1.7"))
            Args.AddRange([
                $"--assetIndex \"{versionInfo.GetAssetIndexVersion()}\"",
                $"--uuid \"{userModel.uuid}\"",
                $"--accessToken \"{userModel.AccessToken.ToString()}\"",
                $"--userType \"{(userModel.IsMsaUser ? "msa" : "legacy")}\"",
                $"--versionType \"OneLauncher\"",
                "--userProperties {}"
            ]);
        else
            Args.Add($"--session \"{userModel.AccessToken}\"");
        if(strategy != null)
            Args.AddRange(strategy.GetAdditionalGameArgs());

        return Args;
    }
}
