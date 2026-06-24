namespace NutriTrack.Web.Services;

/// <summary>
/// Raised when an API call returns a non-success status code, carrying the
/// HTTP status so callers can distinguish failure kinds if needed.
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message)
        => StatusCode = statusCode;
}
