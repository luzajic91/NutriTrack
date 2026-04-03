using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.FoodCatalog;

public class FoodNutrient
{
    public int FoodNutrientId { get; set; }
    public int FoodId { get; set; }
    public int NutrientId { get; set; }
    public decimal ValuePer100g { get; set; }

    public Food Food { get; set; } = default!;
    public Nutrient Nutrient { get; set; } = default!;
}
