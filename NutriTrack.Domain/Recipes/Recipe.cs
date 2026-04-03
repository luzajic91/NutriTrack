using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.Recipes;

public class Recipe
{
    public int RecipeId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int? ServingsCount { get; set; }
    public decimal TotalGrams { get; set; }
    public bool IsPublic { get; set; }

    public ICollection<RecipeItem> RecipeItems { get; set; } = [];
}
