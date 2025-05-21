using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Windows.WindowViewModels;
internal class GameMessage
{
    public string Content { get; }

    public GameMessage(string content)
    {
        Content = content;
    }
}
internal partial class GameTaskerViewModel : BaseViewModel
{
    [ObservableProperty]
    public string _Out = string.Empty;
    public GameTaskerViewModel()
    {
        WeakReferenceMessenger.Default.Register<GameMessage>(this, (recipient, message) 
            => Out += (message.Content));
    }
}