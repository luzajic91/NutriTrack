using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.FoodCatalog;

public class Nutrient
{
    public int NutrientId { get; set; }
    public string Name { get; set; } = default!;
    public string Abv { get; set; } = default!;
    public int MeasurementUnitId { get; set; }

    public MeasurementUnit MeasurementUnit { get; set; } = default!;
}