using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.FoodCatalog;

public class MeasurementUnit
{
    public int MeasurementUnitId { get; set; }
    public string Name { get; set; } = default!;
}