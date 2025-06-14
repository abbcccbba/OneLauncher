using OneLauncher.Core.Downloader;
using OneLauncher.Core.Helper;
using OneLauncher.Core.ModLoader.neoforge.JsonModels;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace OneLauncher.Core.ModLoader.neoforge;
public delegate void ProcessorsOut(int all, int done, string message);
/// <summary>
/// 接管NeoForge几乎所有安装操作
/// </summary>
public class NeoForgeInstallTasker
{
    public readonly Download downloadTask;
    //public List<string> InstallToolsPath;
    public NeoforgeRoot installProfileExample;
    public readonly string librariesPath;
    public readonly string gamePath;
    public readonly string gameVersion;
    public NeoForgeInstallTasker(
        Downloader.Download downloadTask,
        string librariesPath,
        string gamePath,
        string gameVersion)
    {
        this.downloadTask = downloadTask;
        this.librariesPath = librariesPath;
        this.gamePath = gamePath;
        this.gameVersion = gameVersion;
    }
    /// <summary>
    /// 完成安装NeoForge的准备阶段
    /// 下载安装器并拆包，提取必要文件
    /// </summary>
    /// <param name="InstallerUrl">安装程序网络Url链接</param>
    public async Task<(List<NdDowItem>, List<NdDowItem>, string)> StartReady(string InstallerUrl)
    {
        #region 从网络读取
        // 通过网络写入内存流方便后续操作
        HttpClient httpClient = downloadTask.unityClient;
        using var response = await httpClient.GetAsync(InstallerUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        /*
         * 我们需要三个文件
         * version.json
         * install_profile.json
         * data/client.lzma
        */
        using ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        if (archive == null)
            throw new OlanException("NeoForge安装失败", "无法找到的所需的文件", OlanExceptionAction.Error);
        ZipArchiveEntry? versionJson = archive!.GetEntry("version.json");
        ZipArchiveEntry? installProfile = archive!.GetEntry("install_profile.json");
        ZipArchiveEntry? DataClientLazm = archive!.GetEntry("data/client.lzma");
        if (versionJson == null || installProfile == null || DataClientLazm == null)
            throw new OlanException("NeoForge安装失败", "无法从安装包中提取所需的文件", OlanExceptionAction.Error);
        // 读取文件并提取需要下载的文件列表
        using Stream versionJsonStream = versionJson.Open();
        using Stream installProfileStream = installProfile.Open();
        using Stream DataClientLazmStream = DataClientLazm.Open();
        NeoForgeVersionJson? versioninfo = await JsonSerializer.DeserializeAsync<NeoForgeVersionJson>(versionJsonStream,NeoforgeJsonContext.Default.NeoForgeVersionJson);
        NeoforgeRoot? installinfo = await JsonSerializer.DeserializeAsync<NeoforgeRoot>(installProfileStream, NeoforgeJsonContext.Default.NeoforgeRoot);
        installProfileExample = installinfo;
        #endregion
        #region 下载游戏和安装工具的依赖库
        List<NdDowItem> NdLib = new List<NdDowItem>(versioninfo.Libraries.Count);
        foreach (var item in versioninfo.Libraries)
            NdLib.Add(new NdDowItem
                (
                    Url: item.Downloads.Artifact.Url,
                    Path: Path.Combine(librariesPath, Path.Combine(item.Downloads.Artifact.Path.Split("/"))),
                    Size: item.Downloads.Artifact.Size,
                    Sha1: item.Downloads.Artifact.Sha1
                ));
        List<NdDowItem> NdToolsLib = new List<NdDowItem>(versioninfo.Libraries.Count);
        foreach (var item in installinfo.Libraries)
            NdToolsLib.Add(new NdDowItem
                (
                    Url: item.Downloads.Artifact.Url,
                    Path: Path.Combine(librariesPath, Path.Combine(item.Downloads.Artifact.Path.Split("/"))),
                    Size: item.Downloads.Artifact.Size,
                    Sha1: item.Downloads.Artifact.Sha1
                ));

        #endregion
        #region 写入一些文件
        // 重新打开文件，因为原文件流已移动到末尾，不可读取有效信息
        string ClientLzmeTempFileName = Path.GetTempFileName();
        using (var versionJsonStreamR = versionJson.Open())
        using (var fs = new FileStream(Path.Combine(gamePath, $"version.neoforge.json"), FileMode.Create, FileAccess.Write))
            await versionJsonStreamR.CopyToAsync(fs);
        using (var DataClientLazmStreamR = DataClientLazm.Open())
        using (var fs = new FileStream(ClientLzmeTempFileName, FileMode.Create, FileAccess.Write))
            await DataClientLazmStreamR.CopyToAsync(fs);
        #endregion
        return (NdLib, NdToolsLib, ClientLzmeTempFileName);
    }
    public event ProcessorsOut ProcessorsOutEvent;
    /// <summary>
    /// 运行NeoForge处理器
    /// 注意：此方法的所有错误信息必须通过事件抛出
    /// </summary>
    public async Task ToRunProcessors(string MainjarPath, string javaPath, string ClientLzmaFilePath, SystemType osType,CancellationToken token)
    {
        int alls;
        int dones = 0;
        var ArgsExel = new Dictionary<string, string>
        {
            { "SIDE", "client" },
            { "MC_SLIM",Tools.MavenToPath(librariesPath,installProfileExample.Data.MCSlim.Client)},
            { "MC_UNPACKED",Tools.MavenToPath(librariesPath,installProfileExample.Data.MCUnpacked.Client) },
            { "MERGED_MAPPINGS",Tools.MavenToPath(librariesPath,installProfileExample.Data.MergedMappings.Client) },
            { "BINPATCH" , ClientLzmaFilePath},
            { "MCP_VERSION",installProfileExample.Data.MCPVersion.Client.Trim('\'') },
            { "MAPPINGS",Tools.MavenToPath(librariesPath,installProfileExample.Data.Mappings.Client) },
            { "MC_EXTRA",Tools.MavenToPath(librariesPath,installProfileExample.Data.MCExtra.Client) },
            { "MOJMAPS",Tools.MavenToPath(librariesPath,installProfileExample.Data.Mojmaps.Client) },
            { "PATCHED",Tools.MavenToPath(librariesPath,installProfileExample.Data.Patched.Client) },
            { "MC_SRG",Tools.MavenToPath(librariesPath,installProfileExample.Data.MCSRG.Client) },
            { "MINECRAFT_JAR" ,MainjarPath }
        };
        token.ThrowIfCancellationRequested();
        // 创建文件夹
        string destFileName = ArgsExel["MC_SRG"];
        Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
        File.Copy(MainjarPath, destFileName, overwrite: true);
        List<Process> Processors = new List<Process>();

        foreach (var pros in installProfileExample.Processors) 
        {
            token.ThrowIfCancellationRequested();
            if (pros?.Sides == null || pros?.Sides[0] == "client")
            {
                // 定义参数
                string CpArgs = string.Empty;
                string MainClass = string.Empty;
                string StdArgs = string.Empty;
                // 解析cp参数
                {
                    token.ThrowIfCancellationRequested();
                    StringBuilder CpArgsBuilder = new StringBuilder();
                    foreach (var icp in pros.Classpath)
                        CpArgsBuilder.Append(Tools.MavenToPath(librariesPath, icp) + (osType == SystemType.windows ? ";" : ":"));
                    
                    CpArgs =
                        $"-cp \"{CpArgsBuilder.ToString().TrimEnd()}\"";
                }
                // 找到主类名
                {
                    token.ThrowIfCancellationRequested();
                    using (FileStream MainClassFinder = new FileStream(
                        Tools.MavenToPath(librariesPath, pros.Jar), FileMode.Open, FileAccess.Read
                        ))
                    using (ZipArchive ToFindMainClass = new ZipArchive(MainClassFinder, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry? MainClassInFile = ToFindMainClass.GetEntry("META-INF/MANIFEST.MF");
                        if (ToFindMainClass == null || MainClassInFile == null)
                            ProcessorsOutEvent?.Invoke(-1, -1, $"处理器{dones}查找主类名时出错");
                        using (StreamReader MainClassReader = new StreamReader(MainClassInFile.Open()))
                        {
                            string line;
                            while ((line = MainClassReader.ReadLine()) != null)
                                if (line.StartsWith("Main-Class: "))
                                {
                                    MainClass = line.Substring("Main-Class: ".Length).Trim();
                                    break; // 找到后立即退出
                                }
                        }
                    }
                }
                // 解析标准参数
                {
                    token.ThrowIfCancellationRequested();
                    StringBuilder StdArgsBuilder = new StringBuilder();
                    foreach (var aArg in pros.Args)
                    {
                        try
                        {
                            // 代表参数名称，直接返回原始
                            if (aArg[0] == '-')
                                StdArgsBuilder.Append(aArg);
                            // 代表sb仓库坐标，送过去解析
                            else if (aArg[0] == '[')
                                StdArgsBuilder.Append($"\"{Tools.MavenToPath(librariesPath, aArg)}\"");
                            // 代表占位符
                            else if (aArg[0] == '{')
                                StdArgsBuilder.Append($"\"{(ArgsExel[aArg.TrimStart('{').TrimEnd('}')])}\"");
                            else
                                StdArgsBuilder.Append(aArg);
                            StdArgsBuilder.Append(" ");
                        }
                        catch (KeyNotFoundException)
                        {
                            ProcessorsOutEvent?.Invoke(-1, -1, $"处理器{dones}替换占位符时出错，未知占位符{aArg}");
                        }
                    }
                    StdArgs = StdArgsBuilder.ToString();
                }
                token.ThrowIfCancellationRequested();
                Processors.Add(new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = javaPath,
                        Arguments = $"{CpArgs} {MainClass} {StdArgs}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                });
            }
        }
        alls = Processors.Count;
        foreach (var ProItem in Processors)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                dones++;
                ProItem.OutputDataReceived += (sender, e) =>
                {
                    Debug.WriteLine(e.Data);
                    ProcessorsOutEvent?.Invoke(alls, dones, e.Data);
                };
                ProItem.ErrorDataReceived += (sender, e) =>
                {
                    Debug.WriteLine(e.Data);
                    ProcessorsOutEvent?.Invoke(alls, dones, e.Data);
                };
                ProItem.Start();
                ProItem.BeginOutputReadLine();
                ProItem.BeginErrorReadLine();
                // 经过排除估计这些神奇的现象是由这行代码导致的
                // Gemini说没有异步可能导致系统资源严重死锁
                // 改成 await ProItem.WaitForExitAsync(); 后问题减轻
                // 这里原来是// 改成ProItem.WaitForExit(); 
                await ProItem.WaitForExitAsync(token);
                if (ProItem.ExitCode != 0)
                    ProcessorsOutEvent?.Invoke(-1, -1, $"处理器{dones}执行时出错");
            }
            catch
            {
                ProcessorsOutEvent?.Invoke(-1, -1, $"处理器{dones}调用时出错");
            }
        }
    }
}
