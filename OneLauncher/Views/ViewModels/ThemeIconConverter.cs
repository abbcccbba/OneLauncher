using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Styling;

namespace OneLauncher.Views.ViewModels
{
    public class ThemeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string originalPath)
            {
                // 获取当前应用的主题模式
                var currentTheme = Application.Current?.ActualThemeVariant;

                if (currentTheme == ThemeVariant.Dark && originalPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    // 如果是深色模式，且是 .png 文件，尝试转换为 _dark.png
                    int lastDotIndex = originalPath.LastIndexOf('.');
                    if (lastDotIndex != -1)
                    {
                        string darkPath = originalPath.Insert(lastDotIndex, "_dark");
                        // 检查 _dark 文件是否存在，如果不存在则回退到原文件
                        // 注意：在运行时检查文件是否存在通常需要IO操作，这可能不是最优解
                        // 更好的方式是直接尝试加载，如果失败则回退
                        // 这里我们直接返回_dark路径，依赖Image控件或上层逻辑处理找不到资源的情况
                        // 或者你可以尝试在这里加载Bitmap并返回，如果加载_dark失败，则加载原始图片
                        return darkPath;
                    }
                }
                return originalPath; // 浅色模式或非 .png 文件，返回原始路径
            }
            return AvaloniaProperty.UnsetValue; // 或者返回一个默认值
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}