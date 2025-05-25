﻿using System;
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
    public List<UserModel> UserModelList { get; set; } = new List<UserModel>();
    // 默认用户模型，未指定下默认为 Zhi Wei
    public UserModel DefaultUserModel { get; set; } = new UserModel();
    // 默认版本（固定到仪表盘）
    public aVersion DefaultVersion { get; set; }
    // 除了系统自带的Java以外启动器安装的所有Java版本列表
    public List<int> JavaList { get; set; } = new List<int>();
}

public class DBManger
{
    public AppConfig config;
    private readonly string ConfigFilePath;
    private readonly string BasePath;
    public DBManger(AppConfig FirstConfig, string BasePath)
    {
        this.BasePath = BasePath;
        ConfigFilePath = Path.Combine(BasePath, "config.json");
        if (File.Exists(ConfigFilePath))
        {
            Read();
        }
        else
        {
            Write(FirstConfig);
        }
    }

    public void Write(AppConfig config)
    {
        try
        {
            this.config = config;
            Directory.CreateDirectory(BasePath);
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config));
        }
        catch (Exception ex)
        {
            throw new IOException($"配置文件写入错误： {ex.Message}", ex);
        }
    }
    public void Save()
    {
        try
        {
            Write(this.config);
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
            string jsonString = File.ReadAllText(ConfigFilePath);
            AppConfig readConfig = JsonSerializer.Deserialize<AppConfig>(jsonString);
            this.config = readConfig;
            return readConfig;
        }
        catch (Exception ex)
        {
           throw new IOException($"配置文件读取错误： {ex.Message}", ex);
        }
    }
}
