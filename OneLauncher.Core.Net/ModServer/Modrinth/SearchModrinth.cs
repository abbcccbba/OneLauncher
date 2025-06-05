using OneLauncher.Core.Modrinth.JsonModelSearch;
using System.Diagnostics;
using System.Text.Json;

namespace OneLauncher.Core.Modrinth;
public class SearchModrinth : IDisposable
{
    public ModrinthSearch info;
    private readonly HttpClient httpClient;
    public SearchModrinth()
    {
        this.httpClient = new HttpClient();
    }
    public async Task<ModrinthSearch> ToSearch(string Key)
    {
        // 搜索仅限支持fabric或支持neoforge的模组
        string SearchUrl = $"https://api.modrinth.com/v2/search?query=\"{Key}\"&facets=[[\"categories:neoforge\",\"categories:fabric\"]]";
        Debug.WriteLine(SearchUrl);

        HttpResponseMessage response = await httpClient.GetAsync(SearchUrl);
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();

        // 使用带有选项的源生成器反序列化
        info = JsonSerializer.Deserialize<ModrinthSearch>(jsonResponse,ModrinthSearchJsonContext.Default.ModrinthSearch);

        return info;
    }
    public void Dispose()
    {
        httpClient.Dispose();
    }
}
