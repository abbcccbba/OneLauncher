using OneLauncher.Core.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper;

public struct GameData
{
    public string Name { get; set; }
    public string VersionId { get; set; }
    public ModEnum ModLoader { get; set; }
    public DateTime CreationTime { get; set; }
    public UserModel DefaultUserModel { get; set; }
    public string InstanceId { get; set; }
    [JsonIgnore]
    public string InstancePath => Path.Combine(Init.GameRootPath, "instance", InstanceId);
    [JsonConstructor]
    public GameData() { }

    public GameData(string name, string versionId, ModEnum loader, UserModel? userModel)
    {
        Name = name;
        VersionId = versionId;
        ModLoader = loader;
        DefaultUserModel = userModel ?? Init.ConfigManger.config.DefaultUserModel;
        CreationTime = DateTime.Now;
        InstanceId = Guid.NewGuid().ToString();
    }
}
