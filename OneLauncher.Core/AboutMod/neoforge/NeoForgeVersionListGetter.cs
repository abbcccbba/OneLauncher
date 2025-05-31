using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneLauncher.Core.neoforge;

// NeoForgeVersionInfo 类的定义保持不变 (来自你之前的代码)
public class NeoForgeVersionInfo : IComparable<NeoForgeVersionInfo>
{
    public string FullVersionString { get; }
    public Version ParsedNumericVersion { get; }
    public bool IsBeta { get; }
    public string Suffix { get; }
    public int McMajorEquivalent { get; }
    public int McMinorPatchEquivalent { get; }
    public int NeoForgeBuild { get; }
    public int NeoForgeRevision { get; }

    public NeoForgeVersionInfo(string versionStr)
    {
        FullVersionString = versionStr ?? throw new ArgumentNullException(nameof(versionStr));

        string tempSuffix = "";
        string numericPart = versionStr;

        int suffixIndex = versionStr.IndexOf('-');
        if (suffixIndex > 0)
        {
            numericPart = versionStr.Substring(0, suffixIndex);
            tempSuffix = versionStr.Substring(suffixIndex + 1);
        }
        Suffix = tempSuffix;
        IsBeta = Suffix.Equals("beta", StringComparison.OrdinalIgnoreCase);

        string[] numComponents = numericPart.Split('.');
        if (numComponents.Length < 3)
        {
            throw new ArgumentException($"版本字符串 '{versionStr}' 在后缀前至少需要3个数字部分。");
        }

        if (!int.TryParse(numComponents[0], out int n1) ||
            !int.TryParse(numComponents[1], out int n2) ||
            !int.TryParse(numComponents[2], out int n3))
        {
            throw new ArgumentException($"无法解析版本字符串 '{versionStr}' 中的数字部分。");
        }

        McMajorEquivalent = n1;
        McMinorPatchEquivalent = n2;
        NeoForgeBuild = n3;

        if (numComponents.Length >= 4 && int.TryParse(numComponents[3], out int n4))
        {
            NeoForgeRevision = n4;
            ParsedNumericVersion = new Version(n1, n2, n3, n4);
        }
        else
        {
            NeoForgeRevision = 0;
            ParsedNumericVersion = new Version(n1, n2, n3);
        }
    }

