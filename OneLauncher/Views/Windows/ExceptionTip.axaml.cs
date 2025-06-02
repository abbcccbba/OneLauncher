using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OneLauncher.Core;
using OneLauncher.Views.Windows.WindowViewModels;
using System;
using System.Threading.Tasks;

namespace OneLauncher.Views.Windows;
public partial class ExceptionTip : Window
{
    private readonly OlanException olanException;
    public ExceptionTip(OlanException olanException,Action TAF = null)
    {
        InitializeComponent();
        this.olanException = olanException;
        if(TAF != null)
            this.olanException.TryAgainFunction = TAF;
        ErrorTitle.Text = olanException.Title;
        ErrorDetails.Text = olanException.Message;
    }

    private void TryAgainFunction(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (this.olanException.TryAgainFunction != null)
            this.olanException.TryAgainFunction();
    }
    private void IgnoreFunction(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    { 
        this.Close();
    }
}