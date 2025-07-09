using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OneLauncher.Core.Net.Account.Microsoft.JsonModels;

// --- 请求体模型 ---

public class XboxLiveAuthRequest
{
    [JsonPropertyName("Properties")]
    public XboxLiveAuthRequestProperties Properties { get; set; } = new();

    [JsonPropertyName("RelyingParty")]
    public string RelyingParty { get; set; } = string.Empty;

    [JsonPropertyName("TokenType")]
    public string TokenType { get; set; } = string.Empty;
}

public class XboxLiveAuthRequestProperties
{
    [JsonPropertyName("AuthMethod")]
    public string AuthMethod { get; set; } = string.Empty;

    [JsonPropertyName("SiteName")]
    public string SiteName { get; set; } = string.Empty;

    [JsonPropertyName("RpsTicket")]
    public string RpsTicket { get; set; } = string.Empty;
}

public class XSTSAuthRequest
{
    [JsonPropertyName("Properties")]
    public XSTSAuthRequestProperties Properties { get; set; } = new();

    [JsonPropertyName("RelyingParty")]
    public string RelyingParty { get; set; } = string.Empty;

    [JsonPropertyName("TokenType")]
    public string TokenType { get; set; } = string.Empty;
}

public class XSTSAuthRequestProperties
{
    [JsonPropertyName("SandboxId")]
    public string SandboxId { get; set; } = string.Empty;

    [JsonPropertyName("UserTokens")]
    public string[] UserTokens { get; set; } = Array.Empty<string>();
}

public class MinecraftLoginRequest
{
    [JsonPropertyName("identityToken")]
    public string IdentityToken { get; set; } = string.Empty;
}

// --- 响应体模型 (原有的保持不变，只是添加了JsonPropertyName特性以确保与AOT兼容性更好) ---

public class DeviceCodeResponse
{
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; set; } = string.Empty;

    [JsonPropertyName("user_code")]
    public string UserCode { get; set; } = string.Empty;

    [JsonPropertyName("verification_uri")]
    public string VerificationUri { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; }
}

public class TokenResponse
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }
}

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; } = string.Empty;
}

public class XboxLiveAuthResponse
{
    [JsonPropertyName("Token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("DisplayClaims")]
    public XUIDisplayClaims DisplayClaims { get; set; } = new XUIDisplayClaims();
}

public class XUIDisplayClaims
{
    [JsonPropertyName("xui")]
    public XUI[] Xui { get; set; } = Array.Empty<XUI>();
}

public class XUI
{
    [JsonPropertyName("uhs")]
    public string Uhs { get; set; } = string.Empty;
}

public class XSTSAuthResponse
{
    [JsonPropertyName("Token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("DisplayClaims")]
    public XUIDisplayClaims DisplayClaims { get; set; } = new XUIDisplayClaims();
}

public class XSTSErrorResponse
{
    [JsonPropertyName("XErr")]
    public string? XErr { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }
}

public class MinecraftLoginResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("expires_in")]
    public int expires_in { get; set; } = 86400;
}

public class EntitlementsResponse
{
    [JsonPropertyName("items")]
    public EntitlementItem[] Items { get; set; } = Array.Empty<EntitlementItem>();
}

public class EntitlementItem { }

public class MinecraftProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}



[JsonSerializable(typeof(DeviceCodeResponse))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(XboxLiveAuthResponse))]
[JsonSerializable(typeof(XUIDisplayClaims))]
[JsonSerializable(typeof(XUI))]
[JsonSerializable(typeof(XSTSAuthResponse))]
[JsonSerializable(typeof(XSTSErrorResponse))]
[JsonSerializable(typeof(MinecraftLoginResponse))]
[JsonSerializable(typeof(EntitlementsResponse))]
[JsonSerializable(typeof(EntitlementItem))]
[JsonSerializable(typeof(MinecraftProfileResponse))]
[JsonSerializable(typeof(XboxLiveAuthRequest))]
[JsonSerializable(typeof(XboxLiveAuthRequestProperties))]
[JsonSerializable(typeof(XSTSAuthRequest))]
[JsonSerializable(typeof(XSTSAuthRequestProperties))]
[JsonSerializable(typeof(MinecraftLoginRequest))]
public partial class MsaJsonContext : JsonSerializerContext
{
}

public class TextureData
{
    [JsonPropertyName("profileName")]
    public string ProfileName { get; set; }

    [JsonPropertyName("textures")]
    public Textures Textures { get; set; }
}
public class Textures
{
    [JsonPropertyName("SKIN")]
    public TextureInfo Skin { get; set; }

    [JsonPropertyName("CAPE")]
    public TextureInfo Cape { get; set; }
}
public class TextureInfo
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("metadata")]
    public Metadata Metadata { get; set; }
}
public class Metadata
{
    [JsonPropertyName("model")]
    public string Model { get; set; }
}

[JsonSerializable(typeof(TextureData))]
[JsonSerializable(typeof(Textures))]
[JsonSerializable(typeof(TextureInfo))]
[JsonSerializable(typeof(Metadata))]
public partial class MojangProfileJsonContext : JsonSerializerContext { }