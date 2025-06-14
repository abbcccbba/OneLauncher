using Duende.IdentityModel.OidcClient;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Net.msa.JsonModels;
using OneLauncher.Core.Net.QuStellar;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OneLauncher.Core.Net.msa;

public class MsalMicrosoftAuthenticator : IDisposable
{
    private readonly IPublicClientApplication msalClient;
    private MsalCacheHelper cacheHelper; // 新增：用于管理持久化缓存
    private readonly HttpClient httpClient; // 修改：此 HttpClient 现在只用于调用 Xbox 和 Minecraft API

    private static readonly string[] Scopes = { "XboxLive.signin", "offline_access" };

    /// <summary>
    /// 获取所有已缓存的账号列表。
    /// </summary>
    public async Task<IEnumerable<IAccount>> GetCachedAccountsAsync()
    {
        return await msalClient.GetAccountsAsync();
    }

    /// <summary>
    /// 为指定的账号静默获取令牌。
    /// 这是多账号登录的核心，我们明确指定要为哪个账号操作。
    /// </summary>
    public async Task<AuthenticationResult> AcquireTokenSilentAsync(IAccount account)
    {
        return await msalClient.AcquireTokenSilent(Scopes, account).ExecuteAsync();
    }

    /// <summary>
    /// 通过交互式流程添加一个新账号。
    /// </summary>
    public async Task<AuthenticationResult> AcquireTokenInteractiveAsync()
    {
        return await msalClient.AcquireTokenInteractive(Scopes).ExecuteAsync();
    }

    /// <summary>
    /// 【新增】从缓存中移除一个指定的账号（即“登出”）。
    /// </summary>
    public async Task RemoveAccountAsync(IAccount account)
    {
        await msalClient.RemoveAsync(account);
    }

