using Microsoft.Identity.Client;
using OneLauncher.Core.Net.msa;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OneLauncher.Core.Helper.Models;

public enum SortingType
{
    AnTime_OldFront,
    AnTime_NewFront,
    AnVersion_OldFront,
    AnVersion_NewFront,
}
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
    public bool IsQuilt { get; set; }
    public ModEnum ToModEnum()
    {
        if (IsFabric)
            return ModEnum.fabric;
        if (IsNeoForge)
            return ModEnum.neoforge;
        if (IsForge)
            return ModEnum.forge;
        if (IsQuilt) 
            return ModEnum.quilt;

        return ModEnum.none;
    }
    public static bool operator ==(ModType left, ModEnum right)
    {
        if (left.IsFabric && right == ModEnum.fabric)
            return true;
        else if (left.IsNeoForge && right == ModEnum.neoforge)
            return true;
        else if (left.IsForge && right == ModEnum.forge)
            return true;
        else if (left.IsQuilt && right == ModEnum.quilt) 
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
    forge,
    quilt
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
    public Guid UserID { get; set; }
    public string Name { get; set; }
    public Guid uuid { get; set; }
    public string AccessToken { get; set; }
    public bool IsMsaUser { get; set; }
    public string? AccountID { get; set; }
    public DateTimeOffset? AccessTokenExpiration { get; set; }
}