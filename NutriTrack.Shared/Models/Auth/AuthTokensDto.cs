using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Auth;

public class AuthTokensDto
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}
