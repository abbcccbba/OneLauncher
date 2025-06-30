using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace OneLauncher.Core.Launcher;
public class GameLauncher : IGameLauncher, IDisposable
{
    public event Action? GameStartedEvent;
    public event Action? GameClosedEvent;
    public event Action<string>? GameOutputEvent;

    private Process? _gameProcess;
    public CancellationToken CancellationToken = CancellationToken.None; // 外部可以设置

    /// <summary>
    /// 根据提供的 GameData 启动游戏。
    /// </summary>
    public async Task Play(GameData gameData, ServerInfo? serverInfo = null)
    {
        try
        {
            // 刷新令牌
            Task refreshTokenTask =  Init.AccountManager.GetUser(gameData.DefaultUserModelID).IntelligentLogin(Init.MMA);

            var commandBuilder = new LaunchCommandBuilder(
                Init.GameRootPath,
                gameData,
                serverInfo
            );

            // 写进文件
            string launchArgumentsPath = Path.GetTempFileName();
            string arguments = await commandBuilder.BuildCommand(
                Init.ConfigManger.Data.OlanSettings.MinecraftJvmArguments.ToString(commandBuilder.versionInfo.GetJavaVersion()),
                useRootLaunch: false // 根据你的需要调整
            );

            // 针对Windows平台的路径特殊处理
#if WINDOWS
            arguments = arguments.Replace("\\", @"\\");
#endif
            await File.WriteAllTextAsync(launchArgumentsPath, arguments, _cancellationTokenSource.Token);

            // 4. 配置并启动游戏进程
            _gameProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = commandBuilder.GetJavaPath(),
                    Arguments = $"@{launchArgumentsPath}", // 从文件读取参数
                    WorkingDirectory = Init.GameRootPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true 
            };

            _gameProcess.OutputDataReceived += OnOutputDataReceived;
            _gameProcess.ErrorDataReceived += OnErrorDataReceived;
            _gameProcess.Exited += OnGameProcessExited;

            _gameProcess.Start();
            _gameProcess.BeginOutputReadLine();
            _gameProcess.BeginErrorReadLine();

            await _gameProcess.WaitForExitAsync(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            GameOutputEvent?.Invoke("[INFO] Game launch was cancelled." + Environment.NewLine);
        }
        finally
        {
            // 确保资源得到释放
            CleanupAfterGame();
        }
    }

    // 实现接口中的其他 Play 方法
    public Task Play(string InstanceOrVersionId, bool isVersionLauncherMode)
    {
        // 你需要在这里实现通过ID查找 GameData 或 UserVersion 的逻辑
        // 然后调用 Play(gameData)
        throw new NotImplementedException();
    }

    public Task Play(UserVersion userVersion, ServerInfo? serverInfo = null, bool useRootMode = false)
    {
        // 你需要在这里实现通过 UserVersion 查找或创建 GameData 的逻辑
        // 然后调用 Play(gameData)
        throw new NotImplementedException();
    }

    /// <summary>
    /// 强制停止正在运行的游戏进程。
    /// </summary>
    public Task Stop()
    {
        if (_gameProcess != null && !_gameProcess.HasExited)
        {
            _cancellationTokenSource?.Cancel(); // 取消等待
            _gameProcess.Kill(true); // 强制结束进程树
            GameOutputEvent?.Invoke("[INFO] Game process has been terminated by the user." + Environment.NewLine);
        }
        return Task.CompletedTask;
    }

    #region 事件处理
    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;
        GameOutputEvent?.Invoke($"[STDOUT] {e.Data}{Environment.NewLine}");
        // 在游戏启动后触发事件
        if (e.Data.Contains("Backend library: LWJGL version"))
        {
            GameStartedEvent?.Invoke();
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;
        GameOutputEvent?.Invoke($"[ERROR] {e.Data}{Environment.NewLine}");

        // 可以在这里添加更复杂的错误识别和处理逻辑
    }

    private void OnGameProcessExited(object? sender, EventArgs e)
    {
        if (_gameProcess != null)
        {
            GameOutputEvent?.Invoke($"[INFO] Game process exited with code: {_gameProcess.ExitCode}{Environment.NewLine}");
        }

        GameClosedEvent?.Invoke();
    }
    #endregion

    /// <summary>
    /// 清理游戏进程相关的资源。
    /// </summary>
    private void CleanupAfterGame()
    {
        if (_gameProcess != null)
        {
            _gameProcess.OutputDataReceived -= OnOutputDataReceived;
            _gameProcess.ErrorDataReceived -= OnErrorDataReceived;
            _gameProcess.Exited -= OnGameProcessExited;
            _gameProcess.Dispose();
            _gameProcess = null;
        }

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public void Dispose()
    {
        Stop().Wait(); // 等待停止操作完成
        CleanupAfterGame();
    }
}