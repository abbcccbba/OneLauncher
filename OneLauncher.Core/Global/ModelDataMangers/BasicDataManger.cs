using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.Global.ModelDataMangers;

public abstract class BasicDataManager<T> where T : class, new()
{
    public T Data { get; protected set; }

    private readonly string _configPath;
    private readonly JsonSerializerOptions _serializerOptions; // AOT 用得到
    private readonly SemaphoreSlim _saveLock = new(1, 1); // 防止竞态

    protected BasicDataManager(string configPath)
    {
        _configPath = configPath;
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false, // 是否格式化 
            TypeInfoResolver = GetJsonContext()
        };
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath));

        if (!File.Exists(_configPath))
        {
            Data = new T(); 
            await Save(); 
        }
        else
        {
            try
            {
                // 文件存在，尝试读取和解析。
                await using var stream = File.OpenRead(_configPath);
                // 防止空文件导致反序列化失败
                if (stream.Length == 0)
                {
                    Data = new T();
                }
                else
                {
                    Data = await JsonSerializer.DeserializeAsync<T>(stream, _serializerOptions) as T ?? new T();
                }
            }
            catch (Exception ex)
            {
                throw new OlanException($"加载配置文件 {_configPath} 失败", "文件可能已损坏。", OlanExceptionAction.FatalError, ex);
            }
        }

        // 调用各个子类的特殊初始化方法
        await PostInitialize();
    }

    protected virtual Task PostInitialize()
        => Task.CompletedTask; // 部分子类无特别操作
    
    public async Task Save()
    {
        await _saveLock.WaitAsync();
        try
        {
            // 覆盖原始文件
            await using var fs = File.Create(_configPath);
            await JsonSerializer.SerializeAsync<T>(fs,Data, _serializerOptions);
        }
        finally
        {
            _saveLock.Release();
        }
    }
    protected abstract JsonSerializerContext GetJsonContext();
}