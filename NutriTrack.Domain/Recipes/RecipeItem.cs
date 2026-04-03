using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.Recipes;

public class RecipeItem
{
    public int RecipeItemId { get; set; }
    public int RecipeId { get; set; }
    public int FoodId { get; set; }
    public decimal Grams { get; set; }

    public Recipe Recipe { get; set; } = default!;
}
