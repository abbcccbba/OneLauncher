using OneLauncher.Core.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper;
/// <summary>
/// 游戏数据，基于版本（UserVersion）
/// </summary>
public class GameData
{
    public string Name { get; set; }
    public string VersionId { get; set; }
    public ModEnum ModLoader { get; set; }
    public DateTime CreationTime { get; set; }
    public Guid DefaultUserModelID { get; set; }
    public string InstanceId { get; set; }
    public string? CustomIconPath { get; set; } // 自定义图标路径，若为null则据模组加载器类型默认
    [JsonIgnore]
    public string InstancePath => Path.Combine(Init.GameRootPath, "instance", InstanceId);
    [JsonConstructor]
    // 修改这里的参数名以匹配属性名
    public GameData(string name, string versionId, ModEnum modLoader, Guid defaultUserModelID, DateTime creationTime, string instanceId, string? customIconPath)
    {
        Name = name;
        VersionId = versionId;
        ModLoader = modLoader; // 参数 modLoader 对应属性 ModLoader
        DefaultUserModelID = defaultUserModelID; 
        CreationTime = creationTime;
        InstanceId = instanceId;
        CustomIconPath = customIconPath;
    }
    public GameData(string name, string versionId, ModEnum loader, Guid? userModel,string? customIconPath = null)
    {
        Name = name;
        VersionId = versionId;
        ModLoader = loader;
        CustomIconPath = customIconPath;
        DefaultUserModelID = userModel ?? Init.AccountManager.GetDefaultUser().UserID;
        CreationTime = DateTime.Now;
        InstanceId = Guid.NewGuid().ToString()[..8]; // 避免路径过长
    }
    public override string ToString()
        => VersionId;
}
