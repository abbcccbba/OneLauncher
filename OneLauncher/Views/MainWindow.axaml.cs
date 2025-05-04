using Avalonia.Controls;
using OneLauncher.Core;
using OneLauncher.ViewModels;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OneLauncher.Views
{
    public partial class MainWindow : Window
    {
        public static Window mainwindow;
        public MainWindow()
        {
            InitializeComponent();
            Codes.Init.Initialize().GetAwaiter().GetResult();
            mainwindow=this;
        }
    }
}