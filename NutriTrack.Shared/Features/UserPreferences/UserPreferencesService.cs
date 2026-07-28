using NutriTrack.Domain.UserPreferences;
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

        // Append a history point for every tracked metric whose value actually changed
        // (compare the incoming value against the currently-stored one before overwriting).
        var now = DateTime.UtcNow;
        RecordIfChanged(PreferenceMetric.WeightKg, prefs.WeightKg, cmd.WeightKg, now);
        RecordIfChanged(PreferenceMetric.CalorieGoal, prefs.CalorieGoal, cmd.CalorieGoal, now);
        RecordIfChanged(PreferenceMetric.ProteinGoalG, prefs.ProteinGoalG, cmd.ProteinGoalG, now);
        RecordIfChanged(PreferenceMetric.CarbGoalG, prefs.CarbGoalG, cmd.CarbGoalG, now);
        RecordIfChanged(PreferenceMetric.FatGoalG, prefs.FatGoalG, cmd.FatGoalG, now);
        RecordIfChanged(PreferenceMetric.FiberGoalG, prefs.FiberGoalG, cmd.FiberGoalG, now);

        prefs.WeightKg = cmd.WeightKg;
        prefs.CalorieGoal = cmd.CalorieGoal;
        prefs.ProteinGoalG = cmd.ProteinGoalG;
        prefs.CarbGoalG = cmd.CarbGoalG;
        prefs.FatGoalG = cmd.FatGoalG;
        prefs.FiberGoalG = cmd.FiberGoalG;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Preferences updated for user {UserId}", _currentUser.UserId);
    }

    // Records a history point only when the new value is set and differs from the previous
    // one, so no-op saves don't accumulate duplicate rows.
    private void RecordIfChanged(PreferenceMetric metric, decimal? oldValue, decimal? newValue, DateTime recordedAt)
    {
        if (newValue.HasValue && newValue != oldValue)
            _db.PreferenceHistory.Add(new PreferenceHistoryEntry
            {
                UserId = _currentUser.UserId,
                Metric = metric,
                Value = newValue.Value,
                RecordedAt = recordedAt
            });
    }

    public async Task<PreferenceHistoryDto> GetHistoryAsync(
        PreferenceMetric metric, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var query = _db.PreferenceHistory
            .Where(h => h.UserId == _currentUser.UserId && h.Metric == metric);

        if (from.HasValue)
            query = query.Where(h => h.RecordedAt >= from.Value.ToDateTime(TimeOnly.MinValue));

        if (to.HasValue)
            query = query.Where(h => h.RecordedAt <= to.Value.ToDateTime(TimeOnly.MaxValue));

        var points = await query
            .OrderBy(h => h.RecordedAt)
            .Select(h => new PreferenceHistoryPointDto { RecordedAt = h.RecordedAt, Value = h.Value })
            .ToListAsync(ct);

        return new PreferenceHistoryDto { Metric = metric, Points = points };
    }
}
