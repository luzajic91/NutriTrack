using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Recipes;

public class CreateRecipeRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("servingsCount")]
    public int? ServingsCount { get; set; }

    [JsonPropertyName("isPublic")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("items")]
    public List<RecipeItemRequest> Items { get; set; } = new();
}

public record RecipeItemRequest(int FoodId, decimal Grams);
