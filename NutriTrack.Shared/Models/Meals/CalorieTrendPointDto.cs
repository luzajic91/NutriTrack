using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Meals;

public class CalorieTrendPointDto
{
    [JsonPropertyName("date")]     public DateOnly Date { get; set; }
    [JsonPropertyName("calories")] public decimal Calories { get; set; }
}
