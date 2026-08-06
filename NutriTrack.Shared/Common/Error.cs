namespace NutriTrack.Shared.Common;

/// <summary>
/// An expected, non-exceptional failure.
/// </summary>
/// <param name="Code">
/// Stable machine-readable identifier (e.g. <c>auth.invalid_credentials</c>). This is the wire
/// contract clients switch on, so treat it as breaking to change.
/// </param>
/// <param name="Message">Human-readable text. Safe to reword; nothing should match on it.</param>
/// <param name="Type">How the transport layer should surface this.</param>
public sealed record Error(string Code, string Message, ErrorType Type);
