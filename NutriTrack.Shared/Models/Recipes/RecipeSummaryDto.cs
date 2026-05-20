using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Recipes;

public class RecipeSummaryDto
{
    [JsonPropertyName("recipeId")]
    public int RecipeId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("servingsCount")]
    public int? ServingsCount { get; set; }

    [JsonPropertyName("totalGrams")]
    public decimal TotalGrams { get; set; }

    [JsonPropertyName("isPublic")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("itemCount")]
    public int ItemCount { get; set; }
}