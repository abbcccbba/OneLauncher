using Avalonia.Threading;
using OneLauncher.Core;
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
        OlanException olanException = exception;
        if(TryAgainFunction != null)
            olanException.TryAgainFunction = TryAgainFunction;
        return Dispatcher.UIThread.InvokeAsync(async () => 
        {
            if (olanException.Action == OlanExceptionAction.Warning)
                await MainWindow.mainwindow.ShowFlyout(olanException.Message,true);
            if (olanException.Action == OlanExceptionAction.Error)
                await new ExceptionTip(olanException).ShowDialog(MainWindow.mainwindow);
            if (olanException.Action == OlanExceptionAction.FatalError)
            {
                await new ExceptionTip(olanException).ShowDialog(MainWindow.mainwindow);
                Environment.Exit(1);
            }
        });
    }
}
