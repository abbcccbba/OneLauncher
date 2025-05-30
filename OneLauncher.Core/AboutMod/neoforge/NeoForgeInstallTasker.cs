using OneLauncher.Core.fabric.JsonModel;
using OneLauncher.Core.neoforge;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Core.neoforge;
/// <summary>
/// 接管NeoForge几乎所有的下载和安装操作
/// </summary>
public class NeoForgeInstallTasker
{
    public readonly Download downloadTask;
    //public List<string> InstallToolsPath;
    public Root installProfileExample;
    public readonly string librariesPath;
    public readonly string gamePath;
    public readonly string gameVersion;
    public NeoForgeInstallTasker(Download downloadTask,string librariesPath, string gamePath, string gameVersion)
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
    /// <param name="InstallerUrl">安装包Url</param>
    /// <returns>准备阶段需要下载的所有文件列表</returns>
    public async Task<List<NdDowItem>> StartReady(string InstallerUrl)
    {
        // 通过网络写入内存流方便后续操作
        HttpClient httpClient = downloadTask.UnityClient;
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
            throw new OlanException("NeoForge安装失败","无法找到的所需的文件",OlanExceptionAction.Error);
        ZipArchiveEntry? versionJson = archive!.GetEntry("version.json");
        ZipArchiveEntry? installProfile = archive!.GetEntry("install_profile.json");
        ZipArchiveEntry? DataClientLazm = archive!.GetEntry("data/client.lzma");
        if (versionJson == null || installProfile == null || DataClientLazm == null)
            throw new OlanException("NeoForge安装失败","无法从安装包中提取所需的文件",OlanExceptionAction.Error);
        // 读取文件并提取需要下载的文件列表
        using Stream versionJsonStream = versionJson.Open();
        using Stream installProfileStream = installProfile.Open();
        using Stream DataClientLazmStream = DataClientLazm.Open();
        NeoForgeVersionJson? versioninfo = await JsonSerializer.DeserializeAsync<NeoForgeVersionJson>(versionJsonStream);
        Root? installinfo = await JsonSerializer.DeserializeAsync<Root>(installProfileStream);
        installProfileExample = installinfo;
        #region 把游戏和安装工具的依赖库文件添加到需要下载的文件列表
        List<NdDowItem> FilesNeedToDownloadList = new List<NdDowItem>(versioninfo.Libraries.Count+installinfo.Libraries.Count);
        foreach (var item in versioninfo.Libraries)
            FilesNeedToDownloadList.Add(new NdDowItem
                (
                    Url: item.Downloads.Artifact.Url,
                    Path: Path.Combine(librariesPath,Path.Combine(item.Downloads.Artifact.Path.Split("/"))),
                    Size: item.Downloads.Artifact.Size,
                    Sha1: item.Downloads.Artifact.Sha1
                ));
        foreach (var item in installinfo.Libraries)
        {
            FilesNeedToDownloadList.Add(new NdDowItem
                (
                    Url: item.Downloads.Artifact.Url,
                    Path: Path.Combine(librariesPath, Path.Combine(item.Downloads.Artifact.Path.Split("/"))),
                    Size: item.Downloads.Artifact.Size,
                    Sha1: item.Downloads.Artifact.Sha1
                ));
            //InstallToolsPath.Add(item.Downloads.Artifact.Path);
        }
        #endregion
        // 重新打开文件，因为原文件流已移动到末尾，不可读取有效信息
        using (var versionJsonStreamR = versionJson.Open())
        using (var fs = new FileStream(Path.Combine(gamePath,$"{gameVersion}-neoforge.json"),FileMode.Create, FileAccess.Write))
            await versionJsonStreamR.CopyToAsync(fs);
        using (var DataClientLazmStreamR = DataClientLazm.Open())
        using (var fs = new FileStream(Path.Combine(librariesPath,"client.lzma"), FileMode.Create, FileAccess.Write))
            await DataClientLazmStreamR.CopyToAsync(fs);
        return FilesNeedToDownloadList;
    }
    public Task ToRunProcessors(string MainjarPath, string javaPath, IProgress<(int all, int done)> progress) => Task.Run(async () =>
    {
        int alls;
        int dones = 0;
        var ArgsExel = new Dictionary<string, string>
        {
            { "SIDE", "client" },
            { "MC_SLIM",Tools.MavenToPath(librariesPath,installProfileExample.Data.MCSlim.Client)},
            { "MC_UNPACKED",Tools.MavenToPath(librariesPath,installProfileExample.Data.MCUnpacked.Client) },
            { "MERGED_MAPPINGS",Tools.MavenToPath(librariesPath,installProfileExample.Data.MergedMappings.Client) },
            { "BINPATCH" , Path.Combine(librariesPath,"client.lzma")},
            { "MCP_VERSION",installProfileExample.Data.MCPVersion.Client.Trim('\'') },
            { "MAPPINGS",Tools.MavenToPath(librariesPath,installProfileExample.Data.Mappings.Client) },
            { "MC_EXTRA",Tools.MavenToPath(librariesPath,installProfileExample.Data.MCExtra.Client) },
            { "MOJMAPS",Tools.MavenToPath(librariesPath,installProfileExample.Data.Mojmaps.Client) },
            { "PATCHED",Tools.MavenToPath(librariesPath,installProfileExample.Data.Patched.Client) },
            { "MC_SRG",Tools.MavenToPath(librariesPath,installProfileExample.Data.MCSRG.Client) },
            { "MINECRAFT_JAR" ,MainjarPath }
        };
        // 创建文件夹
        string destFileName = ArgsExel["MC_SRG"];
        Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
        File.Copy(MainjarPath, destFileName, overwrite: true);
        List<Process> Processors = new List<Process>();
        
        foreach (var pros in installProfileExample.Processors)
            if (pros?.Sides == null || pros?.Sides[0] == "client")
            {
                // 定义参数
                string CpArgs = string.Empty;
                string MainClass = string.Empty;
                string StdArgs = string.Empty;
                // 解析cp参数
                {
                    StringBuilder CpArgsBuilder = new StringBuilder();
                    foreach (var icp in pros.Classpath)
                        CpArgsBuilder.Append(Tools.MavenToPath(librariesPath, icp) + ";");
                    CpArgs =
                        $"-cp \"{CpArgsBuilder.ToString().TrimEnd()}\"";
                }
                // 找到主类名
                {
                    using (FileStream MainClassFinder = new FileStream(
                        Tools.MavenToPath(librariesPath, pros.Jar), FileMode.Open, FileAccess.Read
                        ))
                    using (ZipArchive ToFindMainClass = new ZipArchive(MainClassFinder, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry? MainClassInFile = ToFindMainClass.GetEntry("META-INF/MANIFEST.MF");
                        if (ToFindMainClass == null || MainClassInFile == null)
                            throw new OlanException("NeoForge安装失败", "无法读取主类名", OlanExceptionAction.Error);
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
                    StringBuilder StdArgsBuilder = new StringBuilder();
                    foreach (var aArg in pros.Args)
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
                    StdArgs = StdArgsBuilder.ToString();
                }
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
        alls = Processors.Count;
        foreach(var ProItem in Processors)
        {
            dones++;
            progress?.Report((alls, dones));
            ProItem.OutputDataReceived += (sender, e) => Debug.WriteLine(e.Data);
            ProItem.ErrorDataReceived += (sender, e) => Debug.WriteLine(e.Data);
            ProItem.Start();
            ProItem.BeginOutputReadLine();
            ProItem.BeginErrorReadLine();
            await ProItem.WaitForExitAsync();
        }
    });
}