    // --- 构造函数（已被静态工厂方法替代） ---
    // 私有化构造函数，强制使用异步的 CreateAsync 方法来正确设置缓存
    private MsalMicrosoftAuthenticator(string clientId)
    {
        var clientBuilder = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority("https://login.microsoftonline.com/consumers")
            .WithRedirectUri("http://localhost"); // 使用系统浏览器进行交互式登录

        msalClient = clientBuilder.Build();

        // 这个 HttpClient 仍然需要，但只用于调用 Microsoft 认证之外的 API (Xbox, Minecraft)
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// 异步工厂方法，用于创建和初始化 MsalMicrosoftAuthenticator 实例。
    /// 这是推荐的模式，因为令牌缓存的设置是异步的。
    /// </summary>
    public static async Task<MsalMicrosoftAuthenticator> CreateAsync(string clientId)
    {
        var authenticator = new MsalMicrosoftAuthenticator(clientId);

        var storageProperties =
         new StorageCreationPropertiesBuilder("onelauncher.msal.cache.dat", @"C:\Users\wwwin\OneLauncher\MsaPlayerData")
         .Build();

        authenticator.cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        authenticator.cacheHelper.RegisterCache(authenticator.msalClient.UserTokenCache);
        return authenticator;
    }

    /// <summary>
    /// 【新增方法】此方法将取代你旧的 AuthUseCode 和 RefreshToken 方法。
    /// 它会首先尝试静默获取令牌（自动处理刷新），如果需要用户交互，则启动浏览器进行登录。
    /// </summary>
    /// <returns>包含更新用户信息的 UserModel，如果认证失败则为 null。</returns>
    public async Task<UserModel?> LoginAsync()
    {
        try
        {
            AuthenticationResult msalResult = await AcquireTokenAsync();

            // 使用从 MSAL 获取的 AccessToken 完成后续的 Minecraft 登录流程
            return await ToLoandauth(msalResult.AccessToken);
        }
        catch (OlanException)
        {
            // 直接抛出我们已知的、处理过的异常
            throw;
        }
        catch (MsalClientException ex) when (ex.ErrorCode == "access_denied")
        {
            // 当用户在浏览器中明确取消登录时，MSAL会抛出此异常
            throw new OlanException(
                "操作已取消",
                "用户取消了 Microsoft 账户的授权操作。",
                OlanExceptionAction.Warning,
                ex);
        }
        catch (Exception ex)
        {
            // 捕获所有其他来自 MSAL 或后续流程的未知异常
            throw new OlanException(
                "认证流程异常",
                $"在认证流程中发生未知错误: {ex.Message}",
                OlanExceptionAction.Error,
                ex);
        }
    }

    /// <summary>
    /// 封装 MSAL 的令牌获取逻辑。
    /// </summary>
    private async Task<AuthenticationResult> AcquireTokenAsync()
    {
        // 尝试从缓存中获取账户信息
        IAccount? account = (await msalClient.GetAccountsAsync()).FirstOrDefault();

        try
        {
            // 尝试在后台静默获取令牌。如果令牌过期，此方法会自动使用刷新令牌换取新令牌。
            return await msalClient.AcquireTokenSilent(Scopes, account).ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            // 如果静默获取失败（例如：首次登录、会话过期、需要用户重新授权），
            // 则回退到交互式登录，这会打开一个浏览器窗口。
            return await msalClient.AcquireTokenInteractive(Scopes).ExecuteAsync();
        }
    }

    /// <summary>
    /// 【修改】此方法现在只接收一个参数：微软的 AccessToken。
    /// 它不再需要关心刷新令牌，因为 MSAL 会自动处理。
    /// </summary>
    private async Task<UserModel?> ToLoandauth(string microsoftAccessToken)
    {
        // 这里的逻辑与你原来的代码几乎完全相同，只是最后创建 UserModel 时不再需要处理刷新令牌。
        try
        {
            var xblAuthResponse = await AuthInXboxLive(microsoftAccessToken);
            var xstsAuthResponse = await AuthInXSTS(xblAuthResponse.Token, xblAuthResponse.DisplayClaims.Xui[0].Uhs);
            if (xstsAuthResponse == null)
            {
                throw new OlanException(
                    "Xbox Live 认证失败",
                    "无法通过XSTS验证Xbox Live令牌。这通常表示您的Xbox档案有问题，例如需要家长同意或未完成资料设置。",
                    OlanExceptionAction.Error
                );
            }

            var mcLoginResponse = await LoginWithXboxAsync(xstsAuthResponse.Token, xstsAuthResponse.DisplayClaims.Xui[0].Uhs);
            if (mcLoginResponse == null)
            {
                throw new OlanException(
                    "Minecraft 登录失败",
                    "无法使用Xbox凭据登录Minecraft服务。",
                    OlanExceptionAction.Error
                );
            }

            var entitlementsResponse = await CheckGameEntitlementsAsync(mcLoginResponse.AccessToken);
            if (entitlementsResponse?.Items == null || entitlementsResponse.Items.Length == 0)
            {
                throw new OlanException(
                    "未拥有Minecraft",
                    "该Microsoft账号未拥有Minecraft游戏。请确保您已购买游戏。",
                    OlanExceptionAction.Error
                );
            }

            var profileResponse = await GetMinecraftProfileAsync(mcLoginResponse.AccessToken);
            if (profileResponse == null)
            {
                throw new OlanException(
                    "获取Minecraft档案失败",
                    "无法获取Minecraft玩家档案。这可能意味着您的Minecraft账号未设置玩家名称或UUID。",
                    OlanExceptionAction.Error
                );
            }

            // 【关键修改】创建 UserModel 时，不再需要保存刷新令牌。
            // MSAL 的持久化缓存已经替我们完成了这项工作。
            // 你可以传递一个空值或修改 UserModel 的构造函数。
            return new UserModel(
                profileResponse.Name,
                Guid.Parse(profileResponse.Id),
                mcLoginResponse.AccessToken
            );
        }
        catch (OlanException)
        {
            throw; // 直接抛出已知的 OlanException
        }
        catch (Exception ex)
        {
            throw new OlanException(
                "Minecraft认证流程异常",
                $"在Minecraft认证流程中发生未知错误: {ex.Message}",
                OlanExceptionAction.Error,
                ex);
        }
    }
    private async Task<XboxLiveAuthResponse?> AuthInXboxLive(string accessToken)
    {
        // 1. 构建请求体 (Request Body)
        var requestBody = new XboxLiveAuthRequest
        {
            Properties = new XboxLiveAuthRequestProperties
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={accessToken}" // "RPS Ticket" 是微软对 Access Token 的一种称呼
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };

        // 2. 将请求体序列化为 JSON 并创建 StringContent
        var content = new StringContent(
            JsonSerializer.Serialize(requestBody, MsaJsonContext.Default.XboxLiveAuthRequest),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("https://user.auth.xboxlive.com/user/authenticate", content);

        // 4. 确保请求成功，否则会抛出 HttpRequestException
        response.EnsureSuccessStatusCode();

        // 5. 读取响应内容并反序列化为目标对象
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.XboxLiveAuthResponse);
    }

    /// <summary>
    /// XSTS 身份验证。
    /// </summary>
    private async Task<XSTSAuthResponse?> AuthInXSTS(string xblToken, string uhs)
    {
        var requestBody = new XSTSAuthRequest
        {
            Properties = new XSTSAuthRequestProperties
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xblToken }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody, MsaJsonContext.Default.XSTSAuthRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://xsts.auth.xboxlive.com/xsts/authorize", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = JsonSerializer.Deserialize(json, MsaJsonContext.Default.XSTSErrorResponse);
            if (errorResponse?.XErr == "214891605")
            {
                Debug.WriteLine("XSTS 认证失败: 该 Xbox Live 账号需要完成资料设置或家长同意。");
                // 返回 null，让上层统一抛 OlanException
            }
            else if (errorResponse?.XErr == "214891606")
            {
                Debug.WriteLine("XSTS 认证失败: 该微软账号未关联 Xbox Live 档案。请确保您已登录过 Xbox.com。");
                // 返回 null，让上层统一抛 OlanException
            }
            else
            {
                Debug.WriteLine($"XSTS 认证失败: {errorResponse?.Message} (XErr: {errorResponse?.XErr})");
                // 返回 null，让上层统一抛 OlanException
            }
            return null; // 返回 null，表示认证失败，上层会统一抛出 OlanException
        }

        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.XSTSAuthResponse);
    }

    /// <summary>
    /// 使用 Xbox 凭据登录 Minecraft。
    /// </summary>
    private async Task<MinecraftLoginResponse?> LoginWithXboxAsync(string xstsToken, string uhs)
    {
        var requestBody = new MinecraftLoginRequest
        {
            IdentityToken = $"XBL3.0 x={uhs};{xstsToken}"
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody, MsaJsonContext.Default.MinecraftLoginRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://api.minecraftservices.com/authentication/login_with_xbox", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.MinecraftLoginResponse);
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
        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.EntitlementsResponse);
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
            return null; // 返回 null，让上层统一抛 OlanException
        }
        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.MinecraftProfileResponse);
    }

    public void Dispose()
    {
        httpClient?.Dispose();
    }
}

