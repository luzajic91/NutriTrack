using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.MealLogging;

public class MealEntryItem
{
    public int MealEntryItemId { get; set; }
    public int MealEntryId { get; set; }
    public int FoodId { get; set; }
    public decimal Grams { get; set; }

    public MealEntry MealEntry { get; set; } = default!;
}
