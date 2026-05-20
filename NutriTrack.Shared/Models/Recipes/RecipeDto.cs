using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Recipes;

public class RecipeDto
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

    [JsonPropertyName("items")]
    public List<RecipeItemDto> Items { get; set; } = new();
}

public class RecipeItemDto
{
    [JsonPropertyName("recipeItemId")]
    public int RecipeItemId { get; set; }

    [JsonPropertyName("foodId")]
    public int FoodId { get; set; }

    [JsonPropertyName("foodName")]
    public string FoodName { get; set; } = string.Empty;

    [JsonPropertyName("grams")]
    public decimal Grams { get; set; }
}