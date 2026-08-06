namespace NutriTrack.Shared.Common;

/// <summary>
/// Classifies an <see cref="Error"/> so the API layer can pick a status code.
/// Deliberately transport-agnostic: this assembly knows nothing about HTTP.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict
}
