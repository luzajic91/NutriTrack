using NutriTrack.Shared.Models.Foods;

namespace NutriTrack.Web.Components;

public record FoodSelectionItem(FoodSummaryDto Food, decimal Grams);
