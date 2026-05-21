using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Meals;

public class DailyNutritionSummaryDto
{
    [JsonPropertyName("date")]      public DateOnly Date { get; set; }
    [JsonPropertyName("nutrients")] public List<NutrientTotalDto> Nutrients { get; set; } = new();
}

public class NutrientTotalDto
{
    [JsonPropertyName("name")]         public string Name { get; set; } = "";
    [JsonPropertyName("abbreviation")] public string Abbreviation { get; set; } = "";
    [JsonPropertyName("total")]        public decimal Total { get; set; }
    [JsonPropertyName("unit")]         public string Unit { get; set; } = "";
}
