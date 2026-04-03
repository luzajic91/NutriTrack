using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Domain.FoodCatalog;

public class Brand
{
    public int BrandId { get; set; }
    public string Name { get; set; } = default!;
}