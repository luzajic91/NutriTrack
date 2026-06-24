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

    [JsonPropertyName("nutrients")]
    public List<FoodNutrientDto> Nutrients { get; set; } = new();

    [JsonPropertyName("servings")]
    public List<FoodServingDto> Servings { get; set; } = new();
}

public class FoodNutrientDto
{
    [JsonPropertyName("nutrientName")]
    public string NutrientName { get; set; } = string.Empty;

    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; } = string.Empty;

    [JsonPropertyName("valuePer100g")]
    public decimal ValuePer100g { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;
}

public class FoodServingDto
{
    [JsonPropertyName("foodServingId")]
    public int FoodServingId { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("gramWeight")]
    public decimal GramWeight { get; set; }

    [JsonPropertyName("servingUnit")]
    public string ServingUnit { get; set; } = string.Empty;
}

public class FoodSummaryDto
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
