using System;
using System.Collections.Generic;
using System.Text;

namespace OneLauncher.Core.Net.msa.JsonModels;

public class DeviceCodeResponse
{
    // 设备代码
    public string device_code { get; set; } = string.Empty;
    // 给用户的代码
    public string user_code { get; set; } = string.Empty;
    // 验证地址
    public string verification_uri { get; set; } = string.Empty;
    // 失效时间（秒）
    public int expires_in { get; set; }
    // 最小轮询间隔（秒）
    public int interval { get; set; }
}

public class TokenResponse
{
    public string token_type { get; set; } = string.Empty;
    public string scope { get; set; } = string.Empty;
    public int expires_in { get; set; }
    public string access_token { get; set; } = string.Empty;
    public string refresh_token { get; set; } = string.Empty;
    public string? id_token { get; set; }
}

public class ErrorResponse
{
    public string error { get; set; } = string.Empty;
    public string error_description { get; set; } = string.Empty;
}

public class XboxLiveAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public XUIDisplayClaims DisplayClaims { get; set; } = new XUIDisplayClaims();
}
public class XUIDisplayClaims
{
    public XUI[] xui { get; set; } = Array.Empty<XUI>();
}
public class XUI
{
    public string uhs { get; set; } = string.Empty;
}


public class XSTSAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public XUIDisplayClaims DisplayClaims { get; set; } = new XUIDisplayClaims();
}
public class XSTSErrorResponse
{
    public string? XErr { get; set; } // 类型是 string 还是 int 取决于 API 返回的具体格式，这里以 string 为例
    public string? Message { get; set; }
}


public class MinecraftLoginResponse
{
    public string access_token { get; set; } = string.Empty;
}

public class EntitlementsResponse
{
    public EntitlementItem[] items { get; set; } = Array.Empty<EntitlementItem>();
}

public class EntitlementItem { }
public class MinecraftProfileResponse
{
    public string id { get; set; } = string.Empty; // 玩家的真实 UUID
    public string name { get; set; } = string.Empty; // 玩家的 Minecraft 用户名
}
