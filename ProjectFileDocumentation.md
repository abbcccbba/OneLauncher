# Welcome to One Launcher on Github!
### 这是一个项目文件说明文档，用来告诉你项目里的一些文件干嘛用的

### OneLauncher  
项目UI与UI处理项目 （C# + Avalonia UI）

### OneLauncher.Core 
项目后端核心逻辑项目 （C#)

### OneLauncher.Desktop 
Avalonia 生成的启动，是启动/发布/编译项目

## OneLauncher 项目内的文件
- Assets 文件夹
  - 里面是一些资源文件，主要是图标和背景图，包含两个字体文件（思源黑体，标准和粗体）
- Codes 文件夹
  - 通常是逻辑代码，现在逻辑部分已经转移到 OneLauncher.Core 了
- Views 文件夹
	- 项目UI的页面，包含主页面、一些窗口、和次级页面等。
	- MessageWindows 文件夹
		- （未完善） 快捷的通过窗口给用户一些信息或得到信息

## OneLauncher.Core 项目内的代码
- DBMager.cs
	- 程序配置管理器
- Download.cs
	- 封装下载方法
- GetVersionInfomation.cs
	- 获取版本列表
	- 获取版本信息
	- 获取版本资源信息
- SFNTD.cs
	- 包含一些自定义的东西
	- SFNTD 结构
		- 描述下载信息
	- aVersion
		- 描述单个版本信息
	- VersionBasicInfo
		- 描述单个版本的基本信息
	- UserModel
		- 描述单个用户登入模型
	- StartArguments
		- 获得启动参数
	