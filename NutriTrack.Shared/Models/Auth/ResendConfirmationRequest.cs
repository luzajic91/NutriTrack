using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Auth;

public class ResendConfirmationRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
