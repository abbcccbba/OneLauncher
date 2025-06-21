using OneLauncher.Core;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// GameData 结构体在 OneLauncher.Core.Helper 命名空间下
// using OneLauncher.Core.Helper; 

// 为了代码清晰，我们定义一个新的命名空间
namespace ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("--- OneLauncher 游戏数据功能测试 (修正版) ---");

            try
            {
                // 1. 初始化核心服务
                Console.WriteLine("[1/5] 初始化启动器核心服务...");
                var initError = await Init.Initialize();
                if (initError != null)
                {
                    Console.WriteLine($"初始化失败: {initError.Title} - {initError.Message}");
                    return;
                }
                Console.WriteLine("核心服务初始化成功！");
                Console.WriteLine($"游戏数据配置文件: {Path.Combine(Init.GameRootPath, "instance", "instance.json")}");

                // 2. 准备基础数据
                Console.WriteLine("\n[2/5] 确保基础测试数据存在...");
                await EnsureTestData(); // 改为 async
                Console.WriteLine("测试数据准备就绪。");

                // 3. 创建新的游戏数据
                Console.WriteLine("\n[3/5] 创建新的游戏数据对象...");
                var vanillaGameData = new GameData(
                    name: "纯净生存",
                    versionId: "1.20.4",
                    loader: ModEnum.none,
                    userModel: Init.ConfigManger.config.DefaultUserModel
                );

                var fabricGameData = new GameData(
                    name: "Fabric 模组",
                    versionId: "1.20.4",
                    loader: ModEnum.fabric,
                    userModel: Init.ConfigManger.config.DefaultUserModel
                );

                // 4. 使用 GameDataManager 添加并保存
                // 使用正确的名称 Init.GameDataManager
                Console.WriteLine("正在添加和保存 '纯净生存' 数据...");
                await Init.GameDataManger.AddGameDataAsync(vanillaGameData);

                Console.WriteLine("正在添加和保存 'Fabric 模组' 数据...");
                await Init.GameDataManger.AddGameDataAsync(fabricGameData);
                Console.WriteLine("游戏数据已成功保存到 instance.json！");

                // 5. 验证启动命令生成
                Console.WriteLine("\n[4/5] 测试启动命令生成...");
                var dataToLaunch = Init.GameDataManger.AllGameData.FirstOrDefault(d => d.InstanceId == fabricGameData.InstanceId);
                if (dataToLaunch.Name == null) // struct 不能为 null，检查其内容
                {
                    Console.WriteLine("错误：无法从管理器中找到刚刚添加的游戏数据！");
                    return;
                }

                Console.WriteLine($"准备为游戏数据 '{dataToLaunch.Name}' 生成启动命令...");
                var userToLaunch = dataToLaunch.DefaultUserModel ?? Init.ConfigManger.config.DefaultUserModel;

                // 为测试创建假的 version.json
                EnsureFakeVersionFiles(dataToLaunch.VersionId);

                // 使用我们新增的构造函数
                var launchBuilder = new LaunchCommandBuilder(
                    Init.GameRootPath,
                    dataToLaunch,
                    userToLaunch
                );

                string command = await launchBuilder.BuildCommand();

                Console.WriteLine("\n[5/5] 生成的启动命令如下：");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"java {command}");
                Console.ResetColor();

                Console.WriteLine("\n请检查上面的 --gameDir 参数是否指向了一个 GUID 文件夹路径。");

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n测试过程中发生未处理的异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }

            Console.WriteLine("\n--- 测试结束 ---");
            Console.ReadLine();
        }

        static async Task EnsureTestData()
        {
            bool needsSave = false;

            if (Init.ConfigManger.config.DefaultUserModel == null)
            {
                var defaultUser = new UserModel("Tester", Guid.NewGuid());
                Init.ConfigManger.config.UserModelList.Add(defaultUser);
                Init.ConfigManger.config.DefaultUserModel = defaultUser;
                needsSave = true;
                Console.WriteLine("  - 创建了默认用户 'Tester'");
            }

            if (!Init.ConfigManger.config.VersionList.Any(v => v.VersionID == "1.20.4"))
            {
                var newVersion = new UserVersion
                {
                    VersionID = "1.20.4",
                    AddTime = DateTime.Now
                };
                Init.ConfigManger.config.VersionList.Add(newVersion);
                needsSave = true;
                Console.WriteLine("  - 创建了 '1.20.4' 的基础版本配置");
            }

            if (needsSave)
            {
                await Init.ConfigManger.Save(); // 改为 await
                Console.WriteLine("  - 已保存更新到 config.json");
            }
        }

        static void EnsureFakeVersionFiles(string versionId)
        {
            var versionDir = Path.Combine(Init.GameRootPath, "versions", versionId);
            Directory.CreateDirectory(versionDir);

            var versionJsonPath = Path.Combine(versionDir, "version.json");
            if (!File.Exists(versionJsonPath))
            {
                string fakeJsonContent = """
                {
                    "id": "1.20.4",
                    "mainClass": "net.minecraft.client.main.Main",
                    "assetIndex": { "id": "12", "url": "" },
                    "downloads": { "client": { "url": "" } },
                    "libraries": []
                }
                """;
                File.WriteAllText(versionJsonPath, fakeJsonContent);
                Console.WriteLine($"  - 创建了假的 'version.json' 用于测试路径: {versionJsonPath}");
            }
        }
    }
}