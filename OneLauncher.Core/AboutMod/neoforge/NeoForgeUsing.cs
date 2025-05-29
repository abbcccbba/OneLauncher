using OneLauncher.Core;
using OneLauncher.Core.Models;
using OneLauncher.Core.neoforge;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class NeoForgeUsing
{
    private readonly HttpClient httpClient;
    public NeoForgeVersionJson info;
    public NeoForgeUsing(HttpClient client = null)
    {
        httpClient = client ?? new HttpClient();
    }
    public async Task Init(string basePath,string version)
    {
        string jsonPath = Path.Combine(basePath, "versions", version, $"{version}-neoforge.json");
        string jsonString = await File.ReadAllTextAsync(jsonPath,Encoding.UTF8);
        info = JsonSerializer.Deserialize<NeoForgeVersionJson>(jsonString);
    }
    /// <summary>
    /// 获取当前NeoForge的依赖库下载列表
    /// </summary>
    public List<NdDowItem> GetLibraries(string LibBasePath)
    {
        List<NdDowItem> ret = new List<NdDowItem>(info.Libraries.Count);
        foreach (var item in info.Libraries)
        {
            ret.Add(
                new NdDowItem(
                Url:item.Downloads.Artifact.Url,
                Path:Path.Combine(LibBasePath, "libraries",
                    //item.Downloads.Artifact.Path),
                    //可选适配分隔符，这里不适配方便后续查找-p参数
                    Path.Combine(item.Downloads.Artifact.Path.Split('/'))),
                Size:item.Downloads.Artifact.Size,
                Sha1:item.Downloads.Artifact.Sha1));
        }
        return ret;
    }
    public async Task<string> DownloadVersionJson(string neoForgeInstallerUrl)
    {
        // 1. 发送 HTTP GET 请求并只读取响应头，内容以流的形式获取。
        using var response = await httpClient.GetAsync(neoForgeInstallerUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        // 2. 将响应内容流完整读取到 MemoryStream 中
        // MemoryStream 是可查找的，可以传递给 ZipArchive
        using var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0; // 将流的位置重置到开头

        // 3. 将 MemoryStream 直接传递给 ZipArchive 构造函数。
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read); // 使用 memoryStream

        // 4. 查找名为 "version.json" 的条目
        var versionJsonEntry = archive.GetEntry("version.json");
        if (versionJsonEntry == null)
        {
            throw new FileNotFoundException("version.json not found in the NeoForge installer JAR.");
        }

        // 5. 打开 version.json 的流并读取内容
        using (Stream entryStream = versionJsonEntry.Open()) // 注意这里是 entryStream
        using (StreamReader reader = new StreamReader(entryStream))
        {
            // 先读取整个 JSON 内容到字符串
            string jsonContent = await reader.ReadToEndAsync();

            // 然后反序列化这个字符串
            this.info = JsonSerializer.Deserialize<NeoForgeVersionJson>(jsonContent);

            // 返回读取到的 JSON 内容
            return jsonContent;
        }
    }
}
