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
public struct NdDowItem
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
public struct aVersion
{
    public string VersionID { get; set; }
    public bool IsMod { get; set; }//预留
    public DateTime AddTime { get; set; } 
}
public struct VersionBasicInfo
{
    public VersionBasicInfo(string name, string type, string url, string time)
    {
        this.name = name;
        this.type = type;
        this.time = time;
        this.url = url;
    }
    public string name { get; set; }
    public string type { get; set; }
    public string time { get; set; }
    public string url { get; set; }
}
public struct UserModel
{
    public UserModel(string Name, string uuid, string accessToken = "0")
    {
        this.Name = Name;
        this.uuid = uuid;
        this.accessToken = accessToken;
        if (accessToken != "0")
            userType = "legacy";
        else
            userType = "msa";
    }
    public string Name { get; set; }
    public string uuid { get; set; }
    public string accessToken { get; set; }
    public string userType { get; set; }
}
