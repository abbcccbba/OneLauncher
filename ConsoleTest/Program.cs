using OneLauncher.Core.Compatible.ImportPCL2Version;
using OneLauncher.Core.Downloader;
using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;

namespace ConsoleTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("OneLauncher PCL2 Importer Test");
        Console.WriteLine("==============================");

        try
        {
            // 1. 初始化启动器的核心系统 (读取配置, 设置路径等)
            Console.WriteLine("Initializing OneLauncher environment...");
            var initError = await Init.Initialize();
            if (initError != null)
            {
                throw initError; // 如果初始化失败，则抛出异常
            }

            // 2. 加载版本清单 (模拟UI启动时加载)
            Init.MojangVersionList = new List<VersionBasicInfo>()
            { 
                new VersionBasicInfo("1.20.1","release",DateTime.Now,"https://piston-meta.mojang.com/v1/packages/b26b44276f71df999e6dc9361595c1c866789194/1.20.1.json"),
            };

            Console.WriteLine("Initialization complete.");
            Console.WriteLine($"OneLauncher Base Path: {Init.BasePath}");
            Console.WriteLine("-------------------------------");

            // 3. 定义要导入的PCL2实例的路径
            // !!! 重要: 请将此路径修改为你自己电脑上的实际路径 !!!
            string pclInstanceToImportPath = @"E:\mc\.minecraft\versions\顶级版本名字";

            Console.WriteLine($"Attempting to import from: {pclInstanceToImportPath}");
            if (!Directory.Exists(pclInstanceToImportPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: The path '{pclInstanceToImportPath}' does not exist.");
                Console.WriteLine("Please update the 'pclInstanceToImportPath' variable in Program.cs to the correct location.");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            // 4. 设置一个进度报告器，在控制台显示导入过程
            var progressReporter = new Progress<(DownProgress Title, int AllFiles, int DownedFiles, string DowingFileName)>(p =>
            {
                Console.WriteLine($"[{p.Title,-25}] ({p.DownedFiles,4}/{p.AllFiles,4}) - {p.DowingFileName}");
            });

            // 5. 创建并运行导入器
            Console.WriteLine("\nStarting import process...\n");
            var importer = new PCL2Importer(progressReporter, CancellationToken.None);
            await importer.ImportAsync(pclInstanceToImportPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n==============================");
            Console.WriteLine("Import process completed successfully!");
            Console.ResetColor();
        }
        catch (OlanException olanEx)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nAn OlanException occurred during the import process:");
            Console.WriteLine($"Title: {olanEx.Title}");
            Console.WriteLine($"Message: {olanEx.Message}");
            if (olanEx.OriginalException != null)
            {
                Console.WriteLine("\n--- Original Exception ---");
                Console.WriteLine(olanEx.OriginalException);
            }
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nAn unexpected error occurred:");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
        }

        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}