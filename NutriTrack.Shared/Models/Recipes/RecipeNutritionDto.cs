using System.Text.Json.Serialization;
using NutriTrack.Shared.Models.Meals;

namespace NutriTrack.Shared.Models.Recipes;

public class RecipeNutritionDto
{
    [JsonPropertyName("recipeId")]
    public int RecipeId { get; set; }

    [JsonPropertyName("recipeName")]
    public string RecipeName { get; set; } = string.Empty;

    [JsonPropertyName("totalGrams")]
    public decimal TotalGrams { get; set; }

    [JsonPropertyName("servingsCount")]
    public int? ServingsCount { get; set; }

    [JsonPropertyName("nutrients")]
    public List<NutrientTotalDto> Nutrients { get; set; } = new();

    [JsonPropertyName("nutrientsPerServing")]
    public List<NutrientTotalDto>? NutrientsPerServing { get; set; }
}
