using System;
using System.Collections.Generic;
using System.Text;

namespace OneLauncher.Core.Net.JavaProviders;
internal interface IJavaProvider
{
    CancellationToken? CancelToken { get; set; }
    Task GetAutoAsync();
    string GetJavaPath();
}
