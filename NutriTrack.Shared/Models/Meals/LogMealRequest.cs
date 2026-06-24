namespace NutriTrack.Shared.Models.Meals;

public class LogMealRequest
{
    public List<MealFoodEntry> Foods { get; set; } = new();
    public List<MealRecipeEntry> Recipes { get; set; } = new();
    public DateTime? ConsumedAt { get; set; }
}

public record MealFoodEntry(int FoodId, decimal Grams);
public record MealRecipeEntry(int RecipeId, decimal Grams);
