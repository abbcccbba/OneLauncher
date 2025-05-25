# OneLauncher

**OneLauncher** 是一个以快速为功能核心的轻量化 Minecraft 启动器

提供了直接在桌面上固定游戏而无需启动器启动的启动游戏方法

快速安装（Windosw PowerShell）：

```powershell
Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/abbcccbba/OneLauncher/master/OneLauncher.Desktop/install.ps1'))
```

## 系统要求

- **操作系统**：提供了跨平台支持，主要为Windows-x64、Linux-x64 和 macOS-arm64 提供了支持
  
  *此项目对x86的支持有限*
  
- **运行环境**：
  - 在依赖框架的构建中需要.NET9（或更高版本 可以在[这里](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)下载）  
    *注意：部分较新的Windows可能自带此依赖，无需安装*
    
  - Java 环境  
    *OneLauncher支持自动下载并使用合适的Java，如果你的系统无Java运行时，请在下载时启动此选项*

## 跨平台安装与构建指南

### 通过下载源代码并构建的方式使用

1. 下载源代码
2. 使用[Visual Studio](https://visualstudio.microsoft.com/)或[Rider](https://www.jetbrains.com/rider/)打开[OneLauncher.sln](https://github.com/abbcccbba/OneLauncher/blob/master/OneLauncher.sln)
3. 将[OneLauncher.Desktop](https://github.com/abbcccbba/OneLauncher/blob/master/OneLauncher.Desktop/OneLauncher.Desktop.csproj)设为启动项目
4. 运行，便可以看到窗口。构建为可执行文件请参考[这里](https://www.google.com/)

## 开源与贡献

OneLauncher 当前处于早期开发阶段，许多功能尚未完成。我们非常欢迎开发者共同参与完善：

如果你有任何问题或请求可以在[这里](https://github.com/abbcccbba/OneLauncher/issues)发起提问

## 更多

[服务条款](https://github.com/abbcccbba/OneLauncher/blob/master/Terms_of_Service.md)
[隐私声明](https://github.com/abbcccbba/OneLauncher/blob/master/Privacy_Policy.md)


