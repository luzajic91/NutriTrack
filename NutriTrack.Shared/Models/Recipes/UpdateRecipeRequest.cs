using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Recipes;

/// <summary>
/// A full replacement of a recipe: every field is sent, and <see cref="Items"/> becomes the
/// complete ingredient list. Nothing outside the recipe references a <c>RecipeItemId</c> —
/// meal logging copies items into <c>MealEntryItem</c> rows at log time — so replacing the
/// rows wholesale is safe and keeps this symmetric with <see cref="CreateRecipeRequest"/>.
/// The recipe id travels in the route, not here.
/// </summary>
public class UpdateRecipeRequest
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
