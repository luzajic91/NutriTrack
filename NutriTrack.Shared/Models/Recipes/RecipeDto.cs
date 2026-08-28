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

    /// <summary>True when the requesting user owns this recipe. A public recipe is visible to
    /// everyone but editable and deletable only by its owner, so the client uses this to decide
    /// whether to offer those actions at all. Computed per request, never persisted.</summary>
    [JsonPropertyName("isOwner")]
    public bool IsOwner { get; set; }

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