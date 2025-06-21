using OneLauncher.Core.Helper;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Global;

// 为AOT编译准备JsonContext
[JsonSerializable(typeof(List<GameData>))]
public partial class GameDataJsonContext : JsonSerializerContext { }

public class GameDataManager
{
    private readonly string configPath;
    public List<GameData> AllGameData { get; private set; }

    private GameDataManager(string gameRootPath)
    {
        var instanceDir = Path.Combine(gameRootPath, "instance");
        Directory.CreateDirectory(instanceDir);
        configPath = Path.Combine(instanceDir, "instance.json");
    }

    public static async Task<GameDataManager> CreateAsync(string gameRootPath)
    {
        var manager = new GameDataManager(gameRootPath);
        await manager.LoadAsync();
        return manager;
    }

    public async Task LoadAsync()
    {
        if (File.Exists(configPath))
        {
            var json = await File.ReadAllTextAsync(configPath);
            AllGameData = JsonSerializer.Deserialize(json, GameDataJsonContext.Default.ListGameData) ?? new List<GameData>();
        }
        else
        {
            AllGameData = new List<GameData>();
            await SaveAsync(); // 如果文件不存在，则创建一个空的
        }
    }

    public Task SaveAsync()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(AllGameData, GameDataJsonContext.Default.ListGameData);
        return File.WriteAllTextAsync(configPath, json);
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
        AllGameData.Remove(dataToRemove);
        // 可以选择性地删除物理文件夹
        // if (Directory.Exists(dataToRemove.InstancePath))
        // {
        //     Directory.Delete(dataToRemove.InstancePath, true);
        // }
        await SaveAsync();
    }
}