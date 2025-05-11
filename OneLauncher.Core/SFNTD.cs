using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core;

public struct SFNTD
{
    public SFNTD(string Url, string Sha1, string Path)
    {
        this.url = Url;
        this.sha1 = Sha1;
        this.path = Path;
    }
    public string url;
    public string sha1;
    public string path;
}
public struct aVersion
{
    public VersionBasicInfo versionBasicInfo { get; set; }
    public string name { get; set; }
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
    public UserModel(string Name, string uuid, string accessToken,string UserType)
    {
        this.Name = Name;
        this.uuid = uuid;
        this.accessToken = accessToken;
        this.UserType = UserType;
    }
    public string Name { get; set; }
    public string uuid { get; set; }
    public string accessToken { get; set; }
    public string UserType { get; set; }
}
