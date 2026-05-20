using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Foods;

public class FoodDto
{
    [JsonPropertyName("foodId")]
    public int FoodId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("brandName")]
    public string? BrandName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class FoodSummaryDto
{
    [JsonPropertyName("foodId")]
    public int FoodId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("brandName")]
    public string? BrandName { get; set; }
}