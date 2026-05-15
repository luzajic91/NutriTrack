namespace NutriTrack.Core.Features.MealLogging;

public record LogMealCommand(
    List<MealFoodEntry> Foods,
    List<MealRecipeEntry> Recipes,
    DateTime? ConsumedAt);

public record MealFoodEntry(int FoodId, decimal Grams);
public record MealRecipeEntry(int RecipeId, decimal Grams);

public class LogMealValidator : AbstractValidator<LogMealCommand>
{
    public LogMealValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Foods.Count > 0 || x.Recipes.Count > 0)
            .WithMessage("A meal must contain at least one food or recipe.");

        RuleForEach(x => x.Foods).ChildRules(f =>
        {
            f.RuleFor(x => x.FoodId).GreaterThan(0);
            f.RuleFor(x => x.Grams).GreaterThan(0);
        });

        RuleForEach(x => x.Recipes).ChildRules(r =>
        {
            r.RuleFor(x => x.RecipeId).GreaterThan(0);
            r.RuleFor(x => x.Grams).GreaterThan(0);
        });
    }
}