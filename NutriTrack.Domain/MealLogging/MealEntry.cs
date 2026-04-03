using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.MealLogging;

public class MealEntry
{
    public int MealEntryId { get; set; }
    public int UserId { get; set; }
    public DateTime ConsumedAt { get; set; }

    public ICollection<MealEntryItem> Items { get; set; } = [];
}
