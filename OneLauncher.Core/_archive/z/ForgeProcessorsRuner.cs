//using OneLauncher.Core.Helper;
//using OneLauncher.Core.ModLoader.forgeseries.JsonModels; 
//using System.Diagnostics;
//using System.IO.Compression;
//using System.Text;
//using System.Text.Json;

//namespace OneLauncher.Core.Mod.ModLoader.forgeseries;

//// 这是你现有类的另一部分，共享所有成员
//public partial class ForgeSeriesInstallTasker
//{
//    public async Task RunForgeProcessors(
//        ForgeSeriesInstallProfileRoot profile,
//        string mainJarPath,
//        //string javaPath,
//        string clientLzmaPath,
//        SystemType osType,
//        CancellationToken token)
//    {
//        // 1. 动态构建占位符字典
//        var placeholderDict = BuildForgePlaceholderDictionary(profile, this.librariesPath, this.gamePath, mainJarPath, clientLzmaPath);

//        // 2. 准备所有需要运行的处理器进程
//        var processorsToRun = new List<Process>();

//        // 过滤出所有 client 端的处理器
//        var clientProcessors = profile.Processors
//            .Where(p => p.Sides == null || p.Sides.Contains("client"))
//            .ToList();

//        int alls = clientProcessors.Count;
//        int dones = 0;

//        ProcessorsOutEvent?.Invoke(alls, 0, "正在准备安装处理器...");

//        foreach (var procDef in clientProcessors)
//        {
//            token.ThrowIfCancellationRequested();

//            // a. 构建 Classpath
//            var cpBuilder = new StringBuilder();
//            foreach (var cpEntry in procDef.Classpath)
//            {
//                cpBuilder.Append(Tools.MavenToPath(this.librariesPath, cpEntry));
//                cpBuilder.Append(osType == SystemType.windows ? ";" : ":");
//            }
//            string cpArgs = $"-cp \"{cpBuilder.ToString().TrimEnd(';', ':')}\"";

//            // b. 找到主类
//            string mainClass = await GetMainClassFromJar(Tools.MavenToPath(this.librariesPath, procDef.Jar), procDef.Jar);
//            if (string.IsNullOrEmpty(mainClass))
//            {
//                // 如果找不到主类，直接抛出异常中断安装
//                throw new InvalidOperationException($"处理器 {procDef.Jar} 在 MANIFEST.MF 中未定义 Main-Class 或无法读取。");
//            }

//            // c. 解析标准参数，并替换占位符
//            string stdArgs = BuildProcessorArguments(procDef.Args, placeholderDict, this.librariesPath, alls, dones + 1);

//            // d. 创建 Process 对象
//            var process = new Process
//            {
//                StartInfo = new ProcessStartInfo
//                {
//                    FileName = javaPath,
//                    Arguments = $"{cpArgs} {mainClass} {stdArgs}",
//                    RedirectStandardOutput = true,
//                    RedirectStandardError = true,
//                    UseShellExecute = false,
//                    CreateNoWindow = true,
//                    WorkingDirectory = this.gamePath // 使用类成员设置工作目录
//                }
//            };
//            processorsToRun.Add(process);
//        }

//        // 3. 依次执行所有处理器
//        foreach (var process in processorsToRun)
//        {
//            token.ThrowIfCancellationRequested();
//            dones++;

//            ProcessorsOutEvent?.Invoke(alls, dones, $"正在运行处理器 {dones}/{alls}: {Path.GetFileName(process.StartInfo.FileName)}...");

//            try
//            {
//                // 将事件处理器放在循环内部，确保 dones 是最新的
//                process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) ProcessorsOutEvent?.Invoke(alls, dones, e.Data); };
//                process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) ProcessorsOutEvent?.Invoke(alls, dones, $"[错误] {e.Data}"); };

//                process.Start();
//                process.BeginOutputReadLine();
//                process.BeginErrorReadLine();

//                await process.WaitForExitAsync(token);

//                if (process.ExitCode != 0)
//                {
//                    string errorMsg = $"处理器 {dones}/{alls} 执行失败，退出代码: {process.ExitCode}。请检查日志获取详细信息。";
//                    ProcessorsOutEvent?.Invoke(-1, -1, errorMsg);
//                    throw new Exception(errorMsg);
//                }
//            }
//            catch (Exception ex)
//            {
//                ProcessorsOutEvent?.Invoke(-1, -1, $"处理器 {dones} 调用时出错: {ex.Message}");
//                throw; // 将异常继续向上抛出
//            }
//        }
//        ProcessorsOutEvent?.Invoke(alls, alls, "所有安装处理器已成功运行！");
//    }

