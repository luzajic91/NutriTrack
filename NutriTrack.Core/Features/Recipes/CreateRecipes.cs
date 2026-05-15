namespace NutriTrack.Core.Features.Recipes;

public record CreateRecipeCommand(
    string Name,
    string? Description,
    int? ServingsCount,
    bool IsPublic,
    List<RecipeItemRequest> Items);

public record RecipeItemRequest(int FoodId, decimal Grams);

public class CreateRecipeValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(200);
        RuleFor(x => x.ServingsCount).GreaterThan(0).When(x => x.ServingsCount.HasValue);
        RuleFor(x => x.Items).NotEmpty().WithMessage("A recipe must have at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.FoodId).GreaterThan(0);
            item.RuleFor(x => x.Grams).GreaterThan(0);
        });
    }
}