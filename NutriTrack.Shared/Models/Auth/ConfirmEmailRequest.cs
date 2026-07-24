using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Auth;

public class ConfirmEmailRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
