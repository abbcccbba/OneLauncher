using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Net.JavaProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Global;
public enum JavaProvider
{
    Adoptium,
    AzulZulu,
    MicrosoftOpenJDK,
    OracleGraalVM,
    OracleJDK
}
public class JavaManager
{
    private readonly DBManager _dbManager = Init.ConfigManager;

    /// <summary>
    /// 解析指定Java版本的可执行文件路径。
    /// </summary>
    public string GetJavaExecutablePath(int version)
    {
        if (_dbManager.Data.AvailableJavas.TryGetValue(version, out var path) && !string.IsNullOrEmpty(path))
        {
            // 使用通用的占位符替换工具
            var placeholders = new Dictionary<string, string>
            {
                { "INSTALLED_PATH", Init.InstalledPath }
            };
            return Tools.ReplacePlaceholders(path, placeholders).Replace('/', Path.DirectorySeparatorChar);
        }
        return "java";
    }

    /// <summary>
    /// 使用指定的提供商安装一个Java版本。
    /// </summary>
    public async Task InstallJava(
        int version,
        JavaProvider provider,
        bool overwrite = false,
        IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        if (_dbManager.Data.AvailableJavas.ContainsKey(version) && !overwrite)
            throw new OlanException("Java版本已存在", $"Java版本 {version} 已经存在于可用列表中。请使用 `overwrite` 参数来覆盖现有版本。",OlanExceptionAction.Warning);

        IJavaProvider javaProvider = provider switch
        {
            JavaProvider.Adoptium => new AdoptiumAPI(version),
            JavaProvider.AzulZulu => new AzulZuluAPI(version),
            JavaProvider.MicrosoftOpenJDK => new MicrosoftBuildofOpenJDKGetter(version),
            JavaProvider.OracleGraalVM => new GraalVMGetter(version),
            JavaProvider.OracleJDK => new OracleJDK(version),
            _ => throw new OlanException("内部错误","不支持的Java提供商。")
        };

        javaProvider.CancelToken = token;

        string installDir = Path.Combine(Init.InstalledPath, "runtimes", version.ToString());

        // 如果覆写删除之前目录，避免出现奇奇怪怪的问题
        if (Directory.Exists(installDir) && overwrite) Directory.Delete(installDir, true);
        Directory.CreateDirectory(installDir);

        try
        {
            int done = 0 ;
            await javaProvider.GetAutoAsync(new Progress<(long start,long end)>(p =>
            {
                // 内部是28分段下载
                Interlocked.Increment(ref done);
                progress?.Report(
                    $"[{done}/28] 分段 起始位：{p.start} 结束位：{p.end}");
            }));

            string javaPath = javaProvider.GetJavaPath();
            string relativePath = Path.GetRelativePath(Init.InstalledPath, javaPath);
            // 保存时，我们依然使用占位符格式
            string placeholderPath = $"${{INSTALLED_PATH}}/{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";

            _dbManager.Data.AvailableJavas[version] = placeholderPath;
            await _dbManager.Save();
        }
        catch (Exception)
        {
            if (Directory.Exists(installDir)) Directory.Delete(installDir, true);
            throw;
        }
    }
}