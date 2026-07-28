using System.Text.Json.Serialization;
using NutriTrack.Domain.UserPreferences;

namespace NutriTrack.Shared.Models.UserPreferences;

public class PreferenceHistoryDto
{
    [JsonPropertyName("metric")]
    public PreferenceMetric Metric { get; set; }

    [JsonPropertyName("points")]
    public List<PreferenceHistoryPointDto> Points { get; set; } = new();
}

public class PreferenceHistoryPointDto
{
    [JsonPropertyName("recordedAt")]
    public DateTime RecordedAt { get; set; }

    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}
