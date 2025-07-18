using System;
using System.Collections.Generic;
using System.Text;

namespace OneLauncher.Core.Net.JavaProviders;
internal interface IJavaProvider
{
    /// <summary>
    /// 执行安装操作的取消令牌
    /// </summary>
    CancellationToken? CancelToken { get; set; }
    /// <summary>
    /// 执行Java安装
    /// </summary>
    Task GetAutoAsync();
    /// <summary>
    /// 获取java可执行文件的路径
    /// </summary>
    string GetJavaPath();
    string ProviderName { get; }
    string ToString();
}
