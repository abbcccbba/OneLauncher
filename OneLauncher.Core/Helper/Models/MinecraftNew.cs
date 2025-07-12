using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.Models;
public readonly struct MinecraftNew
{
    /// <summary>
    /// 新闻标题。
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 新闻的简短介绍文字。
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// 指向“阅读全文”页面的完整URL。
    /// </summary>
    public string DetailsUrl { get; }

    /// <summary>
    /// 指向新闻背景图片的完整URL。
    /// </summary>
    public string ImageUrl { get; }

    public MinecraftNew(string title, string content, string detailsUrl, string imageUrl)
    {
        Title = title;
        Content = content;
        DetailsUrl = detailsUrl;
        ImageUrl = imageUrl;
    }
}
