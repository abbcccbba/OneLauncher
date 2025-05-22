﻿using CommunityToolkit.Mvvm.ComponentModel;
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
    private string _Out = string.Empty; 

    private readonly System.Text.StringBuilder _logBuilder = new System.Text.StringBuilder();
    private const int MaxDisplayLength = 1024 * 50; 

    public GameTaskerViewModel()
    {
        WeakReferenceMessenger.Default.Register<GameMessage>(this, (recipient, message) =>
        {
            // 在 UI 线程上更新 UI 绑定属性，避免跨线程访问问题
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                _logBuilder.Append(message.Content);

                // 如果日志长度超过限制，截断最旧的部分
                if (_logBuilder.Length > MaxDisplayLength)
                {
                    // 移除旧的部分，保留最新的内容
                    // 通常移除一半或四分之一，以避免频繁截断
                    _logBuilder.Remove(0, _logBuilder.Length - (MaxDisplayLength / 2));
                }

                // 将 StringBuilder 的内容更新到绑定属性
                Out = _logBuilder.ToString();

                // 强制 TextBox 滚动到底部（如果需要）
                // 这通常需要在 View 中通过附加属性或行为实现，因为 ViewModel 不直接操作 View
                // 例如，你可以发布一个消息让 View 知道需要滚动：
                // WeakReferenceMessenger.Default.Send(new ScrollToEndMessage());
            });
        });
    }
}