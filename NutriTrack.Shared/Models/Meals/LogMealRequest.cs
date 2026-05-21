namespace NutriTrack.Shared.Models.Meals;

public class LogMealRequest
{
    public List<MealFoodEntry>? Foods { get; set; }
    public List<MealRecipeEntry>? Recipes { get; set; }
    public DateTime? ConsumedAt { get; set; }
}

public class MealFoodEntry
{
    public int FoodId { get; set; }
    public decimal Grams { get; set; }
}

public class MealRecipeEntry
{
    public int RecipeId { get; set; }
    public decimal Grams { get; set; }
}