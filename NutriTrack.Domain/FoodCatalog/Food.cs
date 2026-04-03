using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.FoodCatalog;

public class Food
{
    public int FoodId { get; set; }
    public string Name { get; set; } = default!;
    public int? BrandId { get; set; }
    public string? Description { get; set; }

    public Brand? Brand { get; set; }
    public ICollection<FoodNutrient> FoodNutrients { get; set; } = [];
    public ICollection<FoodServing> FoodServings { get; set; } = [];
}