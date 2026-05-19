namespace NutriTrack.Shared.Models.Recipes;

public class RecipeSummaryDto
{
    public int RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ServingsCount { get; set; }
    public decimal TotalGrams { get; set; }
    public bool IsPublic { get; set; }
    public int ItemCount { get; set; }
}