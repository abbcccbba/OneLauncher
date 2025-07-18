using System;
using System.Collections.Generic;
using System.Text;

namespace OneLauncher.Core.Net.JavaProviders;
internal class MicrosoftBuildofOpenJDKGetter : BaseJavaProvider, IJavaProvider
{
    public MicrosoftBuildofOpenJDKGetter(int javaVersion)
        : base(javaVersion, 11)
    {

    }

    public override string ProviderName => "Microsoft OpenJDK";

    public Task GetAutoAsync(IProgress<(long Start, long End)> progress)
    {
        string fileExtension = systemTypeName == "windows" ? "zip" : "tar.gz";
        string osName = systemTypeName == "macos" ? "macOS" : systemTypeName;
        string downloadUrl = $"https://aka.ms/download-jdk/microsoft-jdk-{javaVersion}-{osName}-{systemArchName}.{fileExtension}";
        return GetAndDownloadAsync(() => Task.FromResult<string?>(downloadUrl), progress);
    }
}