namespace NutriTrack.Web.Services;

/// <summary>
/// Raised when an API call returns a non-success status code, carrying the HTTP status and the
/// server's machine-readable error code so callers can distinguish failure kinds.
/// </summary>
/// <remarks>
/// <see cref="Message"/> is display text from the server and may be reworded at any time —
/// branch on <see cref="Code"/>, never on the message.
/// </remarks>
public class ApiException : Exception
{
    public int StatusCode { get; }

    /// <summary>The server's stable error code, or null if the body could not be parsed.</summary>
    public string? Code { get; }

    public ApiException(int statusCode, string message) : this(statusCode, message, code: null)
    {
    }

    public ApiException(int statusCode, string message, string? code) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}
