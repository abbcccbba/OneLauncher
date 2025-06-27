using OneLauncher.Core.Compatible.ImportPCL2Version;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Downloader.DownloadMinecraftProviders;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.ImportPCL2Version;

/// <summary>
/// 负责处理从 PCL2 实例文件夹导入游戏到 OneLauncher 的全部逻辑。
/// </summary>
public class PCL2Importer
{
    public CancellationToken token;
    public IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> process;
    public PCL2Importer(IProgress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)> process, CancellationToken? token = null)
    {
        this.token = token ?? CancellationToken.None;
        this.process = process;
    }

    public async Task ImportAsync(string pclVersionPath)
    {
        string versionJsonPath = FindAuthoritativeJsonFile(pclVersionPath);
        var info = await JsonSerializer.DeserializeAsync(File.OpenRead(versionJsonPath),PCL2VersionJsonContent.Default.PCL2VersionJsonModels);
        ModEnum modLoaderType = Tools.MainClassToModEnum(info.MainClass);
        GameData gameData = new GameData(info.UserCustomName,info.ClientVersionID,modLoaderType,null);
        await Init.GameDataManger.AddGameDataAsync(gameData);
        await InstallMinecraft(gameData,info.ClientVersionID,modLoaderType);
        await MigrateUserDataAsync(pclVersionPath,gameData.InstancePath);
    }
    #region 辅助方法
    private string FindAuthoritativeJsonFile(string versionPath)
    {
        var jsonFiles = Directory.EnumerateFiles(versionPath, "*.json").ToList();
        if (!jsonFiles.Any())
            throw new OlanException("无法导入","无法在PCL的版本文件内");

        // 优先寻找和文件夹同名的json
        string expectedJsonName = $"{Path.GetDirectoryName(versionPath)}.json";
        string? authoritativeJson = jsonFiles.FirstOrDefault(f => Path.GetFileName(f).Equals(expectedJsonName, StringComparison.OrdinalIgnoreCase));

        // 如果找不到，就用第一个作为备选
        return authoritativeJson ?? jsonFiles.First();
    }
    private async Task InstallMinecraft(GameData gameData,string versionID,ModEnum modLoaderType)
    {
        //using Download downTool = new Download();
        //// 调用自己的方法把版本下了
        //// 这里离开using会释放，所以要在这里等待
        //await new DownloadMinecraft(downTool, new UserVersion()
        //{
        //    VersionID = versionID,
        //    modType = new ModType()
        //    {
        //        IsFabric = modLoaderType == ModEnum.fabric,
        //        IsNeoForge = modLoaderType == ModEnum.neoforge,
        //    }
        //}, Init.MojangVersionList.FirstOrDefault(x => x.ID == versionID),
        //gameData, Init.GameRootPath,
        //process, 
        //token).MinecraftBasic(
        //    Init.ConfigManger.config.OlanSettings.MaximumDownloadThreads,
        //    Init.ConfigManger.config.OlanSettings.MaximumSha1Threads,
        //    Init.ConfigManger.config.OlanSettings.IsSha1Enabled,
        //    Init.ConfigManger.config.OlanSettings.IsAllowToDownloadUseBMLCAPI);
    }
    private async Task MigrateUserDataAsync(string sourcePath, string destinationPath)
    {
        // 定义需要迁移的文件夹和文件列表
        var foldersToMove = new[] { "saves", "mods", "resourcepacks", "shaderpacks", "config", "logs" };
        var filesToMove = new[] { "options.txt" };

        // --- 这是补全的部分 ---
        foreach (var folderName in foldersToMove)
        {
            token.ThrowIfCancellationRequested(); // 在每个循环开始时检查是否已请求取消
            string sourceFolder = Path.Combine(sourcePath, folderName);
            if (Directory.Exists(sourceFolder))
            {
                string destFolder = Path.Combine(destinationPath, folderName);
                // 使用你已有的 Tools.CopyDirectoryAsync 方法来递归复制文件夹
                await Tools.CopyDirectoryAsync(sourceFolder, destFolder, token);
            }
        }
        // --- 补全结束 ---

        foreach (var fileName in filesToMove)
        {
            token.ThrowIfCancellationRequested();
            string sourceFile = Path.Combine(sourcePath, fileName);
            if (File.Exists(sourceFile))
            {
                string destFile = Path.Combine(destinationPath, fileName);
                File.Copy(sourceFile, destFile, true);
            }
        }
    }
    #endregion
}