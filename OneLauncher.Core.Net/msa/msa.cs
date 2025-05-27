using OneLauncher.Core;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Core.Net.msa;
[Serializable]
public class MsaException : Exception
{
    public string Message { get; set; } = string.Empty;
    public string? OriginalMessage { get; set; }
    public MsaException(string message) => Message = message;
    public MsaException(string message, string originalMessage)
    {
        Message = message;
        OriginalMessage = originalMessage;
    }
}
public class MicrosoftAuthenticator : IDisposable
{
    private readonly string clientId;
    private readonly HttpClient httpClient;
    /// <summary>
    /// 使用旧的刷新令牌获取新的访问令牌和刷新令牌，并完成后续的 Minecraft 登录流程。
    /// </summary>
    /// <param name="oldRefreshToken">旧的刷新令牌。</param>
    /// <returns>包含更新用户信息的 UserModel，如果刷新失败则为 null。</returns>
    public async Task<UserModel?> RefreshToken(UserModel oldUserModel)
    {
        try
        {
            var content = new StringContent(
                $"client_id={clientId}&refresh_token={oldUserModel.refreshToken}&grant_type=refresh_token&scope=XboxLive.signin%20offline_access",
                Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await httpClient.PostAsync("https://login.microsoftonline.com/consumers/oauth2/v2.0/token", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token) || string.IsNullOrEmpty(tokenResponse.refresh_token))
            {
                throw new MsaException("刷新令牌失败，返回的令牌信息不完整。");
            }

            return await ToLoandauth(tokenResponse.access_token, tokenResponse.refresh_token);
        }
        catch (Exception)
        {
            throw;
        }
    }
    public static bool IsExpired(DateTime lastTime)
    {
        return Math.Abs((DateTime.UtcNow - lastTime).TotalHours) > 23;
    }
    private async Task<UserModel?> ToLoandauth(
        string microsoftAccessToken, string microsoftRefreshToken)
    {
        // 3. Xbox Live 身份验证
        var xblAuthResponse = await AuthInXboxLive(microsoftAccessToken);

        // 4. XSTS 身份验证
        var xstsAuthResponse = await AuthInXSTS(xblAuthResponse.Token, xblAuthResponse.DisplayClaims.xui[0].uhs);

        // 5. 获取 Minecraft 访问令牌
        var mcLoginResponse = await LoginWithXboxAsync(xstsAuthResponse.Token, xstsAuthResponse.DisplayClaims.xui[0].uhs);

        // 6. 检查游戏拥有情况
        var entitlementsResponse = await CheckGameEntitlementsAsync(mcLoginResponse.access_token);
        if (entitlementsResponse == null || entitlementsResponse.items == null || entitlementsResponse.items.Length == 0)
        {
            Debug.WriteLine("该账号未拥有 Minecraft 游戏。");
            throw new MsaException("该账号未拥有 Minecraft");
        }

        // 7. 获取玩家 UUID 和用户名
        var profileResponse = await GetMinecraftProfileAsync(mcLoginResponse.access_token);

        // 所有步骤成功后，创建并返回 UserModel
        return new UserModel(
            profileResponse.name,
            Guid.Parse(profileResponse.id),
            mcLoginResponse.access_token,
            microsoftRefreshToken
        );
    }
    public MicrosoftAuthenticator()
    {
        this.clientId = Init.AzureApplicationID;
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// 使用设备代码流进行 Minecraft 认证。
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的 CancellationToken。</param>
    /// <param name="pollIntervalSeconds">轮询用户授权状态的间隔时间（秒）。</param>
    /// <returns>包含用户信息的 UserModel，如果认证失败则为 null。</returns>
    public async Task<UserModel?> AuthUseCode(IProgress<(string VerityUrl,string UserCode)> OnUserNeedAction, int pollIntervalSeconds = 5)
    {
        try
        {
            // 1. 获取设备代码对
            var deviceCodeResponse = await GetDeviceCode();
            if (deviceCodeResponse == null) return null;

            OnUserNeedAction.Report((deviceCodeResponse.verification_uri,deviceCodeResponse.user_code));

            // 2. 轮询用户授权状态
            var tokenResponse = await PollAutoState(
                deviceCodeResponse.device_code,
                Math.Max(pollIntervalSeconds, deviceCodeResponse.interval),
                deviceCodeResponse.expires_in);
            if (tokenResponse == null) return null;

            return await ToLoandauth(tokenResponse.access_token,tokenResponse.refresh_token);
        }
        catch (OperationCanceledException)
        {
            throw new MsaException("用户取消了授权操作。");
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// 以代码流验证方式请求用户授权。
    /// </summary>
    private async Task<DeviceCodeResponse?> GetDeviceCode()
    {
        var response = await httpClient.PostAsync
            ("https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode",
            new StringContent(
                $"client_id={clientId}&scope=XboxLive.signin%20offline_access",
                Encoding.UTF8, "application/x-www-form-urlencoded"));
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DeviceCodeResponse>(json);
    }

    /// <summary>
    /// 轮询，以检测用户是否完成授权。
    /// </summary>
    /// <param name="deviceCode">设备代码</param>
    /// <param name="interval">最小轮询间隔</param>
    /// <param name="expiresIn">超时时间</param>
    private async Task<TokenResponse?> PollAutoState(string deviceCode, int interval, int expiresIn)
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(expiresIn))
        {

            var response = await httpClient.PostAsync("https://login.microsoftonline.com/consumers/oauth2/v2.0/token",
                new StringContent(
                $"grant_type=urn:ietf:params:oauth:grant-type:device_code&client_id={clientId}&device_code={deviceCode}",
                Encoding.UTF8, "application/x-www-form-urlencoded"));
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<TokenResponse>(json);
            else
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(json);
                switch (errorResponse?.error)
                {
                    case "authorization_pending":
                        // 继续轮询
                        break;
                    case "authorization_declined":
                        Debug.WriteLine("用户拒绝了授权。");
                        return null;
                    case "expired_token":
                        Debug.WriteLine("授权已过期。");
                        return null;
                    default:
                        Debug.WriteLine($"认证轮询错误: {errorResponse?.error} - {errorResponse?.error_description}");
                        return null;
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(interval));
        }
        Debug.WriteLine("轮询超时，用户未在规定时间内完成授权。");
        return null;
    }

    /// <summary>
    /// 进行 Xbox Live 身份验证。
    /// </summary>
    /// <param name="accessToken">上一步中获取的accessToken</param>
    private async Task<XboxLiveAuthResponse?> AuthInXboxLive(string accessToken)
    {
        // 构建请求体
        var requestBody = new
        {
            Properties = new
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={accessToken}"
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://user.auth.xboxlive.com/user/authenticate", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<XboxLiveAuthResponse>(json);
    }

    /// <summary>
    /// XSTS 身份验证。
    /// </summary>
    private async Task<XSTSAuthResponse?> AuthInXSTS(string xblToken, string uhs)
    {
        var requestBody = new
        {
            Properties = new
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xblToken }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://xsts.auth.xboxlive.com/xsts/authorize", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = JsonSerializer.Deserialize<XSTSErrorResponse>(json);
            if (errorResponse?.XErr == "214891605")
            {
                Console.WriteLine("XSTS 认证失败: 该 Xbox Live 账号需要完成资料设置或家长同意。");
            }
            else if (errorResponse?.XErr == "214891606")
            {
                Console.WriteLine("XSTS 认证失败: 该微软账号未关联 Xbox Live 档案。请确保您已登录过 Xbox.com。");
            }
            else
            {
                Console.WriteLine($"XSTS 认证失败: {errorResponse?.Message} (XErr: {errorResponse?.XErr})");
            }
            return null;
        }

        return JsonSerializer.Deserialize<XSTSAuthResponse>(json);
    }

    /// <summary>
    /// 使用 Xbox 凭据登录 Minecraft。
    /// </summary>
    private async Task<MinecraftLoginResponse?> LoginWithXboxAsync(string xstsToken, string uhs)
    {
        var requestBody = new
        {
            identityToken = $"XBL3.0 x={uhs};{xstsToken}"
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://api.minecraftservices.com/authentication/login_with_xbox", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MinecraftLoginResponse>(json);
    }

    /// <summary>
    /// 检查用户是否拥有 Minecraft 游戏。
    /// </summary>
    private async Task<EntitlementsResponse?> CheckGameEntitlementsAsync(string minecraftAccessToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", minecraftAccessToken);
        var response = await httpClient.GetAsync("https://api.minecraftservices.com/entitlements/mcstore");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EntitlementsResponse>(json);
    }

    /// <summary>
    /// 获取 Minecraft 玩家档案（包含 UUID 和用户名）。
    /// </summary>
    private async Task<MinecraftProfileResponse?> GetMinecraftProfileAsync(string minecraftAccessToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", minecraftAccessToken);
        var response = await httpClient.GetAsync("https://api.minecraftservices.com/minecraft/profile");
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine("获取 Minecraft 档案失败。可能此账号没有设置 Minecraft 档案（例如首次启动游戏时）。");
            return null;
        }
        return JsonSerializer.Deserialize<MinecraftProfileResponse>(json);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
    

    // --- 内部辅助类用于 JSON 反序列化 ---

    private class DeviceCodeResponse
    {
        // 设备代码
        public string device_code { get; set; } = string.Empty;
        // 给用户的代码
        public string user_code { get; set; } = string.Empty;
        // 验证地址
        public string verification_uri { get; set; } = string.Empty;
        // 失效时间（秒）
        public int expires_in { get; set; }
        // 最小轮询间隔（秒）
        public int interval { get; set; }
    }

    private class TokenResponse
    {
        public string token_type { get; set; } = string.Empty;
        public string scope { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string access_token { get; set; } = string.Empty;
        public string refresh_token { get; set; } = string.Empty;
        public string? id_token { get; set; }
    }

    private class ErrorResponse
    {
        public string error { get; set; } = string.Empty;
        public string error_description { get; set; } = string.Empty;
    }

    private class XboxLiveAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public XUIDisplayClaims DisplayClaims { get; set; } = new XUIDisplayClaims();
    }
    private class XUIDisplayClaims
    {
        public XUI[] xui { get; set; } = Array.Empty<XUI>();
    }
    private class XUI
    {
        public string uhs { get; set; } = string.Empty;
    }


    private class XSTSAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public XUIDisplayClaims DisplayClaims { get; set; } = new XUIDisplayClaims();
    }
    private class XSTSErrorResponse
    {
        public string? XErr { get; set; } // 类型是 string 还是 int 取决于 API 返回的具体格式，这里以 string 为例
        public string? Message { get; set; }
    }


    private class MinecraftLoginResponse
    {
        public string access_token { get; set; } = string.Empty;
    }

    private class EntitlementsResponse
    {
        public EntitlementItem[] items { get; set; } = Array.Empty<EntitlementItem>();
    }

    private class EntitlementItem { }
    private class MinecraftProfileResponse
    {
        public string id { get; set; } = string.Empty; // 玩家的真实 UUID
        public string name { get; set; } = string.Empty; // 玩家的 Minecraft 用户名
    }

}

