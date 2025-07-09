using Duende.IdentityModel.OidcClient;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Net.Account.Microsoft.JsonModels;



#if WINDOWS
using Microsoft.Identity.Client.Broker;
#endif
namespace OneLauncher.Core.Net.Account.Microsoft;

public class MsalAuthenticator : IDisposable
{
    private readonly IPublicClientApplication msalClient;
    private MsalCacheHelper cacheHelper; 
    private readonly HttpClient httpClient;

    private static readonly string[] Scopes = { "XboxLive.signin", "offline_access" };
    public Task<IEnumerable<IAccount>> GetCachedAccounts()
        =>msalClient.GetAccountsAsync();
    
    public Task<AuthenticationResult> AcquireTokenSilentc(IAccount account)
        => msalClient.AcquireTokenSilent(Scopes, account).ExecuteAsync();
#if WINDOWS
    /// <summary>
    /// 作为新账号弹出使用仅限微软视窗操作系统的系统内置级窗口以高安全性的形式以弹出交互式获取微软到麻将访问令牌并返回
    /// </summary>
    public async Task<UserModel?> LoginNewAccountToGetMinecraftMojangAccessTokenUseWindowsWebAccountManger(IntPtr windowHandle)
    {
        try
        {
            var brokerOptions = new BrokerOptions(BrokerOptions.OperatingSystems.Windows);
            AuthenticationResult authResult;
            authResult = await msalClient.AcquireTokenInteractive(Scopes)
                .WithParentActivityOrWindow(windowHandle) // 传入从UI层获取的句柄
                .ExecuteAsync();
            return await ToLoandauth(authResult.AccessToken, authResult.Account);
        }
        catch (MsalClientException ex) when (ex.ErrorCode == MsalError.AuthenticationCanceledError)
        {
            Debug.WriteLine("用户取消了授权");
            return null;
        }
    }
#endif
    /// <summary>
    /// 作为新账号弹出使用系统默认浏览器以弹出交互式获取微软到麻将访问令牌并返回
    /// </summary>
    public async Task<UserModel?> LoginNewAccountToGetMinecraftMojangAccessTokenOnSystemBrowser()
    {
        try
        {
            var authResult = await msalClient.AcquireTokenInteractive(Scopes)
                .ExecuteAsync();

            return await ToLoandauth(authResult.AccessToken, authResult.Account);
        }
        catch (MsalClientException ex) when (ex.ErrorCode == MsalError.AuthenticationCanceledError)
        {
            return null;
        }
    }
    public Task RemoveAccount(IAccount account)
        =>msalClient.RemoveAsync(account);

    private MsalAuthenticator(string clientId)
    {
        var clientBuilder = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority("https://login.microsoftonline.com/consumers")
            .WithDefaultRedirectUri();

#if WINDOWS
        // 仅在 Windows 平台上编译时，才添加 Broker 配置
        var brokerOptions = new BrokerOptions(BrokerOptions.OperatingSystems.Windows);
        clientBuilder.WithBroker(brokerOptions);
#endif

        msalClient = clientBuilder.Build();
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    public static async Task<MsalAuthenticator> CreateAsync(string clientId)
    {
        var authenticator = new MsalAuthenticator(clientId);

        var storageProperties =
         new StorageCreationPropertiesBuilder("onelauncher.msal.cache.dat", Path.Combine( Init.BasePath,"playerdata"))
         .Build();
        authenticator.cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        authenticator.cacheHelper.RegisterCache(authenticator.msalClient.UserTokenCache);
        return authenticator;
    }
    public async Task<UserModel?> TryToGetMinecraftMojangAccessTokenForLoginedAccounts(IAccount account)
    {
        try
        {
            AuthenticationResult msalResult = await TryToGetMicrosoftAccessToken(account);
            return await ToLoandauth(msalResult.AccessToken,account);
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
    private async Task<AuthenticationResult> TryToGetMicrosoftAccessToken(IAccount account)
    {
        try
        {
            return await msalClient.AcquireTokenSilent(Scopes, account).ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            return await msalClient.AcquireTokenInteractive(Scopes).ExecuteAsync();
        }
    }
    #region 实现了从微软令牌到麻将令牌
    private async Task<UserModel?> ToLoandauth(string microsoftAccessToken,IAccount account)
    {
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

            return new UserModel(
                Guid.NewGuid(),
                profileResponse.Name,
                Guid.Parse(profileResponse.Id),
                mcLoginResponse.AccessToken,
                account.HomeAccountId.Identifier,
                mcLoginResponse.expires_in
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
        return await JsonSerializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(), MsaJsonContext.Default.XboxLiveAuthResponse);
    }
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

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await JsonSerializer.DeserializeAsync(response.Content.ReadAsStream(), MsaJsonContext.Default.XSTSErrorResponse);
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
        return await JsonSerializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(), MsaJsonContext.Default.XSTSAuthResponse);
    }
    private async Task<MinecraftLoginResponse?> LoginWithXboxAsync(string xstsToken, string uhs)
    {
        var requestBody = new MinecraftLoginRequest
        {
            IdentityToken = $"XBL3.0 x={uhs};{xstsToken}"
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody, MsaJsonContext.Default.MinecraftLoginRequest), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://api.minecraftservices.com/authentication/login_with_xbox", content);
        response.EnsureSuccessStatusCode();
        return await JsonSerializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(), MsaJsonContext.Default.MinecraftLoginResponse);
    }
    private async Task<EntitlementsResponse?> CheckGameEntitlementsAsync(string minecraftAccessToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", minecraftAccessToken);
        var response = await httpClient.GetAsync("https://api.minecraftservices.com/entitlements/mcstore");
        response.EnsureSuccessStatusCode();
        return await JsonSerializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(), MsaJsonContext.Default.EntitlementsResponse);
    }
    private async Task<MinecraftProfileResponse?> GetMinecraftProfileAsync(string minecraftAccessToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", minecraftAccessToken);
        var response = await httpClient.GetAsync("https://api.minecraftservices.com/minecraft/profile");

        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine("获取 Minecraft 档案失败。可能此账号没有设置 Minecraft 档案（例如首次启动游戏时）。");
            return null; // 返回 null，让上层统一抛 OlanException
        }
        Debug.WriteLine(await response.Content.ReadAsStringAsync());
        return await JsonSerializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(), MsaJsonContext.Default.MinecraftProfileResponse);
    }
    #endregion
    public void Dispose()
    {
        httpClient?.Dispose();
    }
}
