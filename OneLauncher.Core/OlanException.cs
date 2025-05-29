using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core;
/// <summary>
/// 出现已知错误时应该做什么？
/// Warning : 仅在右下角显示警告
/// Error : 显示对话框要求用户处理
/// FatalErroe : 致命性错误，显示对话框并提示关闭 OneLauncher
/// </summary>
public enum OlanExceptionAction
{
    Warning,
    Error,
    FatalError
}
public class OlanException : Exception
{
    public OlanExceptionAction Action { get; set; }
    public string Message {  get; set; }
    public Exception? OriginalException { get; set; }
    public OlanException(string Message,OlanExceptionAction action)
    {
        this.Message = Message;
        this.Action = action;
    }
    public OlanException(string Message,OlanExceptionAction action,Exception originalException) 
    { 
        this.Message=Message;
        this.Action=action;
        this.OriginalException = originalException;
    }
}
