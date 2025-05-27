using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneLauncher.Core;

public static class Tools 
{
    /// <summary>
    /// 过滤出Minecraft的纯粹正式版版本号。
    /// </summary>
    /// <param name="versions">包含所有Minecraft版本名称的列表。</param>
    /// <returns>只包含纯粹正式版版本号的列表。</returns>
    public static List<string> McVsFilter(List<string> versions)
    {
        Regex IS_SNAPSHOT_OR_DEV_VARIANT = new Regex(
            @"^\d{2}w\d{2}.*$", // 匹配 YYwWW 后面跟着任意字符（包括空）直到字符串结束
            RegexOptions.IgnoreCase | RegexOptions.Compiled 
        );
        Regex IS_RELEASE_WITH_SUFFIX = new Regex(
            @"^\d+\.\d+(\.\d+)?-.*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        Regex IS_PREFIXED_ALPHA_BETA_RC = new Regex(
            @"^[abr]c?\d+\.\d+(\.\d+)?.*$", // 匹配 a/b/rc 后接 X.Y 或 X.Y.Z，再接任意后缀
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        List<string> officialVersions = versions
                .Where(version =>
                    !IS_SNAPSHOT_OR_DEV_VARIANT.IsMatch(version) &&
                    !IS_RELEASE_WITH_SUFFIX.IsMatch(version) &&
                    !IS_PREFIXED_ALPHA_BETA_RC.IsMatch(version)
                )
                .ToList();
        return officialVersions;
    }
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
    /// <param name="Size">文件大小（单位字节）</param>
    public NdDowItem(string Url, string Path,int Size,string? Sha1 = null)
    {
        this.url = Url;
        this.path = Path;
        if(Sha1 != null)
            this.sha1 = Sha1;
    }
    public string url;
    public string path;
    public int size;
    public string? sha1;
}
public struct aVersion
{
    public string VersionID { get; set; }
    public bool IsMod { get; set; }//预留
    public bool IsVersionIsolation { get; set; }
    public DateTime AddTime { get; set; } 
}
public class VersionBasicInfo
{
    /// <param ID="name">版本标识符</param>
    /// <param ID="type">版本类型</param>
    /// <param ID="url">版本文件下载地址</param>
    /// <param ID="time">版本发布时间</param>
    public VersionBasicInfo(string ID, string type, DateTime time,string url)
    {
        this.ID = ID;
        this.type = type;
        this.time = time;
        this.Url = url;
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
public struct UserModel
{
    /// <param name="accessToken">访问令牌</param>
    /// <param name="refreshToken">刷新令牌</param>
    public UserModel(string Name, Guid uuid, string? accessToken = null, string? refreshToken = null)
    {
        if (accessToken == null)
        {
            this.accessToken = "0000-0000-0000-0000";
            this.refreshToken = "0000-0000-0000-0000";
            this.userType = "legacy";
        }
        else
        {
            userType = "msa";
            this.accessToken = accessToken;
            this.refreshToken = refreshToken ?? string.Empty;
        }
        this.AuthTime = DateTime.UtcNow;
        this.Name = Name;
        this.uuid = uuid;
    }
    public override string ToString()
    {
        return (userType == "msa" ? "正版登入" : "离线登入");
    }

    public string Name { get; set; }
    public Guid uuid { get; set; }
    public string accessToken { get; set; }
    public string userType { get; set; }

    
    public string refreshToken { get; set; }
    public DateTime AuthTime { get; set; } 
}
public enum SystemType
{
    windows,
    osx,
    linux
}
