using OneLauncher.Core.Global;
using OneLauncher.Core.Net.msa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.Models;
public class UserModel
{
    public const string nullToken = "00000000-0000-0000-0000-000000000000";


    #region 构造函数
    /// <summary>
    /// 主要构造函数。
    /// </summary>
    public UserModel(
        Guid UserID,
        string name,
        Guid uuid,
        // 下面的仅限正版用户
        string? accessToken = null,
        string? accountID = null,
        int? accessTokenExpiration = null)
    {
        this.UserID = UserID;
        Name = name;
        this.uuid = uuid;

        if (string.IsNullOrEmpty(accessToken) || accessToken == nullToken)
        {
            AccessToken = nullToken;
            IsMsaUser = false;
            AccountID = null;
            AccessTokenExpiration = null;
        }
        else
        {
            IsMsaUser = true;
            AccessToken = accessToken;
            AccountID = accountID;
            AccessTokenExpiration = DateTimeOffset.UtcNow.AddSeconds(accessTokenExpiration ?? 86400);
        }
    }
    [JsonConstructor]
    public UserModel(
        Guid UserID,
        string Name,
        Guid uuid,
        string accessToken,
        bool IsMsaUser,
        string? AccountID,
        DateTimeOffset? AccessTokenExpiration
        )
    {
        this.UserID = UserID;
        this.Name = Name;
        this.uuid = uuid;
        AccessToken = accessToken;
        this.IsMsaUser = IsMsaUser;
        this.AccountID = AccountID;
        this.AccessTokenExpiration = AccessTokenExpiration;
    }

    #endregion

    /// <summary>
    /// 【新】智能登录方法。
    /// 检查自身令牌是否过期，如果过期则尝试刷新，并返回一个包含最新状态的新实例。
    /// </summary>
    /// <returns>
    /// 【警告】此方法返回一个新的实例，而不是修改我自己
    /// </returns>
    public async Task<UserModel> IntelligentLogin(MsalAuthenticator authenticator)
    {
        // 如果不是正版用户，或令牌未过期，则直接返回自身，无需任何操作。
        if (!IsMsaUser || AccessTokenExpiration.HasValue && AccessTokenExpiration.Value > DateTimeOffset.UtcNow)
        {
            return this;
        }

        // 令牌已过期，需要刷新。检查能否刷新。
        if (authenticator == null || string.IsNullOrEmpty(AccountID))
        {
            // 缺少刷新所需的工具或信息，返回自身。
            return this;
        }

        // 从认证器缓存中找到对应的微软账户
        var accountToRefresh = await Tools.UseAccountIDToFind(AccountID);
        if (accountToRefresh == null)
        {
            return this;
        }

        try
        {
            // 调用认证器执行刷新流程
            UserModel r = await authenticator.TryToGetMinecraftMojangAccessTokenForLoginedAccounts(accountToRefresh) ?? this;

            // 如果刷新成功，返回那个全新的 UserModel并更改自身；如果失败（返回null），则返回当前的（已过期的）实例。
            AccessToken = r.AccessToken;
            AccessTokenExpiration = r.AccessTokenExpiration;
            _=Init.AccountManager.Save();
            return r;
        }
        catch (Exception)
        {
            // 刷新过程中发生异常，返回当前实例。
            return this;
        }
    }

    public override string ToString()
    {
        return IsMsaUser ? "正版登入" : "离线登入";
    }

    // --- 属性 ---
    public Guid UserID { get; set; }
    public string Name { get; set; }
    public Guid uuid { get; set; }
    public string AccessToken { get; set; }
    public bool IsMsaUser { get; set; }
    public string? AccountID { get; set; }
    public DateTimeOffset? AccessTokenExpiration { get; set; }
}