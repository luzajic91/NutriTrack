namespace NutriTrack.Shared.Models.Recipes;

public class CreateRecipeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ServingsCount { get; set; }
    public bool IsPublic { get; set; }
    public List<RecipeItemRequest> Items { get; set; } = new();
}

public class RecipeItemRequest
{
    public int FoodId { get; set; }
    public decimal Grams { get; set; }
}