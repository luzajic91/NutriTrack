using System.Text.Json.Serialization;

namespace NutriTrack.Shared.Models.Meals;

public class MealEntryDto
{
    [JsonPropertyName("mealEntryId")] 
    public int MealEntryId { get; set; }

    [JsonPropertyName("consumedAt")]  
    public DateTime ConsumedAt { get; set; }

    [JsonPropertyName("items")]       
    public List<MealEntryItemDto> Items { get; set; } = new();

    [JsonPropertyName("macros")]      
    public List<NutrientTotalDto>? Macros { get; set; }
}

public class MealEntryItemDto
{
    [JsonPropertyName("foodId")]   
    public int FoodId { get; set; }

    [JsonPropertyName("foodName")] 
    public string FoodName { get; set; } = "";

    [JsonPropertyName("grams")]    
    public decimal Grams { get; set; }
}
