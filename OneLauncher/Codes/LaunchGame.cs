using OneLauncher.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Codes;
public delegate void GameStarted();
public delegate void GameClosed();
internal class Game
{
    public event GameStarted GameStartedEvent;
    public event GameClosed GameClosedEvent;
    /// <param name="GameVersion">游戏版本</param>
    /// <param name="loginUserModel">以哪个用户模型启动游戏</param>
    /// <param name="GamePath">游戏基本路径</param>
    /// <returns></returns>
    public async Task LaunchGame(string GameVersion, UserModel loginUserModel, string GamePath = null)
    {
        using (Process process = new Process())
        {
            process.StartInfo.FileName = "java";
            process.StartInfo.Arguments =
                new LaunchCommandBuilder
                (
                    GamePath ?? Init.BasePath,
                    GameVersion,
                    loginUserModel,
                    Init.systemType
                ).BuildCommand
                (
                    string.Join
                    (
                        " ",
                        "-XX:+UseG1GC",
                        "-XX:+UnlockExperimentalVMOptions",
                        "-XX:-OmitStackTraceInFastThrow",
                        "-Xmn512m -Xmx4096m -XX:ParallelGCThreads=4",
                        "-Djdk.lang.Process.allowAmbiguousCommands=true",
                        "-Dlog4j2.formatMsgNoLookups=true",
                        "-Dfml.ignoreInvalidMinecraftCertificates=True",
                        "-Dfml.ignorePatchDiscrepancies=True",
                        "--enable-native-access=ALL-UNNAMED"
                    )
                );
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                Debug.WriteLine($"[OUT] {e.Data}"); // 输出到控制台
                if (Regex.IsMatch(
                    e.Data, @"\[Render thread/INFO\]") &&
                    e.Data.Contains("[LWJGL] [ThreadLocalUtil] Unsupported JNI version detected"))
                {
                    GameStartedEvent?.Invoke();
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                Debug.WriteLine($"[ERROR] {e.Data}"); // 输出到控制台
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        GameClosedEvent?.Invoke(); // 游戏关闭事件
    }
}

