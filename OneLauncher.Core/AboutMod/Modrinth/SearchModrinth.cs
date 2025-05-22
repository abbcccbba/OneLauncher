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
/*
public class ModBasicInfo
{
    public string ID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<string> SupportVersions { get; set; }
    public string IconUrl { get; set; }
    public object Icon { get; set; }
    public DateTime time { get; set; }
}
*/
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
        HttpResponseMessage response = await httpClient.GetAsync
            (SearchUrl);
        response.EnsureSuccessStatusCode();
        info = JsonSerializer.Deserialize<ModrinthSearch>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return info;
    }
    public void Dispose()
    {
        httpClient.Dispose();
    }
}