//    /// <summary>
//    /// 私有辅助方法：从 JAR 文件的 MANIFEST.MF 中异步读取 Main-Class。
//    /// </summary>
//    private async Task<string> GetMainClassFromJar(string jarPath, string jarIdentifier)
//    {
//        try
//        {
//            using (var fs = new FileStream(jarPath, FileMode.Open, FileAccess.Read))
//            using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
//            {
//                var manifestEntry = archive.GetEntry("META-INF/MANIFEST.MF");
//                if (manifestEntry == null)
//                {
//                    ProcessorsOutEvent?.Invoke(-1, -1, $"错误: 在 {jarIdentifier} 中无法找到 MANIFEST.MF");
//                    return null;
//                }
//                using (var reader = new StreamReader(manifestEntry.Open()))
//                {
//                    string line;
//                    while ((line = await reader.ReadLineAsync()) != null)
//                    {
//                        if (line.StartsWith("Main-Class: "))
//                        {
//                            return line.Substring("Main-Class: ".Length).Trim();
//                        }
//                    }
//                }
//            }
//        }
//        catch (IOException ex)
//        {
//            ProcessorsOutEvent?.Invoke(-1, -1, $"错误: 读取 {jarIdentifier} 文件失败: {ex.Message}");
//        }
//        return null;
//    }

//    /// <summary>
//    /// 私有辅助方法：构建处理器的命令行参数字符串。
//    /// </summary>
//    private string BuildProcessorArguments(List<string> argsTemplate, Dictionary<string, string> placeholders, string librariesPath, int all, int done)
//    {
//        var stdArgsBuilder = new StringBuilder();
//        foreach (var arg in argsTemplate)
//        {
//            try
//            {
//                if (arg.StartsWith("{") && arg.EndsWith("}"))
//                {
//                    string key = arg.TrimStart('{').TrimEnd('}');
//                    if (placeholders.TryGetValue(key, out string value))
//                    {
//                        stdArgsBuilder.Append($"\"{value}\" ");
//                    }
//                    else
//                    {
//                        ProcessorsOutEvent?.Invoke(all, done, $"警告：未找到占位符 {arg}，将忽略此参数。");
//                    }
//                }
//                else if (arg.StartsWith("["))
//                {
//                    stdArgsBuilder.Append($"\"{Tools.MavenToPath(librariesPath, arg)}\" ");
//                }
//                else
//                {
//                    // 对于普通参数（如 --task），不需要加引号
//                    stdArgsBuilder.Append($"{arg} ");
//                }
//            }
//            catch (Exception ex)
//            {
//                ProcessorsOutEvent?.Invoke(-1, -1, $"解析参数 {arg} 时出错: {ex.Message}");
//            }
//        }
//        return stdArgsBuilder.ToString().Trim();
//    }

//    /// <summary>
//    /// 私有辅助方法：动态构建 Forge 安装处理器所需的占位符字典。
//    /// </summary>
//    private Dictionary<string, string> BuildForgePlaceholderDictionary(
//        ForgeSeriesInstallProfileRoot profile,
//        string librariesPath,
//        string gamePath,
//        string minecraftJarPath,
//        string clientLzmaPath)
//    {
//        var placeholders = new Dictionary<string, string>
//        {
//            { "SIDE", "client" },
//            { "MINECRAFT_JAR", minecraftJarPath },
//            { "LIBRARY_DIR", librariesPath },
//            { "ROOT", gamePath },
//        };

//        if (profile.Data?.AdditionalData == null)
//        {
//            return placeholders;
//        }

//        foreach (var entry in profile.Data.)
//        {
//            string key = entry.Key;
//            if (entry.Value is not JsonElement element) continue;

//            string rawValue = "";
//            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("client", out JsonElement clientElement))
//            {
//                rawValue = clientElement.GetString() ?? "";
//            }
//            else if (element.ValueKind == JsonValueKind.String)
//            {
//                rawValue = element.GetString() ?? "";
//            }

//            if (string.IsNullOrEmpty(rawValue)) continue;

//            string processedValue;
//            if (rawValue.StartsWith("[") && rawValue.EndsWith("]"))
//            {
//                processedValue = Tools.MavenToPath(librariesPath, rawValue);
//            }
//            else if (rawValue.StartsWith("'") && rawValue.EndsWith("'"))
//            {
//                processedValue = rawValue.Trim('\'');
//            }
//            else
//            {
//                processedValue = rawValue;
//            }
//            placeholders[key] = processedValue;
//        }

//        if (placeholders.ContainsKey("BINPATCH"))
//        {
//            placeholders["BINPATCH"] = clientLzmaPath;
//        }

//        return placeholders;
//    }
//}