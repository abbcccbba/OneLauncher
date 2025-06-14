using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace OneLauncher.Core.Helper;
public struct ModType
{
    public bool IsFabric { get; set; }
    public bool IsNeoForge { get; set; }
}
public enum ModEnum
{
    none,
    fabric,
    neoforge
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
    /// <param name="Size">文件大小（单位字节）</param>
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
    /// <param ID="name">版本标识符</param>
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
public struct UserModel
{
    public const string nullToken = "00000000-0000-0000-0000-000000000000";
    /// <param name="accessToken">访问令牌</param>
    /// <param name="refreshToken">刷新令牌</param>
    public UserModel(string Name, Guid uuid, string? accessToken = null)
    {
        if (accessToken == null)
        {
            this.accessToken = nullToken;
            IsMsaUser = false;
        }
        else
        {
            IsMsaUser = true;
            this.accessToken = accessToken;
        }
        AuthTime = DateTime.UtcNow;
        this.Name = Name;
        this.uuid = uuid;
    }
    public override string ToString()
    {
        return IsMsaUser ? "正版登入" : "离线登入";
    }

    public string Name { get; set; }
    public Guid uuid { get; set; }
    public string? accessToken { get; set; }
    public bool IsMsaUser { get; set; }

    public DateTime? AuthTime { get; set; }
}
