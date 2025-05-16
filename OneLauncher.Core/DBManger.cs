using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OneLauncher.Core;

namespace OneLauncher.Core;

public class AppConfig
{
    // 当前启动器已安装的所有版本列表，默认初始化为空列表
    public List<aVersion> VersionList { get; set; } = new List<aVersion>();
    // 当前启动器有的所有用户登入模型，默认初始化为空列表
    public UserModel DefaultUserModel { get; set; } = new UserModel();
    public ObservableCollection<UserModel> UserModelList { get; set; } = new ObservableCollection<UserModel>();
}

public class DBManger
{
    public AppConfig config;
    private readonly string ConfigFilePath;
    private readonly string BasePath;
    public DBManger(AppConfig FirstConfig,string BasePath)
    {
        this.BasePath = BasePath;
        ConfigFilePath = Path.Combine(BasePath,"config.json");
        if (File.Exists(ConfigFilePath))
            Read();
        else
            Write(FirstConfig);
    }
    public void Write(AppConfig config)
    {
        try
        {
            this.config = config;
            Directory.CreateDirectory(BasePath);
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            throw new IOException($"配置文件写入错误： {ex.Message}", ex);
        }
    }
    public void AddVersion(aVersion config)
    {
        try
        {
            this.config.VersionList.Add(config);
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this.config, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            throw new IOException($"配置文件写入错误： {ex.Message}", ex);
        }
    }
    public void AddUserModel(UserModel config)
    {
        try
        {
            this.config.UserModelList.Add(config);
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this.config, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            throw new IOException($"配置文件写入错误： {ex.Message}", ex);
        }
    }
    public AppConfig Read()
    {
        try
        {
            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFilePath));
            this.config = config;
            return config;
        }
        catch (Exception ex)
        {
            throw new IOException($"配置文件读取错误： {ex.Message}", ex);
        }
    }
}
