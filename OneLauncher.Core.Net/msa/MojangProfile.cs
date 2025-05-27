using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Net.msa;
public struct MojangSkin
{
    public string SkinUrl;
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
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }
    public async Task<MojangSkin> Get()
    {
        string url = $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}";
        using (var response = await httpClient.GetStreamAsync(url))
        {
            string SkinUrls =  (
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
            var texture = JsonSerializer.Deserialize<TextureData>(SkinUrls);
            // 不加这行代码就会报错，报错内容是null我也不知道为什么
            bool isSlimModel = texture!.Textures.Skin.Metadata?.Model == "slim";
            return new MojangSkin
            {
                SkinUrl = texture!.Textures.Skin.Url,
                IsSlimModel = isSlimModel
            };
        }
    }
    #region 获取皮肤的反序列化类
    public class TextureData
    {
        [JsonPropertyName("profileName")]
        public string ProfileName { get; set; }

        [JsonPropertyName("textures")]
        public Textures Textures { get; set; }
    }

    public class Textures
    {
        [JsonPropertyName("SKIN")]
        public TextureInfo Skin { get; set; }

        [JsonPropertyName("CAPE")]
        public TextureInfo Cape { get; set; }
    }

    public class TextureInfo
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }
    }
    public class Metadata
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } 
    }
    #endregion
    public async Task Set(MojangSkin skin)
    {
        string requestUrl = "https://api.minecraftservices.com/minecraft/profile/skins";
        try
        {
            var payload = new
            {
                url = skin.SkinUrl,
                variant = skin.IsSlimModel ? "slim" : "classic" 
            };
            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 发送 POST 请求
            var response = await httpClient.PostAsync(requestUrl, content);
            response.EnsureSuccessStatusCode(); 

            Debug.WriteLine($"成功通过 URL 更改皮肤: {skin.SkinUrl}");
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine($"请求失败: {e.Message}");
            throw;
        }
    }

    public void Dispose() => httpClient.Dispose();
}
