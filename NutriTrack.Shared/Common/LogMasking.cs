namespace NutriTrack.Shared.Common;

/// <summary>
/// Helpers for keeping sensitive values out of logs. Tokens, secrets, and other
/// credentials must never be logged in full; use <see cref="Mask"/> when a
/// fragment is needed for correlation (e.g. token rotation/reuse events).
/// </summary>
public static class LogMasking
{
    private const int VisiblePrefix = 6;

    /// <summary>
    /// Returns a masked representation that preserves only a short leading
    /// fragment, e.g. <c>"abc123***"</c>. Null/empty/short values collapse to
    /// <c>"***"</c> so nothing meaningful leaks.
    /// </summary>
    public static string Mask(string? value) =>
        string.IsNullOrEmpty(value) || value.Length <= VisiblePrefix
            ? "***"
            : value[..VisiblePrefix] + "***";
}
