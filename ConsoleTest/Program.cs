using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("--- OneLauncher 游戏实例隔离功能测试 ---");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("本测试将验证不同游戏实例的数据目录是否完全独立。");
            Console.ResetColor();
            Console.WriteLine();

            try
            {
                // 1. 初始化核心服务
                var initError = await Init.Initialize();
                if (initError != null)
                {
                    Console.WriteLine($"初始化失败: {initError.Title} - {initError.Message}");
                    return;
                }
                Console.WriteLine($"[OK] 核心服务初始化完毕。游戏根目录: {Init.GameRootPath}");

                // 2. 准备测试数据
                await PrepareTestData();

                // 3. 执行测试
                await TestInstanceIsolation();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n测试过程中发生未处理的异常: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }

            Console.WriteLine("\n--- 隔离测试已完成，祝您晚安！ ---");
            Console.ReadLine();
        }

        static async Task PrepareTestData()
        {
            if (Init.ConfigManger.config.DefaultUserModel == null)
            {
                var defaultUser = new UserModel("Tester", Guid.NewGuid());
                Init.ConfigManger.config.UserModelList.Add(defaultUser);
                Init.ConfigManger.config.DefaultUserModel = defaultUser;
                await Init.ConfigManger.Save();
                Console.WriteLine("[OK] 已创建并设置默认测试用户 'Tester'。");
            }
        }

        // --- 核心测试场景 ---

        static async Task TestInstanceIsolation()
        {
            Console.WriteLine("\n--- 场景: 验证两个独立实例的数据隔离 ---");
            string versionId = "1.20.4";

            // 确保基础的 version.json 文件存在，为 LaunchCommandBuilder 做准备
            await EnsureFakeVersionJsonExists(versionId);

            // 1. 创建第一个游戏实例
            var instanceA = new GameData("隔离测试-实例A", versionId, ModEnum.none, Init.ConfigManger.config.DefaultUserModel);
            await Init.GameDataManger.AddGameDataAsync(instanceA);
            Console.WriteLine($"[OK] 创建了实例A，路径: {instanceA.InstancePath}");

            // 2. 创建第二个游戏实例
            var instanceB = new GameData("隔离测试-实例B", versionId, ModEnum.none, Init.ConfigManger.config.DefaultUserModel);
            await Init.GameDataManger.AddGameDataAsync(instanceB);
            Console.WriteLine($"[OK] 创建了实例B，路径: {instanceB.InstancePath}");

            // 3. 验证 LaunchCommandBuilder 是否为每个实例生成了正确的 --gameDir
            var builderA = new LaunchCommandBuilder(Init.GameRootPath, instanceA);
            string commandA = await builderA.BuildCommand();
            Assert(commandA.Contains($"--gameDir \"{instanceA.InstancePath}\""), "实例A的启动命令指向了正确的独立目录");

            var builderB = new LaunchCommandBuilder(Init.GameRootPath, instanceB);
            string commandB = await builderB.BuildCommand();
            Assert(commandB.Contains($"--gameDir \"{instanceB.InstancePath}\""), "实例B的启动命令指向了正确的独立目录");

            // 4. 在实例A的mods文件夹中创建一个“标记文件”
            string modsPathA = Path.Combine(instanceA.InstancePath, "mods");
            Directory.CreateDirectory(modsPathA);
            string markerFile = Path.Combine(modsPathA, "marker_for_instance_A.txt");
            await File.WriteAllTextAsync(markerFile, "This file belongs to instance A.");
            Console.WriteLine($"[OK] 在实例A的mods目录中创建了标记文件。");

            // 5. 验证实例B的mods文件夹中 **不存在** 这个标记文件
            string correspondingFileInB = Path.Combine(instanceB.InstancePath, "mods", "marker_for_instance_A.txt");
            Assert(!File.Exists(correspondingFileInB), "实例B的目录中不应存在实例A的标记文件，证明隔离成功！");
        }


        // --- 辅助方法 ---

        static async Task EnsureFakeVersionJsonExists(string versionId)
        {
            var versionDir = Path.Combine(Init.GameRootPath, "versions", versionId);
            var versionJsonPath = Path.Combine(versionDir, "version.json");
            Directory.CreateDirectory(versionDir);

            if (File.Exists(versionJsonPath)) return;

            Console.WriteLine($"为 {versionId} 创建一个虚假的 version.json 用于测试...");
            string fakeJson = $@"
            {{
                ""id"": ""{versionId}"",
                ""mainClass"": ""net.minecraft.client.main.Main"",
                ""assetIndex"": {{ ""id"": ""{versionId}"", ""url"": ""fake_url"", ""size"": 123, ""sha1"": ""fake_sha1"" }},
                ""downloads"": {{ ""client"": {{ ""url"": ""fake_url"", ""sha1"": ""fake_sha1"", ""size"": 12345 }}}},
                ""libraries"": []
            }}";
            await File.WriteAllTextAsync(versionJsonPath, fakeJson);
        }

        static void Assert(bool condition, string message)
        {
            if (condition)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [成功] {message}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [失败] {message}");
            }
            Console.ResetColor();
        }
    }
}