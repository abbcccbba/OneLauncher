using System;
using System.Collections.Generic;
using System.Text;

namespace OneLauncher.Core.Net.JavaProviders;
internal class GraalVMGetter : BaseJavaProvider, IJavaProvider
{
    public override string ProviderName => "GrallVM";

    // https://download.oracle.com/graalvm/24/latest/graalvm-jdk-24_linux-x64_bin.tar.gz
    public GraalVMGetter(int javaVersion)
        : base(javaVersion, 21)
    {
        /* 此版本有版本限制，不提供21及以下的JDK版本，且不提供JRE版本 */
    }
    public Task GetAutoAsync()
    {
        string fileExtension = systemTypeName == "windows" ? "zip" : "tar.gz";
        string downloadUrl = $"https://download.oracle.com/graalvm/{javaVersion}/latest/graalvm-jdk-{javaVersion}_{systemTypeName}-{systemArchName}_bin.{fileExtension}";
        return GetAndDownloadAsync(() => Task.FromResult<string?>(downloadUrl));
    }
}