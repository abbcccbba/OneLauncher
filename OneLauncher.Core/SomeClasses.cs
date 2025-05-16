using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core;

/// <summary>
/// 描述单个下载项
/// </summary>
/// 
public class NdDowItem
{
    /// <param name="Url">下载地址</param>
    /// <param name="Path">保存地址（含文件名）</param>
    public NdDowItem(string Url, string Path)
    {
        this.url = Url;
        this.path = Path;
    }
    /// <param name="Url">下载地址</param>
    /// <param name="Sha1">SHA1校验码</param>
    /// <param name="Path">保存地址（含文件名）</param>
    public NdDowItem(string Url, string Sha1, string Path)
    {
        this.url = Url;
        this.sha1 = Sha1;
        this.path = Path;
    }
    public string url;
    public string? sha1;
    public string path;
}
public class aVersion
{
    public string VersionID { get; set; }
    public bool IsMod { get; set; }//预留
    public DateTime AddTime { get; set; } 
}
public class VersionBasicInfo
{
    /// <param name="name">版本标识符</param>
    /// <param name="type">版本类型</param>
    /// <param name="url">版本文件下载地址</param>
    /// <param name="time">版本发布时间</param>
    public VersionBasicInfo(string name, string type, string url, DateTime time)
    {
        this.name = name;
        this.type = type;
        this.time = time;
        this.url = url;
    }
    // 如果不重写该方法 AutoCompleteBox 会报错
    public override string ToString()
    {
        return name;
    }
    public string name { get; set; }
    public string type { get; set; }
    public DateTime time { get; set; }
    public string url { get; set; }
}
public struct UserModel
{
    public UserModel(string Name, Guid uuid, Guid? accessToken = null)
    {
        if (accessToken == null)
        {
            accessToken = Guid.Empty;
            userType = "legacy";
        }
        else
        {
            userType = "msa"; 
            this.accessToken = (Guid)accessToken;
        }
        this.Name = Name;
        this.uuid = uuid;
    }
    public string Name { get; set; }
    public Guid uuid { get; set; }
    public Guid accessToken { get; set; }
    public string userType { get; set; }
}
public enum SystemType
{
    windows,
    osx,
    linux
}
