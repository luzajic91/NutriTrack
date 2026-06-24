using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Auth;

public class RevokeTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}
