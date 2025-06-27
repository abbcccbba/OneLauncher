using Duende.IdentityModel.OidcClient.Browser;
using System.Diagnostics;
using System.Net;
using System.Text;

public class QOauth : IBrowser
{
    private readonly string redirectUri;

    // 构造函数接收一个端口号
    public QOauth(int port)
    {
        // 使用传入的端口号来构建回调地址
        this.redirectUri = $"http://127.0.0.1:{port}/";
    }

    // InvokeAsync 方法和你写的一样，无需改动
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = options.StartUrl,
                UseShellExecute = true
            });

            var context = await listener.GetContextAsync();
            var result = new BrowserResult
            {
                Response = context.Request.Url.ToString(),
                ResultType = BrowserResultType.Success
            };

            var buffer = Encoding.UTF8.GetBytes("<html><body>授权成功! 请返回应用程序。</body></html>");
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            context.Response.OutputStream.Close();

            return result;
        }
        catch (Exception ex)
        {
            return new BrowserResult { ResultType = BrowserResultType.HttpError, Error = ex.Message };
        }
        finally
        {
            listener.Stop();
        }
    }
}