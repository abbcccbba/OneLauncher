using System.Text.Json.Serialization;

namespace OneLauncher.Core.Net.Account.Yggdrasil;

// --- DTOs: All models are now public classes for better AOT compatibility ---

public class AuthRequest
{
    [JsonPropertyName("agent")]
    public AgentInfo Agent { get; set; } = new();

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("clientToken")]
    public string ClientToken { get; set; }

    [JsonPropertyName("requestUser")]
    public bool RequestUser { get; set; } = true;
}

public class AgentInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Minecraft";

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;
}

public class RefreshRequest
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }

    [JsonPropertyName("clientToken")]
    public string ClientToken { get; set; }
}

public class AuthResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }

    [JsonPropertyName("clientToken")]
    public string ClientToken { get; set; }

    [JsonPropertyName("selectedProfile")]
    public Profile? SelectedProfile { get; set; }
}

public class Profile
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; }
}

// --- JsonContext: Central place for AOT serialization info ---

[JsonSerializable(typeof(AuthRequest))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(ErrorResponse))]
internal partial class YggdrasilJsonContext : JsonSerializerContext { }