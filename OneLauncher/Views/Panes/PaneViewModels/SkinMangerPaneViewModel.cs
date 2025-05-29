using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Core;
using OneLauncher.Core.Net.msa;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;

internal partial class SkinMangerPaneViewModel : BaseViewModel
{
#if DEBUG
    public SkinMangerPaneViewModel() { }
#endif
    private AccountPageViewModel accountPageViewModel;
    private UserModel SelUserModel;
    public SkinMangerPaneViewModel(AccountPageViewModel accountPageViewModel,UserModel SelUserModel)
    {
        this.accountPageViewModel = accountPageViewModel;
        this.SelUserModel = SelUserModel;
    }
    private int _selectedIndex;
    public int SelectedIndex
    {
        set
        {
            _selectedIndex = value;
        }
        get 
        { 
            return _selectedIndex;
        }
    }
    [ObservableProperty]
    public bool _IsSteveModel;
    [RelayCommand]
    public async Task ToChooseSkinFile()
    {
        var topLevel = TopLevel.GetTopLevel(MainWindow.mainwindow);
        if (topLevel?.StorageProvider is { } storageProvider && storageProvider.CanOpen)
        {
            // 配置文件选择器选项
            var options = new FilePickerOpenOptions
            {
                Title = "选择皮肤文件", // 对话框标题
                AllowMultiple = false, // 是否允许选择多个文件
                FileTypeFilter = new[]
                {
                    FilePickerFileTypes.ImagePng // 仅限png文件
                }
            };

            // 打开文件选择器
            var files = await storageProvider.OpenFilePickerAsync(options);
            var selectedFile = files.FirstOrDefault();

            if (files == null || !files.Any() || selectedFile == null)
                return;

            // 获取本地路径
            string filePath = selectedFile.Path.LocalPath;

            // 检查皮肤文件有效性
            if (!await MojangProfile.IsValidSkinFile(filePath))
            {
                await MainWindow.mainwindow.ShowFlyout("皮肤文件无效！",true);
                return;
            }
            Debug.WriteLine("有效的皮肤文件");
            using (var task = new MojangProfile(SelUserModel))
            {
                // 上传
                await task.SetUseLocalFile(new MojangSkin()
                {
                    Skin = filePath,
                    IsSlimModel = (IsSteveModel) ? false : true
                });
                // 重新缓存本地皮肤文件
                await task.GetSkinHeadImage();
                // 刷新
                accountPageViewModel.RefList();

                await MainWindow.mainwindow.ShowFlyout("已成功上传皮肤！");
            } 
        }
    }
    [RelayCommand]
    public async Task OpenInNameMC()
    {
        
        var dialog = new NativeWebDialog
        {
            Title = "Avalonia Docs",
            CanUserResize = false,
            Source = new Uri("https://docs.avaloniaui.net/")
        };
    }
}
