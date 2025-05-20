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
public class aVersion
{
    public string VersionID { get; set; }
    public bool IsMod { get; set; }//预留
    public DateTime AddTime { get; set; } 
}
public class VersionBasicInfo
{
    /// <param ID="name">版本标识符</param>
    /// <param ID="type">版本类型</param>
    /// <param ID="url">版本文件下载地址</param>
    /// <param ID="time">版本发布时间</param>
    public VersionBasicInfo(Version ID, string type, DateTime time,string url)
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
    public Version ID { get; set; }
    public string type { get; set; }
    public DateTime time { get; set; }
    public string Url { get; set; }
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
