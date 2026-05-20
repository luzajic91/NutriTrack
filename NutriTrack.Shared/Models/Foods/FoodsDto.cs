namespace NutriTrack.Shared.Models.Foods;

public class FoodDto
{
    public int FoodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? Description { get; set; }
}

public class FoodSummaryDto
{
    public int FoodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BrandName { get; set; }
}