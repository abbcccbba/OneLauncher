﻿using Avalonia.Threading;
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
        if(TryAgainFunction != null)
            exception.TryAgainFunction = TryAgainFunction;
        return Dispatcher.UIThread.InvokeAsync(async () => 
        {
            if (exception.Action == OlanExceptionAction.Warning)
                await MainWindow.mainwindow.ShowFlyout(exception.Message,true);
            if (exception.Action == OlanExceptionAction.Error)
                await new ExceptionTip(exception).ShowDialog(MainWindow.mainwindow);
            if (exception.Action == OlanExceptionAction.FatalError)
            {
                await new ExceptionTip(exception).ShowDialog(MainWindow.mainwindow);
                Environment.Exit(1);
            }
        });
    }
}
