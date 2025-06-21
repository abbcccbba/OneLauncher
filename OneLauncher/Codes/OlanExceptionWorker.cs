using Avalonia.Threading;
using OneLauncher.Core.Global;
using OneLauncher.Views;
using OneLauncher.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Codes;

internal class OlanExceptionWorker
{
    public static Task ForOlanException(OlanException exception, Action TryAgainFunction = null)
    {
        if(TryAgainFunction != null)
            exception.TryAgainFunction = TryAgainFunction;
        return Dispatcher.UIThread.InvokeAsync(async () => 
        {
            if (exception.Action == OlanExceptionAction.Warning)
                MainWindow.mainwindow.ShowFlyout(exception.Message,true);
            if (exception.Action == OlanExceptionAction.Error)
                await new ExceptionTip(exception).ShowDialog(MainWindow.mainwindow);
            if (exception.Action == OlanExceptionAction.FatalError)
            {
                await new ExceptionTip(exception).ShowDialog(MainWindow.mainwindow);
                Environment.Exit(1);
            }
        });
    }
    public static Task ForUnknowException(Exception exception, Action TryAgainFunction = null)
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await new ExceptionTip(new OlanException("出现未知错误",exception.ToString(),OlanExceptionAction.Error)).ShowDialog(MainWindow.mainwindow);
        });
    }
}
