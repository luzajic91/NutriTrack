using System;
using NutriTrack.Domain.Identity;

namespace NutriTrack.Domain.UserPreferences;

/// <summary>
/// An append-only record of a preference value at a point in time. One row is
/// written whenever a tracked preference changes, forming a per-metric time series.
/// </summary>
public class PreferenceHistoryEntry
{
    public int PreferenceHistoryEntryId { get; set; }
    public int UserId { get; set; }
    public PreferenceMetric Metric { get; set; }
    public decimal Value { get; set; }
    public DateTime RecordedAt { get; set; }

    public User User { get; set; } = default!;
}
