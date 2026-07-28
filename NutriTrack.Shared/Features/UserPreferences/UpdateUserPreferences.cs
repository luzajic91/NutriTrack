using NutriTrack.Shared.Models.UserPreferences;

namespace NutriTrack.Shared.Features.UserPreferences;

public class UpdateUserPreferencesValidator : AbstractValidator<UpdateUserPreferencesRequest>
{
    public UpdateUserPreferencesValidator()
    {
        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.")
            .LessThanOrEqualTo(999).WithMessage("Weight cannot exceed 999 kg.")
            .When(x => x.WeightKg.HasValue);

        RuleFor(x => x.CalorieGoal)
            .GreaterThanOrEqualTo(0).WithMessage("Calorie goal must be 0 or greater.")
            .When(x => x.CalorieGoal.HasValue);

        RuleFor(x => x.ProteinGoalG)
            .GreaterThanOrEqualTo(0).WithMessage("Protein goal must be 0 or greater.")
            .When(x => x.ProteinGoalG.HasValue);

        RuleFor(x => x.CarbGoalG)
            .GreaterThanOrEqualTo(0).WithMessage("Carb goal must be 0 or greater.")
            .When(x => x.CarbGoalG.HasValue);

        RuleFor(x => x.FatGoalG)
            .GreaterThanOrEqualTo(0).WithMessage("Fat goal must be 0 or greater.")
            .When(x => x.FatGoalG.HasValue);

        RuleFor(x => x.FiberGoalG)
            .GreaterThanOrEqualTo(0).WithMessage("Fiber goal must be 0 or greater.")
            .When(x => x.FiberGoalG.HasValue);
    }
}
