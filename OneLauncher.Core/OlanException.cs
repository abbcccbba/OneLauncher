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
    public string Title { get; set; }
    public string Message { get; set; }
    public Exception? OriginalException { get; set; }
    // 重试方法，会在窗口显示，并由用户决定是否调用
    public Action? TryAgainFunction { get; set; } = null;
    public OlanException(string Title, string Message, OlanExceptionAction action = OlanExceptionAction.Error)
    {
        this.Title = Title;
        this.Message = Message;
        this.Action = action;
    }
    public OlanException(string Title, string Message, OlanExceptionAction action, Exception originalException)
    {
        this.Title = Title;
        this.Message = Message;
        this.Action = action;
        this.OriginalException = originalException;
    }
    public OlanException(string Title, string Message, OlanExceptionAction action, Exception originalException, Action taf)
    {
        this.Title = Title;
        this.Message = Message;
        this.Action = action;
        this.OriginalException = originalException;
        TryAgainFunction = taf;
    }
}