    public int CompareTo(NeoForgeVersionInfo other)
    {
        if (other == null) return 1;
        int numericCompare = ParsedNumericVersion.CompareTo(other.ParsedNumericVersion);
        if (numericCompare != 0) return numericCompare;
        if (IsBeta != other.IsBeta) return IsBeta ? -1 : 1;
        return string.Compare(Suffix, other.Suffix, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => FullVersionString;
}

public class NeoForgeVersionListGetter
{
    private readonly HttpClient _httpClient;
    private const string NeoForgeMetadataUrl = "https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml";

    public NeoForgeVersionListGetter(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<string> GetLatestSuitableNeoForgeVersionStringAsync(string mcVersion, bool allowBeta)
    {
        NeoForgeVersionInfo latestVersionInfo = await GetLatestSuitableNeoForgeVersionAsync(mcVersion, allowBeta);
        // 如果 GetLatestSuitableNeoForgeVersionAsync 抛出异常，这里不会执行
        // 它现在应该在找不到版本时抛出异常，而不是返回null
        return latestVersionInfo.FullVersionString; // 直接访问，因为预期非null或已抛出异常
    }

    public async Task<NeoForgeVersionInfo> GetLatestSuitableNeoForgeVersionAsync(string mcVersion, bool allowBeta)
    {
        string neoForgeVersionPrefix = ConvertMcVersionToNeoForgePrefix(mcVersion);
        if (string.IsNullOrEmpty(neoForgeVersionPrefix))
        {
            throw new OlanException("无效的Minecraft版本",
                                    $"无法为提供的Minecraft版本 '{mcVersion}' 生成有效的NeoForge版本前缀。请检查版本号格式 (例如 '1.20.2' 或 '1.21')。",
                                    OlanExceptionAction.Warning);
        }
        System.Diagnostics.Debug.WriteLine($"[NeoForgeVersionListGetter] Minecraft版本 '{mcVersion}' 对应的NeoForge前缀: '{neoForgeVersionPrefix}', 允许Beta: {allowBeta}");

        List<NeoForgeVersionInfo> allParsedVersions = await FetchAndParseAllVersionsAsync();
        // FetchAndParseAllVersionsAsync 现在应该在失败时抛出OlanException

        List<NeoForgeVersionInfo> suitableVersions = allParsedVersions
            .Where(v => v.FullVersionString.StartsWith(neoForgeVersionPrefix))
            .Where(v => allowBeta || !v.IsBeta)
            .ToList();

        if (!suitableVersions.Any())
        {
            throw new OlanException("未找到兼容版本",
                                    $"未能找到与Minecraft版本 '{mcVersion}' (NeoForge前缀: '{neoForgeVersionPrefix}', 允许Beta: {allowBeta}) 兼容的NeoForge版本。这可能意味着该Minecraft版本暂无NeoForge支持，或者您可以尝试调整Beta版本选项。",
                                    OlanExceptionAction.Warning);
        }

        NeoForgeVersionInfo bestVersion = suitableVersions.Max(); // Max() 使用 NeoForgeVersionInfo.CompareTo
        if (bestVersion == null) // 以防万一 Max() 在空列表上返回 null 而不是抛错 (通常Linq.Max会抛错)
        {
            throw new OlanException("版本选择失败",
                                   $"在筛选后的版本中未能确定最新版本，Minecraft版本 '{mcVersion}'。",
                                   OlanExceptionAction.Error);
        }
        return bestVersion;
    }

    private string ConvertMcVersionToNeoForgePrefix(string mcVersion)
    {
        if (string.IsNullOrWhiteSpace(mcVersion) || !mcVersion.StartsWith("1."))
        {
            return null;
        }
        string versionPart = mcVersion.Substring(2);
        string[] mcParts = versionPart.Split('.');
        if (mcParts.Length == 1)
        {
            if (int.TryParse(mcParts[0], out _)) return $"{mcParts[0]}.0.";
        }
        else if (mcParts.Length >= 2)
        {
            if (int.TryParse(mcParts[0], out _) && int.TryParse(mcParts[1], out _)) return $"{mcParts[0]}.{mcParts[1]}.";
        }
        return null;
    }

    private async Task<List<NeoForgeVersionInfo>> FetchAndParseAllVersionsAsync()
    {
        string xmlContent;
        try
        {
            xmlContent = await _httpClient.GetStringAsync(NeoForgeMetadataUrl);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NeoForgeVersionListGetter] 请求NeoForge metadata失败: {ex.Message}");
            // 若要提供重试功能，可以在Download.cs捕获此异常后，构造新的OlanException时加入TryAgainFunction
            throw new OlanException("网络连接错误",
                                    $"无法从NeoForge官方源获取版本列表。请检查您的网络连接。\n错误详情: {ex.Message}",
                                    OlanExceptionAction.Error,
                                    ex);
        }
        catch (Exception ex) // 其他例如 TaskCanceledException (超时)
        {
            throw new OlanException("网络请求失败",
                                    $"获取NeoForge版本列表时发生网络相关错误。\n错误详情: {ex.Message}",
                                    OlanExceptionAction.Error,
                                    ex);
        }

        try
        {
            XDocument doc = XDocument.Parse(xmlContent);
            List<NeoForgeVersionInfo> versions = new List<NeoForgeVersionInfo>();
            foreach (XElement versionElement in doc.Descendants("version"))
            {
                try
                {
                    versions.Add(new NeoForgeVersionInfo(versionElement.Value));
                }
                catch (ArgumentException ex) // NeoForgeVersionInfo构造函数可能抛出
                {
                    System.Diagnostics.Debug.WriteLine($"[NeoForgeVersionListGetter] 解析版本号 '{versionElement.Value}' 失败: {ex.Message}。已跳过此版本。");
                    // 选择不为单个错误版本号抛出致命异常，而是记录并继续
                }
            }
            if (!versions.Any() && doc.Descendants("version").Any()) // 如果存在version标签但没有一个成功解析
            {
                throw new OlanException("元数据解析不完整",
                                   "NeoForge版本列表中所有版本条目均无法成功解析，请检查元数据格式或联系开发者。",
                                   OlanExceptionAction.Error);
            }
            return versions;
        }
        catch (System.Xml.XmlException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NeoForgeVersionListGetter] 解析NeoForge metadata XML时发生错误: {ex.Message}");
            throw new OlanException("元数据格式错误",
                                    $"NeoForge版本列表的XML元数据格式无效或已损坏。\n错误详情: {ex.Message}",
                                    OlanExceptionAction.Error,
                                    ex);
        }
        catch (Exception ex) // 其他未知解析错误
        {
            System.Diagnostics.Debug.WriteLine($"[NeoForgeVersionListGetter] 处理NeoForge metadata时发生未知错误: {ex.Message}");
            throw new OlanException("元数据处理错误",
                                    $"处理NeoForge版本列表时发生未知错误。\n错误详情: {ex.Message}",
                                    OlanExceptionAction.Error,
                                    ex);
        }
    }
}