using OneLauncher.Core.Modrinth.JsonModelGet;
using OneLauncher.Core.Modrinth.JsonModelSearch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
        string SearchUrl = $"https://api.modrinth.com/v2/search?query=\"{Key}\"&facets=[[\"categories:fabric\"]]";
        Debug.WriteLine(SearchUrl);

        HttpResponseMessage response = await httpClient.GetAsync(SearchUrl);
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();

        // 使用带有选项的源生成器反序列化
        info = JsonSerializer.Deserialize<ModrinthSearch>(jsonResponse);

        return info;
    }
    public void Dispose()
    {
        httpClient.Dispose();
    }
}
