using System.Net.Http.Json;
using NutriTrack.Shared.Models.Common;

namespace NutriTrack.Web.Services;

/// <summary>
/// Turns a non-success response into an <see cref="ApiException"/> carrying the server's error
/// code and display message. Used by every caller so error handling stays in one place.
/// </summary>
public static class ApiErrorReader
{
    public static async Task<ApiException> ToApiExceptionAsync(
        this HttpResponseMessage response,
        string fallbackMessage,
        CancellationToken ct = default)
    {
        var status = (int)response.StatusCode;

        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct);
            if (error is not null && !string.IsNullOrWhiteSpace(error.Error))
                return new ApiException(status, error.Error, error.Code);
        }
        catch (Exception)
        {
            // Body was absent, truncated or not our contract (e.g. a proxy error page).
            // Fall through to the caller's generic message rather than surfacing raw content.
        }

        return new ApiException(status, fallbackMessage);
    }
}
