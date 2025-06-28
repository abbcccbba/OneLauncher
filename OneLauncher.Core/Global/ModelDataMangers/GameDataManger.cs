using OneLauncher.Core.Helper;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Global.ModelDataMangers;

public class GameDataRoot
{
    // 存储所有游戏数据实例的字典
    [JsonPropertyName("instances")]
    public Dictionary<string,GameData> Instances { get; set; } = new();

    // 存储默认实例的映射
    // Key: VersionId (如 "1.20.1")
    // Value: InstanceId (如 "fd92ded1")
    [JsonPropertyName("defaults")]
    public Dictionary<string, string> DefaultInstanceMap { get; set; } = new();
}

// 以便AOT编译
[JsonSerializable(typeof(List<GameData>))]
[JsonSerializable(typeof(GameDataRoot))] 
public partial class GameDataJsonContext : JsonSerializerContext { }

public class GameDataManager : BasicDataManager<GameDataRoot>
{
    /// <summary>
    /// 获取或创建一个指定版本的游戏数据实例。
    /// 查找逻辑：1. 默认实例 -> 2. 第一个可用实例 -> 3. 创建新实例。
    /// </summary>
    public async Task<GameData> GetOrCreateInstanceAsync(UserVersion userVersion)
    {
        // 尝试获取该版本的默认实例
        var gameData = GetDefaultInstance(userVersion.VersionID);
        if (gameData != null)
            return gameData;
        

        // 2. 如果没有默认实例，则查找该版本的第一个可用实例
        gameData = Data.Instances.FirstOrDefault(x => x.Value.VersionId == userVersion.VersionID).Value;
        if (gameData != null)
        {
            await SetDefaultInstanceAsync(gameData);
            return gameData;
        }

        // 如果完全没有任何实例，则创建一个新的
        // 确定默认游戏数据名称
        string modLoaderName = userVersion.modType.ToModEnum() switch
        {
            ModEnum.fabric => "Fabric",
            ModEnum.neoforge => "NeoForge",
            ModEnum.forge => "Forge",
            _ => "原版"
        };
        string gameDataName = $"{userVersion.VersionID} - {modLoaderName}";

        var newGameData = new GameData(
            name: gameDataName,
            versionId: userVersion.VersionID,
            loader: userVersion.modType.ToModEnum(),
            userModel: Init.AccountManager.GetDefaultUser().UserID
        );

        // 添加并设为默认
        await AddGameDataAsync(newGameData);
        await SetDefaultInstanceAsync(newGameData);

        return newGameData;
    }
    public GameDataManager(string configPath)
        :base(configPath)
    {
    }
    public Task SetDefaultInstanceAsync(GameData targetData)
    {
        Data.DefaultInstanceMap[targetData.VersionId] = targetData.InstanceId;
        return Save();
    }

    public GameData? GetDefaultInstance(string versionId)
    {
        return Data.Instances.FirstOrDefault(x => x.Value.VersionId == versionId).Value;
    }

    public Task AddGameDataAsync(GameData newData)
    {
        Data.Instances.Add(newData.InstanceId,newData);
        // 确保物理文件夹被创建
        Directory.CreateDirectory(newData.InstancePath);
        return Save();
    }

    public Task RemoveGameDataAsync(GameData dataToRemove)
    {
        // 在删除实例前检查并清理它在 defaults 映射中的记录
        if (Data.DefaultInstanceMap.ContainsValue(dataToRemove.InstanceId))
        {
            var entry = Data.DefaultInstanceMap.FirstOrDefault(kvp => kvp.Value == dataToRemove.InstanceId);
            if (!string.IsNullOrEmpty(entry.Key))
                Data.DefaultInstanceMap.Remove(entry.Key);
            
        }

        Data.Instances.Remove(dataToRemove.InstanceId);
        return Save();
    }

    protected override JsonSerializerContext GetJsonContext()
        => GameDataJsonContext.Default;
}