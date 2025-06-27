using OneLauncher.Core.Helper;
using OneLauncher.Core.Mod.ModLoader.fabric;
using OneLauncher.Core.Net.ModService.Modrinth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders.Sources;

internal class FabricProvider : IConcreteProviders
{
    private readonly DownloadInfo _context;
    public FabricProvider(DownloadInfo context)
    {
        _context = context;
    }
    public async Task<List<NdDowItem>> GetDependencies()
    {
        List<NdDowItem> fabricDependencies;
        string fabricMetaFilePath = Path.Combine(
            _context.VersionInstallInfo.VersionPath,
            "version.fabric.json"
        );
        await using var fileStream = new FileStream(fabricMetaFilePath, FileMode.Open, FileAccess.Read);

        // 预留，自定义版本
        var parser = string.IsNullOrEmpty(_context.SpecifiedFabricVersion)
            ? FabricVJParser.ParserAuto(fileStream, _context.GameRootPath)
            : FabricVJParser.ParserUseVersion(fileStream, _context.GameRootPath, _context.SpecifiedFabricVersion);

        fabricDependencies = parser.GetLibraries();

        if (_context.IsDownloadFabricWithAPI)
        {
            var modrinthTask = new GetModrinth(
                "fabric-api", 
                _context.ID,  
                Path.Combine(_context.UserInfo.InstancePath, "mods") 
            );
            await modrinthTask.Init();
            var fabricApiFile = modrinthTask.GetDownloadInfos();
            if (fabricApiFile.HasValue)
            {
                fabricDependencies.Add(fabricApiFile.Value);
            }
        }
        return fabricDependencies;
    }
}