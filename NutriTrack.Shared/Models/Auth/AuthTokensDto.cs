using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Auth;

/// <summary>
/// The service's internal result for login and refresh. This is no longer what the API returns:
/// the controller puts <see cref="RefreshToken"/> into an HttpOnly cookie and responds with an
/// <see cref="AccessTokenDto"/>, so no script ever sees the refresh token.
/// </summary>
public class AuthTokensDto
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When <see cref="RefreshToken"/> stops being valid. Carried here so the cookie's lifetime
    /// comes from the same place as the row's, rather than the controller repeating the constant.
    /// </summary>
    [JsonPropertyName("refreshTokenExpiresAtUtc")]
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}
