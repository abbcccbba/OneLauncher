using OneLauncher.Core.Global;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace OneLauncher.Core.Mod.ModLoader.forgeseries;

/// <summary>
/// 一个统一的工具类，用于获取 Forge 和 NeoForge 的最新版本和安装器 URL。
/// </summary>
public class ForgeVersionListGetter
{
    private readonly HttpClient _httpClient;

    // API URL 常量
    private const string ForgeMetadataUrl = "https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml";
    private const string ForgePromotionsUrl = "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";
    private const string NeoForgeMetadataUrl = "https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml";

    public ForgeVersionListGetter(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// 根据类型和条件，获取最新的安装器下载 URL。
    /// </summary>
    /// <param name="isForge">为 true 时获取 Forge，为 false 时获取 NeoForge。</param>
    /// <param name="mcVersion">Minecraft 版本号，例如 "1.20.1"。</param>
    /// <param name="allowBeta">【仅用于 NeoForge】是否允许选择 Beta 版本。</param>
    /// <param name="useRecommended">【仅用于 Forge】是否优先使用官方推荐版。</param>
    /// <returns>安装器的完整下载 URL。</returns>
    public async Task<string> GetInstallerUrlAsync(bool isForge, string mcVersion, bool allowBeta = true, bool useRecommended = true)
    {
        string fullVersion = isForge
            ? await GetLatestForgeVersionAsync(mcVersion, useRecommended)
            : await GetLatestNeoForgeVersionAsync(mcVersion, allowBeta);

        if (string.IsNullOrEmpty(fullVersion))
        {
            string modType = isForge ? "Forge" : "NeoForge";
            throw new OlanException("版本获取失败", $"未能确定最新的 {modType} 版本。", OlanExceptionAction.Error);
        }

        return isForge
            ? $"https://maven.minecraftforge.net/net/minecraftforge/forge/{fullVersion}/forge-{fullVersion}-installer.jar"
            : $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{fullVersion}/neoforge-{fullVersion}-installer.jar";
    }

    #region Private Implementation Details

    private async Task<string> GetLatestForgeVersionAsync(string mcVersion, bool useRecommended)
    {
        try
        {
            var promotions = await FetchForgePromotionsAsync();
            string key = useRecommended ? $"{mcVersion}-recommended" : $"{mcVersion}-latest";
            if (promotions.TryGetValue(key, out var forgeNum)) return $"{mcVersion}-{forgeNum}";
            if (useRecommended && promotions.TryGetValue($"{mcVersion}-latest", out forgeNum)) return $"{mcVersion}-{forgeNum}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ForgeVersionGetter] 获取 Forge promotions 失败: {ex.Message}。将从完整列表查找。");
        }

        var allVersions = await FetchAndParseAllForgeVersionsAsync();
        var suitableVersion = allVersions.Where(v => v.MinecraftVersion == mcVersion).Max();

        return suitableVersion?.FullVersionString;
    }

    private async Task<string> GetLatestNeoForgeVersionAsync(string mcVersion, bool allowBeta)
    {
        string prefix = ConvertMcVersionToNeoForgePrefix(mcVersion);
        if (prefix == null) throw new OlanException("无效的 Minecraft 版本", $"无法为 '{mcVersion}' 生成有效的 NeoForge 版本前缀。", OlanExceptionAction.Warning);

        var allVersions = await FetchAndParseAllNeoForgeVersionsAsync();
        var suitableVersion = allVersions.Where(v => v.FullVersionString.StartsWith(prefix) && (allowBeta || !v.IsBeta)).Max();

        return suitableVersion?.FullVersionString;
    }

    private async Task<Dictionary<string, string>> FetchForgePromotionsAsync()
    {
        try
        {
            var promoData = 
                await JsonSerializer.DeserializeAsync<ForgePromotionData>(
                    await _httpClient.GetStreamAsync(ForgePromotionsUrl),ForgeSeriesJsonContext.Default.ForgePromotionData);
            return promoData?.Promos ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            throw new OlanException("获取Forge推荐版本失败", "无法从Forge官方源获取推荐版本列表。", OlanExceptionAction.Warning, ex);
        }
    }

    private async Task<List<ForgeVersionInfo>> FetchAndParseAllForgeVersionsAsync()
    {
        string xmlContent;
        try { xmlContent = await _httpClient.GetStringAsync(ForgeMetadataUrl); }
        catch (HttpRequestException ex) { throw new OlanException("网络错误", "无法从 Forge 官方源获取版本列表。", OlanExceptionAction.Error, ex); }

        try
        {
            return XDocument.Parse(xmlContent).Descendants("version")
                .Select(v => { try { return new ForgeVersionInfo(v.Value); } catch { return null; } })
                .Where(v => v != null).ToList();
        }
        catch (System.Xml.XmlException ex) { throw new OlanException("元数据格式错误", "Forge 版本列表的 XML 元数据格式无效。", OlanExceptionAction.Error, ex); }
    }

    private string ConvertMcVersionToNeoForgePrefix(string mcVersion)
    {
        if (string.IsNullOrWhiteSpace(mcVersion) || !mcVersion.StartsWith("1.")) return null;
        var versionPart = mcVersion.Substring(2).Split('.');
        if (versionPart.Length >= 2 && int.TryParse(versionPart[0], out _) && int.TryParse(versionPart[1], out _))
            return $"{versionPart[0]}.{versionPart[1]}.";
        return null;
    }

    private async Task<List<NeoForgeVersionInfo>> FetchAndParseAllNeoForgeVersionsAsync()
    {
        string xmlContent;
        try { xmlContent = await _httpClient.GetStringAsync(NeoForgeMetadataUrl); }
        catch (HttpRequestException ex) { throw new OlanException("网络错误", "无法从 NeoForge 官方源获取版本列表。", OlanExceptionAction.Error, ex); }

        try
        {
            return XDocument.Parse(xmlContent).Descendants("version")
                .Select(v => { try { return new NeoForgeVersionInfo(v.Value); } catch { return null; } })
                .Where(v => v != null).ToList();
        }
        catch (System.Xml.XmlException ex) { throw new OlanException("元数据格式错误", "NeoForge 版本列表的 XML 元数据格式无效。", OlanExceptionAction.Error, ex); }
    }

    #endregion
}