using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Auth;

/// <summary>
/// What login and refresh return to the client. Deliberately carries no refresh token: that
/// travels as an HttpOnly cookie so no script can read it. <see cref="AuthTokensDto"/> remains
/// the service's internal return type, since the controller needs the refresh token to set the
/// cookie.
/// </summary>
public class AccessTokenDto
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
}
