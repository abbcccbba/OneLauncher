using OneLauncher.Core.Downloader;
using OneLauncher.Core.Net.msa.JsonModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Net.msa;
public struct MojangSkin
{
    public string Skin;
    public bool IsSlimModel;
}
public class MojangProfile : IDisposable
{
    public readonly string uuid;
    public readonly string accessToken;
    public readonly HttpClient httpClient;
    public MojangProfile(UserModel userModel)
    {
        this.uuid = userModel.uuid.ToString();
        this.accessToken = userModel.accessToken;
        httpClient = new HttpClient();
        // 设置一些请求头
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
    }
    public async Task<MojangSkin> Get()
    {
        string url = $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}";
        using (var response = await httpClient.GetStreamAsync(url))
        {
            string SkinUrls = (
                // 解码Json并解码Base64
                Encoding.UTF8.GetString(
                    Convert.FromBase64String(
                         (await JsonNode.ParseAsync(response))
                         !["properties"]
                         !.AsArray()
                         ![0]
                         !["value"]
                         !.GetValue<string>()
                        )
                    )
                );
            var texture = JsonSerializer.Deserialize<TextureData>(SkinUrls,MojangProfileJsonContext.Default.TextureData);
            // 不加这行代码就会报错，报错内容是null我也不知道为什么
            bool isSlimModel = texture!.Textures.Skin.Metadata?.Model == "slim";
            return new MojangSkin
            {
                Skin = texture!.Textures.Skin.Url,
                IsSlimModel = isSlimModel
            };
        }
    }
    public async Task SetUseLocalFile(MojangSkin skin)
    {
        const string requestUrl = "https://api.minecraftservices.com/minecraft/profile/skins";
        HttpResponseMessage response = null; // 声明 response，以便在 catch 块中访问

        try
        {
            using (var formData = new MultipartFormDataContent())
            {
                // 添加模型参数
                formData.Add(new StringContent(skin.IsSlimModel ? "slim" : "classic"), "variant");
                var imageFilePath = skin.Skin.ToString();
                var fileStream = System.IO.File.OpenRead(imageFilePath);
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

                formData.Add(streamContent, "file", System.IO.Path.GetFileName(imageFilePath));

                response = await httpClient.PostAsync(requestUrl, formData);
                response.EnsureSuccessStatusCode();

                Debug.WriteLine($"成功上传本地皮肤文件: {imageFilePath}");
                string successResponseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"API 成功响应: {successResponseContent}");
            }
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine($"请求失败: {e.Message}");
            if (response != null) // 尝试读取错误响应内容
            {
                try
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"API 错误响应内容: {errorContent}");
                }
                catch (Exception contentEx)
                {
                    Debug.WriteLine($"读取错误响应内容失败: {contentEx.Message}");
                }
            }
            throw; // 重新抛出异常，让上层处理
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"上传皮肤时发生未知错误: {ex.Message}");
            throw; // 重新抛出异常
        }
    }
    public async Task SetUseUrl(MojangSkin skin)
    {
        const string requestUrl = "https://api.minecraftservices.com/minecraft/profile/skins";
        try
        {
            /*
            var payload = new
            {
                url = skin.Skin.ToString(),
                variant = skin.IsSlimModel ? "slim" : "classic"
            };
            string jsonPayload = JsonSerializer.Serialize(payload);
            */
            string jsonPayload = "{"+$"\"url\":\"{skin.Skin.ToString()}\",\"variant\":\"{(skin.IsSlimModel ? "slim" : "classic")}\""+"}";
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 发送 POST 请求
            var response = await httpClient.PostAsync(requestUrl, content);
            // 这可能代表麻将服务器无法连接到url，这里尝试下载并手动上传
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                try
                {
                    var tempSavePath = Path.GetTempPath();
                    using (var downTask = new Download(httpClient))
                        await downTask.DownloadFile(skin.Skin, tempSavePath,CancellationToken.None);
                    await SetUseLocalFile(new MojangSkin() { Skin = tempSavePath, IsSlimModel = skin.IsSlimModel });
                }
                catch (HttpRequestException e)
                {
                    throw new OlanException("无法上传皮肤", "在再次尝试后依旧捕获到网络错误", OlanExceptionAction.Error, e);
                }
            }
            response.EnsureSuccessStatusCode();

            Debug.WriteLine($"成功通过 URL 更改皮肤: {skin.Skin.ToString()}");
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine($"请求失败: {e.Message}");
            throw;
        }
    }
    public async Task GetSkinHeadImage()
    {
        // 确保输出目录存在
        var outputPath = Path.Combine(Init.BasePath, "MsaPlayerData", "body");
        Directory.CreateDirectory(outputPath);

        // 1. 获取皮肤
        string sessionUrl = $"https://crafatar.com/renders/body/{uuid}";

        // 2. 调用第三方API下载完整的皮肤图片
        byte[] skinImageBytes;
        try
        {
            skinImageBytes = await httpClient.GetByteArrayAsync(sessionUrl);
            await File.WriteAllBytesAsync(Path.Combine(outputPath, $"{uuid}.png"), skinImageBytes);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"下载皮肤图片失败，URL: {sessionUrl}，错误: {ex.Message}", ex);
        }
    }
    public void Dispose() => httpClient.Dispose();
    public static async Task<bool> IsValidSkinFile(string skinFile)
    {
        try
        {
            using (var image = await Image.LoadAsync<Rgba32>(skinFile))
            {
                int width = image.Width;
                int height = image.Height;

                // 2. 检查基本尺寸
                bool isValidDimension = (width == 64 && (height == 32 || height == 64)) ||
                                        (width == 128 && height == 128) ||
                                        (width == 32 && height == 64); // 偶尔会有这种老式布局

                if (!isValidDimension)
                {
                    return false;
                }

                else if (height == 128 && width == 128) // 高清皮肤
                {
                    bool hasTransparentPixels = false;
                    for (int y = 96; y < 104; y++) // 对应 64x64 的 48-52
                    {
                        for (int x = 88; x < 96; x++) // 对应 64x64 的 44-48
                        {
                            if (x < image.Width && y < image.Height && image[x, y].A == 0)
                            {
                                hasTransparentPixels = true;
                                break;
                            }
                        }
                        if (hasTransparentPixels) break;
                    }
                    if (!hasTransparentPixels)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
