using System;
using System.IO;
using System.Text;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // 获取当前工作目录作为根路径
            string rootPath = @"F:\code\codeshare";
            // 输出文件的保存路径
            string outputFile = @"E:\OneLauncherProject\OneLauncher\CombinedCode.txt";
            // 存储拼接后的内容
            StringBuilder combinedContent = new StringBuilder();
            // 要排除的目录（不处理这些目录及其子目录）
            string[] excludedDirs = { "obj", "bin" };

            // 递归获取所有.cs和.axaml文件
            ProcessDirectory(rootPath, rootPath, combinedContent, excludedDirs);

            // 将结果写入文件
            File.WriteAllText(outputFile, combinedContent.ToString(), Encoding.UTF8);
            Console.WriteLine($"文件已成功生成：{outputFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误：{ex.Message}");
        }
    }

    static void ProcessDirectory(string targetDirectory, string rootPath, StringBuilder combinedContent, string[] excludedDirs)
    {
        try
        {
            // 检查当前目录是否在排除列表中
            string dirName = Path.GetFileName(targetDirectory);
            if (excludedDirs.Any(excluded => dirName.Equals(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"跳过排除的目录：{targetDirectory}");
                return;
            }

            // 获取目录下所有.cs和.axaml文件
            string[] fileExtensions = { "*.cs", "*.axaml" };
            foreach (string extension in fileExtensions)
            {
                string[] files = Directory.GetFiles(targetDirectory, extension);
                foreach (string file in files)
                {
                    // 计算相对路径
                    string relativePath = Path.GetRelativePath(rootPath, file).Replace('\\', '/');
                    // 读取文件内容
                    string content = File.ReadAllText(file, Encoding.UTF8);
                    // 添加文件路径和内容的中文分隔符
                    combinedContent.AppendLine($"【代码拼接器】文件路径: /Code/{relativePath}");
                    combinedContent.AppendLine($"【代码拼接器】文件内容开始");
                    combinedContent.AppendLine(content);
                    combinedContent.AppendLine($"【代码拼接器】文件内容结束\n");
                }
            }

            // 递归处理子目录
            string[] subDirectories = Directory.GetDirectories(targetDirectory);
            foreach (string subDir in subDirectories)
            {
                ProcessDirectory(subDir, rootPath, combinedContent, excludedDirs);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理目录 {targetDirectory} 时出错：{ex.Message}");
        }
    }
}