using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;

namespace OneLauncher.Core.Net.Account.Yggdrasil;

/// <summary>
/// 为 Yggdrasil 认证服务器提供认证逻辑的内部抽象基类。
/// </summary>
public abstract class YggdrasilAuthenticator
{
    private readonly HttpClient _httpClient;
    protected abstract string AuthApiRoot { get; }

    internal YggdrasilAuthenticator()
    {
        _httpClient = new();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OneLauncher/" + Init.OneLauncherVersoin);
    }

    /// <summary>
    /// 使用用户名和密码进行认证。
    /// </summary>
    public async Task<UserModel> AuthenticateUseUserNameAndPasswordAsync(string username, string password)
    {
        var endpoint = $"{AuthApiRoot}/authserver/authenticate";

        var payload = new AuthRequest
        {
            Username = username,
            Password = password,
            ClientToken = Guid.NewGuid().ToString()
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, payload, YggdrasilJsonContext.Default.AuthRequest);
            return await ProcessAuthResponse(response, AuthApiRoot, Guid.NewGuid());
        }
        catch (Exception ex) when (ex is not OlanException)
        {
            throw new OlanException("网络错误", $"连接到认证服务器 '{AuthApiRoot}' 时出错: {ex.Message}", OlanExceptionAction.Error, ex);
        }
    }

    public async Task<UserModel> RefreshAccessTokenAsync(UserModel userToRefresh)
    {
        if (userToRefresh.YggdrasilInfo == null)
            throw new OlanException("认证错误", "用户信息中缺少 Yggdrasil 信息，无法刷新会话。", OlanExceptionAction.Error);

        var yggdrasilInfo = userToRefresh.YggdrasilInfo.Value;
        var endpoint = $"{yggdrasilInfo.AuthUrl}/authserver/refresh";

        var payload = new RefreshRequest
        {
            AccessToken = userToRefresh.AccessToken,
            ClientToken = yggdrasilInfo.ClientToken.ToString()
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, payload, YggdrasilJsonContext.Default.RefreshRequest);
            return await ProcessAuthResponse(response, yggdrasilInfo.AuthUrl, userToRefresh.UserID);
        }
        catch (Exception ex) when (ex is not OlanException)
        {
            throw new OlanException("网络错误", $"刷新会话时出错: {ex.Message}", OlanExceptionAction.Error, ex);
        }
    }
    private async Task<UserModel> ProcessAuthResponse(HttpResponseMessage response, string authUrl, Guid userId)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(YggdrasilJsonContext.Default.ErrorResponse);
            throw new OlanException("认证失败", error?.ErrorMessage ?? "服务器返回了未知的错误。", OlanExceptionAction.Error);
        }

        var authData = await response.Content.ReadFromJsonAsync<AuthResponse>(YggdrasilJsonContext.Default.AuthResponse);
        if (authData?.SelectedProfile == null)
            throw new OlanException("认证失败", "服务器未返回有效的玩家档案。", OlanExceptionAction.Error);

        return new UserModel(
            UserID: userId,
            name: authData.SelectedProfile.Name,
            uuid: Guid.Parse(authData.SelectedProfile.Id),
            accessToken: authData.AccessToken,
            yggdrasilInfo: new YggdrasilInfo(Guid.Parse(authData.ClientToken), authUrl)
        );
    }
}