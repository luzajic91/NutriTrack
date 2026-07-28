using NutriTrack.Shared.Models.UserPreferences;

namespace NutriTrack.Shared.Features.UserPreferences;

public class UserPreferencesService
{
    private readonly NutriTrackDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly UpdateUserPreferencesValidator _validator;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(
        NutriTrackDbContext db,
        CurrentUserService currentUser,
        UpdateUserPreferencesValidator validator,
        ILogger<UserPreferencesService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _validator = validator;
        _logger = logger;
    }

    public async Task<UserPreferencesDto> GetAsync(CancellationToken ct)
    {
        var prefs = await _db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == _currentUser.UserId, ct);

        return prefs is null
            ? new UserPreferencesDto()
            : new UserPreferencesDto
            {
                WeightKg = prefs.WeightKg,
                CalorieGoal = prefs.CalorieGoal,
                ProteinGoalG = prefs.ProteinGoalG,
                CarbGoalG = prefs.CarbGoalG,
                FatGoalG = prefs.FatGoalG,
                FiberGoalG = prefs.FiberGoalG
            };
    }

    public async Task UpdateAsync(UpdateUserPreferencesRequest cmd, CancellationToken ct)
    {
        _validator.ValidateAndThrow(cmd);

        var prefs = await _db.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == _currentUser.UserId, ct);

        if (prefs is null)
        {
            prefs = new Domain.UserPreferences.UserPreferences { UserId = _currentUser.UserId };
            _db.UserPreferences.Add(prefs);
        }

        prefs.WeightKg = cmd.WeightKg;
        prefs.CalorieGoal = cmd.CalorieGoal;
        prefs.ProteinGoalG = cmd.ProteinGoalG;
        prefs.CarbGoalG = cmd.CarbGoalG;
        prefs.FatGoalG = cmd.FatGoalG;
        prefs.FiberGoalG = cmd.FiberGoalG;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Preferences updated for user {UserId}", _currentUser.UserId);
    }
}