/*
public class MicrosoftAuthenticator : IDisposable
{
    private readonly string clientId;
    private readonly HttpClient httpClient;

    public MicrosoftAuthenticator()
    {
        // 确保 Init.AzureApplicationID 可访问
        this.clientId = Init.AzureApplicationID;
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// 使用旧的刷新令牌获取新的访问令牌和刷新令牌，并完成后续的 Minecraft 登录流程。
    /// </summary>
    /// <param name="oldRefreshToken">旧的刷新令牌。</param>
    /// <returns>包含更新用户信息的 UserModel，如果刷新失败则为 null。</returns>
    public async Task<UserModel?> RefreshToken(string oldRefreshTokenID)
    {
        string oldRefreshToken = Init.Security.GetRefreshToken(oldRefreshTokenID);
        try
        {
            var content = new StringContent(
                $"client_id={clientId}&refresh_token={oldRefreshToken}&grant_type=refresh_token&scope=XboxLive.signin%20offline_access",
                Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await httpClient.PostAsync("https://login.microsoftonline.com/consumers/oauth2/v2.0/token", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize(json, MsaJsonContext.Default.TokenResponse);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken) || string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                // 抛出 OlanException
                throw new OlanException(
                    "刷新令牌失败",
                    "返回的令牌信息不完整，无法刷新您的Microsoft账户令牌。",
                    OlanExceptionAction.Error
                );
            }

            return await ToLoandauth(tokenResponse.AccessToken, tokenResponse.RefreshToken,oldRefreshTokenID);
        }
        catch (HttpRequestException ex)
        {
            // 捕获网络请求异常
            throw new OlanException(
                "网络请求失败",
                $"在刷新Microsoft令牌时发生网络错误: {ex.Message}",
                OlanExceptionAction.Error,
                ex
            );
        }
        catch (JsonException ex)
        {
            // 捕获 JSON 解析异常
            throw new OlanException(
                "数据解析失败",
                $"无法解析Microsoft令牌响应，数据格式不正确: {ex.Message}",
                OlanExceptionAction.Error,
                ex
            );
        }
        catch (Exception ex)
        {
            // 捕获其他未知异常
            throw new OlanException(
                "刷新令牌异常",
                $"刷新Microsoft令牌时发生未知错误: {ex.Message}",
                OlanExceptionAction.Error,
                ex
            );
        }
    }

    public static bool IsExpired(DateTime lastTime)
    {
        return Math.Abs((DateTime.UtcNow - lastTime).TotalHours) > 23;
    }

    private async Task<UserModel?> ToLoandauth(string microsoftAccessToken, string microsoftRefreshToken,string oldID = null)
    {
        try
        {
            // 3. Xbox Live 身份验证
            var xblAuthResponse = await AuthInXboxLive(microsoftAccessToken);

            // 4. XSTS 身份验证
            var xstsAuthResponse = await AuthInXSTS(xblAuthResponse.Token, xblAuthResponse.DisplayClaims.Xui[0].Uhs);
            if (xstsAuthResponse == null) // XSTSAuth 内部已处理错误并返回 null
            {
                throw new OlanException(
                    "Xbox Live 认证失败",
                    "无法通过XSTS验证Xbox Live令牌。这通常表示您的Xbox档案有问题，例如需要家长同意或未完成资料设置。",
                    OlanExceptionAction.Error
                );
            }

            // 5. 获取 Minecraft 访问令牌
            var mcLoginResponse = await LoginWithXboxAsync(xstsAuthResponse.Token, xstsAuthResponse.DisplayClaims.Xui[0].Uhs);
            if (mcLoginResponse == null)
            {
                throw new OlanException(
                    "Minecraft 登录失败",
                    "无法使用Xbox凭据登录Minecraft服务。",
                    OlanExceptionAction.Error
                );
            }

            // 6. 检查游戏拥有情况
            var entitlementsResponse = await CheckGameEntitlementsAsync(mcLoginResponse.AccessToken);
            if (entitlementsResponse == null || entitlementsResponse.Items == null || entitlementsResponse.Items.Length == 0)
            {
                Debug.WriteLine("该账号未拥有 Minecraft 游戏。");
                throw new OlanException(
                    "未拥有Minecraft",
                    "该Microsoft账号未拥有Minecraft游戏。请确保您已购买游戏。",
                    OlanExceptionAction.Error
                );
            }

            // 7. 获取玩家 UUID 和用户名
            var profileResponse = await GetMinecraftProfileAsync(mcLoginResponse.AccessToken);
            if (profileResponse == null)
            {
                throw new OlanException(
                    "获取Minecraft档案失败",
                    "无法获取Minecraft玩家档案。这可能意味着您的Minecraft账号未设置玩家名称或UUID。",
                    OlanExceptionAction.Error
                );
            }

            // 所有步骤成功后，创建并返回 UserModel
            return new UserModel(
                profileResponse.Name,
                Guid.Parse(profileResponse.Id),
                mcLoginResponse.AccessToken,
                Init.Security.SetRefreshToken(microsoftRefreshToken, oldID ?? null)
            );
        }
        catch (OlanException)
        {
            // 如果是 OlanException，直接抛出
            throw;
        }
        catch (Exception ex)
        {
            // 捕获其他未知异常
            throw new OlanException(
                "Minecraft认证流程异常",
                $"在Minecraft认证流程中发生未知错误: {ex.Message}",
                OlanExceptionAction.Error,
                ex
            );
        }
    }

    /// <summary>
    /// 使用设备代码流进行 Minecraft 认证。
    /// </summary>
    /// <param name="OnUserNeedAction">用于报告用户需要执行的操作的回调。</param>
    /// <param name="pollIntervalSeconds">轮询用户授权状态的间隔时间（秒）。</param>
    /// <returns>包含用户信息的 UserModel，如果认证失败则为 null。</returns>
    public async Task<UserModel?> AuthUseCode(IProgress<(string VerityUrl, string UserCode)> OnUserNeedAction, int pollIntervalSeconds = 5)
    {
        try
        {
            // 1. 获取设备代码对
            var deviceCodeResponse = await GetDeviceCode();
            if (deviceCodeResponse == null)
            {
                throw new OlanException(
                    "获取设备代码失败",
                    "无法从Microsoft获取设备代码，请检查网络连接。",
                    OlanExceptionAction.Error
                );
            }

            OnUserNeedAction.Report((deviceCodeResponse.VerificationUri, deviceCodeResponse.UserCode));

            // 2. 轮询用户授权状态
            var tokenResponse = await PollAutoState(
                deviceCodeResponse.DeviceCode,
                Math.Max(pollIntervalSeconds, deviceCodeResponse.Interval),
                deviceCodeResponse.ExpiresIn);
            if (tokenResponse == null)
            {
                throw new OlanException(
                    "用户未完成授权",
                    "您未在指定时间内完成Microsoft设备代码授权。",
                    OlanExceptionAction.Error
                );
            }

            return await ToLoandauth(tokenResponse.AccessToken, tokenResponse.RefreshToken);
        }
        catch (OperationCanceledException ex)
        {
            // 用户取消了授权操作
            throw new OlanException(
                "操作取消",
                "用户取消了Microsoft账户的授权操作。",
                OlanExceptionAction.Warning, // 这是一个警告，而不是致命错误
                ex
            );
        }
        catch (OlanException)
        {
            // 如果是 OlanException，直接抛出
            throw;
        }
        catch (Exception ex)
        {
            // 捕获其他未知异常
            throw new OlanException(
                "设备代码认证异常",
                $"在使用设备代码流进行认证时发生未知错误: {ex.Message}",
                OlanExceptionAction.Error,
                ex
            );
        }
    }

    /// <summary>
    /// 以代码流验证方式请求用户授权。
    /// </summary>
    private async Task<DeviceCodeResponse?> GetDeviceCode()
    {
        var response = await httpClient.PostAsync(
            "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode",
            new StringContent(
                $"client_id={clientId}&scope=XboxLive.signin%20offline_access",
                Encoding.UTF8, "application/x-www-form-urlencoded"));
        response.EnsureSuccessStatusCode(); // 如果失败会抛出 HttpRequestException
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.DeviceCodeResponse);
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
                return JsonSerializer.Deserialize(json, MsaJsonContext.Default.TokenResponse);
            else
            {
                var errorResponse = JsonSerializer.Deserialize(json, MsaJsonContext.Default.ErrorResponse);
                switch (errorResponse?.Error)
                {
                    case "authorization_pending":
                        // 继续轮询
                        break;
                    case "authorization_declined":
                        Debug.WriteLine("用户拒绝了授权。");
                        return null; // 用户明确拒绝
                    case "expired_token":
                        Debug.WriteLine("授权已过期。");
                        return null; // 授权已过期
                    default:
                        Debug.WriteLine($"认证轮询错误: {errorResponse?.Error} - {errorResponse?.ErrorDescription}");
                        // 对于其他未知错误，也返回null，让上层抛出OlanException
                        return null;
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(interval));
        }
        Debug.WriteLine("轮询超时，用户未在规定时间内完成授权。");
        return null; // 轮询超时
    }

    /// <summary>
    /// 进行 Xbox Live 身份验证。
    /// </summary>
    /// <param name="accessToken">上一步中获取的accessToken</param>
    private async Task<XboxLiveAuthResponse?> AuthInXboxLive(string accessToken)
    {
        var requestBody = new XboxLiveAuthRequest
        {
            Properties = new XboxLiveAuthRequestProperties
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={accessToken}"
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody, MsaJsonContext.Default.XboxLiveAuthRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://user.auth.xboxlive.com/user/authenticate", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.XboxLiveAuthResponse);
    }

    /// <summary>
    /// XSTS 身份验证。
    /// </summary>
    private async Task<XSTSAuthResponse?> AuthInXSTS(string xblToken, string uhs)
    {
        var requestBody = new XSTSAuthRequest
        {
            Properties = new XSTSAuthRequestProperties
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xblToken }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody, MsaJsonContext.Default.XSTSAuthRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://xsts.auth.xboxlive.com/xsts/authorize", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = JsonSerializer.Deserialize(json, MsaJsonContext.Default.XSTSErrorResponse);
            if (errorResponse?.XErr == "214891605")
            {
                Debug.WriteLine("XSTS 认证失败: 该 Xbox Live 账号需要完成资料设置或家长同意。");
                // 返回 null，让上层统一抛 OlanException
            }
            else if (errorResponse?.XErr == "214891606")
            {
                Debug.WriteLine("XSTS 认证失败: 该微软账号未关联 Xbox Live 档案。请确保您已登录过 Xbox.com。");
                // 返回 null，让上层统一抛 OlanException
            }
            else
            {
                Debug.WriteLine($"XSTS 认证失败: {errorResponse?.Message} (XErr: {errorResponse?.XErr})");
                // 返回 null，让上层统一抛 OlanException
            }
            return null; // 返回 null，表示认证失败，上层会统一抛出 OlanException
        }

        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.XSTSAuthResponse);
    }

    /// <summary>
    /// 使用 Xbox 凭据登录 Minecraft。
    /// </summary>
    private async Task<MinecraftLoginResponse?> LoginWithXboxAsync(string xstsToken, string uhs)
    {
        var requestBody = new MinecraftLoginRequest
        {
            IdentityToken = $"XBL3.0 x={uhs};{xstsToken}"
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody, MsaJsonContext.Default.MinecraftLoginRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://api.minecraftservices.com/authentication/login_with_xbox", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.MinecraftLoginResponse);
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
        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.EntitlementsResponse);
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
            return null; // 返回 null，让上层统一抛 OlanException
        }
        return JsonSerializer.Deserialize(json, MsaJsonContext.Default.MinecraftProfileResponse);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
*/