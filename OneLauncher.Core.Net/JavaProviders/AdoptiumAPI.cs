using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneLauncher.Core.Net.JavaProviders;
internal class AdoptiumAPI : BaseJavaProvider, IJavaProvider
{
    //https://api.adoptium.net/v3/assets/feature_releases/21/ga?architecture=x64&os=mac&image_type=jre
    public AdoptiumAPI(int javaVersion) 
        : base(javaVersion,null)
    {
    }

    public override string ProviderName => "Eclipse Adoptium";

    public Task GetAutoAsync()
    {
        string apiUrl = $"https://api.adoptium.net/v3/assets/feature_releases/{javaVersion}/ga?architecture={systemArchName}&os={(systemTypeName == "macos" ? "mac" : systemTypeName)}&image_type=jre";
        return GetAndDownloadAsync(() => GetBinaryPackageLinkAsync(apiUrl, httpClient));
    }

    private async Task<string?> GetBinaryPackageLinkAsync(string apiUrl, HttpClient client)
    {
        using (Stream responseStream = await client.GetStreamAsync(apiUrl, CancelToken ?? CancellationToken.None))
        {
            JsonNode? rootNode = await JsonNode.ParseAsync(responseStream, cancellationToken: CancelToken ?? CancellationToken.None);
            string? link = rootNode?
                            .AsArray()?
                            [0]?
                            ["binaries"]?
                            .AsArray()?
                            [0]?
                            ["package"]?
                            ["link"]?
                            .GetValue<string>();

            return link;
        }
    }
}