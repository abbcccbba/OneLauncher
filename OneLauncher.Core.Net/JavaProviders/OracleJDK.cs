using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Net.JavaProviders;
internal class OracleJDK : BaseJavaProvider, IJavaProvider
{
    public override string ProviderName => "Oracle JDK";
    public OracleJDK(int javaVersion)
        : base(javaVersion, null)
    {

    }
    public Task GetAutoAsync(IProgress<(long Start, long End)> progress)
    {
        string fileExtension = systemTypeName == "windows" ? "zip" : "tar.gz";
        string downloadUrl = $"https://download.oracle.com/java/{javaVersion}/latest/jdk-{javaVersion}_{systemTypeName}-{systemArchName}_bin.{fileExtension}";
        return GetAndDownloadAsync(() => Task.FromResult<string?>(downloadUrl), progress);
    }
}