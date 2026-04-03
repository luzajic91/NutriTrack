using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.FoodCatalog;

public class FoodServing
{
    public int FoodServingId { get; set; }
    public int FoodId { get; set; }
    public int ServingUnitId { get; set; }
    public string DisplayName { get; set; } = default!;
    public decimal GramWeight { get; set; }

    public Food Food { get; set; } = default!;
    public ServingUnit ServingUnit { get; set; } = default!;
}
