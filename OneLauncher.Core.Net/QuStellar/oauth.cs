using Duende.IdentityModel.OidcClient.Browser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace OneLauncher.Core.Net.QuStellar;

public class QOauth : IBrowser
{
    private readonly string redirectUri;

    public QOauth()
    {
        redirectUri = $"http://127.0.0.1:52726/";
    }

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

            // 异步等待浏览器回调
            var context = await listener.GetContextAsync();
            var result = new BrowserResult
            {
                Response = context.Request.Url.ToString(),
                ResultType = BrowserResultType.Success
            };

            var buffer = Encoding.UTF8.GetBytes("<html><head><title>授权成功</title></head><body><h1>授权成功!</h1><p>请返回您的应用程序。</p><script>window.close();</script></body></html>");
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            context.Response.OutputStream.Close();

            return result;
        }
        catch (Exception ex)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.HttpError,
                Error = ex.Message
            };
        }
        finally
        {
            listener.Stop();
        }
    }
}
