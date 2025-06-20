using Microsoft.Identity.Client;
using OneLauncher.Core.Net.msa;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OneLauncher.Core.Helper;
public struct ServerInfo
{
    public string Ip;
    public string Port;
}
public struct ModType
{
    public bool IsFabric { get; set; }
    public bool IsNeoForge { get; set; }
    public bool IsForge { get; set; }
    public static bool operator ==(ModType left, ModEnum right)
    {
        if (left.IsFabric && right == ModEnum.fabric)
            return true;
        else if (left.IsNeoForge && right == ModEnum.neoforge)
            return true;
        else if (left.IsForge && right == ModEnum.forge)
            return true;
        else
            return false;
    }
    public static bool operator !=(ModType left, ModEnum right)
        => !(left == right);
}
public enum ModEnum
{
    none,
    fabric,
    neoforge,
    forge
}
public struct PreferencesLaunchMode
{
    public ModEnum LaunchModType { get; set; }
    public bool IsUseDebugModeLaunch { get; set; }
}


/// <summary>
/// 描述单个下载项
/// </summary>
/// 
public struct NdDowItem
{
    /// <param ID="Url">下载地址</param>
    /// <param ID="Sha1">SHA1校验码</param>
    /// <param ID="Path">保存地址（含文件名）</param>
    /// <param Name="Size">文件大小（单位字节）</param>
    public NdDowItem(string Url, string Path, int Size, string? Sha1 = null)
    {
        url = Url;
        path = Path;
        if (Sha1 != null)
            sha1 = Sha1;
    }
    public string url;
    public string path;
    public int size;
    public string? sha1;
}
public class UserVersion
{
    public string VersionID { get; set; }
    public ModType modType { get; set; }
    public bool IsVersionIsolation { get; set; }
    public DateTime AddTime { get; set; }
    public PreferencesLaunchMode preferencesLaunchMode { get; set; }
    public override string ToString()
    {
        return VersionID;
    }
}
// 不要把他改成结构体，不然会出一些神奇的问题
public class VersionBasicInfo
{
    /// <param ID="Name">版本标识符</param>
    /// <param ID="type">版本类型</param>
    /// <param ID="url">版本文件下载地址</param>
    /// <param ID="time">版本发布时间</param>
    public VersionBasicInfo(string ID, string type, DateTime time, string url)
    {
        this.ID = ID;
        this.type = type;
        this.time = time;
        Url = url;
    }
    // 如果不重写该方法 AutoCompleteBox 会报错
    public override string ToString()
    {
        return ID.ToString();
    }
    public string ID { get; set; }
    public string type { get; set; }
    public DateTime time { get; set; }
    public string Url { get; set; }
}
public enum SystemType
{
    windows,
    osx,
    linux
}
public class UserModel
{
    public const string nullToken = "00000000-0000-0000-0000-000000000000";

    #region 构造函数
    [JsonConstructor] 
    public UserModel(
        string Name,
        Guid uuid,
        string accessToken,
        bool IsMsaUser,
        string? AccountID,
        DateTimeOffset? AccessTokenExpiration 
        )
    {
        this.Name = Name;
        this.uuid = uuid;
        this.AccessToken = accessToken;
        this.IsMsaUser = IsMsaUser;
        this.AccountID = AccountID;
        this.AccessTokenExpiration = AccessTokenExpiration;
    }
    /// <summary>
    /// 主要构造函数。
    /// </summary>
    public UserModel(
        string name, 
        Guid uuid, 
        // 下面的仅限正版用户
        string? accessToken = null, 
        string? accountID = null, 
        int? accessTokenExpiration = null)
    {
        this.Name = name;
        this.uuid = uuid;

        if (string.IsNullOrEmpty(accessToken) || accessToken == nullToken)
        {
            this.AccessToken = nullToken;
            IsMsaUser = false;
            AccountID = null;
            this.AccessTokenExpiration = null;
        }
        else
        {
            IsMsaUser = true;
            this.AccessToken = accessToken;
            AccountID = accountID;
            this.AccessTokenExpiration = DateTimeOffset.UtcNow.AddSeconds((double)(accessTokenExpiration ?? 86400));
        }
    }

    #endregion

    /// <summary>
    /// 【新】智能登录方法。
    /// 检查自身令牌是否过期，如果过期则尝试刷新，并返回一个包含最新状态的新实例。
    /// </summary>
    public async Task<UserModel> IntelligentLogin(MsalMicrosoftAuthenticator authenticator)
    {
        // 如果不是正版用户，或令牌未过期，则直接返回自身，无需任何操作。
        if (!IsMsaUser || (AccessTokenExpiration.HasValue && AccessTokenExpiration.Value > DateTimeOffset.UtcNow))
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
            AccessToken= r.AccessToken;
            AccessTokenExpiration=r.AccessTokenExpiration;
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
    public string Name { get; set; }
    public Guid uuid { get; set; }
    public string AccessToken { get; set; }
    public bool IsMsaUser { get; set; }
    public string? AccountID { get; set; }
    public DateTimeOffset? AccessTokenExpiration { get; set; }
}