using System.Text.Json.Serialization;
using NutriTrack.Shared.Features.Recipes;

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
