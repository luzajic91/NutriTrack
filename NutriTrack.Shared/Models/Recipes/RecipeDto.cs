namespace NutriTrack.Shared.Models.Recipes;

public class RecipeDto
{
    public int RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ServingsCount { get; set; }
    public decimal TotalGrams { get; set; }
    public bool IsPublic { get; set; }
    public List<RecipeItemDto> Items { get; set; } = new();
}

public class RecipeItemDto
{
    public int RecipeItemId { get; set; }
    public int FoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public decimal Grams { get; set; }
}