using OneLauncher.Core.Helper;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Global;

public class GameDataRoot
{
    // 存储所有游戏数据实例的列表
    [JsonPropertyName("instances")]
    public List<GameData> Instances { get; set; } = new();

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

public class GameDataManager
{
    private readonly string configPath;
    private readonly GameDataRoot gameDataRoot;
    public List<GameData> AllGameData { get => gameDataRoot.Instances; set => gameDataRoot.Instances = value; }
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
        gameData = AllGameData.FirstOrDefault(gd => gd.VersionId == userVersion.VersionID);
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
            userModel: Init.AccountManager.GetDefaultUser()
        );

        // 添加并设为默认
        await AddGameDataAsync(newGameData);
        await SetDefaultInstanceAsync(newGameData);

        return newGameData;
    }
    private GameDataManager(GameDataRoot gameDataRoot,string configPath)
    {
        this.gameDataRoot = gameDataRoot;
        this.configPath = configPath;
    }
    public static async Task<GameDataManager> CreateAsync(string gameRootPath)
    {
        string instanceDir = Path.Combine(gameRootPath, "instance");
        string configPath = Path.Combine(instanceDir, "instance.json");
        Directory.CreateDirectory(instanceDir);
        GameDataRoot gameDataRoot = null;
        if (File.Exists(configPath))
            gameDataRoot = await JsonSerializer.DeserializeAsync(File.OpenRead(configPath), GameDataJsonContext.Default.GameDataRoot) ?? new();
        else
            using (FileStream fs = File.Create(configPath))
            {
                gameDataRoot = new();
                await JsonSerializer.SerializeAsync(fs, gameDataRoot, GameDataJsonContext.Default.GameDataRoot);
            }
        return new GameDataManager(gameDataRoot,configPath);
        
    }
    public Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(gameDataRoot, GameDataJsonContext.Default.GameDataRoot);
        return File.WriteAllTextAsync(configPath, json);
    }
    public async Task SetDefaultInstanceAsync(GameData targetData)
    {
        gameDataRoot.DefaultInstanceMap[targetData.VersionId] = targetData.InstanceId;
        await SaveAsync();
    }

    public GameData? GetDefaultInstance(string versionId)
    {
        if (gameDataRoot.DefaultInstanceMap.TryGetValue(versionId, out var instanceId))
        {
            return AllGameData.FirstOrDefault(gd => gd.InstanceId == instanceId);
        }
        return null;
    }

    public async Task AddGameDataAsync(GameData newData)
    {
        AllGameData.Add(newData);
        // 确保物理文件夹被创建
        Directory.CreateDirectory(newData.InstancePath);
        await SaveAsync();
    }

    public async Task RemoveGameDataAsync(GameData dataToRemove)
    {
        // 在删除实例前检查并清理它在 defaults 映射中的记录
        if (gameDataRoot.DefaultInstanceMap.ContainsValue(dataToRemove.InstanceId))
        {
            var entry = gameDataRoot.DefaultInstanceMap.FirstOrDefault(kvp => kvp.Value == dataToRemove.InstanceId);
            if (!string.IsNullOrEmpty(entry.Key))
            {
                gameDataRoot.DefaultInstanceMap.Remove(entry.Key);
            }
        }

        AllGameData.Remove(dataToRemove);
        await SaveAsync();
    }
}